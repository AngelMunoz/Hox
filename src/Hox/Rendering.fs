module Hox.Rendering

open System
open System.Collections.Generic
open System.Diagnostics

open System.Web

open FSharp.Control
open IcedTasks

open Hox.Core
open System.Threading

let getAttributes(attributes: AttributeNode list) =
  attributes
  |> List.fold
    (fun (classes, attributes, asyncAttributes) attribute ->
      match attribute with
      | AttributeNode.Attribute { name = ""; value = value } ->
        (classes, attributes, asyncAttributes)
      | AttributeNode.Attribute { name = "class"; value = value } ->
        (value :: classes, attributes, asyncAttributes)
      | AttributeNode.Attribute attribute ->
        (classes, attribute :: attributes, asyncAttributes)
      | AttributeNode.AsyncAttribute asyncAttribute ->
        (classes, attributes, asyncAttribute :: asyncAttributes))
    ([], [], [])

let renderAttr(node: AttributeNode) = cancellableValueTask {
  match node with
  | AttributeNode.Attribute { name = name; value = value } ->
    return
      $" %s{HttpUtility.HtmlAttributeEncode name}=\"%s{HttpUtility.HtmlAttributeEncode value}\""
  | AsyncAttribute asyncAttribute ->
    let! { name = name; value = value } = asyncAttribute

    if name = String.Empty then
      return String.Empty
    else
      return
        $" %s{HttpUtility.HtmlAttributeEncode name}=\"%s{HttpUtility.HtmlAttributeEncode value}\""
}

/// This module contains functions that are used to render a node to a string
/// It is backed by a StringBuilder.
module Builder =
  open System.Text

  let renderNode(node: Node) : CancellableValueTask<string> = cancellableValueTask {
    let sb = StringBuilder()
    let stack = Stack<struct (Node * bool)>()
    stack.Push(node, false)

    while stack.Count > 0 do
      let struct (node, closing) = stack.Pop()

      match node with
      | Element element when closing ->
        sb.Append("</").Append(element.tag).Append(">") |> ignore
      | Element element ->
        sb.Append("<").Append(element.tag) |> ignore

        let classes, attributes, asyncAttributes =
          getAttributes element.attributes

        match classes with
        | [] -> ()
        | classes ->
          sb.Append(" class=\"").AppendJoin(' ', classes).Append("\"") |> ignore

        for attribute in attributes do
          let! attribute = renderAttr(AttributeNode.Attribute attribute)
          sb.Append(attribute) |> ignore

        for attribute in asyncAttributes do
          let! attribute = renderAttr(AttributeNode.AsyncAttribute attribute)
          sb.Append(attribute) |> ignore

        sb.Append(">") |> ignore

        match element.tag.ToLowerInvariant() with
        | "area"
        | "base"
        | "br"
        | "col"
        | "command"
        | "embed"
        | "hr"
        | "img"
        | "input"
        | "keygen"
        | "link"
        | "meta"
        | "param"
        | "source"
        | "track"
        | "wbr" -> ()
        | tag ->
          stack.Push(Element element, true)

          if element.children.Length > 0 then
            for child in element.children.Length - 1 .. -1 .. 0 do
              stack.Push((element.children[child], false))

      | Text text -> sb.Append(HttpUtility.HtmlEncode text) |> ignore
      | Raw raw -> sb.Append(raw) |> ignore
      | Comment comment -> sb.Append($"<!--%s{comment}-->") |> ignore
      | Fragment nodes ->
        if nodes.Length > 0 then
          for child in nodes.Length - 1 .. -1 .. 0 do
            stack.Push((nodes[child], false))

      | AsyncNode node ->
        let! node = node
        stack.Push(node, false)
      | AsyncSeqNode nodes ->
        let! nodes = nodes |> TaskSeq.toListAsync

        if nodes.Length > 0 then
          for child in nodes.Length - 1 .. -1 .. 0 do
            stack.Push((nodes[child], false))

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
      while stack.Count > 0 do
        let struct (node, closing, depth) = stack.Pop()

        match node with
        | Element element when closing -> $"</%s{element.tag}>"
        | Element element ->
          $"<%s{element.tag}"

          let classes, attributes, asyncAttributes =
            getAttributes element.attributes

          match classes with
          | [] -> ()
          | classes ->
            " class=\""
            yield! classes
            "\""

          for attribute in attributes do
            let! attribute =
              renderAttr (AttributeNode.Attribute attribute) cancellationToken

            attribute

          for attribute in asyncAttributes do
            let! attribute =
              renderAttr
                (AttributeNode.AsyncAttribute attribute)
                cancellationToken

            attribute

          ">"

          match element.tag with
          | "area"
          | "base"
          | "br"
          | "col"
          | "command"
          | "embed"
          | "hr"
          | "img"
          | "input"
          | "keygen"
          | "link"
          | "meta"
          | "param"
          | "source"
          | "track"
          | "wbr" -> ()
          | _ ->
            stack.Push(Element element, true, depth)

            if element.children.Length > 0 then
              for child in element.children.Length - 1 .. -1 .. 0 do
                stack.Push((element.children[child], false, depth + 1))

        | Text text -> HttpUtility.HtmlEncode text
        | Raw raw -> raw
        | Comment comment -> $"<!--%s{comment}-->"
        | Fragment nodes ->
          if nodes.Length > 0 then
            for child in nodes.Length - 1 .. -1 .. 0 do
              stack.Push((nodes[child], false, depth))

        | AsyncNode node ->
          let! node = node cancellationToken
          stack.Push(node, false, depth)

        | AsyncSeqNode nodes ->
          // This number is tricky, I've seen stack frames overflow at 248
          // but also at 355, we'll keep it to 235 for now, we'll need to investigate
          // further or if you're reading this and have an idea, let me know please :).
          if depth > 235 then
            let! nodes = nodes |> TaskSeq.toListAsync

            if nodes.Length > 0 then
              for child in nodes.Length - 1 .. -1 .. 0 do
                stack.Push((nodes[child], false, depth))

          else
            // This part makes me a bit sad, since we can't reverse the sequence
            // we have to add each node to a queue and then dequeue them in order
            // to render them in the correctly in the final html file.
            // so... we're kind of doing the same thing above but still yielding
            // recursively the chunks once available Not fan of this approach
            // we might as well just keep above's approach and that's it.

            // Ideally I'd just for node in nodes do renderThing but that would
            // render the nodes in reverse order.
            // I guess we'll be stuck until recursion us supported for Tasks/TaskSeq in the F# compiler 🫠
            // that or we could use async computations to attempt a recursive solution
            // but that would potentially make ValueTasks Hot which we'd like to avoid untill
            // we know the ValueTask is actually an async operation (see the cases above).
            let items = ResizeArray()

            for node in nodes do
              items.Add node

            for i in items.Count - 1 .. -1 .. 0 do
              stack.Push((items.[i], false, depth))

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

      for chunk in
        Chunked.renderNode(
          Stack([ struct (node, false, 0) ]),
          cancellationToken
        ) do
        let bytes = System.Text.Encoding.UTF8.GetBytes(chunk)
        do! stream.WriteAsync(ReadOnlyMemory(bytes), cancellationToken)
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
