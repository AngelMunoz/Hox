There are two core rendering mechanisms included in Hox, both are stack based.

## AsString

The `renderNode` function in the `Builder` module is a cancellable value task that utilizes a work-item stack for iterative rendering.

```fsharp
let renderNode node (ct: CancellationToken) = valueTask {
    let sb = StringBuilder(4096)
    let stack = Stack<WorkItem>(64)
    stack.Push(RenderNode node)

    while stack.Count > 0 do
      ct.ThrowIfCancellationRequested()
      let work = stack.Pop()

      match work with
      | RenderNode n ->
        // rendering logic
        match n with
        // ...
      | CloseElement tag -> // ...
      // ...
    return sb.ToString()
}
```

It keeps a stack of `WorkItem`s (such as `RenderNode`, `CloseElement`, `RenderChildrenPrefetched`) to manage the rendering process iteratively.

Our rendering algorithm not really optimized for speed or anything but contributions are welcome.

Considerations when you use this approach is that you will have to keep the string in memory before sending it to the client. While I don't expect you to send a 1GB HTML file, it's still something to keep in mind.

This approach works best for small to medium sized documents but leaves HTML streaming (a browser feature) out of the window that improves certain metrics like time to first byte and time to first meaningful paint. If that is not a concern, meaning that you may not be in a web server environment, this approach is perfectly fine.

## `IAsyncEnumerable<string>`

The second approach is to use an `IAsyncEnumerable<string>` sequence and it is meant to be used where sending the HTML content to a consumer is the most important thing. This can be hooked up easily with `System.IO.Stream` objects and the like.

The `Chunked.renderNode` function also uses a stack-based iterative approach similar to the string builder method, but yields strings asynchronously:

```fsharp
  let renderNode(node: Node, ct: CancellationToken) : IAsyncEnumerable<string> = taskSeq {
    let stack = Stack<WorkItem>(64)
    stack.Push(RenderNode node)

    while stack.Count > 0 do
      ct.ThrowIfCancellationRequested()
      let work = stack.Pop()

      match work with
      | RenderNode n ->
        // rendering logic
        match n with
        // ...
      | CloseElement tag -> // ...
      // ...
  }
```

This implementation handles attribute encoding and tag rendering in fine-grained chunks.

You can see the full implementation of this in the `Rendering.Chunked` module.

## Render class

The render class is the public face of the rendering inner mechanisms described above. It's a thin wrapper around the `renderNode` function and it looks as follows:

```fsharp

[<Class>]
type Render =
  [<CompiledName "Start">]
  static member start:
    node: Node * [<OptionalAttribute>] ?cancellationToken: CancellationToken ->
      string IAsyncEnumerable

  [<CompiledName "ToStream">]
  static member toStream:
    node: Node *
    stream: System.IO.Stream *
    [<OptionalAttribute; Struct>] ?chunkSize: int *
    [<OptionalAttribute>] ?cancellationToken: CancellationToken ->
      Task

  [<CompiledName "AsString">]
  static member asString:
    node: Node * [<OptionalAttribute>] ?cancellationToken: CancellationToken ->
      string ValueTask

  static member asStringAsync: node: Node -> string Async
```

From these methods, `ToStream` is worth sharing as it is kind of the built-in streaming mechanism. It looks as follows:

```fsharp
  [<CompiledName "ToStream">]
  static member toStream
    (
      node: Node,
      stream: IO.Stream,
      [<OptionalAttribute; Struct>] ?chunkSize: int,
      [<OptionalAttribute>] ?cancellationToken:
        CancellationToken
    ) =
    taskUnit {
      let ct =
        defaultArg cancellationToken CancellationToken.None

      use writer =
        new StreamWriter(stream, Text.Encoding.UTF8, 4092, leaveOpen = true)

      let chunkSize =
        match chunkSize with
        | ValueSome n when n > 0 -> ValueSome n
        | _ -> ValueNone
      
      // ... implementation handling buffering based on chunkSize ...
    }
```

Our `toStream` implementation allows for optional buffering.

> **_Note_**: The `chunkSize` parameter allows you to specify a buffer size in characters. If provided, the renderer will accumulate chunks into a buffer of this size before writing to the stream and flushing. This can significantly improve performance by reducing the number of I/O operations and flushing overhead.

The main reason I share this is to show you that the building blocks are there and you can take it really far if that's what you need.
