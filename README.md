# Htmelo (Final name TBD)

> **_Note_**: Please note that I'm using the term `async` here to denote any asynchronous operation like `task`, F#'s `async`, `valueTask`

This is an experimental library to provide an engine (like Feliz.Engine) for async html rendering in the server.

There are two main ways to render html, the first one is by asynchronously fill a string builder and just get a string.

This allows to have "Async" nodes within your HTML, e.g. having an async function that returns a "Node" which will be rendered once it is available.

The other way and the more interesting one, is to produce an `IAsyncEnumerator<string>` that contains the whole HTML string, this can be useful specially on web servers as this can be used to stream html to the client via `Transfer-Encoding: chunked` header, some Javascript libraries can use these to provide out of order rendering in the client.

This could enable some F# web servers to have compatible responses to some javascript libraries in the future which after this would be the next stop in the experimentation process.

## HTML DSL

I'm experimenting also with a different DSL to generate these nodes, while the "core" is DSL agnostic as it is not fully typesafe (e.g. it doesn't check that nodes are added to the correct parent/place) a default one sure helps with testing.

Example:

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
        El "script[src=https://unpkg.com/htmx.org@1]"
      ]
      El "body"
      |> Children [
        content
        scripts
      ]
    ]

let streamed (ctx: HttpContext) = taskUnit {
  return!
    ctx.streamView (
      Layout.Default(
        El "main"
        |> Children [
            El "h1" |> Text "Hello World!"
            // Produce nodes from async work
            TaskEl(task {
                do! Task.Delay(200)
                return El "div" |> Text "Encoded text"
            })
            // Produce nodes from async sequences
            AwaitChildren(taskSeq {
                for i in 1..10 do
                    do! Task.Delay(200)
                    yield El "p" |> Text($"Paragraph %d{i}")
            })
        ]
      )
    )
}
```
