namespace Hox.Feliz

open System.Collections.Generic
open System.Threading.Tasks

open FSharp.Control
open IcedTasks

open Feliz

open Hox
open Hox.Core

[<AutoOpen>]
module Engine =

  type HtmlEngine<'Node> with

    member inline _.async(node: Node Async) =
      let tsk = cancellableValueTask {
        let! node = node
        return node
      }

      AsyncNode tsk

    member inline _.task(node: Node Task) =
      let tsk = cancellableValueTask {
        let! node = node
        return node
      }

      AsyncNode tsk

    member inline _.fragment(nodes: Node seq) = Fragment(LinkedList(nodes))

    member inline _.fragment(nodes: Node IAsyncEnumerable) = AsyncSeqNode nodes

    member inline _.sArticle(content: Node list) =
      ScopableElements.article(content |> List.toArray)

    member inline _.sAside(content: Node list) =
      ScopableElements.aside(content |> List.toArray)

    member inline _.sBlockquote(content: Node list) =
      ScopableElements.blockquote(content |> List.toArray)

    member inline _.sBody(content: Node list) =
      ScopableElements.body(content |> List.toArray)

    member inline _.sDiv(content: Node list) =
      ScopableElements.div(content |> List.toArray)

    member inline _.sFooter(content: Node list) =
      ScopableElements.footer(content |> List.toArray)

    member inline _.sH1(content: Node list) =
      ScopableElements.h1(content |> List.toArray)

    member inline _.sH2(content: Node list) =
      ScopableElements.h2(content |> List.toArray)

    member inline _.sH3(content: Node list) =
      ScopableElements.h3(content |> List.toArray)

    member inline _.sH4(content: Node list) =
      ScopableElements.h4(content |> List.toArray)

    member inline _.sH5(content: Node list) =
      ScopableElements.h5(content |> List.toArray)

    member inline _.sH6(content: Node list) =
      ScopableElements.h6(content |> List.toArray)

    member inline _.sHeader(content: Node list) =
      ScopableElements.header(content |> List.toArray)

    member inline _.sMain(content: Node list) =
      ScopableElements.main(content |> List.toArray)

    member inline _.sNav(content: Node list) =
      ScopableElements.nav(content |> List.toArray)

    member inline _.sP(content: Node list) =
      ScopableElements.p(content |> List.toArray)

    member inline _.sSection(content: Node list) =
      ScopableElements.section(content |> List.toArray)

    member inline _.sSpan(content: Node list) =
      ScopableElements.span(content |> List.toArray)


  let H =
    HtmlEngine(
      (fun tag nodes -> h(tag, nodes)),
      Text,
      (fun () -> Fragment(LinkedList()))
    )

  let A =
    AttrEngine(
      (fun k v -> { name = k; value = v }),
      (fun k v -> {
        name = if not v then "" else k
        value = ""
      })
    )

  module Attr =
    let set attr node =
      NodeOps.addAttribute(node, Attribute attr)

    let setTask (attr: HAttribute Task) node =
      NodeOps.addAttribute(
        node,
        AsyncAttribute(
          cancellableValueTask {
            let! attr = attr
            return attr
          }
        )
      )

    let setAsync (attr: HAttribute Async) node =
      NodeOps.addAttribute(
        node,
        AsyncAttribute(
          cancellableValueTask {
            let! attr = attr
            return attr
          }
        )
      )
