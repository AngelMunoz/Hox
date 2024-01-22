[<RequireQualifiedAccess>]
module Hox.Parsers

open Hox.Core

/// <summary>A function that takes a CSS-like selector and produces an Element from it</summary>
/// <example>
/// <code lang="fsharp">
/// let selector = "div#foo.bar"
/// // produces &lt;div id="foo" class="bar"&gt;&lt;/div&gt;
/// </code>
/// </example>
val selector: selector: string -> Element
