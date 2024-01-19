module Htmelo.Rendering

open System.Collections.Generic
open System.Diagnostics

open System.Web

open FSharp.Control
open IcedTasks

let private getAttributes(attributes: AttributeNode list) =
  attributes
  |> List.fold
    (fun (classes, attributes, asyncAttributes) attribute ->
      match attribute with
      | AttributeNode.Attribute { name = "class"; value = value } ->
        (value :: classes, attributes, asyncAttributes)
      | AttributeNode.Attribute attribute ->
        (classes, attribute :: attributes, asyncAttributes)
      | AttributeNode.AsyncAttribute asyncAttribute ->
        (classes, attributes, asyncAttribute :: asyncAttributes))
    ([], [], [])

let private renderAttr(node: AttributeNode) = cancellableValueTask {
  match node with
  | AttributeNode.Attribute { name = name; value = value } ->
    return
      $" %s{HttpUtility.HtmlAttributeEncode name}=\"%s{HttpUtility.HtmlAttributeEncode value}\""
  | AsyncAttribute asyncAttribute ->
    let! { name = name; value = value } = asyncAttribute

    return
      $" %s{HttpUtility.HtmlAttributeEncode name}=\"%s{HttpUtility.HtmlAttributeEncode value}\""
}

module Builder =
  open System.Text

  let rec private renderNode(node: Node) = cancellableValueTask {
    match node with
    | Element element -> return! renderElement element
    | Text text -> return HttpUtility.HtmlEncode text
    | Raw raw -> return raw
    | Comment comment -> return $"<!--%s{comment}-->"
    | Fragment nodes ->
      let! token = CancellableValueTask.getCancellationToken()
      let sb = StringBuilder()

      for node in nodes do
        let! node = renderNode node token
        sb.Append(node) |> ignore

      return sb.ToString()
    | AsyncNode(node) ->
      let! node = node
      return! renderNode node
    | AsyncSeqNode nodes ->
      let! token = CancellableValueTask.getCancellationToken()
      let sb = StringBuilder()

      do!
        nodes
        |> TaskSeq.iterAsync(fun node -> task {
          let! node = renderNode node token
          sb.Append(node) |> ignore
        })

      return sb.ToString()
  }

  and private renderElement(element: Element) = cancellableValueTask {
    let! token = CancellableValueTask.getCancellationToken()
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
          let! child = renderNode child token
          sb.Append(child) |> ignore

        return sb.ToString()
      }

      return sb.Append(children).Append("</").Append(tag).Append(">").ToString()
  }

  module ValueTask =
    let render(node: Node) = cancellableValueTask {
      let! result = renderNode node
      return result
    }

  module Async =
    let inline render(node: Node) =
      ValueTask.render node |> Async.AwaitCancellableValueTask

  module Task =
    let inline render(node: Node) : CancellableTask<string> =
      fun token -> (ValueTask.render node token).AsTask()

module Chunked =
  let rec private renderElement
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

  and private renderNode
    (
      node: Node,
      cancellationToken
    ) : IAsyncEnumerable<string> =
    taskSeq {
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

  let render(node: Node) = fun token -> renderNode(node, token)
