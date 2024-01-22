namespace Hox.Feliz

open System.Collections.Generic
open System.Threading.Tasks

open FSharp.Control
open IcedTasks

open Feliz

open Hox
open Hox.Core

/// <summary>
/// Augments the Feliz.Engine standard DSL with some extra members and helpers.
/// To allow you to use async nodes side by side with sync nodes and match Hox's API.
/// </summary>
[<AutoOpen>]
module Engine =
  type HtmlEngine<'Node> with

    /// <summary>
    /// Allows you to use async nodes side by side with sync nodes.
    /// </summary>
    /// <param name="node">The node to make async</param>
    /// <returns>A node</returns>
    member inline async: node: Async<Node> -> Node
    /// <summary>
    /// Allows you to use task nodes side by side with sync nodes.
    /// </summary>
    /// <param name="node">The node to make async</param>
    /// <returns>A node</returns>
    member inline task: node: Task<Node> -> Node
    /// <summary>
    /// takes a sequence of nodes to render without a parent element.
    /// </summary>
    /// <param name="nodes">The nodes to render</param>
    /// <returns>A fragment</returns>
    member inline fragment: nodes: Node seq -> Node
    /// <summary>
    /// takes an async sequence of nodes to render without a parent element.
    /// </summary>
    /// <param name="nodes">The nodes to render</param>
    /// <returns>A fragment</returns>
    member inline fragment: nodes: IAsyncEnumerable<Node> -> Node
    /// <summary>
    /// Creates a new article element with declarative shadow DOM enabled.
    /// </summary>
    /// <param name="content">The content to add to the article</param>
    /// <returns>A new node</returns>
    /// <remarks>
    /// This element has declarative shdow dom enabled, meaning
    /// you can use slots to project content into the shadow DOM.
    /// </remarks>
    member inline sArticle: content: Node list -> Node
    /// <summary>
    /// Creates a new blockquote element with declarative shadow DOM enabled.
    /// </summary>
    /// <param name="content">The content to add to the blockquote</param>
    /// <returns>A new node</returns>
    /// <remarks>
    /// This element has declarative shdow dom enabled, meaning
    /// you can use slots to project content into the shadow DOM.
    /// </remarks>
    member inline sAside: content: Node list -> Node
    /// <summary>
    /// Creates a new blockquote element with declarative shadow DOM enabled.
    /// </summary>
    /// <param name="content">The content to add to the blockquote</param>
    /// <returns>A new node</returns>
    /// <remarks>
    /// This element has declarative shdow dom enabled, meaning
    /// you can use slots to project content into the shadow DOM.
    /// </remarks>
    member inline sBlockquote: content: Node list -> Node
    /// <summary>
    /// Creates a new body element with declarative shadow DOM enabled.
    /// </summary>
    /// <param name="content">The content to add to the body</param>
    /// <returns>A new node</returns>
    /// <remarks>
    /// This element has declarative shdow dom enabled, meaning
    /// you can use slots to project content into the shadow DOM.
    /// </remarks>
    member inline sBody: content: Node list -> Node
    /// <summary>
    /// Creates a new div element with declarative shadow DOM enabled.
    /// </summary>
    /// <param name="content">The content to add to the div</param>
    /// <returns>A new node</returns>
    /// <remarks>
    /// This element has declarative shdow dom enabled, meaning
    /// you can use slots to project content into the shadow DOM.
    /// </remarks>
    member inline sDiv: content: Node list -> Node
    /// <summary>
    /// Creates a new footer element with declarative shadow DOM enabled.
    /// </summary>
    /// <param name="content">The content to add to the footer</param>
    /// <returns>A new node</returns>
    /// <remarks>
    /// This element has declarative shdow dom enabled, meaning
    /// you can use slots to project content into the shadow DOM.
    /// </remarks>
    member inline sFooter: content: Node list -> Node
    /// <summary>
    /// Creates a new h1 element with declarative shadow DOM enabled.
    /// </summary>
    /// <param name="content">The content to add to the h1</param>
    /// <returns>A new node</returns>
    /// <remarks>
    /// This element has declarative shdow dom enabled, meaning
    /// you can use slots to project content into the shadow DOM.
    /// </remarks>
    member inline sH1: content: Node list -> Node
    /// <summary>
    /// Creates a new h2 element with declarative shadow DOM enabled.
    /// </summary>
    /// <param name="content">The content to add to the h2</param>
    /// <returns>A new node</returns>
    /// <remarks>
    /// This element has declarative shdow dom enabled, meaning
    /// you can use slots to project content into the shadow DOM.
    /// </remarks>
    member inline sH2: content: Node list -> Node
    /// <summary>
    /// Creates a new h3 element with declarative shadow DOM enabled.
    /// </summary>
    /// <param name="content">The content to add to the h3</param>
    /// <returns>A new node</returns>
    /// <remarks>
    /// This element has declarative shdow dom enabled, meaning
    /// you can use slots to project content into the shadow DOM.
    /// </remarks>
    member inline sH3: content: Node list -> Node
    /// <summary>
    /// Creates a new h4 element with declarative shadow DOM enabled.
    /// </summary>
    /// <param name="content">The content to add to the h4</param>
    /// <returns>A new node</returns>
    /// <remarks>
    /// This element has declarative shdow dom enabled, meaning
    /// you can use slots to project content into the shadow DOM.
    /// </remarks>
    member inline sH4: content: Node list -> Node
    /// <summary>
    /// Creates a new h5 element with declarative shadow DOM enabled.
    /// </summary>
    /// <param name="content">The content to add to the h5</param>
    /// <returns>A new node</returns>
    /// <remarks>
    /// This element has declarative shdow dom enabled, meaning
    /// you can use slots to project content into the shadow DOM.
    /// </remarks>
    member inline sH5: content: Node list -> Node
    /// <summary>
    /// Creates a new h6 element with declarative shadow DOM enabled.
    /// </summary>
    /// <param name="content">The content to add to the h6</param>
    /// <returns>A new node</returns>
    /// <remarks>
    /// This element has declarative shdow dom enabled, meaning
    /// you can use slots to project content into the shadow DOM.
    /// </remarks>
    member inline sH6: content: Node list -> Node
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
    member inline sHeader: content: Node list -> Node
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
    member inline sMain: content: Node list -> Node
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
    member inline sNav: content: Node list -> Node
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
    member inline sP: content: Node list -> Node
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
    member inline sSection: content: Node list -> Node
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
    member inline sSpan: content: Node list -> Node

  /// <summary>
  /// Contains the HTML DSL, you can grab the standard HTML elements from here.
  /// </summary>
  /// <example>
  /// <code lang="fsharp">
  /// open Hox.Feliz.Engine
  ///
  /// H.div [
  ///    H.h1 [ H.text "Hello World" ]
  /// ]
  /// </code>
  /// </example>
  val H: HtmlEngine<Node>

  /// <summary>
  /// Contains the attribute DSL, you can grab the standard HTML attributes from here.
  /// </summary>
  /// <example>
  /// <code lang="fsharp">
  /// open Hox.Feliz.Engine
  ///
  /// H.div [
  ///   H.h1 [ H.text "Hello World" ]
  ///   |> Attr.set (A.className "title")
  ///   H.p [ H.text "This is a paragraph" ]
  ///   |> Attr.set (A.className "subtitle")
  /// ]
  /// |> Attr.set (A.id "my-div")
  /// |> Attr.set (A.classes ["my-class"; "my-other-class"])
  /// </code>
  /// </example>
  /// <remarks>
  /// You have to use the `Attr` module to compensate for the fact that
  /// attributes are not part of the `Node` type.
  /// </remarks>
  val A: AttrEngine<HAttribute>

  /// Attribute helpers, we have to provide these because our attributes are not
  /// part of the `Node` type, thus we can't include them in the DSL as usual
  module Attr =
    val set: attr: HAttribute -> node: Node -> Node
    val setTask: attr: Task<HAttribute> -> node: Node -> Node
    val setAsync: attr: Async<HAttribute> -> node: Node -> Node
