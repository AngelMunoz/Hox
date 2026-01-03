module Hox.Rendering

open System
open System.Collections.Generic
open System.Threading
open System.Threading.Tasks
open System.Text
open System.Web
open System.IO

open FSharp.Control
open IcedTasks

open Hox.Core

module Encoding =
  type StringCache =
    abstract GetOrAdd: key: string * valueFactory: (string -> string) -> string

  type EncodingCaches = { html: StringCache; attr: StringCache }

  /// Two-generation cache (approximate LRU) implemented with ConcurrentDictionary.
  ///
  /// Semantics:
  /// - "current" holds the most recently added/promoted items
  /// - "previous" holds the last generation
  /// - when current grows beyond capacity, we rotate (previous <- current; current <- empty)
  ///
  /// This bounds growth to roughly 2 * capacity entries without needing a full LRU list.
  let private createTwoGenStringCache(capacity: int) : StringCache =
    let capacity = max 1 capacity
    let gate = obj()

    let mutable current =
      System.Collections.Concurrent.ConcurrentDictionary<string, string>()

    let mutable previous =
      System.Collections.Concurrent.ConcurrentDictionary<string, string>()

    let rotateIfNeeded() =
      // Count is a snapshot; races are fine for this approximate strategy.
      if current.Count > capacity then
        lock gate (fun () ->
          if current.Count > capacity then
            previous <- current

            current <-
              System.Collections.Concurrent.ConcurrentDictionary<string, string>())

    { new StringCache with
        member _.GetOrAdd(key, valueFactory) =
          match current.TryGetValue(key) with
          | true, v -> v
          | false, _ ->
            match previous.TryGetValue(key) with
            | true, v ->
              // Promote into current for better locality.
              current.TryAdd(key, v) |> ignore
              rotateIfNeeded()
              v
            | false, _ ->
              let v = current.GetOrAdd(key, fun k -> valueFactory k)
              rotateIfNeeded()
              v
    }

  // Default caches (closure-backed, thread-safe via ConcurrentDictionary)
  // We use a two-generation cache to avoid unbounded growth.
  let private caches = {
    html = createTwoGenStringCache(2048)
    attr = createTwoGenStringCache(2048)
  }

  let inline htmlEncode s =
    caches.html.GetOrAdd(
      s,
      fun s ->
        match HttpUtility.HtmlEncode(s) with
        | null -> failwith $"HtmlEncode returned null for input: {s}"
        | encoded -> encoded
    )

  let inline attrEncode s =
    caches.attr.GetOrAdd(
      s,
      fun s ->
        match HttpUtility.HtmlAttributeEncode(s) with
        | null -> failwith $"HtmlAttributeEncode returned null for input: {s}"
        | encoded -> encoded
    )

let voidTags =
  HashSet<string>(
    [
      "area"
      "base"
      "br"
      "col"
      "command"
      "embed"
      "hr"
      "img"
      "input"
      "keygen"
      "link"
      "meta"
      "param"
      "source"
      "track"
      "wbr"
    ],
    StringComparer.OrdinalIgnoreCase
  )

let inline isVoidTag tag = voidTags.Contains(tag)

let prefetchAsyncChildren
  (children: Deque<Node>)
  (ct: CancellationToken)
  : Dictionary<int, ValueTask<Node>> | null =
  // Avoid allocating a dictionary for the common case where there are no async children.
  // We use null to represent "no prefetched async tasks".
  let mutable prefetched: Dictionary<int, ValueTask<Node>> | null = null

  for i = 0 to children.Count - 1 do
    match children[i] with
    | AsyncNode asyncTask ->
      if isNull prefetched then
        prefetched <- Dictionary<int, ValueTask<Node>>()

      (nonNull prefetched).TryAdd(i, asyncTask ct) |> ignore
    | _ -> ()

  prefetched

[<Struct; NoComparison; NoEquality>]
type WorkItem =
  | CloseElement of tag: string
  | RenderChildrenPrefetched of
    children: Deque<Node> *
    index: int *
    prefetched: (Dictionary<int, ValueTask<Node>> | null)
  | RenderNode of node: Node
  | AwaitSeq of enumerator: IAsyncEnumerator<Node>

let renderAttributesToBuilder
  (sb: StringBuilder)
  (attrs: Deque<AttributeNode>)
  (ct: CancellationToken)
  =
  valueTask {
    if attrs.IsEmpty then
      return ()

    let mutable id = ValueNone
    let classes = ResizeArray<string>()
    let others = ResizeArray<struct (string * string)>()

    for attr in attrs do
      match attr with
      | Attribute { name = ""; value = _ } -> ()
      | Attribute { name = "id"; value = v } ->
        if id.IsNone then
          id <- ValueSome(Encoding.attrEncode v)
      | Attribute { name = "class"; value = v } ->
        classes.Add(Encoding.attrEncode v)
      | Attribute { name = n; value = v } ->
        others.Add struct (Encoding.attrEncode n, Encoding.attrEncode v)
      | AsyncAttribute asyncAttr ->
        let! { name = n; value = v } = asyncAttr ct

        if String.IsNullOrEmpty(n) then
          ()
        elif n = "id" then
          if id.IsNone then
            id <- ValueSome(Encoding.attrEncode v)
        elif n = "class" then
          classes.Add(Encoding.attrEncode v)
        else
          others.Add struct (Encoding.attrEncode n, Encoding.attrEncode v)

    match id with
    | ValueSome idVal -> sb.Append(" id=\"").Append(idVal).Append('"') |> ignore
    | _ -> ()

    if classes.Count > 0 then
      sb.Append(" class=\"") |> ignore

      for i = 0 to classes.Count - 1 do
        if i > 0 then
          sb.Append(' ') |> ignore

        sb.Append(classes.[i]) |> ignore

      sb.Append('"') |> ignore

    for struct (n, v) in others do
      sb.Append(' ').Append(n).Append("=\"").Append(v).Append('"') |> ignore
  }

module Builder =
  let renderNode node (ct: CancellationToken) = valueTask {
    let sb = StringBuilder(4096)
    let stack = Stack<WorkItem>(64)
    stack.Push(RenderNode node)

    while stack.Count > 0 do
      ct.ThrowIfCancellationRequested()
      let work = stack.Pop()

      match work with
      | RenderNode n ->
        match n with
        | Element el ->
          if not(isVoidTag el.tag) then
            stack.Push(CloseElement el.tag)

            if not el.children.IsEmpty then
              stack.Push(
                RenderChildrenPrefetched(
                  el.children,
                  0,
                  prefetchAsyncChildren el.children ct
                )
              )

          sb.Append('<').Append(el.tag) |> ignore
          do! renderAttributesToBuilder sb el.attributes ct
          sb.Append('>') |> ignore
        | Text t -> sb.Append(Encoding.htmlEncode t) |> ignore
        | Raw r -> sb.Append(r) |> ignore
        | Comment c ->
          sb.Append("<!--").Append(Encoding.htmlEncode c).Append("-->")
          |> ignore
        | PreRendered html -> sb.Append(html) |> ignore
        | Fragment frag ->
          if not frag.IsEmpty then
            stack.Push(
              RenderChildrenPrefetched(frag, 0, prefetchAsyncChildren frag ct)
            )
        | AsyncNode asyncTask ->
          let! resolved = asyncTask ct
          stack.Push(RenderNode resolved)
        | AsyncSeqNode asyncSeq ->
          stack.Push(AwaitSeq(asyncSeq.GetAsyncEnumerator(ct)))

      | CloseElement tag -> sb.Append("</").Append(tag).Append('>') |> ignore

      | RenderChildrenPrefetched(children, index, prefetched) ->
        if index < children.Count then
          if index + 1 < children.Count then
            stack.Push(
              RenderChildrenPrefetched(children, index + 1, prefetched)
            )

          match prefetched with
          | null -> stack.Push(RenderNode(children[index]))
          | prefetched ->
            match prefetched.TryGetValue(index) with
            | true, startedTask ->
              let! resolved = startedTask
              stack.Push(RenderNode resolved)
            | false, _ -> stack.Push(RenderNode(children[index]))

      | AwaitSeq enumerator ->
        let! hasNext = enumerator.MoveNextAsync()

        if hasNext then
          stack.Push(AwaitSeq enumerator)
          stack.Push(RenderNode enumerator.Current)
        else
          do! enumerator.DisposeAsync()

    return sb.ToString()
  }

[<RequireQualifiedAccess>]
module Chunked =
  // Render attributes as fine-grained chunks, yielding each attribute piece separately
  let private renderAttributesChunked
    (attrs: Deque<AttributeNode>)
    (ct: CancellationToken)
    : IAsyncEnumerable<string> =
    taskSeq {
      if not attrs.IsEmpty then
        let mutable id = ValueNone
        let classes = ResizeArray<string>()
        let others = ResizeArray<struct (string * string)>()

        for attr in attrs do
          match attr with
          | Attribute { name = ""; value = _ } -> ()
          | Attribute { name = "id"; value = v } ->
            if id.IsNone then
              id <- ValueSome(Encoding.attrEncode v)
          | Attribute { name = "class"; value = v } ->
            classes.Add(Encoding.attrEncode v)
          | Attribute { name = n; value = v } ->
            others.Add struct (Encoding.attrEncode n, Encoding.attrEncode v)
          | AsyncAttribute asyncAttr ->
            let! { name = n; value = v } = asyncAttr ct

            if String.IsNullOrEmpty(n) then
              ()
            elif n = "id" then
              if id.IsNone then
                id <- ValueSome(Encoding.attrEncode v)
            elif n = "class" then
              classes.Add(Encoding.attrEncode v)
            else
              others.Add struct (Encoding.attrEncode n, Encoding.attrEncode v)

        // Yield id first if present
        match id with
        | ValueSome idVal -> $" id=\"{idVal}\""
        | _ -> ()

        // Yield class with all values combined
        if classes.Count > 0 then
          " class=\""
          String.Join(" ", classes)
          "\""

        // Yield other attributes
        for struct (n, v) in others do
          $" {n}=\"{v}\""
    }

  let renderNode(node: Node, ct: CancellationToken) : IAsyncEnumerable<string> = taskSeq {
    let stack = Stack<WorkItem>(64)
    stack.Push(RenderNode node)

    while stack.Count > 0 do
      ct.ThrowIfCancellationRequested()
      let work = stack.Pop()

      match work with
      | RenderNode n ->
        match n with
        | Element el ->
          // Yield opening tag start
          $"<{el.tag}"

          // Yield attributes as fine-grained chunks
          yield! renderAttributesChunked el.attributes ct

          // Yield close of opening tag
          ">"

          if not(isVoidTag el.tag) then
            stack.Push(CloseElement el.tag)

            if not el.children.IsEmpty then
              stack.Push(
                RenderChildrenPrefetched(
                  el.children,
                  0,
                  prefetchAsyncChildren el.children ct
                )
              )
        | Text t -> Encoding.htmlEncode t
        | Raw r -> r
        | Comment c -> $"<!--{Encoding.htmlEncode c}-->"
        | PreRendered html -> html
        | Fragment frag ->
          if not frag.IsEmpty then
            stack.Push(
              RenderChildrenPrefetched(frag, 0, prefetchAsyncChildren frag ct)
            )
        | AsyncNode asyncTask ->
          let! resolved = asyncTask ct
          stack.Push(RenderNode resolved)
        | AsyncSeqNode asyncSeq ->
          stack.Push(AwaitSeq(asyncSeq.GetAsyncEnumerator(ct)))

      | CloseElement tag -> $"</{tag}>"

      | RenderChildrenPrefetched(children, index, prefetched) ->
        if index < children.Count then
          if index + 1 < children.Count then
            stack.Push(
              RenderChildrenPrefetched(children, index + 1, prefetched)
            )

          match prefetched with
          | null -> stack.Push(RenderNode(children[index]))
          | prefetched ->
            match prefetched.TryGetValue(index) with
            | true, startedTask ->
              let! resolved = startedTask
              stack.Push(RenderNode resolved)
            | false, _ -> stack.Push(RenderNode(children[index]))
      | AwaitSeq enumerator ->
        let! hasNext = enumerator.MoveNextAsync()

        if hasNext then
          stack.Push(AwaitSeq enumerator)
          stack.Push(RenderNode enumerator.Current)
        else
          do! enumerator.DisposeAsync()
  }

type Render =
  [<CompiledName "Start">]
  static member start
    (
      node,
      [<Runtime.InteropServices.OptionalAttribute; Struct>] ?cancellationToken
    ) =
    let ct = defaultValueArg cancellationToken CancellationToken.None
    Chunked.renderNode(node, ct)

  [<CompiledName "ToStream">]
  static member toStream
    (
      node,
      stream: Stream,
      [<Runtime.InteropServices.OptionalAttribute; Struct>] ?chunkSize,
      [<Runtime.InteropServices.OptionalAttribute; Struct>] ?cancellationToken
    ) =
    taskUnit {
      let ct = defaultValueArg cancellationToken CancellationToken.None

      use writer =
        new StreamWriter(stream, Text.Encoding.UTF8, 4092, leaveOpen = true)

      let chunkSize =
        match chunkSize with
        | ValueSome n when n > 0 -> ValueSome n
        | _ -> ValueNone

      match chunkSize with
      | ValueNone ->
        for chunk in Chunked.renderNode(node, ct) do
          do! writer.WriteAsync(chunk.AsMemory(), ct)
          do! writer.FlushAsync(ct)
      | ValueSome size ->

        let sb = StringBuilder(size * 2)

        for chunk in Chunked.renderNode(node, ct) do
          sb.Append(chunk) |> ignore

          if sb.Length >= size then
            let payload = sb.ToString()
            do! writer.WriteAsync(payload.AsMemory(), ct)
            do! writer.FlushAsync(ct)
            sb.Clear() |> ignore

        if sb.Length > 0 then
          let payload = sb.ToString()
          do! writer.WriteAsync(payload.AsMemory(), ct)
          do! writer.FlushAsync(ct)

      do! writer.FlushAsync(ct)
    }


  [<CompiledName "AsString">]
  static member asString
    (
      node,
      [<Runtime.InteropServices.OptionalAttribute; Struct>] ?cancellationToken
    ) =
    let ct = defaultValueArg cancellationToken CancellationToken.None
    Builder.renderNode node ct

  static member asStringAsync node = asyncEx {
    let! ct = Async.CancellationToken
    return! Builder.renderNode node ct
  }
