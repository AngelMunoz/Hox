open System
open System.Net.Http
open System.Text.Json
open System.Threading.Tasks

open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.DependencyInjection

open IcedTasks
open FSharp.Control

open Hox
open Hox.Core
open Hox.Rendering

open Server


module Results =

  let Stream(view: Node) =
    { new IResult with
        member _.ExecuteAsync(ctx: HttpContext) : Task = taskUnit {
          ctx.Response.ContentType <- "text/html; charset=utf-8"

          do! ctx.Response.StartAsync(ctx.RequestAborted)

          do!
            Render.toStream(
              view,
              ctx.Response.Body,
              cancellationToken = ctx.RequestAborted
            )

          do! ctx.Response.CompleteAsync()
        }
    }

  let Text(view: Node) =
    { new IResult with
        member _.ExecuteAsync(ctx: HttpContext) : Task = taskUnit {
          ctx.Response.ContentType <- "text/html; charset=utf-8"

          do! ctx.Response.StartAsync(ctx.RequestAborted)

          let! content =
            Render.asString(view, cancellationToken = ctx.RequestAborted)

          do! ctx.Response.WriteAsync(content)

          return! ctx.Response.CompleteAsync()
        }
    }

type Layout =
  static member inline Default(content: Node, ?head: Node, ?scripts: Node) =
    let head = defaultArg head (empty)
    let scripts = defaultArg scripts (empty)

    h(
      "html[lang=en].sl-theme-light",
      h(
        "head",
        Styles.App,
        h "meta[charset=utf-8]",
        h "meta[name=viewport][content=width=device-width, initial-scale=1.0]",
        h("title", text "Hox"),
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

let inline asString(factory: IHttpClientFactory) = task {
  let http = factory.CreateClient()
  let todos = renderTodos http

  let content =
    Layout.Default(
      h(
        "main",
        h("h1", text "Hello World!"),
        h("ul.todo-list", todos),
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

  return Results.Text content
}

let inline streamed(factory: IHttpClientFactory) = task {
  let http = factory.CreateClient()
  let todos = renderTodos http

  let content =
    Layout.Default(
      h(
        "main",
        h("h1", text "Hello World!"),
        h("ul.todo-list", todos),
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

  return Results.Stream content
}

[<EntryPoint>]
let main args =
  let builder = WebApplication.CreateBuilder(args)
  builder.Services.AddHttpClient() |> ignore
  let app = builder.Build()

  app.MapGet("/", Func<IHttpClientFactory, Task<IResult>>(streamed)) |> ignore

  app.MapGet("/string", Func<IHttpClientFactory, Task<IResult>>(asString))
  |> ignore

  app.Run()

  0 // Exit code
