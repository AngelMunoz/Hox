open System
open System.Runtime.CompilerServices
open System.Threading.Tasks

open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.DependencyInjection

open IcedTasks
open FSharp.Control

open Htmelo
open Htmelo.Core
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
    static member inline streamView(ctx: HttpContext, view: Node) = vTaskUnit {
      ctx.Response.ContentType <- "text/html; charset=utf-8"

      let writer = ctx.Response.BodyWriter

      for chunk in Chunked.render view ctx.RequestAborted do
        let chunk = ReadOnlyMemory(Encoding.UTF8.GetBytes(chunk))
        do! writer.WriteAsync(chunk, ctx.RequestAborted) |> ValueTask.ignore
        do! writer.FlushAsync() |> ValueTask.ignore
    }

    [<Extension>]
    static member inline renderView(ctx: HttpContext, view: Node) = task {
      let! result = Builder.Task.render view ctx.RequestAborted
      return Results.Text(result, "text/html", Encoding.UTF8)
    }

type Layout =
  static member inline Default(content: Node, ?head: Node, ?scripts: Node) =
    let head = defaultArg head (Fragment [])
    let scripts = defaultArg scripts (Fragment [])

    h(
      "html[lang=en].sl-theme-light",
      h(
        "head",
        Styles.App,
        h "meta[charset=utf-8]",
        h "meta[name=viewport][content=width=device-width, initial-scale=1.0]",
        h("title", text "Htmelo"),
        h("link[rel=stylesheet]")
          .attr(
            "href",
            "https://cdn.jsdelivr.net/npm/@shoelace-style/shoelace@2.12.0/cdn/themes/light.css"
          )
          .attr("media", "(prefers-color-scheme:light)"),
        h("link[rel=stylesheet]")
          .attr(
            "href",
            "https://cdn.jsdelivr.net/npm/@shoelace-style/shoelace@2.12.0/cdn/themes/dark.css"
          )
          .attr("media", "(prefers-color-scheme:dark)")
          .attr(
            "onload",
            "document.documentElement.classList.add('sl-theme-dark');"
          ),
        Styles.Lists,
        head
      ),
      h(
        "body",
        content,
        h(
          @"script[type=module]
                   [src=https://cdn.jsdelivr.net/npm/@shoelace-style/shoelace@2.12.0/cdn/shoelace-autoloader.js]"
        ),
        scripts
      )
    )


let renderTodos(http: HttpClient) = taskSeq {
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
    h(
      "li",
      h(
        $"sl-details[summary={todo.title}]",
        h("p", text $"Todo Id: %i{todo.id}"),
        h(
          "p",
          text
            $"Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna\naliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat."
        )
      )
    )
}

let inline index (ctx: HttpContext) (factory: IHttpClientFactory) = task {
  // let http = factory.CreateClient()
  // let todos = renderTodos http

  return!
    ctx.renderView(
      Layout.Default(
        h(
          "main",
          h("h1", text "Hello World!"),
          // el("ul.todo-list", todos),
          Scoped.card(
            Scoped.cardHeader(h("h2", text "Card Header")),
            Scoped.cardContent(h("p", text "Card Content")),
            Scoped.cardFooter(h("p", text "Card Footer"))
          ),
          h(
            "p",
            text
              "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna\naliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat."
          )
        )
          .attr(
            "style",
            css "padding: 1em; display: flex; flex-direction: column"
          )
      )
    )
}

let inline streamed (ctx: HttpContext) (factory: IHttpClientFactory) = taskUnit {
  // let http = factory.CreateClient()
  // let todos = renderTodos http

  return!
    ctx.streamView(
      Layout.Default(
        h(
          "main",
          h("h1", text "Hello World!"),
          // el("ul.todo-list", todos),
          Scoped.card(
            Scoped.cardFooter(h("p", text "Card Footer")),
            Scoped.cardContent(h("p", text "Card Content")),
            Scoped.cardHeader(h("h1", text "Card Header"))
          ),
          h(
            "p",
            text
              "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna\naliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat."
          )
        )
          .attr(
            "style",
            css "padding: 1em; display: flex; flex-direction: column"
          )
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
