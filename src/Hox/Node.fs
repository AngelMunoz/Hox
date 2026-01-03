namespace Hox.Core

open System
open System.Collections.Generic
open IcedTasks

[<Sealed; NoComparison; NoEquality>]
type Deque<'T>(?initialCapacity: int) =
  let mutable buffer: 'T[] = Array.zeroCreate(defaultArg initialCapacity 4)
  let mutable head: int = 0
  let mutable tail: int = 0
  let mutable count: int = 0

  member _.Count = count
  member _.IsEmpty = count = 0

  member private _.Grow() =
    let newCap = max 4 (buffer.Length * 2)
    let newBuf = Array.zeroCreate<'T> newCap

    if count > 0 then
      if head < tail then
        Array.Copy(buffer, head, newBuf, 0, count)
      else
        let headLen = buffer.Length - head
        Array.Copy(buffer, head, newBuf, 0, headLen)
        Array.Copy(buffer, 0, newBuf, headLen, tail)

    buffer <- newBuf
    head <- 0
    tail <- count

  member this.AddLast(item: 'T) =
    if count = buffer.Length then
      this.Grow()

    buffer.[tail] <- item
    tail <- (tail + 1) % buffer.Length
    count <- count + 1

  member this.AddFirst(item: 'T) =
    if count = buffer.Length then
      this.Grow()

    head <- (head - 1 + buffer.Length) % buffer.Length
    buffer.[head] <- item
    count <- count + 1

  member _.RemoveFirst() =
    if count = 0 then
      raise(InvalidOperationException("Deque is empty"))

    let item = buffer.[head]
    buffer.[head] <- Unchecked.defaultof<'T>
    head <- (head + 1) % buffer.Length
    count <- count - 1
    item

  member _.RemoveLast() =
    if count = 0 then
      raise(InvalidOperationException("Deque is empty"))

    tail <- (tail - 1 + buffer.Length) % buffer.Length
    let item = buffer.[tail]
    buffer.[tail] <- Unchecked.defaultof<'T>
    count <- count - 1
    item

  member _.PeekFirst() =
    if count = 0 then
      raise(InvalidOperationException("Deque is empty"))

    buffer.[head]

  member _.PeekLast() =
    if count = 0 then
      raise(InvalidOperationException("Deque is empty"))

    buffer.[(tail - 1 + buffer.Length) % buffer.Length]

  member _.Item
    with get (index: int) =
      if index < 0 || index >= count then
        raise(IndexOutOfRangeException())

      buffer.[(head + index) % buffer.Length]

  member _.GetEnumerator() =
    let mutable i = 0
    let mutable current = Unchecked.defaultof<'T>
    let bufRef = buffer
    let headRef = head
    let countRef = count

    { new IEnumerator<'T> with
        member _.Current = current
        member _.Current = box current

        member _.MoveNext() =
          if i < countRef then
            current <- bufRef.[(headRef + i) % bufRef.Length]
            i <- i + 1
            true
          else
            false

        member _.Reset() = i <- 0
        member _.Dispose() = ()
    }

  interface IEnumerable<'T> with
    member this.GetEnumerator() = this.GetEnumerator()

    member this.GetEnumerator() =
      this.GetEnumerator() :> System.Collections.IEnumerator

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
  | Fragment of nodes: Deque<Node>
  | AsyncNode of node: Node CancellableValueTask
  | AsyncSeqNode of nodes: Node IAsyncEnumerable
  | PreRendered of html: string

and [<NoComparison; NoEquality>] Element = {
  tag: string
  attributes: Deque<AttributeNode>
  children: Deque<Node>
}

module Element =
  let inline create tag = {
    tag = tag
    attributes = Deque(4)
    children = Deque(4)
  }

  let inline createWithCapacity tag attrCap childCap = {
    tag = tag
    attributes = Deque(attrCap)
    children = Deque(childCap)
  }

  let inline addChild child (el: Element) =
    el.children.AddLast(child)
    el

  let inline addAttribute attr (el: Element) =
    el.attributes.AddLast(attr)
    el

module Node =
  let inline text s = Text s
  let inline raw s = Raw s
  let inline comment s = Comment s
  let inline preRendered s = PreRendered s
  let inline element tag = Element(Element.create tag)

  let inline fragment nodes =
    let d = Deque(8)

    for n in nodes do
      d.AddLast(n)

    Fragment d

  let inline fragmentOf([<ParamArray>] nodes: Node[]) =
    let d = Deque(nodes.Length)

    for n in nodes do
      d.AddLast(n)

    Fragment d

  let empty = Fragment(Deque(0))

  let addChild child target =
    match target with
    | Element el ->
      el.children.AddLast(child)
      target
    | Fragment frag ->
      frag.AddLast(child)
      target
    | AsyncNode targetTask ->
      AsyncNode(
        cancellableValueTask {
          let! resolved = targetTask

          match resolved with
          | Element el ->
            el.children.AddLast(child)
            return resolved
          | Fragment frag ->
            frag.AddLast(child)
            return resolved
          | _ -> return resolved
        }
      )
    | AsyncSeqNode targetSeq ->
      let d = Deque(2)
      d.AddLast(AsyncSeqNode targetSeq)
      d.AddLast(child)
      Fragment d
    | Text t ->
      match child with
      | Text t2 -> Text(t + t2)
      | _ -> target
    | Raw r ->
      match child with
      | Raw r2 -> Raw(r + r2)
      | Text t -> Raw(r + t)
      | _ -> target
    | Comment c ->
      match child with
      | Comment c2 -> Comment(c + c2)
      | Text t -> Comment(c + t)
      | _ -> target
    | PreRendered _ -> target

  let addAttribute attr target =
    match target with
    | Element el ->
      el.attributes.AddLast(attr)
      target
    | AsyncNode targetTask ->
      AsyncNode(
        cancellableValueTask {
          let! resolved = targetTask

          match resolved with
          | Element el ->
            el.attributes.AddLast(attr)
            return resolved
          | _ -> return resolved
        }
      )
    | _ -> target

module NodeOps =
  let inline (<+) target child = Node.addChild child target
  let inline (<+.) target attr = Node.addAttribute attr target
