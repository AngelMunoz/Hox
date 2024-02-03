namespace Server

open System
open Hox


module private TemplateDefinitions =
  let card =
    sh(
      "x-card",
      Styles.Card,
      h "slot[name=header]",
      h "slot",
      h "slot[name=footer]"
    )

  let cardHeader = sh("x-card-header[slot=header]", Styles.CardHeader, h "slot")

  let cardContent = sh("x-card-content", empty, h "slot")

  let cardFooter = sh("x-card-footer[slot=footer]", Styles.CardFooter, h "slot")

type Scoped =
  static member card([<ParamArray>] content: _ array) =
    content |> fragment |> TemplateDefinitions.card

  static member cardHeader([<ParamArray>] content: _ array) =
    content |> fragment |> TemplateDefinitions.cardHeader

  static member cardContent([<ParamArray>] content: _ array) =
    content |> fragment |> TemplateDefinitions.cardContent

  static member cardFooter([<ParamArray>] content: _ array) =
    content |> fragment |> TemplateDefinitions.cardFooter
