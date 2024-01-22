# Hox

Hox is an extensible HTML rendering library, it provides a set of functions to work with **Nodes** and compose them together as well as a couple of async rendering functions that support cancellation.

The core features are:

- A single core `Node` type that drives the whole rendering mechanism.
- Side-by-side Asynchronous Nodes, you can add sync or async nodes into sync/async parents/sibling nodes.
- Cancellable sync/async Rendering process that leverages ValueTasks.
- Render to a single string or `IAsyncEnumerable<string>`.
- A simplistic core DSL based on css selector parsing to generate nodes.

A couple of opt-in extra supported features are:

- Declarative Shadow DOM support for built-in html elements (e.g. div, article, nav, etc.)
- Templating functions for Declarative Shadow DOM custom elements.
- C# and VB.NET compatibility out of the box for the core constructs.
- Feliz-like API thanks to Feliz.Engine for an F# flavored style.

The core bits are somewhat low level building blocks to enable these kinds of features and possibly more in the future.
Feel free to chime in if you have a use case that could be solved by this library!

## HTML DSL

I'm experimenting also with a different DSL to generate these nodes, while the "core" is DSL agnostic as it is not fully typesafe (e.g. it doesn't check that nodes are added to the correct parent/place) a default one sure helps with testing.

Example:

Current Iteration:

Reduced the core node builders to basically provide the core types

- `h` - Generates `Node.Element` or adds `Node` to an existing `Node`
- `text` - Generates `Node.Text`
- `raw` - Generates `Node.Raw`
- `comment` - Generates `Node.Comment`
- `fragment` - Generates `Node.Fragment`

The element parser now allows multiline strings so you can split attributes and classes when they are too large to fit in one single line string

```fsharp
type Layout =
  static member inline Default(content: Node, ?head: Node, ?scripts: Node) =
    let head = defaultArg head (Fragment [])
    let scripts = defaultArg scripts (Fragment [])

    h(
      "html[lang=en].sl-theme-light",
      h(
        "head",
        Styles.App,
        el "meta[charset=utf-8]",
        el "meta[name=viewport][content=width=device-width, initial-scale=1.0]",
        h("title", text "Htmelo"),
        h(@"link[rel=stylesheet]
                [href=https://cdn.jsdelivr.net/npm/@shoelace-style/shoelace@2.12.0/cdn/themes/light.css]
                [media=(prefers-color-scheme:light)]"),
        h(@"link[rel=stylesheet]
                [href=https://cdn.jsdelivr.net/npm/@shoelace-style/shoelace@2.12.0/cdn/themes/dark.css]
                [media=(prefers-color-scheme:dark)]
                [onload=document.documentElement.classList.add('sl-theme-dark');]"),
        Styles.Lists,
        head
      ),
      h(
        "body",
        content,
        h("script[type=module]
                 [src=https://cdn.jsdelivr.net/npm/@shoelace-style/shoelace@2.12.0/cdn/shoelace-autoloader.js]"),
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
    h("li",
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


let inline streamed (ctx: HttpContext) (factory: IHttpClientFactory) = taskUnit {
  let http = factory.CreateClient()
  let todos = renderTodos http

  return!
    ctx.streamView(
      Layout.Default(
        h(
          "main",
          h("h1", text "Hello World!"),
          h("ul.todo-list", todos),
          h(
            "p",
            text
              "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna\naliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat."
          )
        )
          .attr("style", css "padding: 1em; display: flex; flex-direction: column")
      )
    )
}
```

Previous Iteration:

```fsharp
type Layout =
  static member inline Default(content: Node, ?head: Node, ?scripts: Node) =
    let head = defaultArg head (Fragment [])
    let scripts = defaultArg scripts (Fragment [])

    el(
      "html[lang=en].sl-theme-light",
      el(
        "head",
        Styles.App,
        el "meta[charset=utf-8]",
        el "meta[name=viewport][content=width=device-width, initial-scale=1.0]",
        el("title", text "Htmelo"),
        el("link[rel=stylesheet]")
          .href(
            "https://cdn.jsdelivr.net/npm/@shoelace-style/shoelace@2.12.0/cdn/themes/light.css"
          )
          .media("(prefers-color-scheme:light)"),
        el("link[rel=stylesheet]")
          .href(
            "https://cdn.jsdelivr.net/npm/@shoelace-style/shoelace@2.12.0/cdn/themes/dark.css"
          )
          .media("(prefers-color-scheme:dark)")
          .custom(
            "onload",
            "document.documentElement.classList.add('sl-theme-dark');"
          ),
        Styles.Lists,
        head
      ),
      el(
        "body",
        content,
        el("script[type=module]")
          .src(
            "https://cdn.jsdelivr.net/npm/@shoelace-style/shoelace@2.12.0/cdn/shoelace-autoloader.js"
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
    el("li")
      .child(
        el(
          $"sl-details[summary={todo.title}]",
          el("p", text $"Todo Id: %i{todo.id}"),
          el(
            "p",
            text
              $"Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna\naliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat."
          )
        )
      )
}


let inline streamed (ctx: HttpContext) (factory: IHttpClientFactory) = taskUnit {
  let http = factory.CreateClient()
  let todos = renderTodos http

  return!
    ctx.streamView(
      Layout.Default(
        el(
          "main",
          el("h1", text "Hello World!"),
          el("ul.todo-list", todos),
          el(
            "p",
            text
              "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna\naliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat."
          )
        )
          .style(css "padding: 1em; display: flex; flex-direction: column")
      )
    )
}
```

Previous Iteration:

```fsharp
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

let asyncRender (ctx: HttpContext) (factory: IHttpClientFactory) = task {
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

let streamRender (ctx: HttpContext) (factory: IHttpClientFactory) = taskUnit {
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

```
