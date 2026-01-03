module Hox.Rendering

open System
open System.Collections.Generic
open System.Threading
open System.Threading.Channels
open System.Threading.Tasks
open System.Text
open System.Web
open System.IO

open IcedTasks
open FSharp.Control

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
  let inline collectAttributes
    (attrs: Deque<AttributeNode>)
    (ct: CancellationToken)
    : ValueTask<
        struct (string voption *
        ResizeArray<string> *
        ResizeArray<struct (string * string)>)
       >
    =
    valueTask {
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

      return struct (id, classes, others)
    }

  let appendCollectedAttributes
    (sb: StringBuilder)
    (id: string voption)
    (classes: ResizeArray<string>)
    (others: ResizeArray<struct (string * string)>)
    =
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

  valueTask {
    if attrs.IsEmpty then
      return ()

    let! struct (id, classes, others) = collectAttributes attrs ct
    appendCollectedAttributes sb id classes others
  }

module Builder =
  let inline private processElement
    (sb: StringBuilder)
    (stack: Stack<WorkItem>)
    (ct: CancellationToken)
    (el: Element)
    : ValueTask<unit> =
    valueTask {
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
    }

  let inline private processNode
    (sb: StringBuilder)
    (stack: Stack<WorkItem>)
    (ct: CancellationToken)
    (n: Node)
    : ValueTask<unit> =
    valueTask {
      match n with
      | Element el -> do! processElement sb stack ct el
      | Text t -> sb.Append(Encoding.htmlEncode t) |> ignore
      | Raw r -> sb.Append(r) |> ignore
      | Comment c ->
        sb.Append("<!--").Append(Encoding.htmlEncode c).Append("-->") |> ignore
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
    }

  let inline private processChildrenPrefetched
    (stack: Stack<WorkItem>)
    (ct: CancellationToken)
    (children: Deque<Node>)
    (index: int)
    (prefetched: Dictionary<int, ValueTask<Node>> | null)
    : ValueTask<unit> =
    valueTask {
      if index < children.Count then
        if index + 1 < children.Count then
          stack.Push(RenderChildrenPrefetched(children, index + 1, prefetched))

        match prefetched with
        | null -> stack.Push(RenderNode(children[index]))
        | prefetched ->
          match prefetched.TryGetValue(index) with
          | true, startedTask ->
            let! resolved = startedTask
            stack.Push(RenderNode resolved)
          | false, _ -> stack.Push(RenderNode(children[index]))
    }

  let inline private processAwaitSeq
    (stack: Stack<WorkItem>)
    (enumerator: IAsyncEnumerator<Node>)
    : ValueTask<unit> =
    valueTask {
      let! hasNext = enumerator.MoveNextAsync()

      if hasNext then
        stack.Push(AwaitSeq enumerator)
        stack.Push(RenderNode enumerator.Current)
      else
        do! enumerator.DisposeAsync()
    }

  let inline private processWorkItem
    (sb: StringBuilder)
    (stack: Stack<WorkItem>)
    (ct: CancellationToken)
    (work: WorkItem)
    : ValueTask<unit> =
    valueTask {
      match work with
      | RenderNode n -> do! processNode sb stack ct n

      | CloseElement tag -> sb.Append("</").Append(tag).Append('>') |> ignore

      | RenderChildrenPrefetched(children, index, prefetched) ->
        do! processChildrenPrefetched stack ct children index prefetched

      | AwaitSeq enumerator -> do! processAwaitSeq stack enumerator
    }

  let renderNode node (ct: CancellationToken) = valueTask {
    let sb = StringBuilder(4096)
    let stack = Stack<WorkItem>(64)
    stack.Push(RenderNode node)

    while stack.Count > 0 do
      ct.ThrowIfCancellationRequested()
      let work = stack.Pop()
      do! processWorkItem sb stack ct work

    return sb.ToString()
  }

[<RequireQualifiedAccess>]
module Chunked =
  let inline private pushElementWork
    (stack: Stack<WorkItem>)
    (ct: CancellationToken)
    (el: Element)
    =
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

  let inline private pushFragmentWork
    (stack: Stack<WorkItem>)
    (ct: CancellationToken)
    (frag: Deque<Node>)
    =
    if not frag.IsEmpty then
      stack.Push(
        RenderChildrenPrefetched(frag, 0, prefetchAsyncChildren frag ct)
      )

  // Channel-backed chunked renderer to avoid taskSeq resumable state machines.
  // The producer is ValueTask-based to keep a fast synchronous path.

  let inline private write
    (writer: ChannelWriter<string>)
    (ct: CancellationToken)
    (chunk: string)
    : ValueTask =
    writer.WriteAsync(chunk, ct)

  let inline private completeOk(writer: ChannelWriter<string>) =
    writer.TryComplete() |> ignore

  let inline private completeError (writer: ChannelWriter<string>) (ex: exn) =
    writer.TryComplete(ex) |> ignore

  let inline private emitAttributes
    (writer: ChannelWriter<string>)
    (ct: CancellationToken)
    (attrs: Deque<AttributeNode>)
    : ValueTask =
    valueTaskUnit {
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
      | ValueSome idVal -> do! write writer ct $" id=\"{idVal}\""
      | _ -> ()

      if classes.Count > 0 then
        let cls = String.Join(" ", classes)
        // Preserve historical chunk boundaries relied on by tests:
        //  - " class=\"" then value then "\""
        do! write writer ct " class=\""
        do! write writer ct cls
        do! write writer ct "\""

      for struct (n, v) in others do
        do! write writer ct $" {n}=\"{v}\""
    }

  let inline private emitElement
    (stack: Stack<WorkItem>)
    (writer: ChannelWriter<string>)
    (ct: CancellationToken)
    (el: Element)
    : ValueTask =
    valueTaskUnit {
      do! write writer ct $"<{el.tag}"
      do! emitAttributes writer ct el.attributes
      do! write writer ct ">"
      pushElementWork stack ct el
    }

  let inline private emitNode
    (stack: Stack<WorkItem>)
    (writer: ChannelWriter<string>)
    (ct: CancellationToken)
    (n: Node)
    : ValueTask =
    valueTaskUnit {
      match n with
      | Element el -> do! emitElement stack writer ct el
      | Text t -> do! write writer ct (Encoding.htmlEncode t)
      | Raw r -> do! write writer ct r
      | Comment c -> do! write writer ct $"<!--{Encoding.htmlEncode c}-->"
      | PreRendered html -> do! write writer ct html
      | Fragment frag -> pushFragmentWork stack ct frag
      | AsyncNode asyncTask ->
        let! resolved = asyncTask ct
        stack.Push(RenderNode resolved)
      | AsyncSeqNode asyncSeq ->
        stack.Push(AwaitSeq(asyncSeq.GetAsyncEnumerator(ct)))
    }

  let inline private processChildrenPrefetched
    (stack: Stack<WorkItem>)
    (ct: CancellationToken)
    (children: Deque<Node>)
    (index: int)
    (prefetched: Dictionary<int, ValueTask<Node>> | null)
    : ValueTask =
    valueTaskUnit {
      if index < children.Count then
        if index + 1 < children.Count then
          stack.Push(RenderChildrenPrefetched(children, index + 1, prefetched))

        match prefetched with
        | null -> stack.Push(RenderNode(children[index]))
        | prefetched ->
          match prefetched.TryGetValue(index) with
          | true, startedTask ->
            let! resolved = startedTask
            stack.Push(RenderNode resolved)
          | false, _ -> stack.Push(RenderNode(children[index]))
    }

  let inline private processAwaitSeq
    (stack: Stack<WorkItem>)
    (enumerator: IAsyncEnumerator<Node>)
    : ValueTask =
    valueTaskUnit {
      let! hasNext = enumerator.MoveNextAsync()

      if hasNext then
        stack.Push(AwaitSeq enumerator)
        stack.Push(RenderNode enumerator.Current)
      else
        do! enumerator.DisposeAsync()
    }

  let inline private stepWorkItem
    (stack: Stack<WorkItem>)
    (writer: ChannelWriter<string>)
    (ct: CancellationToken)
    (work: WorkItem)
    : ValueTask =
    valueTaskUnit {
      match work with
      | RenderNode n -> do! emitNode stack writer ct n
      | CloseElement tag -> do! write writer ct $"</{tag}>"
      | RenderChildrenPrefetched(children, index, prefetched) ->
        do! processChildrenPrefetched stack ct children index prefetched
      | AwaitSeq enumerator -> do! processAwaitSeq stack enumerator
    }

  let private produce
    (node: Node)
    (writer: ChannelWriter<string>)
    (ct: CancellationToken)
    : ValueTask =
    valueTaskUnit {
      try
        let stack = Stack<WorkItem>(64)
        stack.Push(RenderNode node)

        while stack.Count > 0 do
          ct.ThrowIfCancellationRequested()
          let work = stack.Pop()
          do! stepWorkItem stack writer ct work

        completeOk writer
      with ex ->
        completeError writer ex
    }

  type private ChannelStringEnumerator
    (
      reader: ChannelReader<string>,
      ct: CancellationToken,
      producerTask: Task voption
    ) =
    let mutable current = Unchecked.defaultof<string>

    interface IAsyncEnumerator<string> with
      member _.Current = current

      member _.MoveNextAsync() : ValueTask<bool> =
        let rec loop() = valueTask {
          let! ok = reader.WaitToReadAsync(ct)

          if not ok then
            return false
          else
            match reader.TryRead(&current) with
            | true -> return true
            | false -> return! loop()
        }

        loop()

      member _.DisposeAsync() : ValueTask = valueTaskUnit {
        match producerTask with
        | ValueSome t -> do! t
        | ValueNone -> ()
      }

  type private ChannelStringEnumerable
    (reader: ChannelReader<string>, producerTask: Task voption) =
    interface IAsyncEnumerable<string> with
      member _.GetAsyncEnumerator(ct: CancellationToken) =
        new ChannelStringEnumerator(reader, ct, producerTask)
        :> IAsyncEnumerator<string>

  /// Channel-backed chunked renderer (no taskSeq).
  let renderNode(node: Node, ct: CancellationToken) : IAsyncEnumerable<string> =
    let ch =
      Channel.CreateUnbounded<string>(
        UnboundedChannelOptions(
          SingleWriter = true,
          SingleReader = true,
          AllowSynchronousContinuations = true
        )
      )

    // Start producer immediately. If it goes async, promote to Task so we can
    // observe exceptions and await it during enumerator disposal.
    let vt = produce node ch.Writer ct

    let producerTask =
      if vt.IsCompletedSuccessfully then
        ValueNone
      else
        ValueSome(vt.AsTask())

    new ChannelStringEnumerable(ch.Reader, producerTask)
    :> IAsyncEnumerable<string>

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
