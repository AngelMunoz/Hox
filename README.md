# Hox

> Check the documentation at [hox's website'](https://hox.tunaxor.me) for more details.

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

