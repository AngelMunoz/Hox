module Hox.Rendering

open System
open System.Collections.Generic
open System.Threading.Tasks

open System.Web

open FSharp.Control
open IcedTasks

open Hox.Core
open System.Threading


module private Seq =
  let headTail (a : IEnumerable<_>) =
      let e = a.GetEnumerator()
      let hasNext = e.MoveNext()
      match hasNext with
      | true ->
        let head = e.Current
        let tail = seq {
          use e = e
          while e.MoveNext() do
            yield e.Current
        }
        ValueSome (head, tail)
      | false ->
        e.Dispose()
        ValueNone

module private TaskSeq =

  let headTail (a : IAsyncEnumerable<_>) =
    cancellableValueTask {
      let! ct = CancellableValueTask.getCancellationToken()
      let e = a.GetAsyncEnumerator ct
      let! hasNext = e.MoveNextAsync()
      match hasNext with
      | true ->
        let head = e.Current
        let tail = taskSeq {
          use e = e
          while! e.MoveNextAsync() do
            yield e.Current
        }
        return ValueSome (head, tail)
      | false ->
        do! e.DisposeAsync()
        return ValueNone
    }


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
  /// </summary>
  let renderNode (n : Node, cancellationToken : CancellationToken) : IAsyncEnumerable<string> =
    let cachedHtmlEncode =
      getEncodedCache EscapeMode.Html (Dictionary<string, string>())

    let cachedAttrEncode =
      getEncodedCache EscapeMode.Attribute (Dictionary<string, string>())

    taskSeq {
      let dfs = Stack<struct (Node * bool)>()
      dfs.Push(n, false)
      while dfs.Count > 0 do
        cancellationToken.ThrowIfCancellationRequested()
        let struct (node, closing) = dfs.Pop()
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

          dfs.Push(Element element, true)
          if not(voidTags.Value.Contains(element.tag)) then
            let nodes = element.children
            nodes
            |> Seq.headTail
            |> ValueOption.iter (fun (h, ts) ->
              // small optimization to avoid pushing the fragment if it's the only child
                if nodes.Count > 1 then
                  // Since this is a stack, we need to push nodes we want to process later first
                  let ts = ts |> LinkedList
                  dfs.Push(Fragment ts, false)
                // then push the first node so it can render next
                // this prevents long lists from stalling rendering
                dfs.Push(h, false)
            )
        | Text text ->  cachedHtmlEncode text
        | Raw raw -> raw
        | Comment comment -> $"<!--{cachedHtmlEncode comment}-->"
        | Fragment nodes ->
          nodes
          |> Seq.headTail
          |> ValueOption.iter (fun (h, ts) ->
              // small optimization to avoid pushing the fragment if it's the only child
              if nodes.Count > 1 then
                // Since this is a stack, we need to push nodes we want to process later first
                let ts =  ts |> LinkedList
                dfs.Push(Fragment ts, false)
              // then push the first node so it can render next
              // this prevents long lists from stalling rendering
              dfs.Push(h, false)
          )
        | AsyncNode node ->
          let! node = node cancellationToken
          dfs.Push(node, false)
        | AsyncSeqNode nodes ->
          match! TaskSeq.headTail nodes cancellationToken with
          | ValueSome (node, nodes) ->
            // Since this is a stack, we need to push nodes we want to process later first
            // Can't do the same optimization above because we don't know how many nodes are in the sequence
            dfs.Push(AsyncSeqNode nodes, false)
              // then push the first node so it can render next
              // this prevents long lists from stalling rendering
            dfs.Push(node, false)
          | ValueNone -> ()
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
    Chunked.renderNode(node, cancellationToken)

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
        Chunked.renderNode(node, cancellationToken)
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
