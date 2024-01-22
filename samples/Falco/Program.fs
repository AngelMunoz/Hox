open System
open FSharp.Control

open Falco
open Falco.Routing
open Falco.HostBuilder
open Falco.Markup

open Hox
open Hox.Core
open Hox.Rendering

let inline xmlAttrToHtmelo(attr: XmlAttribute) : AttributeNode =
  match attr with
  | KeyValueAttr(key, value) -> Attribute { name = key; value = value }
  | NonValueAttr key -> Attribute { name = key; value = "" }

let inline foldAttributes (node: Node) (attr: XmlAttribute) : Node =
  match attr with
  | KeyValueAttr(key, value) ->
    NodeOps.addAttribute(node, Attribute { name = key; value = value })
  | NonValueAttr key ->
    NodeOps.addAttribute(node, Attribute { name = key; value = "" })

let rec xmlNodeToHtmelo(fmNode: XmlNode) : Node =
  match fmNode with
  | ParentNode((tagName, attributes), children) ->
    attributes
    |> List.fold
      foldAttributes
      (h(tagName, children |> List.map xmlNodeToHtmelo))
  | TextNode text -> Text text
  | SelfClosingNode((tagName, attributes)) ->
    attributes |> List.fold foldAttributes (h tagName)

module Response =
  let ofHtmlStream(node: XmlNode) : HttpHandler =
    Response.withContentType "text/html; charset=utf-8"
    >> fun ctx -> task {
      do! ctx.Response.StartAsync(ctx.RequestAborted)

      do!
        Render.toStream(
          xmlNodeToHtmelo node,
          ctx.Response.Body,
          cancellationToken = ctx.RequestAborted
        )

      do! ctx.Response.CompleteAsync()
    }

  let ofHox(node: Node) : HttpHandler =
    Response.withContentType "text/html; charset=utf-8"
    >> fun ctx -> task {
      do! ctx.Response.StartAsync(ctx.RequestAborted)

      do!
        Render.toStream(
          node,
          ctx.Response.Body,
          cancellationToken = ctx.RequestAborted
        )

      do! ctx.Response.CompleteAsync()
    }

let FalcoView =
  Elem.html [ Attr.lang "en" ] [
    Elem.head [] [
      Elem.title [] [ Text.enc "Hello world" ]
      Elem.link [
        Attr.rel "stylesheet"
        Attr.media "(prefers-color-scheme:light)"
        Attr.href
          "https://cdn.jsdelivr.net/npm/@shoelace-style/shoelace@2.12.0/cdn/themes/light.css"
      ]
      Elem.link [
        Attr.rel "stylesheet"
        Attr.media "(prefers-color-scheme:dark)"
        Attr.onload "document.documentElement.classList.add('sl-theme-dark');"
        Attr.href
          "https://cdn.jsdelivr.net/npm/@shoelace-style/shoelace@2.12.0/cdn/themes/dark.css"
      ]
    ]
    Elem.body [] [

      Elem.create "sl-card" [] [
        Text.enc
          "This is just a basic card. No image, no header, and no footer. Just your content."
      ]
      Elem.script [
        Attr.src
          "https://cdn.jsdelivr.net/npm/@shoelace-style/shoelace@2.12.0/cdn/shoelace-autoloader.js"
        Attr.type' "module"
      ] []
    ]
  ]

let HoxView =
  h(
    "html[lang=en]",
    h(
      "head",
      h("title", text "Hello world"),
      h(
        "link[rel=stylesheet][media=(prefers-color-scheme:light)]
             [href=https://cdn.jsdelivr.net/npm/@shoelace-style/shoelace@2.12.0/cdn/themes/light.css]"
      ),
      h(
        "link[rel=stylesheet][media=(prefers-color-scheme:dark)]
             [onload=document.documentElement.classList.add('sl-theme-dark');]
             [href=https://cdn.jsdelivr.net/npm/@shoelace-style/shoelace@2.12.0/cdn/themes/dark.css]"
      )
    ),
    h(
      "body",
      h(
        "sl-card",
        text
          "This is just a basic card. No image, no header, and no footer. Just your content."
      ),
      h(
        "script[type=module][src=https://cdn.jsdelivr.net/npm/@shoelace-style/shoelace@2.12.0/cdn/shoelace-autoloader.js]"
      )
    )
  )

[<EntryPoint>]
let main args =
  webHost args {
    endpoints [
      get "/" (Response.ofHox HoxView)
      get "/stream-falco" (Response.ofHtmlStream FalcoView)
      get "/string-falco" (Response.ofHtml FalcoView)
    ]
  }

  0
