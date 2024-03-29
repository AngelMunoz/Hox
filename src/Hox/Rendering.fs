module Hox.Rendering

open System
open System.Collections.Generic
open System.Threading.Tasks

open System.Web

open FSharp.Control
open IcedTasks

open Hox.Core
open System.Threading

[<Struct; RequireQualifiedAccess>]
type EscapeMode =
  | Html
  | Attribute

let inline getEncodedCache
  escapeMode
  (encodedCache: Dictionary<string, string>)
  =
  let cachedHtmlEncode(s: string) =
    match encodedCache.TryGetValue(s) with
    | true, encoded -> encoded
    | false, _ ->
      let encoded =
        match escapeMode with
        | EscapeMode.Html -> HttpUtility.HtmlEncode(s)
        | EscapeMode.Attribute -> HttpUtility.HtmlAttributeEncode(s)

      encodedCache.[s] <- encoded
      encoded

  cachedHtmlEncode


let getAttributes(attributes: AttributeNode LinkedList) = cancellableValueTask {
  let cachedHtmlEncode =
    getEncodedCache EscapeMode.Attribute (Dictionary<string, string>())

  let clsSeq = ResizeArray()
  let attrSeq = ResizeArray()
  let mutable id = ValueNone

  // ids and classes have to be HtmlAttributeEncode'ed because we're
  // handling them separately, attributes are handled by the renderAttr function
  // which will HtmlAttributeEncode them.
  let mutable node = attributes.First

  while node <> null do
    match node.Value with
    | AttributeNode.Attribute { name = ""; value = _ } -> ()
    | AttributeNode.Attribute { name = "class"; value = value } ->
      clsSeq.Add(value |> cachedHtmlEncode)
    | AttributeNode.Attribute { name = "id"; value = value } ->
      id <-
        id
        |> ValueOption.orElseWith(fun _ -> ValueSome(value |> cachedHtmlEncode))
    | AttributeNode.Attribute attribute -> attrSeq.Add(attribute)
    | AttributeNode.AsyncAttribute asyncAttribute ->
      let! { name = name; value = value } = asyncAttribute

      if name = String.Empty then
        ()
      elif name = "id" then
        id <-
          id
          |> ValueOption.orElseWith(fun _ ->
            ValueSome(value |> cachedHtmlEncode))
      elif name = "class" then
        clsSeq.Add(value |> cachedHtmlEncode)
      else
        attrSeq.Add({ name = name; value = value })

    node <- node.Next

  return id, clsSeq, attrSeq
}

let renderAttr(node: AttributeNode) = cancellableValueTask {

  match node with
  | AttributeNode.Attribute { name = name; value = value } when
    String.IsNullOrWhiteSpace name
    ->
    return String.Empty
  | AttributeNode.Attribute { name = name; value = value } ->
    return $" %s{name}=\"%s{value}\""
  | AsyncAttribute asyncAttribute ->
    let! { name = name; value = value } = asyncAttribute

    if name = String.Empty then
      return String.Empty
    else
      return $" %s{name}=\"%s{value}\""
}

let voidTags =
  lazy
    (HashSet(
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
      StringComparer.InvariantCultureIgnoreCase
    ))

/// This module contains functions that are used to render a node to a string
/// It is backed by a StringBuilder.
module Builder =
  open System.Text



  let renderNode(node: Node) : CancellableValueTask<string> = cancellableValueTask {
    let! token = CancellableValueTask.getCancellationToken()

    let cachedAttrEncode =
      getEncodedCache EscapeMode.Attribute (Dictionary<string, string>())

    let cachedHtmlEncode =
      getEncodedCache EscapeMode.Html (Dictionary<string, string>())

    let sb = StringBuilder()
    let stack = Stack<struct (Node * bool)>()
    stack.Push(node, false)

    while stack.Count > 0 do
      token.ThrowIfCancellationRequested()

      let struct (node, closing) = stack.Pop()

      match node with
      | Element element when closing ->
        sb.Append("</").Append(element.tag).Append(">") |> ignore
      | Element element ->
        sb.Append("<").Append(element.tag) |> ignore

        let! id, classes, attributes = getAttributes element.attributes

        match id with
        | ValueSome id -> sb.Append(" id=\"").Append(id).Append("\"") |> ignore
        | ValueNone -> ()

        match classes with
        | classes when classes.Count = 0 -> ()
        | classes ->
          sb.Append(" class=\"").AppendJoin(' ', classes).Append("\"") |> ignore

        for attribute in attributes do
          let! attribute =
            renderAttr(
              AttributeNode.Attribute {
                attribute with
                    value = cachedAttrEncode attribute.value
                    name = cachedAttrEncode attribute.name
              }
            )

          sb.Append(attribute) |> ignore

        sb.Append(">") |> ignore

        if not(voidTags.Value.Contains(element.tag)) then
          stack.Push(Element element, true)
          let mutable node = element.children.Last

          while node <> null do
            stack.Push(node.Value, false)
            node <- node.Previous

      | Text text -> sb.Append(cachedHtmlEncode text) |> ignore
      | Raw raw -> sb.Append(raw) |> ignore
      | Comment comment ->
        sb.Append("<!--").Append(cachedHtmlEncode comment).Append("-->")
        |> ignore
      | Fragment nodes ->
        let mutable node = nodes.Last

        while node <> null do
          stack.Push(node.Value, false)
          node <- node.Previous
      | AsyncNode node ->
        // These nodes are already handling cancellation semantics
        // when they're added to the parent node, so we don't need to
        // do anything here.
        let! node = node
        stack.Push(node, false)
      | AsyncSeqNode nodes ->
        // This is a complicated case, we need to handle cancellation
        // but TaskSeq.toListAsync doesn't support cancellation
        let! nodes = nodes |> TaskSeq.toArrayAsync

        if nodes.Length > 0 then
          for child = nodes.Length - 1 downto 0 do
            stack.Push(nodes[child], false)

    return sb.ToString()
  }

/// This module contains functions that are used to render a node to a sequence of strings
/// As soon as a chunk is ready it is yielded to the caller.
[<RequireQualifiedAccess>]
module Chunked =

  /// <summary>
  /// Renders the node and it's children to an asynchronous sequence of strings
  /// The sequence is chunked by node and we keep track of the depth of the node
  /// so we can choose between rendering recursively or buffering the async sequences
  /// to avoid stack overflows.
  /// </summary>
  /// <param name="stack">The stack of nodes to render</param>
  /// <param name="cancellationToken">The cancellation token to use</param>
  /// <returns>An asynchronous sequence of strings</returns>
  /// <remarks>
  /// This function will switch between rendering recursively and buffering the async sequences
  /// depending on the depth of the node for nodes 200+ levels deep we'll default to buffering the chunk.
  /// </remarks>
  let rec renderNode
    (
      stack: Stack<struct (Node * bool * int)>,
      cancellationToken: CancellationToken
    ) : IAsyncEnumerable<string> =
    taskSeq {
      let cachedHtmlEncode =
        getEncodedCache EscapeMode.Html (Dictionary<string, string>())

      let cachedAttrEncode =
        getEncodedCache EscapeMode.Attribute (Dictionary<string, string>())

      while stack.Count > 0 do
        cancellationToken.ThrowIfCancellationRequested()

        let struct (node, closing, depth) = stack.Pop()

        match node with
        | Element element when closing -> $"</%s{element.tag}>"
        | Element element ->
          $"<%s{element.tag}"

          let! id, classes, attributes =
            getAttributes element.attributes cancellationToken

          match id with
          | ValueSome id -> $" id=\"%s{id}\""
          | ValueNone -> ()

          match classes with
          | classes when classes.Count = 0 -> ()
          | classes ->
            " class=\""
            System.String.Join(' ', classes)
            "\""

          for attribute in attributes do
            let! attribute =
              renderAttr
                (AttributeNode.Attribute {
                  attribute with
                      value = cachedAttrEncode attribute.value
                      name = cachedAttrEncode attribute.name
                })
                cancellationToken

            attribute

          ">"

          // Only non-void tags have children
          // if the element is not a void tag also close it
          if not(voidTags.Value.Contains(element.tag)) then
            stack.Push(Element element, true, depth)

            let mutable node = element.children.Last

            while node <> null do
              stack.Push(node.Value, false, depth)
              node <- node.Previous

        | Text text -> cachedHtmlEncode text
        | Raw raw -> raw
        | Comment comment -> $"<!--%s{cachedHtmlEncode comment}-->"
        | Fragment nodes ->

          let mutable node = nodes.Last

          while node <> null do
            stack.Push(node.Value, false, depth)
            node <- node.Previous
        | AsyncNode node ->
          // These nodes are already handling cancellation semantics
          // when they're added to the parent node, so we don't need to
          // do anything here.
          let! node = node cancellationToken
          stack.Push(node, false, depth)

        | AsyncSeqNode nodes ->
          // This number is tricky, I've seen stack frames overflow at 248
          // but also at 355, we'll keep it to 235 for now, we'll need to investigate
          // further or if you're reading this and have an idea, let me know please :).
          if depth > 235 then
            // Similarly to the Builder module, we can't cancell this operation
            // as it doesn't support cancellation, so we'll just have to wait
            // for it to finish.
            let! nodes = nodes |> TaskSeq.toArrayAsync

            if nodes.Length > 0 then
              for child = nodes.Length - 1 downto 0 do
                stack.Push(nodes[child], false, depth)

          else
            // This part makes me a bit sad, since we can't reverse the sequence
            // we have to add each node to a queue and then dequeue them in order
            // to render them in the correctly in the final html string.
            // so... we're kind of doing the same thing above but still yielding
            // recursively the chunks once available Not fan of this approach
            // we might as well just keep above's approach and that's it.

            // Ideally I'd just for node in nodes do renderThing but that would
            // render the nodes in reverse order.
            // I guess we'll be stuck until recursion us supported for Tasks/TaskSeq in the F# compiler 🫠
            // that or we could use async computations to attempt a recursive solution
            // but that would potentially make ValueTasks Hot which we'd like to avoid untill
            // we know the ValueTask is actually an async operation (see the cases above).
            let items = LinkedList()

            for node in nodes do
              items.AddFirst(node) |> ignore

            for item in items do
              stack.Push(item, false, depth)
            // The next renderNode call will check it's own
            // cancellation token and throw if needed.
            yield! renderNode(stack, cancellationToken)
    }

type Render =

  [<CompiledName "Start">]
  static member start
    (
      node: Node,
      [<Runtime.InteropServices.OptionalAttribute>] ?cancellationToken:
        CancellationToken
    ) =
    let cancellationToken = defaultArg cancellationToken CancellationToken.None

    Chunked.renderNode(Stack([ struct (node, false, 0) ]), cancellationToken)

  [<CompiledName "ToStream">]
  static member toStream
    (
      node: Node,
      stream: IO.Stream,
      [<Runtime.InteropServices.OptionalAttribute>] ?cancellationToken:
        CancellationToken
    ) =
    taskUnit {
      let cancellationToken =
        defaultArg cancellationToken CancellationToken.None

      let operation =
        Chunked.renderNode(
          Stack([ struct (node, false, 0) ]),
          cancellationToken
        )
        |> TaskSeq.map(System.Text.Encoding.UTF8.GetBytes >> ReadOnlyMemory)

      for chunk in operation do
        do! stream.WriteAsync(chunk, cancellationToken)
        do! stream.FlushAsync()
    }

  [<CompiledName "AsString">]
  static member asString
    (
      node: Node,
      [<Runtime.InteropServices.OptionalAttribute>] ?cancellationToken:
        CancellationToken
    ) =
    let cancellationToken = defaultArg cancellationToken CancellationToken.None
    Builder.renderNode node cancellationToken

  static member asStringAsync(node: Node) = async {
    return! Builder.renderNode node
  }
