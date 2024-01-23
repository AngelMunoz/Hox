namespace Hox

open System
open System.Collections.Generic
open System.Threading.Tasks
open System.Runtime.CompilerServices

open FSharp.Control
open IcedTasks

open Hox
open Hox.Core

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

  [<AutoOpen>]
  module Operators =
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

  static member inline h(cssSelector: string, child: Node Task) =
    let child =
      AsyncNode(
        cancellableValueTask {
          let! child = child
          return child
        }
      )

    Element(Parsers.selector cssSelector) <+ child

  static member inline h(cssSelector: string, child: Node Async) =
    let child =
      AsyncNode(
        cancellableValueTask {
          let! child = child
          return child
        }
      )

    Element(Parsers.selector cssSelector) <+ child

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

  static member inline h(element: Node Task) =
    AsyncNode(
      cancellableValueTask {
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
    AsyncNode(
      cancellableValueTask {
        let! element = element
        return element
      }
    )

  static member inline h(element: Node Async, child: Node) =
    AsyncNode(
      cancellableValueTask {
        let! element = element
        return element <+ child
      }
    )

  static member inline h(element: Node Async, children: Node seq) =
    AsyncNode(
      cancellableValueTask {
        let! element = element
        return element <+ Fragment(children |> Seq.toList)
      }
    )

  static member inline h(element: Node, children: IAsyncEnumerable<Node>) =
    element <+ AsyncSeqNode children

  static member inline h(element: Node Task, children: IAsyncEnumerable<Node>) =
    AsyncNode(
      cancellableValueTask {
        let! element = element
        return element <+ AsyncSeqNode children
      }
    )

  static member inline h
    (
      element: Node Async,
      children: IAsyncEnumerable<Node>
    ) =
    AsyncNode(
      cancellableValueTask {
        let! element = element
        return element <+ AsyncSeqNode children
      }
    )

  static member inline text(text: string) = Text text

  static member inline text(text: string Task) =
    AsyncNode(
      cancellableValueTask {
        let! text = text
        return Text text
      }
    )

  static member inline text(text: string Async) =
    AsyncNode(
      cancellableValueTask {
        let! text = text
        return Text text
      }
    )

  static member inline raw(raw: string) = Raw raw

  static member inline raw(raw: string Task) =
    AsyncNode(
      cancellableValueTask {
        let! raw = raw
        return Raw raw
      }
    )

  static member inline raw(raw: string Async) =
    AsyncNode(
      cancellableValueTask {
        let! raw = raw
        return Raw raw
      }
    )

  static member inline comment(comment: string) = Comment comment

  static member inline fragment(nodes: Node seq) = Fragment(nodes |> Seq.toList)

  static member inline fragment(nodes: Node seq Task) =
    AsyncNode(
      cancellableValueTask {
        let! nodes = nodes
        return Fragment(nodes |> Seq.toList)
      }
    )

  static member inline fragment(nodes: Node seq Async) =
    AsyncNode(
      cancellableValueTask {
        let! nodes = nodes
        return Fragment(nodes |> Seq.toList)
      }
    )

  static member inline fragment(nodes: IAsyncEnumerable<Node>) =
    AsyncSeqNode nodes

[<Extension>]
type NodeExtensions =

  [<Extension>]
  static member inline attr(node: Node, name: string, ?value: string) =
    node
    <+. Attribute {
      name = name
      value = defaultArg value String.Empty
    }

  [<Extension>]
  static member inline attr(node: Node, name: string, value: bool) =
    if value then
      node <+. Attribute { name = name; value = String.Empty }
    else
      node

  [<Extension>]
  static member inline attr(node: Node, name: string, value: int) =
    node <+. Attribute { name = name; value = $"%i{value}" }

  [<Extension>]
  static member inline attr(node: Node, name: string, value: float) =
    node <+. Attribute { name = name; value = $"%f{value}" }


  [<Extension>]
  static member inline attr(node: Node, name: string, value: string Task) =
    node
    <+. AsyncAttribute(
      cancellableValueTask {
        let! value = value
        return { name = name; value = value }
      }
    )

  [<Extension>]
  static member inline attr(node: Node, name: string, value: string Async) =
    node
    <+. AsyncAttribute(
      cancellableValueTask {
        let! value = value
        return { name = name; value = value }
      }
    )

  [<Extension>]
  static member inline attr(node: Node, name: string, value: bool Task) =

    node
    <+. AsyncAttribute(
      cancellableValueTask {
        let! value = value

        if value then
          return { name = name; value = String.Empty }
        else
          return {
            name = String.Empty
            value = String.Empty
          }
      }
    )

  [<Extension>]
  static member inline attr(node: Node, name: string, value: bool Async) =
    node
    <+. AsyncAttribute(
      cancellableValueTask {
        let! value = value

        if value then
          return { name = name; value = String.Empty }
        else
          return {
            name = String.Empty
            value = String.Empty
          }
      }
    )

  [<Extension>]
  static member inline attr(node: Node, name: string, value: int Task) =
    node
    <+. AsyncAttribute(
      cancellableValueTask {
        let! value = value
        return { name = name; value = $"%i{value}" }
      }
    )

  [<Extension>]
  static member inline attr(node: Node, name: string, value: int Async) =
    node
    <+. AsyncAttribute(
      cancellableValueTask {
        let! value = value
        return { name = name; value = $"%i{value}" }
      }
    )

  [<Extension>]
  static member inline attr(node: Node, name: string, value: float Task) =
    node
    <+. AsyncAttribute(
      cancellableValueTask {
        let! value = value
        return { name = name; value = $"%f{value}" }
      }
    )

  [<Extension>]
  static member inline attr(node: Node, name: string, value: float Async) =
    node
    <+. AsyncAttribute(
      cancellableValueTask {
        let! value = value
        return { name = name; value = $"%f{value}" }
      }
    )


[<AutoOpen>]
type DeclarativeShadowDom =

  static member inline sh
    (
      tagName: string,
      [<ParamArray>] templateDefinition: Node array
    ) =
    let tpl = h("template[shadowrootmode=open]", templateDefinition)

    fun instanceContent -> h(tagName, tpl, instanceContent)

  static member inline shcs
    (
      tagName: string,
      [<ParamArray>] templateDefinition: Node array
    ) =
    let tpl = h("template[shadowrootmode=closed]", templateDefinition)

    Func<Node, Node>(fun instanceContent -> h(tagName, tpl, instanceContent))

type ScopableElements =

  static member inline article([<ParamArray>] content: _ array) =
    content |> fragment |> sh "article"

  static member inline aside([<ParamArray>] content: _ array) =
    content |> fragment |> sh "aside"

  static member inline blockquote([<ParamArray>] content: _ array) =
    content |> fragment |> sh "blockquote"

  static member inline body([<ParamArray>] content: _ array) =
    content |> fragment |> sh "body"

  static member inline div([<ParamArray>] content: _ array) =
    content |> fragment |> sh "div"

  static member inline footer([<ParamArray>] content: _ array) =
    content |> fragment |> sh "footer"

  static member inline h1([<ParamArray>] content: _ array) =
    content |> fragment |> sh "h1"

  static member inline h2([<ParamArray>] content: _ array) =
    content |> fragment |> sh "h2"

  static member inline h3([<ParamArray>] content: _ array) =
    content |> fragment |> sh "h3"

  static member inline h4([<ParamArray>] content: _ array) =
    content |> fragment |> sh "h4"

  static member inline h5([<ParamArray>] content: _ array) =
    content |> fragment |> sh "h5"

  static member inline h6([<ParamArray>] content: _ array) =
    content |> fragment |> sh "h6"

  static member inline header([<ParamArray>] content: _ array) =
    content |> fragment |> sh "header"

  static member inline main([<ParamArray>] content: _ array) =
    content |> fragment |> sh "main"

  static member inline nav([<ParamArray>] content: _ array) =
    content |> fragment |> sh "nav"

  static member inline p([<ParamArray>] content: _ array) =
    content |> fragment |> sh "p"

  static member inline section([<ParamArray>] content: _ array) =
    content |> fragment |> sh "section"

  static member inline span([<ParamArray>] content: _ array) =
    content |> fragment |> sh "span"
