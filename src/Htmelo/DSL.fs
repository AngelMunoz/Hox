namespace Htmelo

open System
open System.Collections.Generic
open System.Threading.Tasks
open System.Runtime.CompilerServices

open FSharp.Control
open IcedTasks
open Htmelo
open Htmelo.Core

module NodeOps =
  let rec addToNode(target: Node, value: Node) =
    match target, value with
    | Element target, Element value ->
      Element {
        target with
            children = [ yield! target.children; Element value ]
      }
    | Element target, Text value ->
      Element {
        target with
            children = [ yield! target.children; Text value ]
      }
    | Element target, Raw value ->
      Element {
        target with
            children = [ yield! target.children; Raw value ]
      }
    | Element target, AsyncNode value ->
      let tsk = cancellableValueTask {
        let! value = value
        return addToNode(Element target, value)
      }

      AsyncNode tsk
    | Element target, Fragment value ->
      Element {
        target with
            children = target.children @ value
      }
    | Element target, AsyncSeqNode value ->
      Element {
        target with
            children = [ yield! target.children; AsyncSeqNode value ]
      }
    | AsyncNode target, Element value ->
      let tsk = cancellableValueTask {
        let! target = target
        return addToNode(target, Element value)
      }

      AsyncNode tsk
    | AsyncNode target, Text value ->
      let tsk = cancellableValueTask {
        let! target = target
        return addToNode(target, Text value)
      }

      AsyncNode tsk

    | AsyncNode target, Raw value ->
      let tsk = cancellableValueTask {
        let! target = target
        return addToNode(target, Raw value)
      }

      AsyncNode tsk
    | AsyncNode target, AsyncNode value ->
      let tsk = cancellableValueTask {
        let! target = target
        let! value = value
        return addToNode(target, value)
      }

      AsyncNode tsk
    | AsyncNode target, AsyncSeqNode value ->
      let tsk = cancellableValueTask {
        let! target = target
        return addToNode(target, AsyncSeqNode value)
      }

      AsyncNode tsk
    | AsyncNode target, Fragment value ->
      let tsk = cancellableValueTask {
        let! target = target
        return addToNode(target, Fragment value)
      }

      AsyncNode tsk
    | AsyncSeqNode target, Element value ->
      AsyncSeqNode(
        taskSeq {
          yield! target
          Element value
        }
      )
    | AsyncSeqNode target, Text value ->
      AsyncSeqNode(
        taskSeq {
          yield! target
          Text value
        }
      )
    | AsyncSeqNode target, Raw value ->
      AsyncSeqNode(
        taskSeq {
          yield! target
          Raw value
        }
      )
    | AsyncSeqNode target, AsyncNode value ->
      let tsk = cancellableValueTask {
        let! value = value

        return
          AsyncSeqNode(
            taskSeq {
              yield! target
              value
            }
          )
      }

      AsyncNode tsk
    | AsyncSeqNode target, AsyncSeqNode value ->
      AsyncSeqNode(
        taskSeq {
          yield! target
          yield! value
        }
      )
    | AsyncSeqNode target, Fragment value ->
      AsyncSeqNode(
        taskSeq {
          yield! target
          yield! value
        }
      )
    | Fragment target, Element value ->
      Fragment([ yield! target; Element value ])
    | Fragment target, Text value -> Fragment([ yield! target; Text value ])
    | Fragment target, Raw value -> Fragment([ yield! target; Raw value ])
    | Fragment target, AsyncNode value ->
      let tsk = cancellableValueTask {
        let! value = value
        return Fragment([ yield! target; value ])
      }

      AsyncNode tsk
    | Fragment target, AsyncSeqNode value ->
      AsyncSeqNode(
        taskSeq {
          yield! target
          yield! value
        }
      )
    | Fragment target, Fragment value -> Fragment(target @ value)
    | Text target, Text value -> Text(target + value)
    | Text target, Raw value -> Text(target + value)
    | Text target, Element value ->
      Element {
        value with
            children = [ yield! value.children; Text target ]
      }
    | Text target, AsyncNode value ->
      let tsk = cancellableValueTask {
        let! value = value
        return addToNode(Text target, value)
      }

      AsyncNode tsk
    | _, _ -> target

  let rec addAttribute(target: Node, attribute: AttributeNode) =
    match target with
    | Element target ->
      Element {
        target with
            attributes = attribute :: target.attributes
      }
    | AsyncNode target ->
      let tsk = cancellableValueTask {
        let! target = target
        return addAttribute(target, attribute)
      }

      AsyncNode tsk
    | _ -> target

  /// Adds the 'value' node to the 'target' node, it can be seen as ading
  /// a child to a parent.
  let rec inline (<+) (target: Node) (value: Node) = addToNode(target, value)

  /// Adds an attribute to the 'target' node.
  let rec inline (<+.) (target: Node) (value: AttributeNode) =
    addAttribute(target, value)

open NodeOps

[<AutoOpen>]
type NodeBuilder =

  static member inline h(cssSelector: string) =
    Element(Parsers.selector cssSelector)

  static member inline h(cssSelector: string, children: Node seq) =
    Element(Parsers.selector cssSelector) <+ Fragment(children |> Seq.toList)

  static member inline h
    (
      cssSelector: string,
      [<ParamArray>] children: Node array
    ) =
    Element(Parsers.selector cssSelector) <+ Fragment(children |> Seq.toList)

  static member inline h
    (
      cssSelector: string,
      children: IAsyncEnumerable<Node>
    ) =
    Element(Parsers.selector cssSelector) <+ AsyncSeqNode children

  static member inline h(cssSelector: string, child: Node ValueTask) =
    let child =
      AsyncNode(
        cancellableValueTask {
          let! child = child
          return child
        }
      )

    Element(Parsers.selector cssSelector) <+ child

  static member inline h(element: Node ValueTask) =
    AsyncNode(
      cancellableValueTask {
        let! element = element
        return element
      }
    )

  static member inline h(element: Node ValueTask, child: Node) =
    let element =
      AsyncNode(
        cancellableValueTask {
          let! element = element
          return element
        }
      )

    element <+ child

  static member inline h(element: Node ValueTask, children: Node seq) =
    let element =
      AsyncNode(
        cancellableValueTask {
          let! element = element
          return element
        }
      )

    element <+ Fragment(children |> Seq.toList)

  static member inline h(element: Node Task) =
    NodeBuilder.h(
      valueTask {
        let! element = element
        return element
      }
    )

  static member inline h(element: Node Task, child: Node) =
    let element =
      AsyncNode(
        cancellableValueTask {
          let! element = element
          return element
        }
      )

    element <+ child

  static member inline h(element: Node Task, children: Node seq) =
    let element =
      AsyncNode(
        cancellableValueTask {
          let! element = element
          return element
        }
      )

    element <+ Fragment(children |> Seq.toList)

  static member inline h(element: Node Async) =
    NodeBuilder.h(
      valueTask {
        let! element = element
        return element
      }
    )

  static member inline h(element: Node Async, child: Node) =
    let element =
      AsyncNode(
        cancellableValueTask {
          let! element = element
          return element
        }
      )

    element <+ child

  static member inline h(element: Node Async, children: Node seq) =
    let element =
      AsyncNode(
        cancellableValueTask {
          let! element = element
          return element
        }
      )

    element <+ Fragment(children |> Seq.toList)

  static member inline h(element: Node, children: IAsyncEnumerable<Node>) =
    element <+ AsyncSeqNode children

  static member inline h
    (
      element: Node ValueTask,
      children: IAsyncEnumerable<Node>
    ) =
    let element =
      AsyncNode(
        cancellableValueTask {
          let! element = element
          return element
        }
      )

    element <+ AsyncSeqNode children

  static member inline h(element: Node Task, children: IAsyncEnumerable<Node>) =
    let element =
      AsyncNode(
        cancellableValueTask {
          let! element = element
          return element
        }
      )

    element <+ AsyncSeqNode children

  static member inline h
    (
      element: Node Async,
      children: IAsyncEnumerable<Node>
    ) =
    let element =
      AsyncNode(
        cancellableValueTask {
          let! element = element
          return element
        }
      )

    element <+ AsyncSeqNode children

  static member inline text(text: string) = Text text

  static member inline raw(raw: string) = Raw raw

  static member inline comment(comment: string) = Comment comment

  static member inline fragment(nodes: seq<Node>) =
    Fragment(nodes |> Seq.toList)

  static member inline fragment(nodes: IAsyncEnumerable<Node>) =
    AsyncSeqNode nodes

[<Extension>]
type NodeExtensions =

  [<Extension>]
  static member inline children(node: Node, children: Node seq) =
    node <+ NodeBuilder.fragment children

  [<Extension>]
  static member inline children(node: Node, children: IAsyncEnumerable<Node>) =
    node <+ NodeBuilder.fragment children

  [<Extension>]
  static member inline children
    (
      node: Node,
      [<ParamArrayAttribute>] nodes: Node array
    ) =
    node <+ NodeBuilder.fragment nodes

  [<Extension>]
  static member inline child(node: Node, child: Node) = node <+ child

  [<Extension>]
  static member inline child(node: Node, child: Node ValueTask) =
    let child =
      AsyncNode(
        cancellableValueTask {
          let! child = child
          return child
        }
      )

    node <+ child

  [<Extension>]
  static member inline child(node: Node, child: Node Task) =
    node.child(
      valueTask {
        let! child = child
        return child
      }
    )

  [<Extension>]
  static member inline child(node: Node, child: Node Async) =
    node.child(
      valueTask {
        let! child = child
        return child
      }
    )

  [<Extension>]
  static member inline attr(node: Node, name: string, ?value: string) =
    node
    <+. AttributeNode.Attribute {
      name = name
      value = defaultArg value System.String.Empty
    }


  [<Extension>]
  static member inline attr(node: Node, name: string, value: bool) =
    if value then
      node <+. AttributeNode.Attribute { name = name; value = "" }
    else
      node

  [<Extension>]
  static member inline attr(node: Node, name: string, value: int) =
    node <+. AttributeNode.Attribute { name = name; value = $"%i{value}" }

  [<Extension>]
  static member inline attr(node: Node, name: string, value: float) =
    node <+. AttributeNode.Attribute { name = name; value = $"%f{value}" }


  [<Extension>]
  static member inline attr(node: Node, name: string, value: string ValueTask) =
    let value =
      AttributeNode.AsyncAttribute(
        cancellableValueTask {
          let! value = value

          return { name = name; value = value }
        }
      )

    node <+. value

  [<Extension>]
  static member inline attr(node: Node, name: string, value: string Task) =
    node.attr(
      name,
      valueTask {
        let! value = value
        return value
      }
    )

  [<Extension>]
  static member inline attr(node: Node, name: string, value: string Async) =
    node.attr(
      name,
      valueTask {
        let! value = value
        return value
      }
    )

  [<Extension>]
  static member inline attr(node: Node, name: string, value: bool ValueTask) =
    let value = cancellableValueTask {
      let! value = value

      if value then
        return { name = name; value = String.Empty }
      else
        return {
          name = String.Empty
          value = String.Empty
        }
    }

    node <+. AttributeNode.AsyncAttribute value

  [<Extension>]
  static member inline attr(node: Node, name: string, value: bool Task) =
    node.attr(
      name,
      valueTask {
        let! value = value
        return value
      }
    )

  [<Extension>]
  static member inline attr(node: Node, name: string, value: bool Async) =
    node.attr(
      name,
      valueTask {
        let! value = value
        return value
      }
    )

  [<Extension>]
  static member inline attr(node: Node, name: string, value: int ValueTask) =
    let value = cancellableValueTask {
      let! value = value
      return { name = name; value = $"%i{value}" }
    }

    node <+. AttributeNode.AsyncAttribute value

  [<Extension>]
  static member inline attr(node: Node, name: string, value: int Task) =
    node.attr(
      name,
      valueTask {
        let! value = value
        return value
      }
    )

  [<Extension>]
  static member inline attr(node: Node, name: string, value: int Async) =
    node.attr(
      name,
      valueTask {
        let! value = value
        return value
      }
    )

  [<Extension>]
  static member inline attr(node: Node, name: string, value: float ValueTask) =
    let value = cancellableValueTask {
      let! value = value
      return { name = name; value = $"%f{value}" }
    }

    node <+. AttributeNode.AsyncAttribute value

  [<Extension>]
  static member inline attr(node: Node, name: string, value: float Task) =
    node.attr(
      name,
      valueTask {
        let! value = value
        return value
      }
    )

  [<Extension>]
  static member inline attr(node: Node, name: string, value: float Async) =
    node.attr(
      name,
      valueTask {
        let! value = value
        return value
      }
    )
