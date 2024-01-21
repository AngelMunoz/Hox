open Feliz

open System.Collections.Generic
open System.Threading.Tasks

open FSharp.Control
open IcedTasks

open Htmelo
open Htmelo.Core
open Htmelo.Rendering
open System.Threading


let Html =
  HtmlEngine((fun tag nodes -> h(tag, nodes)), Text, (fun () -> Fragment []))

let Svg =
  SvgEngine((fun tag nodes -> h(tag, nodes)), Text, (fun () -> Fragment []))

let Attr =
  AttrEngine(
    (fun k v -> Attribute { name = k; value = v }),
    (fun k v ->
      Attribute {
        name = if not v then "" else k
        value = ""
      })
  )

type HtmlEngine<'Node> with

  member inline _.Async(node: Node Async) =
    let tsk = cancellableValueTask {
      let! node = node
      return node
    }

    AsyncNode tsk

  member inline _.Task(node: Node Task) =
    let tsk = cancellableValueTask {
      let! node = node
      return node
    }

    AsyncNode tsk

  member inline _.fragment(nodes: Node seq) = Fragment(nodes |> List.ofSeq)

  member inline _.fragment(nodes: Node IAsyncEnumerable) = AsyncSeqNode nodes

let inline add (attr: AttributeNode) (node: Node) =
  NodeOps.addAttribute(node, attr)

let content =
  Html.html [
    Html.head [ Html.custom("title", [ Html.text "Feliz Engine" ]) ]
    Html.body [
      Html.h1 [ Html.text "Feliz Engine" ]
      Html.p [ Html.text "This is a sample of Feliz Engine" ]
      |> add(Attr.className "paragraph")
      |> add(Attr.id "paragraph")
      |> add(Attr.style "color: red;")
      Html.fragment(
        taskSeq {
          for i in 1..10 do
            Html.p [ Html.text $"This is paragraph {i}" ]
            do! Task.Delay(50)
        }
      )
      Html.p [
        Html.text "This is a "
        Html.a [ Html.text "link" ] |> add(Attr.href "https://google.com")
        Html.text " to nowhere"
        Html.Async(
          async {
            do! Async.Sleep(200)
            return Html.text " (async)"
          }
        )
      ]
    ]
  ]

task {
  for chunk in Chunked.render content CancellationToken.None do
    printf $"%s{chunk}"
}
|> Async.AwaitTask
|> Async.RunSynchronously
