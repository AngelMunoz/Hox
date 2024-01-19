namespace Htmelo

open System.Threading.Tasks
open IcedTasks
open System.Collections.Generic

[<Struct>]
type Attribute = { name: string; value: string }

[<Struct>]
type AttributeNode =
  | Attribute of attribute: Attribute
  | AsyncAttribute of asyncAttribute: Attribute CancellableValueTask

type Node =
  | Element of element: Element
  | Text of text: string
  | Raw of raw: string
  | Comment of comment: string
  | Fragment of nodes: Node list
  | AsyncNode of node: Node CancellableValueTask
  | AsyncSeqNode of nodes: Node IAsyncEnumerable

and Element = {
  tag: string
  attributes: AttributeNode list
  children: Node list
}
