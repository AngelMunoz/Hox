Hox can also be used in C# and VB.NET projects. Part of the side effect of looking for a simple and and easy DSL is that Hox can be used in any .NET language.

## C# Usage

Since Hox is written in F#, much of the API looks like a static class to C# code. This means you have to add `using static` references to the Hox namespace.

The following example shows how to use Hox in a C# project.

```csharp
using static Hox.NodeDsl;
using static Hox.NodeExtensions;
using static Hox.Rendering;

var node =
    h("html",
        h("head",
            h("title", "Hello World")
        ),
        h("body",
            h("h1", "Hello World")
                .attr("id", "first-h1")
                .attr("class", "title is-primary")
                .attr("style", "color: red;"),
            h("p.subtitle", "This is a paragraph.")
        )
    )
        .attr("lang", "en");

var html = await Render.AsString(node);
// do something with the html
```

## VB.NET Usage

Similarly to C#, VB.NET can also use Hox. The following example shows how to use Hox in a VB.NET project.

```vb
Imports Hox.NodeDsl
Imports Hox.NodeExtensions

Module Views
    Dim Main =
        h("html",
            h("head",
                h("title", "Hello World")
            ),
            h("body",
                h("h1", "Hello World")
                    .attr("id", "first-h1")
                    .attr("class", "title is-primary")
                    .attr("style", "color: red;"),
                h("p.subtitle", "This is a paragraph.")
            )
        )
            .attr("lang", "en")
End Module

Sub Async Sub DoWork()
    Dim html = Await Render.AsString(Views.Main)
    ' do something with the html
End Sub
```

In general, both languages can use Hox up to the same extent as F# except by the Feliz API which uses specific F# types so, you can following the rest of the guides and reference will be useful for you as well.
