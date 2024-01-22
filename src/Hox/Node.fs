namespace Hox.Core

open IcedTasks
open System.Collections.Generic

[<Struct>]
type HAttribute = { name: string; value: string }

[<Struct; NoComparison; NoEquality>]
type AttributeNode =
  | Attribute of attribute: HAttribute
  | AsyncAttribute of asyncAttribute: HAttribute CancellableValueTask

[<NoComparison; NoEquality>]
type Node =
  | Element of element: Element
  | Text of text: string
  | Raw of raw: string
  | Comment of comment: string
  | Fragment of nodes: Node list
  | AsyncNode of node: Node CancellableValueTask
  | AsyncSeqNode of nodes: Node IAsyncEnumerable

and [<NoComparison; NoEquality>] Element = {
  tag: string
  attributes: AttributeNode list
  children: Node list
}
