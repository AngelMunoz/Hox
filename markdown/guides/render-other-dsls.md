Sometimes you may already have a big codebase written in another DSL, if you want to leverage their DSL and slowly migrate to Hox or a Hox based alternative writing an adapter is not a complex task.

### Falco.Markup

For example, if you want to use [Falco.Markup](https://www.falcoframework.com/docs/markup.html) we already have a sample in our [samples](https://github.com/AngelMunoz/Hox/tree/bd1fdb82de26389d4ebfd49f65b58b2340cb8999/samples/Falco) directory.

```fsharp
open Falco.Markup

open Hox
open Hox.Core
open Hox.Rendering

// take their DSL attributes and convert them to Hox attributes
let inline xmlAttrToHox(attr: XmlAttribute) : AttributeNode =
  match attr with
  | KeyValueAttr(key, value) -> Attribute { name = key; value = value }
  | NonValueAttr key -> Attribute { name = key; value = "" }

// folding function to use further below, we're basically
// just adding all of the found attributes to the specified node
let inline foldAttributes (node: Node) (attr: XmlAttribute) : Node =
  match attr with
  | KeyValueAttr(key, value) ->
    NodeOps.addAttribute(node, Attribute { name = key; value = value })
  | NonValueAttr key ->
    NodeOps.addAttribute(node, Attribute { name = key; value = "" })

// Falco.Markup's DSL uses XmlNodes as their core type, so
// as long as we can convert an XmlNode to a Hox Node we're good
let rec xmlNodeToHox(fmNode: XmlNode) : Node =
  match fmNode with
  | ParentNode((tagName, attributes), children) ->
    attributes
    |> List.fold
      foldAttributes
      (h(tagName, children |> List.map xmlNodeToHox))
  | TextNode text -> Text text
  | SelfClosingNode((tagName, attributes)) ->
    attributes |> List.fold foldAttributes (h tagName)
```

In the code above we're just converting Falco.Markup's DSL to Hox's DSL, we're using the `h` function to create a node and then we're adding all of the attributes to it.

To render it later on we just need to call this newly created function

```fsharp
let render (fmNode: XmlNode) =
  let convertedNode = fmNode |> xmlNodeToHox

  Render.asString(convertedNode)
```

### Giraffe.ViewEngine

Giraffe's engine is in a similar situation, you can find a [samples](https://github.com/AngelMunoz/Hox/tree/bd1fdb82de26389d4ebfd49f65b58b2340cb8999/samples/Giraffe) in our repository as well.

```fsharp
open Giraffe.ViewEngine

open Hox
open Hox.Core
open Hox.Rendering

// in a similar fashion to Falco.Markup we're just
// converting Giraffe.ViewEngine's DSL to Hox's DSL
let inline xmlAttrToHox(attr: XmlAttribute) : AttributeNode =
  match attr with
  | KeyValue(key, value) -> Attribute { name = key; value = value }
  | Boolean key -> Attribute { name = key; value = "" }

let inline foldAttributes (node: Node) (attr: XmlAttribute) : Node =
  match attr with
  | KeyValue(key, value) ->
    NodeOps.addAttribute(node, Attribute { name = key; value = value })
  | Boolean key ->
    NodeOps.addAttribute(node, Attribute { name = key; value = "" })

// Giraffe ViewEngine also uses a type called XmlNode, but
// it's different from Falco.Markup's XmlNode, so we need
// to convert it to Hox as well
let rec xmlNodeToHoxNode(fmNode: XmlNode) : Node =
  match fmNode with
  | ParentNode((tagName, attributes), children) ->
    attributes
    |> Array.fold
      foldAttributes
      (h(tagName, children |> List.map xmlNodeToHoxNode))
  | HtmlElements.Text text -> Core.Text text
  | VoidElement((tagName, attributes)) ->
    attributes |> Array.fold foldAttributes (h tagName)
```

Once we have this small interop layer we can render it as well

```fsharp
let render (fmNode: XmlNode) =
  let convertedNode = fmNode |> xmlNodeToHoxNode

  Render.asString(convertedNode)
```

### A word on moving to Hox

Keep in mind that their DSL doesn't support asynchronous nodes, so think of these solutions as a way to either interop or migrate to Hox.

Me as the Hox author I'd rather urge you to help them and find a way to add support for asynchronous nodes and rendering to their DSL, to keep pushing the boundaries of what's possible in F#.

# Moving out of Hox

If you want to move out of Hox enable Hox nodes in your DSL and then use the `Hox.Core` module to convert them to your DSL's nodes.

```fsharp
open System
open System.Web
// IAsyncEnumerable Support
open FSharp.Control
// Cancellable Tasks and ValueTasks
open IcedTasks

open Falco.Markup

open Hox
open Hox.Core
open Hox.Rendering


let rec hoxNodeToXmlNode(node: Node) = cancellableValueTask {
  let! token = CancellableValueTask.getCancellationToken()

  // Usually we would take the cancellation process
  // within our own rendering engine, but since we're
  // creating an adapter most likely we need handle cancellation
  // in the adapter itself.
  // since this function is recursive and cancellbleValueTask
  // cooperatively binds the cancellation token, we can be sure that
  // if the token is cancelled, the whole tree will be cancelled.
  token.ThrowIfCancellationRequested()

  match node with
  | Text text -> return TextNode (HttpUtility.HtmlEncode text)
  | Raw raw -> return TextNode raw
  | Comment comment ->
    return TextNode ("<!-- " + HttpUtility.HtmlEncode comment + " -->")
  | Element {
              tag = tag
              attributes = attributes
              children = children
            } ->
    let attrBag = ResizeArray<XmlAttribute>()
    let childrenBag = ResizeArray<XmlNode>()

    for attr in attributes do
      let! attr = hoxAttrToXmlAttr attr
      attrBag.Add(attr)


    for child in children do
      let! child = hoxNodeToXmlNode child
      childrenBag.Add(child)

    return ParentNode((tag, attrBag |> List.ofSeq), childrenBag |> List.ofSeq)
  // Falco.Markup doesn't support fragments, we'll add a parent node
  // but this is not the right semantic.
  | AsyncSeqNode nodes ->
    let bag = ResizeArray<XmlNode>()

    for node in nodes do
      // A word of caution, recursion is not well supported
      // in Computation Expressions, if the tree is really deep
      // this will likely end up in a stack overflow.
      // so an alternative approach needs to be taken.
      let! node = hoxNodeToXmlNode node
      bag.Add(node)

    return ParentNode(("", []), bag |> List.ofSeq)

  // Falco.Markup doesn't support fragments, we'll add a parent node
  // but this is not the right semantic.
  | Fragment nodes ->
    let bag = ResizeArray<XmlNode>()

    for node in nodes do
      // similarly as above, if the tree is really deep
      // this will likely end up in a stack overflow.
      let! node = hoxNodeToXmlNode node
      bag.Add(node)

    return ParentNode(("", []), bag |> List.ofSeq)
  | AsyncNode op ->
    let! node = op
    return! hoxNodeToXmlNode node
}
```

Once we're done with the conversion functions we can render it in the usual means.

Hox Nodes by nature will always be cancellableValueTasks as we support cancellation as well, so to interact with other DSLs we first need to resolve the node tree before passing it onto their rendering engine unless they support async rendering in some way.

```fsharp
let view (): Task<string> =  task {
  let node = h("p", "Hello World")
  // conver it to an XML node
  let! node = node |> hoxNodeToXmlNode
  return! XmlNodeRenderer.renderNode node
}
```

Hox is meant to be a building block for HTML rendering so, it is extensible enough to either migrate to it or away from it.
