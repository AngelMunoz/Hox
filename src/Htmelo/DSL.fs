[<AutoOpen>]
module Htmelo.DSL

open System
open System.Threading.Tasks
open System.Runtime.CompilerServices

open IcedTasks
open System.Collections.Generic
open FSharp.Control


let rec addToNode(target: Node, value: Node) =
  match target, value with
  | Element target, Element value ->
    Element {
      target with
          children = target.children @ [ Element value ]
    }
  | Element target, Text value ->
    Element {
      target with
          children = target.children @ [ Text value ]
    }
  | Element target, Raw value ->
    Element {
      target with
          children = target.children @ [ Raw value ]
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
          children = target.children @ [ AsyncSeqNode value ]
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
    AsyncSeqNode(target |> TaskSeq.append(taskSeq { Element value }))
  | AsyncSeqNode target, Text value ->
    AsyncSeqNode(target |> TaskSeq.append(taskSeq { Text value }))
  | AsyncSeqNode target, Raw value ->
    AsyncSeqNode(target |> TaskSeq.append(taskSeq { Raw value }))
  | AsyncSeqNode target, AsyncNode value ->
    let tsk = cancellableValueTask {
      let! value = value
      return AsyncSeqNode(target |> TaskSeq.append(taskSeq { value }))
    }

    AsyncNode tsk
  | AsyncSeqNode target, AsyncSeqNode value ->
    AsyncSeqNode(target |> TaskSeq.append value)
  | AsyncSeqNode target, Fragment value ->
    AsyncSeqNode(target |> TaskSeq.append(taskSeq { Fragment value }))
  | Fragment target, Element value -> Fragment(target @ [ Element value ])
  | Fragment target, Text value -> Fragment(target @ [ Text value ])
  | Fragment target, Raw value -> Fragment(target @ [ Raw value ])
  | Fragment target, AsyncNode value ->
    let tsk = cancellableValueTask {
      let! value = value
      return Fragment(target @ [ value ])
    }

    AsyncNode tsk
  | Fragment target, AsyncSeqNode value ->
    AsyncSeqNode(target |> TaskSeq.ofList |> TaskSeq.append value)
  | Fragment target, Fragment value -> Fragment(target @ value)
  | Text target, Text value -> Text(target + value)
  | Text target, Raw value -> Text(target + value)
  | Text target, Element value ->
    Element {
      value with
          children = value.children @ [ Text target ]
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

type Htmelo =

  static member inline el(cssSelector: string) =
    Element(Parsers.selector cssSelector)

  static member inline el(cssSelector: string, child: Node) =
    addToNode(Element(Parsers.selector cssSelector), child)

  static member inline el(cssSelector: string, children: Node seq) =
    addToNode(
      Element(Parsers.selector cssSelector),
      Fragment(children |> Seq.toList)
    )

  static member inline el
    (
      cssSelector: string,
      [<ParamArray>] children: Node array
    ) =
    addToNode(
      Element(Parsers.selector cssSelector),
      Fragment(children |> Seq.toList)
    )

  static member inline el
    (
      cssSelector: string,
      children: IAsyncEnumerable<Node>
    ) =
    addToNode(Element(Parsers.selector cssSelector), AsyncSeqNode children)

  static member inline el(cssSelector: string, child: Node ValueTask) =
    let child =
      AsyncNode(
        cancellableValueTask {
          let! child = child
          return child
        }
      )

    addToNode(Element(Parsers.selector cssSelector), child)


  static member inline el(element: Node ValueTask) =
    AsyncNode(
      cancellableValueTask {
        let! element = element
        return element
      }
    )

  static member inline el(element: Node ValueTask, child: Node) =
    let element =
      AsyncNode(
        cancellableValueTask {
          let! element = element
          return element
        }
      )

    addToNode(element, child)

  static member inline el(element: Node ValueTask, children: Node seq) =
    let element =
      AsyncNode(
        cancellableValueTask {
          let! element = element
          return element
        }
      )

    addToNode(element, Fragment(children |> Seq.toList))

  static member inline el(element: Node Task) =
    Htmelo.el(
      valueTask {
        let! element = element
        return element
      }
    )

  static member inline el(element: Node Task, child: Node) =
    let element =
      AsyncNode(
        cancellableValueTask {
          let! element = element
          return element
        }
      )

    addToNode(element, child)

  static member inline el(element: Node Task, children: Node seq) =
    let element =
      AsyncNode(
        cancellableValueTask {
          let! element = element
          return element
        }
      )

    addToNode(element, Fragment(children |> Seq.toList))

  static member inline el(element: Node Async) =
    Htmelo.el(
      valueTask {
        let! element = element
        return element
      }
    )

  static member inline el(element: Node Async, child: Node) =
    let element =
      AsyncNode(
        cancellableValueTask {
          let! element = element
          return element
        }
      )

    addToNode(element, child)

  static member inline el(element: Node Async, children: Node seq) =
    let element =
      AsyncNode(
        cancellableValueTask {
          let! element = element
          return element
        }
      )

    addToNode(element, Fragment(children |> Seq.toList))

  static member inline el(element: Node, children: IAsyncEnumerable<Node>) =
    addToNode(element, AsyncSeqNode children)

  static member inline el
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

    addToNode(element, AsyncSeqNode children)

  static member inline el
    (
      element: Node Task,
      children: IAsyncEnumerable<Node>
    ) =
    let element =
      AsyncNode(
        cancellableValueTask {
          let! element = element
          return element
        }
      )

    addToNode(element, AsyncSeqNode children)

  static member inline el
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

    addToNode(element, AsyncSeqNode children)

  static member inline text(text: string) = Text text

  static member inline raw(raw: string) = Raw raw

  static member inline fragment(nodes: seq<Node>) =
    Fragment(nodes |> Seq.toList)

  static member inline fragment(nodes: IAsyncEnumerable<Node>) =
    AsyncSeqNode nodes

[<Extension>]
type NodeExtensions =

  [<Extension>]
  static member inline children(node: Node, children: Node seq) =
    addToNode(node, Htmelo.fragment children)

  [<Extension>]
  static member inline children(node: Node, children: IAsyncEnumerable<Node>) =
    addToNode(node, AsyncSeqNode children)

  [<Extension>]
  static member inline children
    (
      node: Node,
      [<ParamArrayAttribute>] nodes: Node array
    ) =
    addToNode(node, fragment nodes)

  [<Extension>]
  static member inline child(node: Node, child: Node) = addToNode(node, child)

  [<Extension>]
  static member inline child(node: Node, child: Node ValueTask) =
    let child =
      AsyncNode(
        cancellableValueTask {
          let! child = child
          return child
        }
      )

    addToNode(node, child)

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
  static member inline class'(node: Node, className: string) =
    addAttribute(
      node,
      AttributeNode.Attribute { name = "class"; value = className }
    )

  [<Extension>]
  static member inline class'(node: Node, classes: #seq<string>) =
    let classes = classes |> Seq.toList |> String.concat " "

    addAttribute(
      node,
      AttributeNode.Attribute { name = "class"; value = classes }
    )

  [<Extension>]
  static member inline class'(node: Node, className: string ValueTask) =
    let attr = cancellableValueTask {
      let! className = className
      return { name = "class"; value = className }
    }

    addAttribute(node, AttributeNode.AsyncAttribute attr)

  [<Extension>]
  static member inline class'(node: Node, className: string Task) =
    node.class'(
      valueTask {
        let! className = className
        return className
      }
    )

  [<Extension>]
  static member inline class'(node: Node, className: string Async) =
    node.class'(
      valueTask {
        let! className = className
        return className
      }
    )

  [<Extension>]
  static member inline id(node: Node, id: string) =
    addAttribute(node, AttributeNode.Attribute { name = "id"; value = id })

  [<Extension>]
  static member inline id(node: Node, id: string ValueTask) =
    let attr = cancellableValueTask {
      let! id = id
      return { name = "id"; value = id }
    }

    addAttribute(node, AttributeNode.AsyncAttribute attr)

  [<Extension>]
  static member inline id(node: Node, id: string Task) =
    node.id(
      valueTask {
        let! id = id
        return id
      }
    )

  [<Extension>]
  static member inline id(node: Node, id: string Async) =
    node.id(
      valueTask {
        let! id = id
        return id
      }
    )

  [<Extension>]
  static member inline media(node: Node, media: string) =
    addAttribute(
      node,
      AttributeNode.Attribute { name = "media"; value = media }
    )

  [<Extension>]
  static member inline media(node: Node, media: string ValueTask) =
    let attr = cancellableValueTask {
      let! media = media
      return { name = "media"; value = media }
    }

    addAttribute(node, AttributeNode.AsyncAttribute attr)

  [<Extension>]
  static member inline media(node: Node, media: string Task) =
    node.media(
      valueTask {
        let! media = media
        return media
      }
    )

  [<Extension>]
  static member inline media(node: Node, media: string Async) =
    node.media(
      valueTask {
        let! media = media
        return media
      }
    )

  [<Extension>]
  static member inline src(node: Node, src: string) =
    addAttribute(node, AttributeNode.Attribute { name = "src"; value = src })

  [<Extension>]
  static member inline src(node: Node, src: string ValueTask) =
    let attr = cancellableValueTask {
      let! src = src
      return { name = "src"; value = src }
    }

    addAttribute(node, AttributeNode.AsyncAttribute attr)

  [<Extension>]
  static member inline src(node: Node, src: string Task) =
    node.src(
      valueTask {
        let! src = src
        return src
      }
    )

  [<Extension>]
  static member inline src(node: Node, src: string Async) =
    node.src(
      valueTask {
        let! src = src
        return src
      }
    )

  [<Extension>]
  static member inline href(node: Node, href: string) =
    addAttribute(node, AttributeNode.Attribute { name = "href"; value = href })

  [<Extension>]
  static member inline href(node: Node, href: string ValueTask) =
    let attr = cancellableValueTask {
      let! href = href
      return { name = "href"; value = href }
    }

    addAttribute(node, AttributeNode.AsyncAttribute attr)

  [<Extension>]
  static member inline href(node: Node, href: string Task) =
    node.href(
      valueTask {
        let! href = href
        return href
      }
    )

  [<Extension>]
  static member inline href(node: Node, href: string Async) =
    node.href(
      valueTask {
        let! href = href
        return href
      }
    )

  [<Extension>]
  static member inline style(node: Node, style: string) =
    addAttribute(
      node,
      AttributeNode.Attribute { name = "style"; value = style }
    )

  [<Extension>]
  static member inline style(node: Node, style: string ValueTask) =
    let attr = cancellableValueTask {
      let! style = style
      return { name = "style"; value = style }
    }

    addAttribute(node, AttributeNode.AsyncAttribute attr)

  [<Extension>]
  static member inline style(node: Node, style: string Task) =
    node.style(
      valueTask {
        let! style = style
        return style
      }
    )

  [<Extension>]
  static member inline style(node: Node, style: string Async) =
    node.style(
      valueTask {
        let! style = style
        return style
      }
    )

  [<Extension>]
  static member inline type'(node: Node, type': string) =
    addAttribute(node, AttributeNode.Attribute { name = "type"; value = type' })

  [<Extension>]
  static member inline type'(node: Node, type': string ValueTask) =
    let attr = cancellableValueTask {
      let! type' = type'
      return { name = "type"; value = type' }
    }

    addAttribute(node, AttributeNode.AsyncAttribute attr)

  [<Extension>]
  static member inline type'(node: Node, type': string Task) =
    node.type'(
      valueTask {
        let! type' = type'
        return type'
      }
    )

  [<Extension>]
  static member inline type'(node: Node, type': string Async) =
    node.type'(
      valueTask {
        let! type' = type'
        return type'
      }
    )

  [<Extension>]
  static member inline value(node: Node, value: string) =
    addAttribute(
      node,
      AttributeNode.Attribute { name = "value"; value = value }
    )

  [<Extension>]
  static member inline value(node: Node, value: string ValueTask) =
    let attr = cancellableValueTask {
      let! value = value
      return { name = "value"; value = value }
    }

    addAttribute(node, AttributeNode.AsyncAttribute attr)

  [<Extension>]
  static member inline value(node: Node, value: string Task) =
    node.value(
      valueTask {
        let! value = value
        return value
      }
    )

  [<Extension>]
  static member inline value(node: Node, value: string Async) =
    node.value(
      valueTask {
        let! value = value
        return value
      }
    )

  [<Extension>]
  static member inline placeholder(node: Node, placeholder: string) =
    addAttribute(
      node,
      AttributeNode.Attribute {
        name = "placeholder"
        value = placeholder
      }
    )

  [<Extension>]
  static member inline placeholder(node: Node, placeholder: string ValueTask) =
    let attr = cancellableValueTask {
      let! placeholder = placeholder

      return {
        name = "placeholder"
        value = placeholder
      }
    }

    addAttribute(node, AttributeNode.AsyncAttribute attr)

  [<Extension>]
  static member inline placeholder(node: Node, placeholder: string Task) =
    node.placeholder(
      valueTask {
        let! placeholder = placeholder
        return placeholder
      }
    )

  [<Extension>]
  static member inline placeholder(node: Node, placeholder: string Async) =
    node.placeholder(
      valueTask {
        let! placeholder = placeholder
        return placeholder
      }
    )

  [<Extension>]
  static member inline disabled(node: Node, disabled: bool) =
    addAttribute(
      node,
      AttributeNode.Attribute {
        name = "disabled"
        value = disabled.ToString()
      }
    )

  [<Extension>]
  static member inline disabled(node: Node, disabled: bool ValueTask) =
    let attr = cancellableValueTask {
      let! disabled = disabled

      return {
        name = "disabled"
        value = disabled.ToString()
      }
    }

    addAttribute(node, AttributeNode.AsyncAttribute attr)

  [<Extension>]
  static member inline disabled(node: Node, disabled: bool Task) =
    node.disabled(
      valueTask {
        let! disabled = disabled
        return disabled
      }
    )

  [<Extension>]
  static member inline disabled(node: Node, disabled: bool Async) =
    node.disabled(
      valueTask {
        let! disabled = disabled
        return disabled
      }
    )

  [<Extension>]
  static member inline checked'(node: Node, checked': bool) =
    if checked' then
      addAttribute(
        node,
        AttributeNode.Attribute { name = "checked"; value = "checked" }
      )
    else
      node

  [<Extension>]
  static member inline checked'(node: Node, checked': bool ValueTask) =
    let attr = cancellableValueTask {
      let! checked' = checked'

      return {
        name = "checked"
        value = if checked' then "checked" else ""
      }
    }

    addAttribute(node, AttributeNode.AsyncAttribute attr)

  [<Extension>]
  static member inline checked'(node: Node, checked': bool Task) =
    node.checked'(
      valueTask {
        let! checked' = checked'
        return checked'
      }
    )

  [<Extension>]
  static member inline checked'(node: Node, checked': bool Async) =
    node.checked'(
      valueTask {
        let! checked' = checked'
        return checked'
      }
    )
