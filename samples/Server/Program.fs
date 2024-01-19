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
open Server

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
        Styles.App
        El "meta[charset=utf-8]"
        El "meta[name=viewport][content=width=device-width, initial-scale=1.0]"
        El "title" |> Text "Hello World!"
        El
          "link[rel=stylesheet][media=(prefers-color-scheme:light)][href=https://cdn.jsdelivr.net/npm/@shoelace-style/shoelace@2.12.0/cdn/themes/light.css]"
        El
          "link[rel=stylesheet][media=(prefers-color-scheme:dark)][onload=document.documentElement.classList.add('sl-theme-dark');][href=https://cdn.jsdelivr.net/npm/@shoelace-style/shoelace@2.12.0/cdn/themes/dark.css]"
        Styles.Lists
        head
      ]
      El "body"
      |> Children [
        content
        El
          "script[type=module][src=https://cdn.jsdelivr.net/npm/@shoelace-style/shoelace@2.12.0/cdn/shoelace-autoloader.js]"
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
    El "li"
    |> Children [
      El $"sl-details[summary={todo.title}]"
      |> Children [
        El "p" |> Text($"Todo Id: %i{todo.id}")
        El "p"
        |> Text(
          $"Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna\naliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat."
        )
      ]
    ]
}

let inline index (ctx: HttpContext) (factory: IHttpClientFactory) = task {
  let http = factory.CreateClient()
  let todos = renderTodos http

  return!
    ctx.renderView (
      Layout.Default(
        El "main"
        |> Attr.style (
          css "padding: 1em; display: flex; flex-direction: column"
        )
        |> Children [
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
        El "main"
        |> Attr.style (
          css "padding: 1em; display: flex; flex-direction: column"
        )
        |> Children [
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
