namespace Server

open System
open Server.Styles
open Htmelo

type Scoped =
  static member inline card([<ParamArray>] content: Node array) =

    let tpl =
      el(
        "template[shadowrootmode=open]",
        Styles.Card,
        el "slot[name=header]",
        el "slot",
        el "slot[name=footer]"
      )

    el("x-card", tpl, fragment content)

  static member inline cardHeader([<ParamArray>] content: Node array) =
    let tpl = el("template[shadowrootmode=open]", el "slot")
    el("x-card-header[slot=header]", tpl, fragment content)

  static member inline cardContent([<ParamArray>] content: Node array) =
    let tpl = el("template[shadowrootmode=open]", el "slot")
    el("x-card-content", tpl, fragment content)

  static member inline cardFooter([<ParamArray>] content: Node array) =
    let tpl = el("template[shadowrootmode=open]", el "slot")
    el("x-card-footer[slot=footer]", tpl, fragment content)
