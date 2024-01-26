This library was born out of the Javascript ecosystem where there are these concepts called "islands" in view libraries.

These islands are basically holes in your HTML where asynchronous components can perform work while the rest of the page is rendered synchronously, this of course requires scripting capabilities in the browser, because as soon as nodes start resolving, they are added to the DOM. In the server we don't have DOM, but we can still leverage the fact that we can place asynchronous work side-by-side with synchronous work.

In server environments, it improves the so called DX (developer experience) as you don't have to coordinate between asynchronous functions to render in a single final place while also improving user experience, as the page can be streamed to the client as soon as a rendered chunk is available, instead of waiting for the whole page to be rendered. Modern browsers support this out of the box.

The rendering process is also cancellable unlike previous approaches where you'd pass a token to each coordinated function to avoid overworking before having the data to render the document in a single pass.

In Hox, every asynchronous node node is backed by a `CancellableValueTask<T>` which is just an alias for `CancellationToken -> ValueTask<T>`, this also means that every asynchronous node is aware of the main cancellation token, and when a rendering process is cancelled, rather than starting the asynchronous work, it will just return an empty node. The rest of the rendering process is stopped when processing the next node in the internal stack.

This also highlights the fact that Hox, more than a templating library is a rendering library, so you can build other kinds of templating/domain specific languages libraries on top of it, that's how we provide the Feliz API for example.

For cases where every node of the document is synchronous, the rendering process is backed by `ValueTask<T>` so synchronous work will be executed synchronously as usual.

In any case, this is a small library that hopes to push the web dev ecosystem in F# forward and spark some ideas in the community.

## Special Thanks

- [IcedTasks] - For the cancellable async work semantics added to tasks and many other async builders.
- [Feliz.Engine] - For the Feliz API which is beloved by many F# developers.
- [FSharp.Control.TaskSeq] - For allowing us to have `IAsyncEnumerable<T>` support.
- [FParsec] - For the building blocks of the CSS selector parsing.

[IcedTasks]: https://github.com/TheAngryByrd/IcedTasks
[Feliz.Engine]: https://github.com/alfonsogarciacaro/Feliz.Engine
[FSharp.Control.TaskSeq]: https://github.com/fsprojects/FSharp.Control.TaskSeq
[FParsec]: https://github.com/stephan-tolksdorf/fparsec
