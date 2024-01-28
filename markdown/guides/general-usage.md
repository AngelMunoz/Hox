> While not required, it is recommended to check out [IcedTasks], [FSharp.Control.TaskSeq], to have some extra Computation Expressions that allow using tasks/async/valueTask and IAsyncEnumerable in simple ways.

The library by itself is quite simple and can be reduced to a single type with helper functions.

- **Render.start** - Produces an `IAsyncEnumerable<string>` from a `Node`
- **Render.toStream** - Takes a stream and uses the same mechanism as `Render.start` to write to it, it is merely a convenience function.
- **Render.asString** - Renders a `Node` to a string asynchronously
- **Render.asStringAsync** - Renders a `Node` using F#'s `Async` type

### A few examples

```fsharp
open Hox.Rendering

task {
    // assuming token is a CancellationToken
    // assuming Layout.Default() produces a `Node`
    let node = Layout.Default()
    let! result = Render.asString(node, token)
    printfn $"Produced:\n\n{result}"
}
```

Rendering to a string is the most basic use case, useful for small node trees.

In case of larger trees or when you want to stream the result to a file or a network stream, you can use `Render.toStream`:

```fsharp
open Hox.Rendering

task {
    // assuming token is a CancellationToken
    // assuming Layout.Default() produces a `Node`
    let node = Layout.Default()
    use file = File.OpenWrite("output.html")
    do! Render.toStream(node, file, token)
}
```

If you want more control over what to do with each chunk, you can use `Render.start`:

```fsharp
// using FSharp.Control.TaskSeq
open FSharp.Control

open Hox.Rendering

task {
    // assuming token is a CancellationToken
    // assuming Layout.Default() produces a `Node`
    let node = Layout.Default()
    for chunk in Render.start(nod, token) do
        printfn $"Produced:\n\n{chunk}"
}
```

## How to create nodes

To create nodes manually you have to use the `Hox.Core` module, which contains the `Node` type

```fsharp
open Hox.Core

let textNode = Node.Text "Hello World"
let element = Node.Element { tag = "div"; attributes = []; children = [] }

let fragment = Node.Fragment [ textNode; element ]

let asyncNode = Node.AsyncNode(cancellableValueTask {
    do! Async.Sleep 1000
    return Node.Text "Hello World"
})
```

However composing these nodes and appending them to each other can be quite tedious, we offer two DSLs out of the box a new alternative simplistic DSL or the more familiar flavor Feliz.

## Hox DSL

> If you're looking for the Feliz Flavor, check out the [Feliz DSL](guides/using-feliz.html)

```fsharp
open Hox

let node = h "div"
let text = text "Hello World"
let fragment = fragment [ node; text ]
let asyncNode = h("div", async { // or task {
    do! Async.Sleep 1000
    return text "Hello World"
})
```

As you can see nodes are simpler to create this way, and composing them is also easier.
each node accepts a variable number of children, or a sequence of nodes, the `h` overloads are thought to be as simple to use as possible.

```fsharp
open Hox
let nodeWithChildren = h("div", h("span", "Hello World"))
let children = [ h("span", "Hello World"); h("span", "Hello World") ]
let nodeWithChildren2 = h("div", children)
let nodeWithChildren3 = h("div", fragment children)
```

Depending on how you're obtaining the child nodes, you can choose the overload that suits you best.

### Attributes

Within the Hox DSL you can also specify attributes for element nodes, the attributes are specified as a css selector, speciall attributes such as class and id are also supported.

```fsharp
open Hox
let node = h("div#main", h("span", "Hello World"))
let node1 = h("div.main.is-primary", h("span", "Hello World"))
let node2 = h("link[rel='stylesheet'][href=style.css]")
let combined = h("div#main.is-primary[data-name=main]", h("span", "Hello World"))
```

The syntax is as follows:

`<element-name><#id><.class><[attribute=value]>...`

Where:

- `element-name` is the name of the element, element names should follow the HTML spec to define tag names.
- `#id` is specified with a `#` followed by the value of said id, if more than one id attribute is present only the first one will be picked up.
- `.class` is specified with a `.` followed by the value of said class.
- `[attribute=value]` is specified with a `[` followed by the name of the attribute, followed by a required `=` even for no-value atributes (like `checked`), after te `=` symbol anything will be taken as the string until a `]` is found, even break line characters.

You can specify attributes in any order or with spaces and break lines in between the attribute declarations, example:

- `div#main.is-primary`
- `div.is-primary#main`

Those examples above are equivalent and will produce the following structure

```html
<div id="main" class="is-primary"></div>
```

Attributes will always render in the following order "id", "class" and the rest in the order they were specified, as an example:

```fsharp
open Hox
let node =
    h("div#main
          .is-primary.is-medium
          [data-name=main]
          [data-sample=anything here, even spaces! or <symbols-&>]",
      h("span", "Hello World")
    )
```

Will produce the following structure

```html
<div
  id="main"
  class="is-primary is-medium"
  data-name="main"
  data-sample="anything here, even spaces! or &lt;symbols-&amp;&gt;"
></div>
```

Attributes are also available via the `attr` function, which can be used to add attributes to any node, this is useful when you want to add attributes to a node that is being returned from a function.

```fsharp
open Hox

let getAttributeValue() = async {
    do! Async.Sleep 1000
    return "value"
}

h("div", h("span", "Hello World"))
    .attr("data-marker", getAttributeValue())
    .attr("data-marker2", "value")
    // will be rendered as data-marker3=""
    .attr("data-marker3", true)
    // will not be rendered
    .attr("data-marker4", false)
```

### Nodes and Attribute encoding

By default every node and attribute is encoded to prevent XSS attacks, this means that any special character will be encoded to its HTML entity equivalent, this is done by default.

For cases where you want to render raw HTML, then you should use `raw`

```fsharp
let rawNode = h("div", raw "<span data-random='my attribute'>Hello World</span>")
```

Raw nodes will not be encoded, and will be rendered as is, but BE CAREFUL, and please escape any HTML that you store in your database or comes from user input, otherwise you will be vulnerable to XSS attacks.

For every other node where text is accepted, it will be encoded, this means that you can safely use `h` and `text` to render user input.

### Fragments

These are special utility nodes that can be used to group nodes without a parent element, a good example would be rendering `li` elements in a function that returns a `Node` to be later used inside an `ul` or `ol` element.

```fsharp
open Hox

let computeItems() =
    // do something and return the computed items
    [
        h("li", "Item 1")
        h("li", "Item 2")
        h("li", "Item 3")
    ]

let node = h("ul", h("li", "content"), fragment items)
// or if the sequence is the only parameter
let node = h("ul", items)
```

### Asynchronous nodes

One of the "big deals" is the ability to use asynchronous nodes, just like you would use synchronous nodes, this bridges a gap between the two worlds and allows you to use the same mental model for both.

Also, asynchronous nodes are cold (or also called lazy), this means that they will not be executed until they are requested to render.

```fsharp
open Hox

let asyncNode = h("div", async {
    do! Async.Sleep 1000
    return h("span", "Hello async World")
})

let syncNode = h("div", h("span", "Hello sync World"))

h("article",
    asyncNode,
    syncNode
)
```

A perhaps more useful example would be fetching the user profile from a database while structuring the whole page.

```fsharp
open Hox

let fetchProfile userId = task {
    let! profile = db.FetchProfileAsync userId
    return h("section",
        h("h1", profile.Name),
        h("p", profile.Description)
    )
}

let mainView() =
    h("article",
      h("header", h("h1", "My Profile")),
      h("aside", tableofContents()),
      h("main", fetchProfile 1)
      h("footer", h("p", "This is my footer"))
    )
```

It is also possible to add children to asynchronous items, for example when you need to set certain well known items in a list but you need to fill the rest asynchronously.

```fsharp
open Hox

let fetchItems() = task {
    let! items = db.FetchItemsAsync()
    return h("ul",
        h("li", "Item 1"),
        h("li", "Item 2"),
        h("li", "Item 3")
    )
}

let fetchItem6() = task {
    let! item = db.FetchItem6Async()
    return h("li", item.Name)
}

h("ul",
    h(fetchItems(),
      h("li", "Item 4"),
      h("li", "Item 5"),
      fetchItem6()
    )
)
```

In the case above, `ul` will have a total of 6 items, 3 of them will be rendered from `fetchItems` asynchronously, the items 4 and 5 will be rendered synchronously and the sixth item will also be fetched asynchronously.

### Asynchronous sequences

Asynchronous sequences are also supported, this can be useful when you're interoperating with external libraries that return `IAsyncEnumerable<T>` or you want to produce an `IAyncEnumerable<Node>` for your own purposes.

```fsharp
open Hox
open Hox.Rendering

let getitems(service, secondService) = taskSeq {
    let! items = service.GetItemsAsync()
    for item in items do
        let! item = secondService.withExtendedInfo item
        h("tr",
            h("td", item.Id),
            h("td", item.Name),
            h("td", item.Description),
            h("td", item.Price),
            h("td", item.Quantity),
            h("td", item.Total)
        )
}

let mainView(container) =
    h("table",
        h("thead",
            h("tr",
                h("th", "Id"),
                h("th", "Name"),
                h("th", "Description"),
                h("th", "Price"),
                h("th", "Quantity"),
                h("th", "Total")
            )
        ),
        h("tbody", getitems(container.GetService(), container.GetService()))
    )

// For example, in a web server you could do something like this
let view ctx = task {
    let view = Layout.Default("Hox", mainView(ctx.RequestServices))
    do! Render.toStream(view, ctx.Response.Body, ctx.RequestAborted)
}
```

All of the concepts above can be combined to produce complex views, without having to worry about when to put the asynchronous work where in the rendered tree.

[IcedTasks]: https://github.com/TheAngryByrd/IcedTasks
[FSharp.Control.TaskSeq]: https://github.com/fsprojects/FSharp.Control.TaskSeq
