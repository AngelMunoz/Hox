namespace Hox

open System
open System.Collections.Generic
open System.Threading.Tasks
open System.Runtime.CompilerServices

open FSharp.Control
open IcedTasks

open Hox
open Hox.Core

module private Helpers =
  let inline mkFragment(nodes: Node seq) =
    let d = Deque<Node>(8)

    for n in nodes do
      d.AddLast(n)

    Fragment d

  let inline mkFragmentOf(nodes: Node[]) =
    let d = Deque<Node>(nodes.Length)

    for n in nodes do
      d.AddLast(n)

    Fragment d

  let emptyNode = Fragment(Deque<Node>(0))

open Helpers

module NodeOps =
  let rec addToNode(target: Node, value: Node) =
    match target, value with
    | Element current, Element value ->
      current.children.AddLast(Element value)
      target
    | Element current, Text value ->
      current.children.AddLast(Text value)
      target
    | Element current, Comment value ->
      current.children.AddLast(Comment value)
      target
    | Element current, Raw value ->
      current.children.AddLast(Raw value)
      target
    | Element current, PreRendered value ->
      current.children.AddLast(PreRendered value)
      target
    | Element _, AsyncNode value ->
      let tsk = cancellableValueTask {
        let! value = value
        return addToNode(target, value)
      }

      AsyncNode tsk
    | Element current, Fragment value ->
      for node in value do
        current.children.AddLast(node)

      target
    | Element current, AsyncSeqNode value ->
      current.children.AddLast(AsyncSeqNode value)
      target
    | AsyncNode target, AsyncNode value ->
      let tsk = cancellableValueTask {
        let! target = target
        and! value = value
        return addToNode(target, value)
      }

      AsyncNode tsk
    | AsyncNode target, value ->
      let tsk = cancellableValueTask {
        let! target = target
        return addToNode(target, value)
      }

      AsyncNode tsk
    | AsyncSeqNode target, Fragment value ->
      AsyncSeqNode(
        taskSeq {
          yield! target
          yield! value
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
    | AsyncSeqNode target, value ->
      AsyncSeqNode(
        taskSeq {
          yield! target
          value
        }
      )
    | Fragment current, AsyncNode value ->
      let tsk = cancellableValueTask {
        let! value = value
        current.AddLast(value)
        return target
      }

      AsyncNode tsk
    | Fragment target, AsyncSeqNode value ->
      AsyncSeqNode(
        taskSeq {
          yield! target
          yield! value
        }
      )
    | Fragment current, Fragment value ->
      for node in value do
        current.AddLast(node)

      target
    | Fragment current, value ->
      current.AddLast(value)
      target
    | Text target, Text value -> Text(target + value)
    | Text target, Raw _ -> Text target
    | Raw target, Text value -> Raw(target + value)
    | Raw target, Raw value -> Raw(target + value)
    | Comment target, Comment value -> Comment(target + value)
    | Comment target, Text value -> Comment(target + value)
    | Comment target, Raw _ -> Comment target
    | _, _ -> target

  let rec addAttribute(target: Node, attribute: AttributeNode) =
    match target with
    | Element current ->
      current.attributes.AddLast(attribute)
      target
    | AsyncNode target ->
      let tsk = cancellableValueTask {
        let! target = target
        return addAttribute(target, attribute)
      }

      AsyncNode tsk
    | _ -> target

  [<TailCall>]
  let rec getInnerMostChild(element: Element) : Element =
    if element.children.IsEmpty then
      element
    else
      match element.children.PeekFirst() with
      | Element child -> getInnerMostChild child
      | _ -> element

  [<AutoOpen>]
  module Operators =
    let inline (<+) (target: Node) (value: Node) = addToNode(target, value)

    let inline (<+.) (target: Node) (value: AttributeNode) =
      addAttribute(target, value)

open NodeOps

[<AutoOpen>]
type NodeDsl =

  static member inline h(cssSelector: string) =
    Element(Parsers.selector cssSelector)

  static member inline h
    (cssSelector: string, [<ParamArray>] textNodes: string array)
    =
    let element = Parsers.selector cssSelector
    let innerMostChild = NodeOps.getInnerMostChild element

    for node in textNodes do
      innerMostChild.children.AddLast(Text node)

    Element(element)

  static member inline h(cssSelector: string, child: Node Task) =
    let child =
      AsyncNode(
        cancellableValueTask {
          let! token = CancellableValueTask.getCancellationToken()

          if token.IsCancellationRequested then
            return NodeDsl.empty
          else
            let! child = child
            return child
        }
      )

    let element = Parsers.selector cssSelector
    let innerMostChild = NodeOps.getInnerMostChild element
    innerMostChild.children.AddLast(child)
    Element(element)

  static member inline h(cssSelector: string, child: Node Async) =
    let child =
      AsyncNode(
        cancellableValueTask {
          let! token = CancellableValueTask.getCancellationToken()

          if token.IsCancellationRequested then
            return NodeDsl.empty
          else
            let! child = child
            return child
        }
      )

    let element = Parsers.selector cssSelector
    let innerMostChild = NodeOps.getInnerMostChild element
    innerMostChild.children.AddLast(child)
    Element(element)

  static member inline h(cssSelector: string, children: Node seq) =
    let element = Parsers.selector cssSelector
    let innerMostChild = NodeOps.getInnerMostChild element

    for node in children do
      innerMostChild.children.AddLast(node)

    Element(element)

  static member inline h
    (cssSelector: string, [<ParamArray>] children: Node array)
    =
    let element = Parsers.selector cssSelector
    let innerMostChild = NodeOps.getInnerMostChild element

    for node in children do
      innerMostChild.children.AddLast(node)

    Element(element)

  static member inline h
    (cssSelector: string, children: IAsyncEnumerable<Node>)
    =
    let element = Parsers.selector cssSelector
    let innerMostChild = NodeOps.getInnerMostChild element
    innerMostChild.children.AddLast(AsyncSeqNode children)
    Element(element)

  static member inline h(element: Node Task) =
    AsyncNode(
      cancellableValueTask {
        let! token = CancellableValueTask.getCancellationToken()

        if token.IsCancellationRequested then
          return NodeDsl.empty
        else
          let! element = element
          return element
      }
    )

  static member inline h(element: Node Task, child: Node) =
    let element =
      AsyncNode(
        cancellableValueTask {
          let! token = CancellableValueTask.getCancellationToken()

          if token.IsCancellationRequested then
            return NodeDsl.empty
          else
            let! element = element
            return element
        }
      )

    element <+ child

  static member inline h(element: Node Task, children: Node seq) =
    let element =
      AsyncNode(
        cancellableValueTask {
          let! token = CancellableValueTask.getCancellationToken()

          if token.IsCancellationRequested then
            return NodeDsl.empty
          else
            let! element = element
            return element
        }
      )

    element <+ mkFragment children

  static member inline h(element: Node Async) =
    AsyncNode(
      cancellableValueTask {
        let! token = CancellableValueTask.getCancellationToken()

        if token.IsCancellationRequested then
          return NodeDsl.empty
        else
          let! element = element
          return element
      }
    )

  static member inline h(element: Node Async, child: Node) =
    let node =
      AsyncNode(
        cancellableValueTask {
          let! token = CancellableValueTask.getCancellationToken()

          if token.IsCancellationRequested then
            return NodeDsl.empty
          else
            let! element = element
            return element
        }
      )

    node <+ child

  static member inline h(element: Node Async, children: Node seq) =
    let node =
      AsyncNode(
        cancellableValueTask {
          let! token = CancellableValueTask.getCancellationToken()

          if token.IsCancellationRequested then
            return NodeDsl.empty
          else
            let! element = element
            return element
        }
      )

    node <+ mkFragment children

  static member inline h(element: Node, children: IAsyncEnumerable<Node>) =
    element <+ AsyncSeqNode children

  static member inline h(element: Node Task, children: IAsyncEnumerable<Node>) =
    let node =
      AsyncNode(
        cancellableValueTask {
          let! token = CancellableValueTask.getCancellationToken()

          if token.IsCancellationRequested then
            return NodeDsl.empty
          else
            let! element = element
            return element
        }
      )

    node <+ AsyncSeqNode children

  static member inline h
    (element: Node Async, children: IAsyncEnumerable<Node>)
    =
    let node =
      AsyncNode(
        cancellableValueTask {
          let! token = CancellableValueTask.getCancellationToken()

          if token.IsCancellationRequested then
            return NodeDsl.empty
          else
            let! element = element
            return element
        }
      )

    node <+ AsyncSeqNode children

  static member inline text(text: string) = Text text

  static member inline text(text: string Task) =
    AsyncNode(
      cancellableValueTask {
        let! token = CancellableValueTask.getCancellationToken()

        if token.IsCancellationRequested then
          return Text String.Empty
        else
          let! text = text
          return Text text
      }
    )

  static member inline text(text: string Async) =
    AsyncNode(
      cancellableValueTask {
        let! token = CancellableValueTask.getCancellationToken()

        if token.IsCancellationRequested then
          return Text String.Empty
        else
          let! text = text
          return Text text
      }
    )

  static member inline raw(raw: string) = Raw raw

  static member inline raw(raw: string Task) =
    AsyncNode(
      cancellableValueTask {
        let! token = CancellableValueTask.getCancellationToken()

        if token.IsCancellationRequested then
          return Raw String.Empty
        else
          let! raw = raw
          return Raw raw
      }
    )

  static member inline raw(raw: string Async) =
    AsyncNode(
      cancellableValueTask {
        let! token = CancellableValueTask.getCancellationToken()

        if token.IsCancellationRequested then
          return Raw String.Empty
        else
          let! raw = raw
          return Raw raw
      }
    )

  static member inline comment(comment: string) = Comment comment

  static member inline fragment(nodes: Node seq) = mkFragment nodes

  static member inline fragment([<ParamArray>] nodes: Node array) =
    mkFragmentOf nodes

  static member inline fragment(nodes: Node seq Task) =
    AsyncNode(
      cancellableValueTask {
        let! token = CancellableValueTask.getCancellationToken()

        if token.IsCancellationRequested then
          return NodeDsl.empty
        else
          let! nodes = nodes
          return mkFragment nodes
      }
    )

  static member inline fragment(nodes: Node seq Async) =
    AsyncNode(
      cancellableValueTask {
        let! token = CancellableValueTask.getCancellationToken()

        if token.IsCancellationRequested then
          return NodeDsl.empty
        else
          let! nodes = nodes
          return mkFragment nodes
      }
    )

  static member inline fragment(nodes: IAsyncEnumerable<Node>) =
    AsyncSeqNode nodes

  static member empty = emptyNode

  static member inline attribute(name: string, value: string) =
    Attribute { name = name; value = value }

  static member inline attribute(name: string, value: string Task) =
    AsyncAttribute(
      cancellableValueTask {
        let! token = CancellableValueTask.getCancellationToken()

        if token.IsCancellationRequested then
          return {
            name = String.Empty
            value = String.Empty
          }
        else
          let! value = value
          return { name = name; value = value }
      }
    )

  static member inline attribute(name: string, value: string Async) =
    AsyncAttribute(
      cancellableValueTask {
        let! token = CancellableValueTask.getCancellationToken()

        if token.IsCancellationRequested then
          return {
            name = String.Empty
            value = String.Empty
          }
        else
          let! value = value
          return { name = name; value = value }
      }
    )

[<Extension>]
type NodeExtensions =

  [<Extension>]
  static member inline attr(node: Node, attribute: AttributeNode) =
    node <+. attribute

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
        let! token = CancellableValueTask.getCancellationToken()

        if token.IsCancellationRequested then
          return {
            name = String.Empty
            value = String.Empty
          }
        else
          let! value = value
          return { name = name; value = value }
      }
    )

  [<Extension>]
  static member inline attr(node: Node, name: string, value: string Async) =
    node
    <+. AsyncAttribute(
      cancellableValueTask {
        let! token = CancellableValueTask.getCancellationToken()

        if token.IsCancellationRequested then
          return {
            name = String.Empty
            value = String.Empty
          }
        else
          let! value = value
          return { name = name; value = value }
      }
    )

  [<Extension>]
  static member inline attr(node: Node, name: string, value: bool Task) =
    node
    <+. AsyncAttribute(
      cancellableValueTask {
        let! token = CancellableValueTask.getCancellationToken()

        if token.IsCancellationRequested then
          return {
            name = String.Empty
            value = String.Empty
          }
        else
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
        let! token = CancellableValueTask.getCancellationToken()

        if token.IsCancellationRequested then
          return {
            name = String.Empty
            value = String.Empty
          }
        else
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
        let! token = CancellableValueTask.getCancellationToken()

        if token.IsCancellationRequested then
          return {
            name = String.Empty
            value = String.Empty
          }
        else
          let! value = value
          return { name = name; value = $"%i{value}" }
      }
    )

  [<Extension>]
  static member inline attr(node: Node, name: string, value: int Async) =
    node
    <+. AsyncAttribute(
      cancellableValueTask {
        let! token = CancellableValueTask.getCancellationToken()

        if token.IsCancellationRequested then
          return {
            name = String.Empty
            value = String.Empty
          }
        else
          let! value = value
          return { name = name; value = $"%i{value}" }
      }
    )

  [<Extension>]
  static member inline attr(node: Node, name: string, value: float Task) =
    node
    <+. AsyncAttribute(
      cancellableValueTask {
        let! token = CancellableValueTask.getCancellationToken()

        if token.IsCancellationRequested then
          return {
            name = String.Empty
            value = String.Empty
          }
        else
          let! value = value
          return { name = name; value = $"%f{value}" }
      }
    )

  [<Extension>]
  static member inline attr(node: Node, name: string, value: float Async) =
    node
    <+. AsyncAttribute(
      cancellableValueTask {
        let! token = CancellableValueTask.getCancellationToken()

        if token.IsCancellationRequested then
          return {
            name = String.Empty
            value = String.Empty
          }
        else
          let! value = value
          return { name = name; value = $"%f{value}" }
      }
    )

[<AutoOpen>]
type DeclarativeShadowDom =
  static member inline sh(tagName: string, templateDefinition: Node) =
    h(tagName, h("template[shadowrootmode=open]", templateDefinition))

  static member inline sh
    (tagName: string, [<ParamArray>] templateDefinition: Node array)
    =
    let tpl = h("template[shadowrootmode=open]", templateDefinition)
    fun instanceContent -> h(tagName, tpl, instanceContent)

  static member inline shcs
    (tagName: string, [<ParamArray>] templateDefinition: Node array)
    =
    let tpl = h("template[shadowrootmode=open]", templateDefinition)
    Func<Node, Node>(fun instanceContent -> h(tagName, tpl, instanceContent))

type ScopableElements =
  static member inline article([<ParamArray>] content: _ array) =
    sh("article", fragment content)

  static member inline aside([<ParamArray>] content: _ array) =
    sh("aside", fragment content)

  static member inline blockquote([<ParamArray>] content: _ array) =
    sh("blockquote", fragment content)

  static member inline body([<ParamArray>] content: _ array) =
    sh("body", fragment content)

  static member inline div([<ParamArray>] content: _ array) =
    sh("div", fragment content)

  static member inline footer([<ParamArray>] content: _ array) =
    sh("footer", fragment content)

  static member inline h1([<ParamArray>] content: _ array) =
    sh("h1", fragment content)

  static member inline h2([<ParamArray>] content: _ array) =
    sh("h2", fragment content)

  static member inline h3([<ParamArray>] content: _ array) =
    sh("h3", fragment content)

  static member inline h4([<ParamArray>] content: _ array) =
    sh("h4", fragment content)

  static member inline h5([<ParamArray>] content: _ array) =
    sh("h5", fragment content)

  static member inline h6([<ParamArray>] content: _ array) =
    sh("h6", fragment content)

  static member inline header([<ParamArray>] content: _ array) =
    sh("header", fragment content)

  static member inline main([<ParamArray>] content: _ array) =
    sh("main", fragment content)

  static member inline nav([<ParamArray>] content: _ array) =
    sh("nav", fragment content)

  static member inline p([<ParamArray>] content: _ array) =
    sh("p", fragment content)

  static member inline section([<ParamArray>] content: _ array) =
    sh("section", fragment content)

  static member inline span([<ParamArray>] content: _ array) =
    sh("span", fragment content)
