module Hox.Rendering

open System.Collections.Generic
open System.Threading
open System.Threading.Tasks
open System.Runtime.InteropServices

open FSharp.Control

open Hox.Core

[<Class>]
type Render =

  /// <summary>
  /// Renders the node to a sequence of strings, this is useful if you want to stream the
  /// output to the client as soon as it's ready.
  /// </summary>
  /// <param name="node">The node to render</param>
  /// <param name="cancellationToken">The cancellation token to use</param>
  /// <returns>A sequence of strings that represents the rendering operation</returns>
  /// <remarks>
  /// This is the preferred way to render a node for any use case.
  /// C# users can use await foreach to consume the sequence.
  /// F# users can use `FSharp.Control.TaskSeq` to consume the sequence or
  /// call `GetAsyncEnumerator(token)` manually.
  /// </remarks>
  [<CompiledName "Start">]
  static member start:
    node: Node * [<OptionalAttribute>] ?cancellationToken: CancellationToken ->
      string IAsyncEnumerable

  /// <summary>
  /// Writes the node in string chunks to a stream.
  /// </summary>
  /// <param name="node"></param>
  /// <param name="stream"></param>
  /// <param name="bufferSize"></param>
  /// <param name="cancellationToken">The cancellation token required to stop the rendering process.</param>
  /// <returns>A sequence of strings that represents the rendering operation</returns>
  /// <remarks>
  /// This method uses a `BufferedStream` internally, please adjust the buffer size accordingly
  /// to your use case.
  /// </remarks>
  [<CompiledName "ToStream">]
  static member toStream:
    node: Node *
    stream: System.IO.Stream *
    [<OptionalAttribute>] ?bufferSize: int *
    [<OptionalAttribute>] ?cancellationToken: CancellationToken ->
      Task

  /// <summary>
  /// Renders a Hox node to a string.
  /// </summary>
  /// <param name="node">The node to render</param>
  /// <param name="cancellationToken">The cancellation token to use</param>
  /// <returns>A value task that represents the rendering operation</returns>
  /// <remarks>
  /// If there are no async nodes in the tree, then the operation will be synchronous
  /// otherwise it will be asynchronous given how ValueTasks work.
  /// </remarks>
  [<CompiledName "AsString">]
  static member asString:
    node: Node * [<OptionalAttribute>] ?cancellationToken: CancellationToken ->
      string ValueTask

  /// <summary>
  /// Renders the node to a string using F#'s async workflows
  /// </summary>
  /// <param name="node">The node to render</param>
  /// <returns>An async workflow that represents the rendering operation</returns>
  /// <remarks>
  /// Cancellation token is not required as async workflows support cancellation
  /// out of the box.
  /// </remarks>
  static member asStringAsync: node: Node -> string Async
