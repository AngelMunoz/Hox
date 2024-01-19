[<AutoOpen>]
module Htmelo.DSL

open System.Threading.Tasks

open IcedTasks
open System.Collections.Generic



let inline El (cssSelector: string) = Element(Parsers.selector cssSelector)

let inline ElResult (cssSelector: string) =
  Parsers.selectorResult cssSelector |> Result.map Element

let inline TryEl (cssSelector: string) =
  Parsers.trySelector cssSelector |> Option.map Element

let AsyncEl (element: Node Async) : Node =
  let tsk = cancellableValueTask {
    let! element = element
    return element
  }

  AsyncNode tsk

let TaskEl (element: Node Task) : Node =
  let tsk = cancellableValueTask {
    let! element = element
    return element
  }

  AsyncNode tsk

let vTaskEl (element: Node ValueTask) : Node =
  let tsk = cancellableValueTask {
    let! element = element
    return element
  }

  AsyncNode tsk

let AwaitChildren (elements: IAsyncEnumerable<Node>) : Node =
  AsyncSeqNode elements

let rec Children (children: Node list) (element: Node) =
  match element with
  | Element element ->
    Element {
      element with
          children = element.children @ children
    }
  | AsyncNode node ->
    let tsk = cancellableValueTask {
      let! node = node
      return Children children node
    }

    AsyncNode tsk
  | _ -> element

let inline Text (text: string) (element: Node) = Children [ Text text ] element

let inline Raw (raw: string) (element: Node) = Children [ Raw raw ] element

module Attr =
  let inline create (name: string) (value: string) (element: Node) =
    match element with
    | Element element ->
      Element {
        element with
            attributes =
              AttributeNode.Attribute { name = name; value = value }
              :: element.attributes
      }
    | _ -> element

  let inline class' (value: string) (element: Node) =
    match element with
    | Element element ->
      Element {
        element with
            attributes =
              AttributeNode.Attribute { name = "class"; value = value }
              :: element.attributes
      }
    | _ -> element

  let inline id (value: string) (element: Node) =
    match element with
    | Element element ->
      Element {
        element with
            attributes =
              AttributeNode.Attribute { name = "id"; value = value }
              :: element.attributes
      }
    | _ -> element

  let inline style (value: string) (element: Node) =
    match element with
    | Element element ->
      Element {
        element with
            attributes =
              AttributeNode.Attribute { name = "style"; value = value }
              :: element.attributes
      }
    | _ -> element
