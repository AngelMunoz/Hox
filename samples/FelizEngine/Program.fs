open System.Threading
open System.Threading.Tasks

open FSharp.Control

open Hox
open Hox.Core
open Hox.Rendering
open Hox.Feliz
open System

let rec createNestedStructure depth : Node =
  H.fragment(
    taskSeq {
      if depth = 0 then
        H.text "Item 0"
      else
        do! Task.Delay(1)
        let soup = ResizeArray()

        for i in 0..depth do
          H.div [ H.p [ H.text $"I'm doing a thing {i}" ] ]

          if i < 255 then
            soup.Add(byte i)

        let a = System.Text.Encoding.UTF32.GetString([| yield! soup |])
        H.p [ H.text $"Then again... {a}" ]

        H.ul [
          H.li [ H.text $"Item {depth}"; createNestedStructure(depth - 1) ]
        ]
    }
  )

// funnily enough creting a value for this particular taskSeq doesn't overflow the stack on my computer
let bigNestedStructure() = createNestedStructure 100

// but it did when I was rendering it inside the node tree below
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
        [
          for i in 1..10 do
            H.p [ H.text $"This is Fragment paragraph {i}" ]
        // bigNestedStructure()
        ]
      )
      // bigNestedStructure()
      H.fragment(
        taskSeq {
          for i in 1..10 do
            H.p [ H.text $"This is an async paragraph {i}" ]

            H.fragment(
              taskSeq {
                for j in 1..i do
                  H.p [ H.text $"This is an async inner paragraph {i + j}" ]
              }
            )
        }
      )
      H.p [
        H.text "This is a "
        H.a [ H.text "link" ] |> Attr.set(A.href "https://google.com")
        H.text " to nowhere"
        H.async(
          async {
            do! Async.Sleep(10)
            return H.fragment([ H.text " (async)" ])
          }
        )
      ]
    ]
  ]

open System.IO


task {

  // let! content = Render.asString(content, CancellationToken.None)
  // printf $"%s{content}"
  printf "Starting rendering..."

  use file =
    File.Open(
      "./test.html",
      FileMode.OpenOrCreate,
      FileAccess.ReadWrite,
      FileShare.Read
    )

  do! Render.toStream(content, file, cancellationToken = CancellationToken.None)
  printf "Done rendering..."
// for chunk in Render.start(content, CancellationToken.None) do
//   printf $"%s{chunk}"
}
|> Async.AwaitTask
|> Async.RunSynchronously
