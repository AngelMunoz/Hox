namespace Server

open System

open Htmelo
open Htmelo.Core

open Server.Styles

type Scoped =
  static member inline card([<ParamArray>] content: Node array) =

    let tpl =
      h(
        "template[shadowrootmode=open]",
        Styles.Card,
        h "slot[name=header]",
        h "slot",
        h "slot[name=footer]"
      )

    h("x-card", tpl, fragment content)

  static member inline cardHeader([<ParamArray>] content: Node array) =
    let tpl = h("template[shadowrootmode=open]", h "slot")
    h("x-card-header[slot=header]", tpl, fragment content)

  static member inline cardContent([<ParamArray>] content: Node array) =
    let tpl = h("template[shadowrootmode=open]", h "slot")
    h("x-card-content", tpl, fragment content)

  static member inline cardFooter([<ParamArray>] content: Node array) =
    let tpl = h("template[shadowrootmode=open]", h "slot")
    h("x-card-footer[slot=footer]", tpl, fragment content)
