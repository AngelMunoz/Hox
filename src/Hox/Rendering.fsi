module Hox.Rendering

open System
open System.Collections.Generic
open System.Threading.Tasks


open FSharp.Control

open Hox.Core

/// This module contains functions that are used to render a node to a string
/// It is backed by a StringBuilder.
module Builder =

  /// Render the node in a ValueTask&lt;string&gt; operation
  [<RequireQualifiedAccess>]
  module ValueTask =
    /// <summary>
    /// Renders the node to a string using value tasks, for C# users this
    /// is the recommended way to render a node.
    /// </summary>
    /// <param name="node">The node to render</param>
    /// <param name="token">The cancellation token to use</param>
    /// <returns>A value task that represents the rendering operation</returns>
    /// <remarks>
    /// If there are no async nodes in the tree, then the operation will be synchronous
    /// otherwise it will be asynchronous given how ValueTasks work.
    /// </remarks>
    val render:
      node: Node -> token: Threading.CancellationToken -> ValueTask<string>

  /// Render the node in an Async&lt;string&gt; operation
  [<RequireQualifiedAccess>]
  module Async =
    /// <summary>
    /// Renders the node to a string using F#'s async workflows
    /// </summary>
    /// <param name="node">The node to render</param>
    /// <returns>An async workflow that represents the rendering operation</returns>
    /// <remarks>
    /// Cancellation token is not required as async workflows support cancellation
    /// out of the box.
    /// </remarks>
    val inline render: node: Node -> Async<string>

  /// Render the node in a Task&lt;string&gt; operation
  [<RequireQualifiedAccess>]
  module Task =
    /// <summary>
    /// Renders the node to a string using a Task, for F# users this is the recommended way
    /// to render a node if they're working extensively with tasks already, otherwise
    /// see the Async.render function.
    /// </summary>
    /// <param name="node">The node to render</param>
    /// <param name="token">The cancellation token to use</param>
    /// <returns>A task that represents the rendering operation</returns>
    val inline render:
      node: Node -> token: Threading.CancellationToken -> Task<string>

/// This module contains functions that are used to render a node to a sequence of strings
/// As soon as a chunk is ready it is yielded to the caller.
[<RequireQualifiedAccess>]
module Chunked =
  /// <summary>
  /// Renders the node to a sequence of strings, this is useful if you want to stream the
  /// output to the client as soon as it's ready.
  /// </summary>
  /// <param name="node">The node to render</param>
  /// <param name="token">The cancellation token to use</param>
  /// <returns>A sequence of strings that represents the rendering operation</returns>
  /// <remarks>
  /// This is the preferred way to render a node for any use case.
  /// C# users can use await foreach to consume the sequence.
  /// F# users can use `FSharp.Control.TaskSeq` to consume the sequence.
  /// </remarks>
  ///
  val render:
    node: Node -> token: Threading.CancellationToken -> IAsyncEnumerable<string>
