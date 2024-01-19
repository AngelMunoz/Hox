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

    el (
      "html[lang=en].sl-theme-light",
      el (
        "head",
        Styles.App,
        el "meta[charset=utf-8]",
        el "meta[name=viewport][content=width=device-width, initial-scale=1.0]",
        el ("title", text "Htmelo"),
        el(
          "link[rel=stylesheet][href=https://cdn.jsdelivr.net/npm/@shoelace-style/shoelace@2.12.0/cdn/themes/light.css]"
        )
          .media ("(prefers-color-scheme:light)"),
        el(
          "link[rel=stylesheet][onload=document.documentElement.classList.add('sl-theme-dark');]"
        )
          .href(
            "https://cdn.jsdelivr.net/npm/@shoelace-style/shoelace@2.12.0/cdn/themes/dark.css"
          )
          .media ("(prefers-color-scheme:dark)"),
        Styles.Lists,
        head
      ),
      el (
        "body",
        content,
        el("script[type=module]")
          .src (
            "https://cdn.jsdelivr.net/npm/@shoelace-style/shoelace@2.12.0/cdn/shoelace-autoloader.js"
          ),
        scripts
      )
    )


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
    el("li")
      .child (
        el (
          $"sl-details[summary={todo.title}]",
          el ("p", text $"Todo Id: %i{todo.id}"),
          el (
            "p",
            text
              $"Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna\naliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat."
          )
        )
      )
}

let inline index (ctx: HttpContext) (factory: IHttpClientFactory) = task {
  let http = factory.CreateClient()
  let todos = renderTodos http

  return!
    ctx.renderView (
      Layout.Default(
        el("main")
          .style(css "padding: 1em; display: flex; flex-direction: column")
          .children (el ("h1", text "Hello World!"), el ("ul.todo-list", todos))
      )
    )
}

let inline streamed (ctx: HttpContext) (factory: IHttpClientFactory) = taskUnit {
  let http = factory.CreateClient()
  let todos = renderTodos http

  return!
    ctx.streamView (
      Layout.Default(
        el("main")
          .style(css "padding: 1em; display: flex; flex-direction: column")
          .children (el ("h1", text "Hello World!"), el ("ul.todo-list", todos))
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
