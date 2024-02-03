namespace Hox.Core

open IcedTasks
open System.Collections.Generic
open System.Collections.Immutable


/// This type is used to represent HTML attributes e.g `class="foo"`
[<Struct>]
type HAttribute = { name: string; value: string }

/// Attributes can be synchronous or asynchronous we use this type to make the distinction
/// between them and to be able to handle them properly when we're going to render the attribute
/// itself.
[<Struct; NoComparison; NoEquality>]
type AttributeNode =
  | Attribute of attribute: HAttribute
  | AsyncAttribute of asyncAttribute: HAttribute CancellableValueTask

/// A node is a single element of the DOM tree, and it is one of the basic bulding blocks of this library
/// It can be a single element, a text node, a raw node, a comment node, a fragment node, an async node or an async sequence node
/// Having async nodes allows us to place async operations in the middle of the rendering process side by side with synchronous operations
/// this provides great developer experience as they don't have to worry about co-locating async operations in the right place.
[<NoComparison; NoEquality>]
type Node =
  | Element of element: Element
  | Text of text: string
  | Raw of raw: string
  | Comment of comment: string
  | Fragment of nodes: Node LinkedList
  /// Async nodes require a cancellation token to be passed to them,
  /// As we'd like to be able to cancel the rendering process in case
  /// the user decides to cancel the operation.
  | AsyncNode of node: Node CancellableValueTask
  | AsyncSeqNode of nodes: Node IAsyncEnumerable

/// An element is a single HTML element e.g `<div></div>`
/// It has a tag, a seq of attributes and a seq of children
/// Since the children are nodes, any node can be added to this seq
/// however that may lead to invalid HTML, so it's up to the user to make sure
/// that the HTML is valid.
and [<NoComparison; NoEquality>] Element = {
  tag: string
  attributes: AttributeNode LinkedList
  children: Node LinkedList
}
