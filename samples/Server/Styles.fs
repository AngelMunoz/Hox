namespace Server

open Htmelo.DSL

[<AutoOpen>]
module Extensions =
  let inline css(content: string) = content

module Styles =

  let inline toStyle(content: string) = el("style", raw content)

  let App =
    css
      """
:not(:defined) > template[shadowrootmode] ~ * {
  display: none;
}
html {
  font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, Helvetica, Arial, sans-serif,
    "Apple Color Emoji", "Segoe UI Emoji", "Segoe UI Symbol";
  font-size: 16px;
  line-height: 1.5;
  color: green;
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

  let Card =
    css
      """
:host {
  display: flex;
  color: rebeccapurple;
  flex-direction: column;
  border-radius: 0.25rem;
  background: #fff;
  height: 100%;
  box-shadow: 0 0 0 1px rgba(0, 0, 0, 0.05), 0 1px 3px rgba(0, 0, 0, 0.15);
  margin: 0.5rem 0;
  padding: 1rem;
}
x-card-header {
  display: block;
  flex: 0 1;
  margin-bottom: auto;
  padding-bottom: 0.5rem;
  border-bottom: 1px solid #eee;
}
x-card-content {
  display: block;
  flex: 1 0;
}
x-card-footer {
  display: block;
  flex: 0 1;
  margin-top: auto;
  padding-top: 0.5rem;
  border-top: 1px solid #eee;
}
      """
    |> toStyle

  let CardHeader =
    css
      """
:host { display: block; }
    """
    |> toStyle

  let CardFooter =
    css
      """
:host { display: block; }
    """
    |> toStyle
