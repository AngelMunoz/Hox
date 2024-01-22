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

  let rec renderNode(node: Node) = cancellableValueTask {
    match node with
    | Element element -> return! renderElement element
    | Text text -> return HttpUtility.HtmlEncode text
    | Raw raw -> return raw
    | Comment comment -> return $"<!--%s{comment}-->"
    | Fragment nodes ->
      let sb = StringBuilder()

      for node in nodes do
        let! node = renderNode node
        sb.Append(node) |> ignore

      return sb.ToString()
    | AsyncNode(node) ->
      let! node = node
      return! renderNode node
    | AsyncSeqNode nodes ->
      let sb = StringBuilder()

      for node in nodes do
        let! node = renderNode node
        sb.Append(node) |> ignore

      return sb.ToString()
  }

  and renderElement(element: Element) = cancellableValueTask {
    let classes, attributes, asyncAttributes = getAttributes element.attributes
    let sb = StringBuilder()
    sb.Append("<").Append(element.tag) |> ignore

    match classes with
    | [] -> ()
    | classes ->
      sb.Append(" class=\"").AppendJoin(' ', classes).Append("\"") |> ignore


    let! attributes = cancellableValueTask {
      let sb = StringBuilder()

      for attribute in attributes do
        let! attribute = renderAttr(AttributeNode.Attribute attribute)
        sb.Append(attribute) |> ignore

      for attribute in asyncAttributes do
        let! attribute = renderAttr(AttributeNode.AsyncAttribute attribute)
        sb.Append(attribute) |> ignore

      return sb.ToString()
    }

    sb.Append(attributes).Append(">") |> ignore

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
    | "wbr" ->
      if element.children.Length > 0 then
        Debug.WriteLine(
          $"Warning: Self closing tag has children",
          element.children |> Seq.cast<obj> |> Seq.toArray
        )

      return sb.ToString()
    | tag ->
      let! children = cancellableValueTask {
        let sb = StringBuilder()

        for child in element.children do
          let! child = renderNode child
          sb.Append(child) |> ignore

        return sb.ToString()
      }

      return sb.Append(children).Append("</").Append(tag).Append(">").ToString()
  }


/// This module contains functions that are used to render a node to a sequence of strings
/// As soon as a chunk is ready it is yielded to the caller.
[<RequireQualifiedAccess>]
module Chunked =
  let rec renderNode(node: Node, cancellationToken) : IAsyncEnumerable<string> = taskSeq {
    match node with
    | Element element -> yield! renderElement(element, cancellationToken)
    | Text text -> HttpUtility.HtmlEncode text
    | Raw raw -> raw
    | Comment comment -> $"<!--{comment}-->"
    | Fragment nodes ->
      for node in nodes do
        yield! renderNode(node, cancellationToken)
    | AsyncNode node ->
      let! node = node cancellationToken

      yield! renderNode(node, cancellationToken)
    | AsyncSeqNode nodes ->
      for node in nodes do
        yield! renderNode(node, cancellationToken)
  }

  and renderElement
    (
      element: Element,
      cancellationToken
    ) : IAsyncEnumerable<string> =
    taskSeq {
      let classes, attributes, asyncAttributes =
        getAttributes element.attributes

      $"<{element.tag}"

      if classes.Length > 0 then
        " class=\""
        yield! classes
        "\""

      for attribute in attributes do
        let! attr =
          renderAttr (AttributeNode.Attribute attribute) cancellationToken

        attr

      for asyncAttribute in asyncAttributes do
        let! attr =
          renderAttr
            (AttributeNode.AsyncAttribute asyncAttribute)
            cancellationToken

        attr

      ">"

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

        for child in element.children do
          yield! renderNode(child, cancellationToken)

        $"</{tag}>"
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
      [<Runtime.InteropServices.OptionalAttribute>] ?bufferSize: int,
      [<Runtime.InteropServices.OptionalAttribute>] ?cancellationToken:
        CancellationToken
    ) =
    taskUnit {
      let cancellationToken =
        defaultArg cancellationToken CancellationToken.None

      let bufferSize = defaultArg bufferSize 1440
      use writer = new IO.BufferedStream(stream, bufferSize)

      for chunk in Chunked.renderNode(node, cancellationToken) do
        let bytes = System.Text.Encoding.UTF8.GetBytes(chunk)
        do! writer.WriteAsync(ReadOnlyMemory(bytes), cancellationToken)
        do! writer.FlushAsync()
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
