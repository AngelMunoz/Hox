While the [Hox DSL](guides/general-usage.html#hox-dsl) is the favored flavor for Hox, we understand that developers prefer the type safety of the F# language. For this reason, we've added a Feliz-flavored API to Hox.

```fsharp
open Hox.Feliz

let view =
    H.html [
        H.head [
            H.title "Hello, world!"
        ]
        H.body [
            H.h1 "Hello, world!"
            |> Attr.set(A.id "first-h1")
            |> Attr.set(A.className "title")
        ]
    ]
```

For the most part, the Feliz DSL works as you would expectit excepto from attributes. In Hox, attributes are part of the element's children unlike the traditional Feliz DSL, where they are another Node in the tree. To make this work, we've added a `Attr` module which contains helper functions to deal with attributes.

### Main differences with Hox DSL

First and foremost, the syntax is obviously different. However there are some other differences that are worth noting.

- Raw Nodes are not supported, because of how Feliz.Engine creates nodes. If you need to create a raw node, you can use the `raw` function within the `Hox` namespace.
- Attributes are not part of the element's children, but rather a separate list of attributes. This is because of how Feliz.Engine handles attributes.
- Nodes that support Declarative Shadow DOM are not in a separate type, they were included within the same HTML Engine type.

### Fragments

Working with sequences of nodes is relatively simpler with the Feliz API, given that every function requires already a list of nodes, so you can use the `yield!` keyword to add a sequence of nodes, however the `fragment` function is still available if you need it.

```fsharp
open Hox.Feliz

let computeItems() =
    // compute items
    [ H.li [ H.text "Item 1" ]
      H.li [ H.text "Item 2" ]
      H.li [ H.text "Item 3" ] ]

let node =
    H.div [
        H.h1 "Hello, world!"
        H.ul [
            yield! computeItems()
            // or
            H.fragment (computeItems())
        ]
    ]
```

Fragments are still useful for `IAsyncEnumerable<Node>` though, so you don't need to resort to the Hox DSL.

```fsharp
// using FSharp.Control.TaskSeq
open FSharp.Control
open Hox.Feliz

H.ul [
    H.fragment(taskSeq {
        for i in 1 .. 10 do
            do! Task.Delay 5
            H.li [ H.text (sprintf "Item %d" i) ]
    })
]
```

### Asynchronous nodes

Just because you're using the Feliz API doesn't mean you can't use asynchronous nodes. The `async` and `task` are available for you.

```fsharp
open Hox.Feliz

H.div [
    H.h1 "Hello, world!"
    H.async(async {
        do! Async.Sleep 50
        return H.p [ H.text "This is an async node" ]
    })
    H.task(task {
        do! Task.Delay 50
        return H.p [ H.text "This is a task node" ]
    })
]
```
