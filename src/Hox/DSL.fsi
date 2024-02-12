namespace Hox

open System
open System.Collections.Generic
open System.Threading.Tasks
open System.Runtime.CompilerServices

open FSharp.Control
open Hox.Core

/// This module contains functions that are used to add notes to other nodes
module NodeOps =
  /// <summary>
  /// Adds the 'value' node to the 'target' node, it can be seen as ading
  /// a child to a parent.
  /// </summary>
  /// <param name="target">The node to add the child to</param>
  /// <param name="value">The node to add as a child</param>
  /// <returns>The target node with the value node added to it</returns>
  /// <remarks>
  /// Tags that don't support children will ignore said children.
  /// Text based nodes will be merged together, so if you add a text node to another text node
  /// the text will be merged together.
  /// </remarks>
  /// <remarks>
  /// Text based nodes will be merged together, so if you add a text node to another text node
  /// the text will be merged together.
  /// </remarks>
  val addToNode: target: Node * value: Node -> Node

  /// <summary>
  /// Adds an attribute to the 'target' node.
  /// </summary>
  /// <param name="target">The node to add the attribute to</param>
  /// <param name="attribute">The attribute to add</param>
  /// <returns>The target node with the attribute added to it</returns>
  /// <remarks>
  /// If the target node is not an element, then the attribute will be ignored.
  /// </remarks>
  val addAttribute: target: Node * attribute: AttributeNode -> Node

  [<TailCall>]
  val getInnerMostChild: element: Element -> Element

  [<AutoOpen>]
  module Operators =
    /// Adds the 'value' node to the 'target' node, it can be seen as ading
    /// a child to a parent.
    val inline (<+): target: Node -> value: Node -> Node
    /// Adds an attribute to the 'target' node.
    val inline (<+.): target: Node -> value: AttributeNode -> Node

/// This class contains a small core DSL that is used to build nodes in a declarative way.
[<Class; AutoOpen>]
type NodeDsl =
  /// <summary>
  /// Creates a new element from a css selector, the selector supports classes, attributes and id
  /// in a standard way.
  /// </summary>
  /// <param name="cssSelector">The tag name of the element</param>
  /// <returns>A new node</returns>
  /// <remarks>
  /// The css selector supports multiple lines to avoid long lines.
  /// </remarks>
  /// <remarks>
  /// The css selector is parsed on a strict subset, it is not a full CSS parser, so it does not
  /// support all the features of CSS.
  /// </remarks>
  /// <example>
  /// <code lang="fsharp">
  /// let node = h "div#the-id.class-one.class-two[attribute=value]"
  /// </code>
  /// </example>
  /// <example>
  /// <code lang="fsharp">
  /// let node =
  ///  h """
  ///   div#the-id
  ///     .class-one
  ///     .class-two
  ///     [attribute=value]
  /// """
  /// </code>
  /// </example>
  static member inline h: cssSelector: string -> Node

  /// <summary>
  /// Creates a new element from a css selector, the selector supports classes, attributes and id
  /// in a standard way.
  /// </summary>
  /// <param name="cssSelector">The tag name of the element</param>
  /// <param name="textNodes">text nodes to add</param>
  /// <returns>A new node</returns>
  /// <remarks>
  /// The css selector supports multiple lines to avoid long lines.
  /// </remarks>
  /// <remarks>
  /// The css selector is parsed on a strict subset, it is not a full CSS parser, so it does not
  /// support all the features of CSS.
  /// </remarks>
  /// <example>
  /// <code lang="fsharp">
  /// let node = h("p", "Hello World", "Hello Second World")
  /// </code>
  /// </example>
  static member inline h:
    cssSelector: string * [<ParamArray>] textNodes: string array -> Node

  /// <summary>
  /// Creates a new element from a css selector, the selector supports classes, attributes and id
  /// in a standard way.
  /// </summary>
  /// <param name="cssSelector">The tag name of the element</param>
  /// <param name="child">The async child to add to the newly created element</param>
  /// <returns>A new node</returns>
  /// <remarks>
  /// The css selector supports multiple lines to avoid long lines.
  /// </remarks>
  /// <remarks>
  /// The css selector is parsed on a strict subset, it is not a full CSS parser, so it does not
  /// support all the features of CSS.
  /// </remarks>
  /// <example>
  /// <code lang="fsharp">
  /// let node = h "div#the-id.class-one.class-two[attribute=value]"
  /// </code>
  /// </example>
  /// <example>
  /// <code lang="fsharp">
  /// let getNode() = task { let! data = getData(); return h $"article[data-csv-data={data}]" }
  /// let node =
  ///  h("div#the-id
  ///        .class-one
  ///        .class-two
  ///        [attribute=value]",
  ///    getNode()
  ///  )
  /// </code>
  /// </example>
  static member inline h: cssSelector: string * child: Node Task -> Node

  /// <summary>
  /// Creates a new element from a css selector, the selector supports classes, attributes and id
  /// in a standard way.
  /// </summary>
  /// <param name="cssSelector">The tag name of the element</param>
  /// <param name="child">The tag name of the element</param>
  /// <returns>A new node</returns>
  /// <remarks>
  /// The css selector supports multiple lines to avoid long lines.
  /// </remarks>
  /// <remarks>
  /// The css selector is parsed on a strict subset, it is not a full CSS parser, so it does not
  /// support all the features of CSS.
  /// </remarks>
  /// <example>
  /// <code lang="fsharp">
  /// let node = h "div#the-id.class-one.class-two[attribute=value]"
  /// </code>
  /// </example>
  /// <example>
  /// <code lang="fsharp">
  /// let getNode() = async { let! data = getData(); return h $"article[data-csv-data={data}]" }
  /// let node =
  ///  h("div#the-id
  ///        .class-one
  ///        .class-two
  ///        [attribute=value]",
  ///    getNode()
  ///  )
  /// </code>
  /// </example>
  static member inline h: cssSelector: string * child: Node Async -> Node


  /// <summary>
  /// Creates a new element from a css selector, the selector supports classes, attributes and id
  /// in a standard way.
  /// </summary>
  /// <param name="cssSelector">The tag name of the element</param>
  /// <param name="children">A sequence of nodes to add to the newly created node</param>
  /// <returns>A new node</returns>
  /// <remarks>
  /// The css selector supports multiple lines to avoid long lines.
  /// </remarks>
  /// <remarks>
  /// The css selector is parsed on a strict subset, it is not a full CSS parser, so it does not
  /// support all the features of CSS.
  /// </remarks>
  /// <example>
  /// <code lang="fsharp">
  /// let node =
  ///   h("div#the-id.class-one.class-two[attribute=value]", [
  ///     text "Hello World"
  ///     text "second"
  ///   ])
  /// </code>
  /// </example>
  static member inline h: cssSelector: string * children: Node seq -> Node

  /// <summary>
  /// Creates a new element from a css selector, the selector supports classes, attributes and id
  /// in a standard way.
  /// </summary>
  /// <param name="cssSelector">The tag name of the element</param>
  /// <param name="children">A sequence of nodes to add to the newly created node</param>
  /// <returns>A new node</returns>
  /// <remarks>
  /// The css selector supports multiple lines to avoid long lines.
  /// </remarks>
  /// <remarks>
  /// The css selector is parsed on a strict subset, it is not a full CSS parser, so it does not
  /// support all the features of CSS.
  /// </remarks>
  /// <example>
  /// <code lang="fsharp">
  /// let node =
  ///  h("div#the-id.class-one.class-two[attribute=value]",
  ///    h("div", text "Hello World"),
  ///    h("div", text "second")
  ///   )
  /// </code>
  /// </example>
  static member inline h:
    cssSelector: string * [<ParamArray>] children: Node array -> Node

  /// <summary>
  /// Creates a new element from a css selector, the selector supports classes, attributes and id
  /// in a standard way.
  /// </summary>
  /// <param name="cssSelector">The tag name of the element</param>
  /// <param name="children">An async sequence of nodes to add to the newly created node</param>
  /// <returns>A new node</returns>
  /// <remarks>
  /// The css selector supports multiple lines to avoid long lines.
  /// </remarks>
  /// <remarks>
  /// The css selector is parsed on a strict subset, it is not a full CSS parser, so it does not
  /// support all the features of CSS.
  /// </remarks>
  /// <example>
  /// <code lang="fsharp">
  /// let node =
  ///  h("ul.my-list", taskSeq {
  ///   for i in 1 .. 10 do
  ///     // simulate async work e.g. pulling data from a database
  ///     do! Task.Delay(50)
  ///     h("li.my-list-item", text $"Item {i}")
  /// })
  /// </code>
  /// </example>
  /// <example>
  /// <code lang="csharp">
  /// static IAsyncEnumerable&lt;Node&gt; ListItems() {
  ///  for (var i = 1; i &lt;= 10; i++) {
  ///    // simulate async work e.g. pulling data from a database
  ///    await Task.Delay(50);
  ///   yield return h("li.my-list-item", text $"Item {i}");
  /// }
  /// var node = h("ul.my-list", ListItems());
  /// </code>
  /// </example>
  static member inline h:
    cssSelector: string * children: IAsyncEnumerable<Node> -> Node

  /// <summary>
  /// Creates a new element from a css selector, the selector supports classes, attributes and id
  /// in a standard way.
  /// </summary>
  /// <param name="element">The parent node to add the child node or nodes</param>
  /// <returns>A new node</returns>
  /// <remarks>
  /// The css selector supports multiple lines to avoid long lines.
  /// </remarks>
  /// <remarks>
  /// The css selector is parsed on a strict subset, it is not a full CSS parser, so it does not
  /// support all the features of CSS.
  /// </remarks>
  /// <example>
  /// <code lang="fsharp">
  /// let node = h("div", task { return text "Hello World" })
  /// </code>
  /// </example>
  /// <example>
  /// <code lang="csharp">
  /// var node = h("div", Task.FromResult(text "Hello World"));
  /// </code>
  /// </example>
  static member inline h: element: Task<Node> -> Node

  /// <summary>
  /// Creates a new element from a css selector, the selector supports classes, attributes and id
  /// in a standard way.
  /// </summary>
  /// <param name="element">The parent node to add the child node or nodes</param>
  /// <param name="child">A single node to add to the newly created node</param>
  /// <returns>A new node</returns>
  /// <remarks>
  /// The css selector supports multiple lines to avoid long lines.
  /// </remarks>
  /// <remarks>
  /// The css selector is parsed on a strict subset, it is not a full CSS parser, so it does not
  /// support all the features of CSS.
  /// </remarks>
  /// <example>
  /// <code lang="fsharp">
  /// let getNode() = task { let! data = getData(); return h $"article[data-csv-data={data}]" }
  /// let node = h(getNode(), h("section", text $"Item"))
  /// </code>
  /// </example>
  static member inline h: element: Task<Node> * child: Node -> Node

  /// <summary>
  /// Creates a new element from a css selector, the selector supports classes, attributes and id
  /// in a standard way.
  /// </summary>
  /// <param name="element">The parent node to add the child node or nodes</param>
  /// <param name="children">A single node to add to the newly created node</param>
  /// <returns>A new node</returns>
  /// <remarks>
  /// The css selector supports multiple lines to avoid long lines.
  /// </remarks>
  /// <remarks>
  /// The css selector is parsed on a strict subset, it is not a full CSS parser, so it does not
  /// support all the features of CSS.
  /// </remarks>
  /// <example>
  /// <code lang="fsharp">
  /// let getNode() = task { let! data = getData(); return h $"ul[data-csv-data={data}]" }
  /// let node = h(getNode(), [ for i in 1 .. 10 -> h("li", text $"Item {i}") ])
  /// </code>
  /// </example>
  static member inline h: element: Task<Node> * children: Node seq -> Node

  /// <summary>
  /// Creates a new element from a css selector, the selector supports classes, attributes and id
  /// in a standard way.
  /// </summary>
  /// <param name="element">The parent node to add the child node or nodes</param>
  /// <returns>A new node</returns>
  /// <remarks>
  /// The css selector supports multiple lines to avoid long lines.
  /// </remarks>
  /// <remarks>
  /// The css selector is parsed on a strict subset, it is not a full CSS parser, so it does not
  /// support all the features of CSS.
  /// </remarks>
  /// <example>
  /// <code lang="fsharp">
  /// let node = h("div", async { return text "Hello World" })
  /// </code>
  /// </example>
  static member inline h: element: Async<Node> -> Node

  /// <summary>
  /// Creates a new element from a css selector, the selector supports classes, attributes and id
  /// in a standard way.
  /// </summary>
  /// <param name="element">The parent node to add the child node or nodes</param>
  /// <param name="child">A single node to add to the newly created node</param>
  /// <returns>A new node</returns>
  /// <remarks>
  /// The css selector supports multiple lines to avoid long lines.
  /// </remarks>
  /// <remarks>
  /// The css selector is parsed on a strict subset, it is not a full CSS parser, so it does not
  /// support all the features of CSS.
  /// </remarks>
  /// <example>
  /// <code lang="fsharp">
  /// let getNode() = async { let! data = getData(); return h $"article[data-csv-data={data}]" }
  /// let node = h(getNode(), h("section", text $"Item"))
  /// </code>
  /// </example>
  static member inline h: element: Async<Node> * child: Node -> Node

  /// <summary>
  /// Creates a new element from a css selector, the selector supports classes, attributes and id
  /// in a standard way.
  /// </summary>
  /// <param name="element">The parent node to add the child node or nodes</param>
  /// <param name="children">A single node to add to the newly created node</param>
  /// <returns>A new node</returns>
  /// <remarks>
  /// The css selector supports multiple lines to avoid long lines.
  /// </remarks>
  /// <remarks>
  /// The css selector is parsed on a strict subset, it is not a full CSS parser, so it does not
  /// support all the features of CSS.
  /// </remarks>
  /// <example>
  /// <code lang="fsharp">
  /// let getNode() = async { let! data = getData(); return h $"ul[data-csv-data={data}]" }
  /// let node = h(getNode(), [ for i in 1 .. 10 -> h("li", text $"Item {i}") ])
  /// </code>
  /// </example>
  static member inline h: element: Async<Node> * children: Node seq -> Node

  /// <summary>
  /// Creates a new element from a css selector, the selector supports classes, attributes and id
  /// in a standard way. This overload is used when the children are async sequences.
  /// </summary>
  /// <param name="element">The parent node to add the child node or nodes</param>
  /// <param name="children">An async sequence of nodes to add to the newly created node</param>
  /// <returns>A new node</returns>
  /// <remarks>
  /// The css selector supports multiple lines to avoid long lines.
  /// </remarks>
  /// <remarks>
  /// The css selector is parsed on a strict subset, it is not a full CSS parser, so it does not
  /// support all the features of CSS.
  /// </remarks>
  /// <example>
  /// <code lang="fsharp">
  /// let node = h("ul.my-list", asyncSeq {
  ///   for i in 1 .. 10 do
  ///     // simulate async work e.g. pulling data from a database
  ///      do! Task.Delay(50)
  ///       h("li.my-list-item", text $"Item {i}")
  /// })
  /// </code>
  /// </example>
  /// <example>
  /// <code lang="csharp">
  /// static IAsyncEnumerable&lt;Node&gt; ListItems() {
  ///   for (var i = 1; i &lt;= 10; i++) {
  ///     // simulate async work e.g. pulling data from a database
  ///     await Task.Delay(50);
  ///     yield return h("li.my-list-item", text $"Item {i}");
  ///   }
  /// }
  /// var node = h("ul.my-list", ListItems());
  /// </code>
  /// </example>
  static member inline h:
    element: Node * children: IAsyncEnumerable<Node> -> Node

  /// <summary>
  /// Creates a new element from a css selector, the selector supports classes, attributes and id
  /// in a standard way.
  /// </summary>
  /// <param name="element">The parent node to add the child node or nodes</param>
  /// <param name="children">An async sequence of nodes to add to the newly created node</param>
  /// <returns>A new node</returns>
  /// <remarks>
  /// The css selector supports multiple lines to avoid long lines.
  /// </remarks>
  /// <remarks>
  /// The css selector is parsed on a strict subset, it is not a full CSS parser, so it does not
  /// support all the features of CSS.
  /// </remarks>
  /// <example>
  /// <code lang="fsharp">
  /// let getNode() = task { let! data = getData(); return h $"ul[data-csv-data={data}]" }
  /// let node = h(getNode(), asyncSeq {
  ///   for i in 1 .. 10 do
  ///     // simulate async work e.g. pulling data from a database
  ///     do! Task.Delay(50)
  ///     h("li", text $"Item {i}")
  /// })
  /// </code>
  /// </example>
  /// <example>
  /// <code lang="csharp">
  /// static IAsyncEnumerable&lt;Node&gt; ListItems() {
  ///   for (var i = 1; i &lt;= 10; i++) {
  ///     // simulate async work e.g. pulling data from a database
  ///     await Task.Delay(50);
  ///     yield return h("li.my-list-item", text $"Item {i}");
  ///   }
  /// }
  /// static Task&lt;Node&gt; GetNode() {
  ///    var data = await GetData();
  ///    return h($"ul[data-csv-data={data}]");
  /// }
  /// var node = h(GetNode(), ListItems());
  /// </code>
  /// </example>
  static member inline h:
    element: Task<Node> * children: IAsyncEnumerable<Node> -> Node

  /// <summary>
  /// Creates a new element from a css selector, the selector supports classes, attributes and id
  /// in a standard way.
  /// </summary>
  /// <param name="element">The parent node to add the child node or nodes</param>
  /// <param name="children">An async sequence of nodes to add to the newly created node</param>
  /// <returns>A new node</returns>
  /// <remarks>
  /// The css selector supports multiple lines to avoid long lines.
  /// </remarks>
  /// <remarks>
  /// The css selector is parsed on a strict subset, it is not a full CSS parser, so it does not
  /// support all the features of CSS.
  /// </remarks>
  /// <example>
  /// <code lang="fsharp">
  /// let getNode() = async { let! data = getData(); return h $"ul[data-csv-data={data}]" }
  /// let node = h(getNode(), asyncSeq {
  ///    for i in 1 .. 10 do
  ///      // simulate async work e.g. pulling data from a database
  ///      do! Task.Delay(50)
  ///      h("li", text $"Item {i}")
  /// })
  /// </code>
  /// </example>
  static member inline h:
    element: Async<Node> * children: IAsyncEnumerable<Node> -> Node

  /// <summary>
  /// Creates a new text node
  /// </summary>
  /// <param name="text">The text to add to the node</param>
  /// <returns>A new node</returns>
  /// <remarks>
  /// Text nodes created with this function are automatically escaped.
  /// </remarks>
  /// <example>
  /// <code lang="fsharp">
  /// let node = text "Hello World"
  /// </code>
  /// </example>
  /// <example>
  /// <code lang="csharp">
  /// var node = text("Hello World");
  /// </code>
  /// </example>
  static member inline text: text: string -> Node

  /// <summary>
  /// Creates a new text node
  /// </summary>
  /// <param name="text">The text to add to the node</param>
  /// <returns>A new node</returns>
  /// <remarks>
  /// Text nodes created with this function are automatically escaped.
  /// </remarks>
  /// <example>
  /// <code lang="fsharp">
  /// let node = text (task { return "Hello World" })
  /// </code>
  /// </example>
  /// <example>
  /// <code lang="csharp">
  /// var node = text(Task.FromResult("Hello World"));
  /// </code>
  /// </example>
  static member inline text: text: Task<string> -> Node

  /// <summary>
  /// Creates a new text node
  /// </summary>
  /// <param name="text">The text to add to the node</param>
  /// <returns>A new node</returns>
  /// <remarks>
  /// Text nodes created with this function are automatically escaped.
  /// </remarks>
  /// <example>
  /// <code lang="fsharp">
  /// let node = text (async { return "Hello World" })
  /// </code>
  /// </example>
  static member inline text: text: Async<string> -> Node

  /// <summary>
  /// Creates a new text node
  /// </summary>
  /// <param name="raw">The text to add to the node</param>
  /// <returns>A new node</returns>
  /// <remarks>
  /// WARNING: Text nodes created with this function are not escaped, so you should
  /// make sure that the text is safe to render otherwise you could be vulnerable to XSS attacks.
  /// </remarks>
  /// <example>
  /// <code lang="fsharp">
  /// let node = raw "Hello World"
  /// </code>
  /// </example>
  /// <example>
  /// <code lang="csharp">
  /// var node = raw("Hello World");
  /// </code>
  /// </example>
  static member inline raw: raw: string -> Node

  /// <summary>
  /// Creates a new text node
  /// </summary>
  /// <param name="raw">The text to add to the node</param>
  /// <returns>A new node</returns>
  /// <remarks>
  /// WARNING: Text nodes created with this function are not escaped, so you should
  /// make sure that the text is safe to render otherwise you could be vulnerable to XSS attacks.
  /// </remarks>
  /// <example>
  /// <code lang="fsharp">
  /// let node = raw (task { return "Hello World" })
  /// </code>
  /// </example>
  /// <example>
  /// <code lang="csharp">
  /// var node = raw(Task.FromResult("Hello World"));
  /// </code>
  /// </example>
  static member inline raw: raw: Task<string> -> Node

  /// <summary>
  /// Creates a new text node
  /// </summary>
  /// <param name="raw">The text to add to the node</param>
  /// <returns>A new node</returns>
  /// <remarks>
  /// WARNING: Text nodes created with this function are not escaped, so you should
  /// make sure that the text is safe to render otherwise you could be vulnerable to XSS attacks.
  /// </remarks>
  /// <example>
  /// <code lang="fsharp">
  /// let node = raw (async { return "Hello World" })
  /// </code>
  /// </example>
  static member inline raw: raw: Async<string> -> Node

  /// <summary>
  /// Creates a new HTML comment node, this will be rendered in the final HTML output
  /// </summary>
  /// <param name="comment">The comment to add to the node</param>
  /// <returns>A new node</returns>
  /// <example>
  /// <code lang="fsharp">
  /// let node = comment "Hello World"
  /// </code>
  /// </example>
  /// <example>
  /// <code lang="csharp">
  /// var node = comment("Hello World");
  /// </code>
  /// </example>
  static member inline comment: comment: string -> Node

  /// <summary>
  /// Creates a "fragment" node, this node is used to group multiple nodes together
  /// without a parent node.
  /// </summary>
  /// <param name="nodes">The nodes to add to the fragment</param>
  /// <returns>A new node</returns>
  /// <example>
  /// <code lang="fsharp">
  /// let node = fragment [ for i in 1 .. 10 -> h("li", text $"Item {i}") ]
  /// </code>
  /// </example>
  /// <example>
  /// <code lang="csharp">
  /// var node = fragment([ text "Hello World", text "second" ]);
  /// </code>
  /// </example>
  static member inline fragment: nodes: (Node seq) -> Node

  /// <summary>
  /// Creates a "fragment" node, this node is used to group multiple nodes together
  /// without a parent node.
  /// </summary>
  /// <param name="nodes">The nodes to add to the fragment</param>
  /// <returns>A new node</returns>
  /// <example>
  /// <code lang="fsharp">
  /// let node =
  ///   fragment(
  ///     h "link[rel=stylesheet][href=/assets/main.css]",
  ///     h "link[rel=stylesheet][href=/assets/shared.css]"
  ///   )
  /// </code>
  /// </example>
  /// <example>
  /// <code lang="csharp">
  /// var node =
  ///   fragment(
  ///     h "link[rel=stylesheet][href=/assets/main.css]",
  ///     h "link[rel=stylesheet][href=/assets/shared.css]"
  ///   );
  /// </code>
  /// </example>
  static member inline fragment: [<ParamArray>] nodes: (Node array) -> Node

  /// <summary>
  /// Creates a "fragment" node, this node is used to group multiple nodes together
  /// without a parent node.
  /// </summary>
  /// <param name="nodes">The nodes to add to the fragment</param>
  /// <returns>A new node</returns>
  /// <example>
  /// <code lang="fsharp">
  /// let node = fragment (task { return [ for i in 1 .. 10 -> h("li", text $"Item {i}") ] })
  /// </code>
  /// </example>
  static member inline fragment: nodes: Task<(Node seq)> -> Node

  /// <summary>
  /// Creates a "fragment" node, this node is used to group multiple nodes together
  /// without a parent node.
  /// </summary>
  /// <param name="nodes">The nodes to add to the fragment</param>
  /// <returns>A new node</returns>
  /// <example>
  /// <code lang="fsharp">
  /// let node = fragment (async { return [ for i in 1 .. 10 -> h("li", text $"Item {i}") ] })
  /// </code>
  /// </example>
  static member inline fragment: nodes: Async<(Node seq)> -> Node

  /// <summary>
  /// Adds an attribute to the 'target' node.
  /// </summary>
  /// <param name="nodes">The nodes to add to the fragment</param>
  /// <returns>A new node</returns>
  /// <example>
  /// <code lang="fsharp">
  /// let node = h("ul.my-list", taskSeq {
  ///   for i in 1 .. 10 do
  ///     // simulate async work e.g. pulling data from a database
  ///     do! Task.Delay(50)
  ///     h("li.my-list-item", text $"Item {i}")
  /// })
  /// </code>
  /// </example>
  static member inline fragment: nodes: IAsyncEnumerable<Node> -> Node

  /// <summary>
  /// Creates an empty node (shorthand for `fragment []`)
  /// </summary>
  /// <returns>A new node</returns>
  static member empty: Node

  /// <summary>
  /// Creates a new attribute node, this is meant to be added to nodes using the
  /// `NodeOps.addAttribute` function.
  /// </summary>
  /// <param name="name">The nodes to add to the fragment</param>
  /// <param name="value">the value of the attribute</param>
  /// <returns>A new AttributeNode</returns>
  /// <example>
  /// <code lang="fsharp">
  /// attr("data-foo", "bar")
  /// </code>
  /// </example>
  static member inline attribute: name: string * value: string -> AttributeNode

  /// <summary>
  /// Creates a new attribute node, this is meant to be added to nodes using the
  /// `NodeOps.addAttribute` function.
  /// </summary>
  /// <param name="name">The nodes to add to the fragment</param>
  /// <param name="value">the value of the attribute</param>
  /// <returns>A new AttributeNode</returns>
  /// <example>
  /// <code lang="fsharp">
  /// attr("data-foo", task {
  ///   let! data = getData()
  ///   return data
  /// })
  /// </code>
  /// </example>
  static member inline attribute:
    name: string * value: string Task -> AttributeNode

  /// <summary>
  /// Creates a new attribute node, this is meant to be added to nodes using the
  /// `NodeOps.addAttribute` function.
  /// </summary>
  /// <param name="name">The nodes to add to the fragment</param>
  /// <param name="value">the value of the attribute</param>
  /// <returns>A new AttributeNode</returns>
  /// <example>
  /// <code lang="fsharp">
  /// attr("data-foo", async {
  ///   let! data = getData()
  ///   return data
  /// })
  /// </code>
  /// </example>
  static member inline attribute:
    name: string * value: string Async -> AttributeNode

/// This class contains extensions to add attributes to nodes post creation.
[<Class; Extension>]
type NodeExtensions =

  /// <summary>
  /// Adds an attribute to the 'target' node.
  /// </summary>
  /// <param name="node">The node to add the attribute to</param>
  /// <param name="attribute">an existing attribute</param>
  /// <returns>The target node with the attribute added to it</returns>
  /// <remarks>
  /// If the target node is not an element, then the attribute will be ignored.
  /// </remarks>
  /// <example>
  /// <code lang="fsharp">
  /// let dataFoo = attr("data-foo", "bar")
  /// let node = h("div", text "Hello World").attr(dataFoo)
  /// </code>
  /// </example>
  static member inline attr: node: Node * attribute: AttributeNode -> Node

  /// <summary>
  /// Adds an attribute to the 'target' node.
  /// </summary>
  /// <param name="node">The node to add the attribute to</param>
  /// <param name="name">The name of the attribute</param>
  /// <param name="value">The value of the attribute</param>
  /// <returns>The target node with the attribute added to it</returns>
  /// <remarks>
  /// If the target node is not an element, then the attribute will be ignored.
  /// </remarks>
  /// <example>
  /// <code lang="fsharp">
  /// let node = h("div", text "Hello World").attr("data-foo", "bar")
  /// </code>
  /// </example>
  [<Extension>]
  static member inline attr: node: Node * name: string * ?value: string -> Node

  /// <summary>
  /// Adds an attribute to the 'target' node.
  /// </summary>
  /// <param name="node">The node to add the attribute to</param>
  /// <param name="name">The name of the attribute</param>
  /// <param name="value">The value of the attribute</param>
  /// <returns>The target node with the attribute added to it</returns>
  /// <remarks>
  /// If the target node is not an element, then the attribute will be ignored.
  /// </remarks>
  /// <remarks>
  /// If the value is false, then the attribute will not be added to the node.
  /// </remarks>
  /// <example>
  /// <code lang="fsharp">
  /// let node = h("div", text "Hello World").attr("data-foo", true)
  /// </code>
  /// </example>
  [<Extension>]
  static member inline attr: node: Node * name: string * value: bool -> Node

  /// <summary>
  /// Adds an attribute to the 'target' node.
  /// </summary>
  /// <param name="node">The node to add the attribute to</param>
  /// <param name="name">The name of the attribute</param>
  /// <param name="value">The value of the attribute</param>
  /// <returns>The target node with the attribute added to it</returns>
  /// <remarks>
  /// If the target node is not an element, then the attribute will be ignored.
  /// </remarks>
  /// <example>
  /// <code lang="fsharp">
  /// let node = h("div", text "Hello World").attr("data-foo", 42)
  /// </code>
  /// </example>
  [<Extension>]
  static member inline attr: node: Node * name: string * value: int -> Node

  /// <summary>
  /// Adds an attribute to the 'target' node.
  /// </summary>
  /// <param name="node">The node to add the attribute to</param>
  /// <param name="name">The name of the attribute</param>
  /// <param name="value">The value of the attribute</param>
  /// <returns>The target node with the attribute added to it</returns>
  /// <remarks>
  /// If the target node is not an element, then the attribute will be ignored.
  /// </remarks>
  /// <example>
  /// <code lang="fsharp">
  /// let node = h("div", text "Hello World").attr("data-foo", 42.0)
  /// </code>
  /// </example>
  [<Extension>]
  static member inline attr: node: Node * name: string * value: float -> Node

  /// <summary>
  /// Adds an attribute to the 'target' node.
  /// </summary>
  /// <param name="node">The node to add the attribute to</param>
  /// <param name="name">The name of the attribute</param>
  /// <param name="value">The value of the attribute</param>
  /// <returns>The target node with the attribute added to it</returns>
  /// <remarks>
  /// If the target node is not an element, then the attribute will be ignored.
  /// </remarks>
  /// <example>
  /// <code lang="fsharp">
  /// let node =
  ///   h("div", text "Hello World")
  ///   .attr("data-foo", task { return "bar" })
  /// </code>
  /// </example>
  [<Extension>]
  static member inline attr:
    node: Node * name: string * value: Task<string> -> Node

  /// <summary>
  /// Adds an attribute to the 'target' node.
  /// </summary>
  /// <param name="node">The node to add the attribute to</param>
  /// <param name="name">The name of the attribute</param>
  /// <param name="value">The value of the attribute</param>
  /// <returns>The target node with the attribute added to it</returns>
  /// <remarks>
  /// If the target node is not an element, then the attribute will be ignored.
  /// </remarks>
  /// <example>
  /// <code lang="fsharp">
  /// let node =
  ///  h("div", text "Hello World")
  ///   .attr("data-foo", async { return "bar" })
  /// </code>
  /// </example>
  [<Extension>]
  static member inline attr:
    node: Node * name: string * value: Async<string> -> Node

  /// <summary>
  /// Adds an attribute to the 'target' node.
  /// </summary>
  /// <param name="node">The node to add the attribute to</param>
  /// <param name="name">The name of the attribute</param>
  /// <param name="value">The value of the attribute</param>
  /// <returns>The target node with the attribute added to it</returns>
  /// <remarks>
  /// If the target node is not an element, then the attribute will be ignored.
  /// </remarks>
  /// <example>
  /// <code lang="fsharp">
  /// let node =
  ///   h("div", text "Hello World")
  ///     .attr("data-foo", task { return true })
  /// </code>
  /// </example>
  [<Extension>]
  static member inline attr:
    node: Node * name: string * value: Task<bool> -> Node

  /// <summary>
  /// Adds an attribute to the 'target' node.
  /// </summary>
  /// <param name="node">The node to add the attribute to</param>
  /// <param name="name">The name of the attribute</param>
  /// <param name="value">The value of the attribute</param>
  /// <returns>The target node with the attribute added to it</returns>
  /// <remarks>
  /// If the target node is not an element, then the attribute will be ignored.
  /// </remarks>
  /// <example>
  /// <code lang="fsharp">
  /// let node =
  ///   h("div", text "Hello World")
  ///     .attr("data-foo", async { return true })
  /// </code>
  /// </example>
  [<Extension>]
  static member inline attr:
    node: Node * name: string * value: Async<bool> -> Node

  /// <summary>
  /// Adds an attribute to the 'target' node.
  /// </summary>
  /// <param name="node">The node to add the attribute to</param>
  /// <param name="name">The name of the attribute</param>
  /// <param name="value">The value of the attribute</param>
  /// <returns>The target node with the attribute added to it</returns>
  /// <remarks>
  /// If the target node is not an element, then the attribute will be ignored.
  /// </remarks>
  /// <example>
  /// <code lang="fsharp">
  /// let node =
  ///   h("div", text "Hello World")
  ///     .attr("data-foo", task { return 42 })
  /// </code>
  /// </example>
  [<Extension>]
  static member inline attr:
    node: Node * name: string * value: Task<int> -> Node

  /// <summary>
  /// Adds an attribute to the 'target' node.
  /// </summary>
  /// <param name="node">The node to add the attribute to</param>
  /// <param name="name">The name of the attribute</param>
  /// <param name="value">The value of the attribute</param>
  /// <returns>The target node with the attribute added to it</returns>
  /// <remarks>
  /// If the target node is not an element, then the attribute will be ignored.
  /// </remarks>
  /// <example>
  /// <code lang="fsharp">
  /// let node =
  ///   h("div", text "Hello World")
  ///     .attr("data-foo", async { return 42 })
  /// </code>
  /// </example>
  [<Extension>]
  static member inline attr:
    node: Node * name: string * value: Async<int> -> Node

  /// <summary>
  /// Adds an attribute to the 'target' node.
  /// </summary>
  /// <param name="node">The node to add the attribute to</param>
  /// <param name="name">The name of the attribute</param>
  /// <param name="value">The value of the attribute</param>
  /// <returns>The target node with the attribute added to it</returns>
  /// <remarks>
  /// If the target node is not an element, then the attribute will be ignored.
  /// </remarks>
  /// <example>
  /// <code lang="fsharp">
  /// let node =
  ///   h("div", text "Hello World")
  ///     .attr("data-foo", task { return 42.0 })
  /// </code>
  /// </example>
  [<Extension>]
  static member inline attr:
    node: Node * name: string * value: Task<float> -> Node

  /// <summary>
  /// Adds an attribute to the 'target' node.
  /// </summary>
  /// <param name="node">The node to add the attribute to</param>
  /// <param name="name">The name of the attribute</param>
  /// <param name="value">The value of the attribute</param>
  /// <returns>The target node with the attribute added to it</returns>
  /// <remarks>
  /// If the target node is not an element, then the attribute will be ignored. If the value is
  /// false, then the attribute will not be added to the node.
  /// </remarks>
  /// <example>
  /// <code lang="fsharp">
  /// let node =
  ///   h("div", text "Hello World")
  ///     .attr("data-foo", async { return 42.0})
  /// </code>
  /// </example>
  [<Extension>]
  static member inline attr:
    node: Node * name: string * value: Async<float> -> Node

/// This class contains an extension to help create elements with declarative shadow DOM
[<Class; AutoOpen>]
type DeclarativeShadowDom =

  /// <summary>
  /// Produces a function that will create a Declarative Shadow DOM element.
  /// This function only creates the template content. If you need to use Slots, please
  /// use the overload that takes a template definition and returns a factory function instead.
  /// </summary>
  /// <param name="tagName">The tag name of the element</param>
  /// <param name="templateDefinition">
  /// This content is going to
  /// be added inside the `&lt;template&gt;` tag that is part of the declarative shadow DOM
  /// </param>
  /// <returns>A declarative shadow dom enabled node</returns>
  /// <remarks>
  /// This function is meant to be consumed by the F# folks, if you are using C# then you should
  /// use the `shcs` function instead.
  /// </remarks>
  /// <example>
  /// <code lang="fsharp">
  /// let myShadowDiv =
  ///   sh("div", el("header", text "Hello World"),el("footer", text "Bye World"))
  ///
  /// let node = myShadowDiv
  /// // produces
  /// // &lt;div&gt;
  /// //   &lt;template shadowroot="open"&gt;
  /// //    &lt;header&gt;Hello World&lt;/header&gt;
  /// //    &lt;footer&gt;Bye World&lt;/footer&gt;
  /// //   &lt;/template&gt;
  /// // &lt;/div&gt;
  /// </code>
  /// </example>
  static member inline sh: tagName: string * templateDefinition: Node -> Node

  /// <summary>
  /// Produces a function that will create an HTML tag with a template tag enabled with declarative shadow DOM.
  /// The resulting function will take a child node and append it to the final template.
  /// This function is particularly useful when you're using slots, otherwise you can use the overload
  /// that takes a single node.
  /// </summary>
  /// <param name="tagName">The tag name of the element</param>
  /// <param name="templateDefinition">
  /// This content is going to
  /// be added inside the `&lt;template&gt;` tag that is part of the declarative shadow DOM
  /// </param>
  /// <returns>Function that will take a child node and append it to the final template</returns>
  /// <remarks>
  /// This function is meant to be consumed by the F# folks, if you are using C# then you should
  /// use the `shcs` function instead.
  /// </remarks>
  /// <example>
  /// <code lang="fsharp">
  /// let myShadowDiv content =
  ///   sh("div", el("header", text "Hello World"), el "slot", el("footer", text "Bye World"))
  ///
  /// let node = myShadowDiv(h("section", text "Hello World"))
  /// // produces
  /// // &lt;div&gt;
  /// //   &lt;template shadowroot="open"&gt;
  /// //    &lt;header&gt;Hello World&lt;/header&gt;
  /// //    &lt;slot&gt;&lt;/slot&gt;
  /// //    &lt;footer&gt;Bye World&lt;/footer&gt;
  /// //   &lt;/template&gt;
  /// //   &lt;section&gt;Hello World&lt;/section&gt;
  /// // &lt;/div&gt;
  /// </code>
  /// </example>
  static member inline sh:
    tagName: string * [<ParamArray>] templateDefinition: Node array ->
      (Node -> Node)

  /// <summary>
  /// Produces a function that will create an HTML tag with a template tag enabled with declarative shadow DOM.
  /// The resulting function will take a child node and append it to the final template.
  /// </summary>
  /// <param name="tagName">The tag name of the element</param>
  /// <param name="templateDefinition">
  /// This content is going to
  /// be added inside the `&lt;template&gt;` tag that is part of the declarative shadow DOM
  /// </param>
  /// <returns>Function that will take a child node and append it to the final template</returns>
  /// <remarks>
  /// This function is meant to be consumed by the C# folks, if you are using F# then you should
  /// use the `sh` function instead.
  /// </remarks>
  /// <example>
  /// <code lang="csharp">
  /// var myShadowDiv content =
  ///   sh("div", el("header", text "Hello World"), el "slot", el("footer", text "Bye World"));
  ///
  /// var node = myShadowDiv(h("section", text "Hello World"));
  /// // produces
  /// // &lt;div&gt;
  /// //   &lt;template shadowroot="open"&gt;
  /// //    &lt;header&gt;Hello World&lt;/header&gt;
  /// //    &lt;slot&gt;&lt;/slot&gt;
  /// //    &lt;footer&gt;Bye World&lt;/footer&gt;
  /// //   &lt;/template&gt;
  /// //   &lt;section&gt;Hello World&lt;/section&gt;
  /// // &lt;/div&gt;
  /// </code>
  /// </example>
  static member inline shcs:
    tagName: string * [<ParamArray>] templateDefinition: Node array ->
      Func<Node, Node>

/// <summary>
/// This class contains a set of functions that can be used to create HTML elements
/// with a declarative shadow DOM enabled.
/// </summary>
/// <remarks>
/// since these elements are common, we've decided to add them to the library but require
/// opt in, to avoid mistaking them with regular HTML elements that don't have declarative shadow dom enabled.
/// </remarks>
/// <example>
/// <code lang="fsharp">
/// open type ScopableElements
/// let node = div(h("section", text "Hello World"))
/// // produces
/// // &lt;div&gt;
/// //   &lt;template shadowroot="open"&gt;
/// //    &lt;section&gt;Hello World&lt;/section&gt;
/// //   &lt;/template&gt;
/// // &lt;/div&gt;
/// </code>
/// </example>
/// <example>
/// <code lang="csharp">
/// using static ScopableElements;
/// var node = div(h("section", text "Hello World"));
/// // produces
/// // &lt;div&gt;
/// //   &lt;template shadowroot="open"&gt;
/// //    &lt;section&gt;Hello World&lt;/section&gt;
/// //   &lt;/template&gt;
/// // &lt;/div&gt;
/// </code>
/// </example>
[<Class>]
type ScopableElements =

  /// <summary>
  /// Creates a new article element with declarative shadow DOM enabled.
  /// </summary>
  /// <param name="content">The content to add to the article</param>
  /// <returns>A new node</returns>
  /// <remarks>
  /// This element has declarative shdow dom enabled, meaning
  /// you can use slots to project content into the shadow DOM.
  /// </remarks>
  /// <remarks>
  /// The styles node will be added into the template tag with declarative shadow dom
  /// </remarks>
  static member inline article: [<ParamArray>] content: Node array -> Node

  /// <summary>
  /// Creates a new blockquote element with declarative shadow DOM enabled.
  /// </summary>
  /// <param name="content">The content to add to the blockquote</param>
  /// <returns>A new node</returns>
  /// <remarks>
  /// This element has declarative shdow dom enabled, meaning
  /// you can use slots to project content into the shadow DOM.
  /// </remarks>
  /// <remarks>
  /// The styles node will be added into the template tag with declarative shadow dom
  /// </remarks>
  static member inline aside: [<ParamArray>] content: Node array -> Node

  /// <summary>
  /// Creates a new blockquote element with declarative shadow DOM enabled.
  /// </summary>
  /// <param name="content">The content to add to the blockquote</param>
  /// <returns>A new node</returns>
  /// <remarks>
  /// This element has declarative shdow dom enabled, meaning
  /// you can use slots to project content into the shadow DOM.
  /// </remarks>
  /// <remarks>
  /// The styles node will be added into the template tag with declarative shadow dom
  /// </remarks>
  static member inline blockquote: [<ParamArray>] content: Node array -> Node

  /// <summary>
  /// Creates a new body element with declarative shadow DOM enabled.
  /// </summary>
  /// <param name="content">The content to add to the body</param>
  /// <returns>A new node</returns>
  /// <remarks>
  /// This element has declarative shdow dom enabled, meaning
  /// you can use slots to project content into the shadow DOM.
  /// </remarks>
  /// <remarks>
  /// The styles node will be added into the template tag with declarative shadow dom
  /// </remarks>
  static member inline body: [<ParamArray>] content: Node array -> Node

  /// <summary>
  /// Creates a new div element with declarative shadow DOM enabled.
  /// </summary>
  /// <param name="content">The content to add to the div</param>
  /// <returns>A new node</returns>
  /// <remarks>
  /// This element has declarative shdow dom enabled, meaning
  /// you can use slots to project content into the shadow DOM.
  /// </remarks>
  /// <remarks>
  /// The styles node will be added into the template tag with declarative shadow dom
  /// </remarks>
  static member inline div: [<ParamArray>] content: Node array -> Node

  /// <summary>
  /// Creates a new footer element with declarative shadow DOM enabled.
  /// </summary>
  /// <param name="content">The content to add to the footer</param>
  /// <returns>A new node</returns>
  /// <remarks>
  /// This element has declarative shdow dom enabled, meaning
  /// you can use slots to project content into the shadow DOM.
  /// </remarks>
  /// <remarks>
  /// The styles node will be added into the template tag with declarative shadow dom
  /// </remarks>
  static member inline footer: [<ParamArray>] content: Node array -> Node

  /// <summary>
  /// Creates a new h1 element with declarative shadow DOM enabled.
  /// </summary>
  /// <param name="content">The content to add to the h1</param>
  /// <returns>A new node</returns>
  /// <remarks>
  /// This element has declarative shdow dom enabled, meaning
  /// you can use slots to project content into the shadow DOM.
  /// </remarks>
  /// <remarks>
  /// The styles node will be added into the template tag with declarative shadow dom
  /// </remarks>
  static member inline h1: [<ParamArray>] content: Node array -> Node

  /// <summary>
  /// Creates a new h2 element with declarative shadow DOM enabled.
  /// </summary>
  /// <param name="content">The content to add to the h2</param>
  /// <returns>A new node</returns>
  /// <remarks>
  /// This element has declarative shdow dom enabled, meaning
  /// you can use slots to project content into the shadow DOM.
  /// </remarks>
  /// <remarks>
  /// The styles node will be added into the template tag with declarative shadow dom
  /// </remarks>
  static member inline h2: [<ParamArray>] content: Node array -> Node

  /// <summary>
  /// Creates a new h3 element with declarative shadow DOM enabled.
  /// </summary>
  /// <param name="content">The content to add to the h3</param>
  /// <returns>A new node</returns>
  /// <remarks>
  /// This element has declarative shdow dom enabled, meaning
  /// you can use slots to project content into the shadow DOM.
  /// </remarks>
  /// <remarks>
  /// The styles node will be added into the template tag with declarative shadow dom
  /// </remarks>
  static member inline h3: [<ParamArray>] content: Node array -> Node

  /// <summary>
  /// Creates a new h4 element with declarative shadow DOM enabled.
  /// </summary>
  /// <param name="content">The content to add to the h4</param>
  /// <returns>A new node</returns>
  /// <remarks>
  /// This element has declarative shdow dom enabled, meaning
  /// you can use slots to project content into the shadow DOM.
  /// </remarks>
  /// <remarks>
  /// The styles node will be added into the template tag with declarative shadow dom
  /// </remarks>
  static member inline h4: [<ParamArray>] content: Node array -> Node

  /// <summary>
  /// Creates a new h5 element with declarative shadow DOM enabled.
  /// </summary>
  /// <param name="content">The content to add to the h5</param>
  /// <returns>A new node</returns>
  /// <remarks>
  /// This element has declarative shdow dom enabled, meaning
  /// you can use slots to project content into the shadow DOM.
  /// </remarks>
  /// <remarks>
  /// The styles node will be added into the template tag with declarative shadow dom
  /// </remarks>
  static member inline h5: [<ParamArray>] content: Node array -> Node

  /// <summary>
  /// Creates a new h6 element with declarative shadow DOM enabled.
  /// </summary>
  /// <param name="content">The content to add to the h6</param>
  /// <returns>A new node</returns>
  /// <remarks>
  /// This element has declarative shdow dom enabled, meaning
  /// you can use slots to project content into the shadow DOM.
  /// </remarks>
  /// <remarks>
  /// The styles node will be added into the template tag with declarative shadow dom
  /// </remarks>
  static member inline h6: [<ParamArray>] content: Node array -> Node

  /// <summary>
  /// Creates a new header element with declarative shadow DOM enabled.
  /// </summary>
  /// <param name="content">The content to add to the header</param>
  /// <returns>A new node</returns>
  /// <remarks>
  /// This element is inside the Shadow DOM so the styles are encapsulated.
  /// This is a native "scoping" solution to css styles.
  /// You can include style and link tags and the styling
  /// won't leak to the outside of this element.
  /// </remarks>
  /// <remarks>
  /// The styles node will be added into the template tag with declarative shadow dom
  /// </remarks>
  static member inline header: [<ParamArray>] content: Node array -> Node

  /// <summary>
  /// Creates a new main element with declarative shadow DOM enabled.
  /// </summary>
  /// <param name="content">The content to add to the main</param>
  /// <returns>A new node</returns>
  /// <remarks>
  /// This element is inside the Shadow DOM so the styles are encapsulated.
  /// This is a native "scoping" solution to css styles.
  /// You can include style and link tags and the styling
  /// won't leak to the outside of this element.
  /// </remarks>
  /// <remarks>
  /// The styles node will be added into the template tag with declarative shadow dom
  /// </remarks>
  static member inline main: [<ParamArray>] content: Node array -> Node

  /// <summary>
  /// Creates a new nav element with declarative shadow DOM enabled.
  /// </summary>
  /// <param name="content">The content to add to the nav</param>
  /// <returns>A new node</returns>
  /// <remarks>
  /// This element is inside the Shadow DOM so the styles are encapsulated.
  /// This is a native "scoping" solution to css styles.
  /// You can include style and link tags and the styling
  /// won't leak to the outside of this element.
  /// </remarks>
  /// <remarks>
  /// The styles node will be added into the template tag with declarative shadow dom
  /// </remarks>
  static member inline nav: [<ParamArray>] content: Node array -> Node

  /// <summary>
  /// Creates a new p element with declarative shadow DOM enabled.
  /// </summary>
  /// <param name="content">The content to add to the p</param>
  /// <returns>A new node</returns>
  /// <remarks>
  /// This element is inside the Shadow DOM so the styles are encapsulated.
  /// This is a native "scoping" solution to css styles.
  /// You can include style and link tags and the styling
  /// won't leak to the outside of this element.
  /// </remarks>
  /// <remarks>
  /// The styles node will be added into the template tag with declarative shadow dom
  /// </remarks>
  static member inline p: [<ParamArray>] content: Node array -> Node

  /// <summary>
  /// Creates a new section element with declarative shadow DOM enabled.
  /// </summary>
  /// <param name="content">The content to add to the section</param>
  /// <returns>A new node</returns>
  /// <remarks>
  /// This element is inside the Shadow DOM so the styles are encapsulated.
  /// This is a native "scoping" solution to css styles.
  /// You can include style and link tags and the styling
  /// won't leak to the outside of this element.
  /// </remarks>
  /// <remarks>
  /// The styles node will be added into the template tag with declarative shadow dom
  /// </remarks>
  static member inline section: [<ParamArray>] content: Node array -> Node

  /// <summary>
  /// Creates a new span element with declarative shadow DOM enabled.
  /// </summary>
  /// <param name="content">The content to add to the span</param>
  /// <returns>A new node</returns>
  /// <remarks>
  /// This element is inside the Shadow DOM so the styles are encapsulated.
  /// This is a native "scoping" solution to css styles.
  /// You can include style and link tags and the styling
  /// won't leak to the outside of this element.
  /// </remarks>
  /// <remarks>
  /// The styles node will be added into the template tag with declarative shadow dom
  /// </remarks>
  static member inline span: [<ParamArray>] content: Node array -> Node
