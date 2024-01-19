open System
open System.Runtime.CompilerServices
open System.Collections.Generic
open System.IO
open System.Threading.Tasks

open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Http

open IcedTasks
open FSharp.Control

open Htmelo
open Htmelo.DSL
open Htmelo.Rendering
open System.Net.Http
open System.Text.Json

[<AutoOpen>]
module Extensions =
  open System.Text

  [<Extension>]
  type HttpContextExtensions =

    [<Extension>]
    static member streamView(ctx: HttpContext, view: Node) = taskUnit {
      use writer =
        new StreamWriter(ctx.Response.Body, System.Text.Encoding.UTF8)

      ctx.Response.ContentType <- "text/html; charset=utf-8"

      for chunk in Chunked.render view ctx.RequestAborted do
        do! writer.WriteAsync(chunk)
        do! writer.FlushAsync()
    }

    [<Extension>]
    static member renderView(ctx: HttpContext, view: Node) = task {
      let! result = Builder.Task.render view ctx.RequestAborted
      return Results.Text(result, "text/html", Encoding.UTF8)
    }

type Layout =
  static member inline Default(content: Node, ?head: Node, ?scripts: Node) =
    let head = defaultArg head (Fragment [])
    let scripts = defaultArg scripts (Fragment [])

    El "html[lang=en]"
    |> Children [
      El "head"
      |> Children [
        El "meta[charset=utf-8]"
        El "meta[name=viewport][content=width=device-width, initial-scale=1.0]"
        El "title" |> Text "Hello World!"
        El
          "link[rel=stylesheet][href=https://cdnjs.cloudflare.com/ajax/libs/bulma/0.9.4/css/bulma.min.css]"
        head
      ]
      El "body"
      |> Children [
        content
        El "script[src=https://unpkg.com/htmx.org@1.9.10/dist/htmx.min.js]"
        scripts
      ]
    ]

let renderTodos (http: HttpClient) = taskSeq {
  use! todos = http.GetStreamAsync("https://jsonplaceholder.typicode.com/todos")

  let! todos =
    JsonSerializer.DeserializeAsync<{|
      userId: int
      id: int
      title: string
      completed: bool
    |} list>(
      todos
    )

  for todo in todos do
    El "li.content"
    |> Children [
      El "h2.title" |> Text todo.title
      El "p" |> Text($"Todo Id: %i{todo.id}")
    ]
}

let inline index (ctx: HttpContext) (factory: IHttpClientFactory) = task {
  let http = factory.CreateClient()
  let todos = renderTodos http

  return!
    ctx.renderView (
      Layout.Default(
        Fragment [
          El "h1" |> Text "Hello World!"
          El "ul.todo-list" |> Children [ AwaitChildren(todos) ]
        ]
      )
    )
}

let inline streamed (ctx: HttpContext) (factory: IHttpClientFactory) = taskUnit {
  let http = factory.CreateClient()
  let todos = renderTodos http

  return!
    ctx.streamView (
      Layout.Default(
        Fragment [
          El "h1" |> Text "Hello World!"
          El "ul.todo-list" |> Children [ AwaitChildren(todos) ]
        ]
      )
    )
}

[<EntryPoint>]
let main args =
  let builder = WebApplication.CreateBuilder(args)
  builder.Services.AddHttpClient() |> ignore
  let app = builder.Build()

  app.MapGet("/", Func<HttpContext, IHttpClientFactory, Task<IResult>>(index))
  |> ignore

  app.MapGet("/streamed", Func<HttpContext, IHttpClientFactory, Task>(streamed))
  |> ignore

  app.Run()

  0 // Exit code
