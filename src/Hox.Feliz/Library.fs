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

    member inline _.fragment(nodes: Node seq) = Fragment(nodes |> List.ofSeq)

    member inline _.fragment(nodes: Node IAsyncEnumerable) = AsyncSeqNode nodes

    member inline _.sArticle(styles: Node, content: Node list) =
      ScopableElements.article(styles, content |> Array.ofList)

    member inline _.sAside(styles: Node, content: Node list) =
      ScopableElements.aside(styles, content |> Array.ofList)

    member inline _.sBlockquote(styles: Node, content: Node list) =
      ScopableElements.blockquote(styles, content |> Array.ofList)

    member inline _.sBody(styles: Node, content: Node list) =
      ScopableElements.body(styles, content |> Array.ofList)

    member inline _.sDiv(styles: Node, content: Node list) =
      ScopableElements.div(styles, content |> Array.ofList)

    member inline _.sFooter(styles: Node, content: Node list) =
      ScopableElements.footer(styles, content |> Array.ofList)

    member inline _.sH1(styles: Node, content: Node list) =
      ScopableElements.h1(styles, content |> Array.ofList)

    member inline _.sH2(styles: Node, content: Node list) =
      ScopableElements.h2(styles, content |> Array.ofList)

    member inline _.sH3(styles: Node, content: Node list) =
      ScopableElements.h3(styles, content |> Array.ofList)

    member inline _.sH4(styles: Node, content: Node list) =
      ScopableElements.h4(styles, content |> Array.ofList)

    member inline _.sH5(styles: Node, content: Node list) =
      ScopableElements.h5(styles, content |> Array.ofList)

    member inline _.sH6(styles: Node, content: Node list) =
      ScopableElements.h6(styles, content |> Array.ofList)

    member inline _.sHeader(styles: Node, content: Node list) =
      ScopableElements.header(styles, content |> Array.ofList)

    member inline _.sMain(styles: Node, content: Node list) =
      ScopableElements.main(styles, content |> Array.ofList)

    member inline _.sNav(styles: Node, content: Node list) =
      ScopableElements.nav(styles, content |> Array.ofList)

    member inline _.sP(styles: Node, content: Node list) =
      ScopableElements.p(styles, content |> Array.ofList)

    member inline _.sSection(styles: Node, content: Node list) =
      ScopableElements.section(styles, content |> Array.ofList)

    member inline _.sSpan(styles: Node, content: Node list) =
      ScopableElements.span(styles, content |> Array.ofList)


  let H =
    HtmlEngine((fun tag nodes -> h(tag, nodes)), Text, (fun () -> Fragment []))

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
