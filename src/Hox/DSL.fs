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
      target.children.AddLast(LinkedListNode(Element value))
      Element target
    | Element target, Text value ->
      target.children.AddLast(LinkedListNode(Text value))
      Element target
    | Element target, Comment value ->
      target.children.AddLast(LinkedListNode(Comment value))
      Element target
    | Element target, Raw value ->
      target.children.AddLast(LinkedListNode(Raw value))
      Element target
    | Element target, AsyncNode value ->
      let tsk = cancellableValueTask {
        let! value = value
        return addToNode(Element target, value)
      }

      AsyncNode tsk
    | Element target, Fragment value ->
      for node in value do
        target.children.AddLast(LinkedListNode(node))

      Element target
    | Element target, AsyncSeqNode value ->
      target.children.AddLast(LinkedListNode(AsyncSeqNode value))
      Element target
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
        and! value = value
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
      target.AddLast(LinkedListNode(Element value))
      Fragment target
    | Fragment target, Text value ->
      target.AddLast(LinkedListNode(Text value))
      Fragment target
    | Fragment target, Raw value ->
      target.AddLast(LinkedListNode(Raw value))
      Fragment target
    | Fragment target, AsyncNode value ->

      let tsk = cancellableValueTask {
        let! value = value
        target.AddLast(LinkedListNode(value))
        return Fragment(target)
      }

      AsyncNode tsk
    | Fragment target, AsyncSeqNode value ->
      AsyncSeqNode(
        taskSeq {
          yield! target
          yield! value
        }
      )
    | Fragment target, Fragment value ->
      for node in value do
        target.AddLast(LinkedListNode(node))

      Fragment target

    | Text target, Text value -> Text(target + value)
    // merging a text node with a raw node is not supported
    // as the raw node contents will be scaped by the text node
    | Text target, Raw _ -> Text target

    // WARNING: merging anything with raw node will result in converting the content
    // into RAW nodes, which can lead to unescaped inputs.
    // make sure to note this in the documentation
    | Raw target, Text value -> Raw(target + value)
    | Raw target, Raw value -> Raw(target + value)

    | Comment target, Comment value -> Comment(target + value)
    | Comment target, Text value -> Comment(target + value)
    // merging a comment node with a raw node is not supported
    // as the raw node contents will be scaped by the comment node
    | Comment target, Raw _ -> Comment target
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
      target.attributes.AddLast(LinkedListNode(attribute))
      Element target
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

/// A for us, node represents a renderable item in an HTML document structure
/// it can be a text node, an element node, a comment node, a raw node or a fragment node.
/// The fragment node is a special node that allows you to group multiple nodes together
/// without having to wrap them in a parent element.
/// Attributes in our case are not a separate node, but rather a property of an element node.
/// While we could have attributes as nodes themselves, we don't really need them to be at least
/// for the moment.
///
/// For asynchronous nodes (e.g. Task&gt;Node&lt;, or Async&lt;Node&gt;) we'll implement cancellation
/// semantics at this level, the ValueTask that areused to wrap these applications should
/// check for cancellation before trying to await the value, in case that cancellation has been requested:
/// - Nodes should return empty nodes e.g. Fragment []
/// - Attributes should return empty attributes e.g. Attribute { name = String.Empty; value = String.Empty }
///
/// For IAsyncEnumerable&gt;Node&lt; we'll implement cancellation semantics at the rendering level.
[<AutoOpen>]
type NodeDsl =

  static member inline h(cssSelector: string) =
    Element(Parsers.selector cssSelector)

  static member inline h
    (
      cssSelector: string,
      [<ParamArray>] textNodes: string array
    ) =

    let items = LinkedList<Node>()

    for node in textNodes do
      items.AddLast(LinkedListNode(Text node))

    Element(Parsers.selector cssSelector) <+ Fragment(items)

  static member inline h(cssSelector: string, child: Node Task) =
    let child =
      AsyncNode(
        cancellableValueTask {
          let! token = CancellableValueTask.getCancellationToken()

          if token.IsCancellationRequested then
            return Fragment(LinkedList())
          else
            let! child = child
            return child
        }
      )

    Element(Parsers.selector cssSelector) <+ child

  static member inline h(cssSelector: string, child: Node Async) =
    let child =
      AsyncNode(
        cancellableValueTask {
          let! token = CancellableValueTask.getCancellationToken()

          if token.IsCancellationRequested then
            return Fragment(LinkedList())
          else
            let! child = child
            return child
        }
      )

    Element(Parsers.selector cssSelector) <+ child

  static member inline h(cssSelector: string, children: Node seq) =
    Element(Parsers.selector cssSelector) <+ Fragment(LinkedList(children))

  static member inline h
    (
      cssSelector: string,
      [<ParamArray>] children: Node array
    ) =
    Element(Parsers.selector cssSelector) <+ Fragment(LinkedList(children))

  static member inline h
    (
      cssSelector: string,
      children: IAsyncEnumerable<Node>
    ) =
    Element(Parsers.selector cssSelector) <+ AsyncSeqNode children

  static member inline h(element: Node Task) =
    AsyncNode(
      cancellableValueTask {
        let! token = CancellableValueTask.getCancellationToken()

        if token.IsCancellationRequested then
          return Fragment(LinkedList())
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
            return Fragment(LinkedList())
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
            return Fragment(LinkedList())
          else
            let! element = element
            return element
        }
      )

    element <+ Fragment(LinkedList(children))

  static member inline h(element: Node Async) =
    AsyncNode(
      cancellableValueTask {
        let! token = CancellableValueTask.getCancellationToken()

        if token.IsCancellationRequested then
          return Fragment(LinkedList())
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
            return Fragment(LinkedList())
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
            return Fragment(LinkedList())
          else
            let! element = element
            return element
        }
      )

    node <+ Fragment(LinkedList(children))

  static member inline h(element: Node, children: IAsyncEnumerable<Node>) =
    element <+ AsyncSeqNode children

  static member inline h(element: Node Task, children: IAsyncEnumerable<Node>) =
    let node =
      AsyncNode(
        cancellableValueTask {
          let! token = CancellableValueTask.getCancellationToken()

          if token.IsCancellationRequested then
            return Fragment(LinkedList())
          else
            let! element = element
            return element
        }
      )

    node <+ AsyncSeqNode children

  static member inline h
    (
      element: Node Async,
      children: IAsyncEnumerable<Node>
    ) =
    let node =
      AsyncNode(
        cancellableValueTask {
          let! token = CancellableValueTask.getCancellationToken()

          if token.IsCancellationRequested then
            return Fragment(LinkedList())
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

  static member inline fragment(nodes: Node seq) = Fragment(LinkedList(nodes))

  static member inline fragment([<ParamArray>] nodes: Node array) =
    Fragment(LinkedList(nodes))

  static member inline fragment(nodes: Node seq Task) =
    AsyncNode(
      cancellableValueTask {
        let! token = CancellableValueTask.getCancellationToken()

        if token.IsCancellationRequested then
          return Fragment(LinkedList())
        else
          let! nodes = nodes
          return Fragment(LinkedList(nodes))
      }
    )

  static member inline fragment(nodes: Node seq Async) =
    AsyncNode(
      cancellableValueTask {
        let! token = CancellableValueTask.getCancellationToken()

        if token.IsCancellationRequested then
          return Fragment(LinkedList())
        else
          let! nodes = nodes
          return Fragment(LinkedList(nodes))
      }
    )

  static member inline fragment(nodes: IAsyncEnumerable<Node>) =
    AsyncSeqNode nodes

  static member inline empty = Fragment(LinkedList())

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
