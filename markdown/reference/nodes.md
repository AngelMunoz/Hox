Nodes are the core of the Hox library. They allow you to represent HTML as a tree of nodes.

While most nodes are not interesting by themselves (e.g Text, Raw, Fragment), asynchronous ones are, and they are the reason why Hox exists.

> **_Node_**: Given the limitations of the type system and the incompatibilities between F# and C# records, lack of discriminated uninions and other things. This reference guide is meant for F# users, while C#/VB.NET folks can still use Hox, to manipulate the core library requires using F#.

```fsharp
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
```

## AttributeNode

One of the design choices that might be a little questionable is that attributes are embeded into the Element type, rather than defined as part of a Node, this is intentional.

There are two types of nodes synchronous and asynchronous. The reason they are separated rather than `ValueTask<HAttribute>` is to have them co-exist side by side with synchronous nodes. The goal is to blur the lines between synchronous and asynchronous code, and make it easy to mix and match. and this enable DSL authors to create their own attributes that can be used in both synchronous and asynchronous contexts.

## Element

Elements represent HTML tags, they have a tag name, a list of attributes and a list of children. As you can read from the type definition, Attributes and Nodes are one dimensional lists, once again bluring the lines bwteen synchronous and asynchronous code.

## Node

The Node contains the possible representations of work that can co-exist to generate an HTML tree. In the future someone might come up with a new type of node worth of it's own type, which ultimately should be able to resolve to a normal node. In the meantime we'll dive into the existing types.

> **_Note_**: Unless specified by the `Raw` node, every other part where a string is expected, it is expected to be HTML encoded. This includes attributes, text nodes, and comments.

### Fragments

These represent a "_parentless_" list of nodes. A concept that is not precisely present in HTML but it is quite popular in frontend frameworks by its versatility when composing nodes together, specially when you have a list of children that may agnostic to parent elements.

### AsyncNodes

The async node is the asynchronous version of the node, it is a wrapper around a `CancellableValueTask<Node>`. This is the type of node that is returned by asynchronous attributes.

When you create an async node it is very important that you add cancellation semantics. `CancellableValueTask<T>` is a type alias to `CancellationToken -> ValueTask<T>`, so this means that any potential asynchronous work is lazily evaluated, and it is only executed when we're resolving the node to be rendered or added to the render pending stack.

For Example the Nodes created by the `h` function have the following pattern:

```fsharp
// `work` can be either `Task<Node>` or F#'s `Async<Node>`
// It could also be a `CancellableValueTask<Node>` however, it makes
// little sense for users as they are already doing sync or async work.
let asyncNode work =
    AsyncNode(cancellableValueTask {
        // tokens are cooperative, so whoever the parent is, it will pass
        // and bind the token to the child computation as well.
        let! token = CancellableValueTask.getCancellationToken()
        if token.IsCancellationRequested then
            return Node.Fragment []
        else
            let! node = work
            return node
    })
```

So if you're taking asynchronous work in a DSL, in a library, or another context it is here where you could implement cancellation semantics to avoid performing work. cancellableValueTask will bind async, task and value tasks in the workflow so regardless of the type it will be able to handle it.

### AsyncSeqNode

These nodes were included to support `IAsyncEnumerable<T>` which is a type that is not supported by F# in a normal setting, however `FSharp.Control.TaskSeq` provides the `taskSeq` computation expression that allows to create, and consume `IAsyncEnumerable<T>`.

`taskSeq` at the time of writing does not support a simple way to streamline cancellation semantics, so it is up to the user to implement them.
