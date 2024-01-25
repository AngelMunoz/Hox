module Cancellation

open System
open System.Diagnostics
open System.Threading
open System.Threading.Tasks

open Xunit

open FSharp.Control
open IcedTasks

open Hox
open Hox.Core
open Hox.Rendering

[<Fact>]
let ``Render as string should cancel``() = taskUnit {
  use cts = new CancellationTokenSource()

  let node =
    h(
      "div",
      task {
        do! Task.Delay(1000)
        return text "Hello World"
      }
    )

  let actual = Render.asString(node, cts.Token)
  cts.CancelAfter(200)

  try
    do! actual |> ValueTask.ignore

    Assert.Fail("Operation should have been cancelled and thrown an exception")
  with :? OperationCanceledException ->
    return ()

}


[<Fact>]
let ``cancellation token can be used to cancel a task``() = taskUnit {
  use cts = new CancellationTokenSource()

  let node =
    let list0 =
      fragment(
        taskSeq {
          for i in 1..10 do
            if i = 5 then
              cts.Cancel()
            else

              Text $"Iteration: %i{i}"

            do! Task.Delay(100)
        }
      )

    let list1 =
      fragment(
        taskSeq {
          for i in 1..10 do
            if i = 5 then
              cts.Cancel()
            else

              Text $"Iteration: %i{i}"

            do! Task.Delay(100)
        }
      )

    h("div", h("ul", list0, h("ul", list1)))


  let actual = ResizeArray()

  try
    for chunk in Render.start(node, cts.Token) do
      actual.Add(chunk)
  with :? OperationCanceledException ->
    Debug.WriteLine("Operation cancelled", "Cancellation Triggered")
    return ()


  let expected = [ "<div"; ">"; "<ul"; ">" ]

  actual |> Seq.zip expected |> Seq.iter(fun (e, a) -> Assert.Equal(e, a))
}
