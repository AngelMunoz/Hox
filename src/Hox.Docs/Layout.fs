namespace Hox.Docs


open Hox

type Layout =

  static member inline Default() =
    h(
      "html[lang=en]",
      h("head", h("title", "Hox Docs")),
      h("body", h("div", "Hello World!"))
    )

  static member inline Blog() =
    h(
      "html[lang=en]",
      h("head", h("title", "Hox Docs")),
      h("body", h("div", "Hello World!"))
    )

  static member inline Reference() =
    h(
      "html[lang=en]",
      h("head", h("title", "Hox Docs")),
      h("body", h("div", "Hello World!"))
    )
