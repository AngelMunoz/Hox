open System.Threading
open System.Threading.Tasks

open FSharp.Control

open Hox.Rendering
open Hox.Feliz

let content =
  H.html [
    H.head [ H.custom("title", [ H.text "Feliz Engine" ]) ]
    H.body [
      H.h1 [ H.text "Feliz Engine" ]
      H.p [ H.text "This is a sample of Feliz Engine" ]
      |> Attr.set(A.className "paragraph")
      |> Attr.set(A.id "paragraph")
      |> Attr.set(A.style "color: red;")
      H.fragment(
        taskSeq {
          for i in 1..10 do
            H.p [ H.text $"This is paragraph {i}" ]
            do! Task.Delay(50)
        }
      )
      H.p [
        H.text "This is a "
        H.a [ H.text "link" ] |> Attr.set(A.href "https://google.com")
        H.text " to nowhere"
        H.async(
          async {
            do! Async.Sleep(200)
            return H.text " (async)"
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
