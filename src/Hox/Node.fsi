namespace Hox.Core

open System
open System.Collections.Generic
open IcedTasks

/// A double-ended queue with efficient O(1) amortized access to both ends.
/// Uses a circular buffer internally for cache-friendly iteration.
[<Sealed; NoComparison; NoEquality>]
type Deque<'T> =
  new: ?initialCapacity: int -> Deque<'T>
  member Count: int
  member IsEmpty: bool
  member AddLast: item: 'T -> unit
  member AddFirst: item: 'T -> unit
  member RemoveFirst: unit -> 'T
  member RemoveLast: unit -> 'T
  member PeekFirst: unit -> 'T
  member PeekLast: unit -> 'T
  member Item: index: int -> 'T with get
  interface IEnumerable<'T>

/// This type is used to represent HTML attributes e.g `class="foo"`
[<Struct>]
type HAttribute = { name: string; value: string }

/// Attributes can be synchronous or asynchronous
[<Struct; NoComparison; NoEquality>]
type AttributeNode =
  | Attribute of attribute: HAttribute
  | AsyncAttribute of asyncAttribute: HAttribute CancellableValueTask

/// A node is a single element of the DOM tree
[<NoComparison; NoEquality>]
type Node =
  | Element of element: Element
  | Text of text: string
  | Raw of raw: string
  | Comment of comment: string
  | Fragment of nodes: Deque<Node>
  | AsyncNode of node: Node CancellableValueTask
  | AsyncSeqNode of nodes: Node IAsyncEnumerable
  | PreRendered of html: string

and [<NoComparison; NoEquality>] Element = {
  tag: string
  attributes: Deque<AttributeNode>
  children: Deque<Node>
}
