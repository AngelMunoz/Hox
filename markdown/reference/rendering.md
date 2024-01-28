There are two core rendering mechanisms included in Hox, both are stack based.

> **_Note_**: Given our use of Cancellable Value Tasks, we are limited in the recursive department. This require compiler support which is being tracked in this issue https://github.com/fsharp/fslang-suggestions/issues/1006

## AsString

The `renderNode` function in the `Rendering` module is itself a cancellable value task and it looks as follows:

```fsharp
let renderNode(node: Node) = cancellableValueTask {
  let! token = CancellableValueTask.getCancellationToken()
    let sb = StringBuilder()
    let stack = Stack<struct (Node * bool)>()
    stack.Push(node, false)

    while stack.Count > 0 do
      token.ThrowIfCancellationRequested()
      let struct (node, closing) = stack.Pop()
      // rendering logic
      match node with
      // ...
}
```

It keeps a stack of nodes to render and a boolean flag indicating whether the node is closing it's tag on the next pass.

Our rendering algorithm not really optimized for speed or anything but contributions are welcome.

Considerations when you use this approach is that you will have to keep the string in memory before sending it to the client. While I don't expect you to send a 1GB HTML file, it's still something to keep in mind.

This approach works best for small to medium sized documents but leaves HTML streaming (a browser feature) out of the window that improves certain metrics like time to first byte and time to first meaningful paint. If that is not a concern, meaning that you may not be in a web server environment, this approach is perfectly fine.

## `IAsyncEnumerable<Node>`

The second approach is to use an `IAsyncEnumerable<Node>` sequence and it is meant to be used where sending the HTML content to a consumer is the most important thing. This can be hooked up easily with `System.IO.Stream` objects and the like.

The `renderNode` function is slightly different in this case because here we support partial recursive ness:

```fsharp
let rec renderNode(
    stack: Stack<struct(Node * bool * int)>,
    cancellationToken: CancellationToken
  ) =
  taskSeq {
    while stack.Count > 0 do
      cancellationToken.ThrowIfCancellationRequested()
      let struct (node, closing, depth) = stack.Pop()
      // rendering logic
      match node with
      // ...
  }
```

In this case we are passing the same stack around and we're keeping tabs of the node depth. This is very important as it will allows us to decide to render between a recursive approach or a buffered approach.

Due the limitations noted in a quote above, we can't do a full recursive approach. When we're around 235 nodes deep we will switch to a buffered approach. This is a bit of a magic number as I haven't run true science on this but it seems to work well enough.

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
      [<Runtime.InteropServices.OptionalAttribute>] ?cancellationToken:
        CancellationToken
    ) =
    taskUnit {
      let cancellationToken =
        defaultArg cancellationToken CancellationToken.None

      let operation =
        Chunked.renderNode(
          Stack([ struct (node, false, 0) ]),
          cancellationToken
        )
        |> TaskSeq.map(System.Text.Encoding.UTF8.GetBytes)

      for chunk in operation do
        do! stream.WriteAsync(ReadOnlyMemory(chunk), cancellationToken)
        do! stream.FlushAsync()
    }
```

Our `toStream` is not optimized in any way, it simply takes a stream and as soon as we have information written to it, we flush it. There might be better approaches with `BufferedStream` play with buffer sizes. I'd love the performance folks to chime in on this and help us improve it where possible.

The main reason I share this is to show you that the building blocks are there and you can take it really far if that's what you need.
