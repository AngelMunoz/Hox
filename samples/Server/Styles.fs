namespace Server

open Htmelo.DSL

[<AutoOpen>]
module Extensions =
  let inline css (content: string) = content

module Styles =

  let inline toStyle (content: string) = El "style" |> Raw content

  let App =
    css
      """
:not(:defined) {
  visibility: hidden;
}
html {
  font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, Helvetica, Arial, sans-serif,
    "Apple Color Emoji", "Segoe UI Emoji", "Segoe UI Symbol";
  font-size: 16px;
  line-height: 1.5;
}
body {
  margin: 0;
  padding: 0;
  height: 100dvh;
  overflow-y: auto;
}
        """
    |> toStyle

  let Lists =
    css
      """
.todo-list {
  list-style: none;
  padding: 0;
  margin: 0;
}
.todo-list li {
  padding: 1em 0;
  margin: 0;
}
    """
    |> toStyle
