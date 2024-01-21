open System
open System.Text

open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.Hosting
open Microsoft.AspNetCore.Http

open Giraffe
open Giraffe.ViewEngine

open FSharp.Control

open Htmelo
open Htmelo.Core
open Htmelo.Rendering


let inline xmlAttrToHtmelo(attr: XmlAttribute) : AttributeNode =
  match attr with
  | KeyValue(key, value) -> Attribute { name = key; value = value }
  | Boolean key -> Attribute { name = key; value = "" }

let inline foldAttributes (node: Node) (attr: XmlAttribute) : Node =
  match attr with
  | KeyValue(key, value) ->
    NodeOps.addAttribute(node, Attribute { name = key; value = value })
  | Boolean key ->
    NodeOps.addAttribute(node, Attribute { name = key; value = "" })

let rec xmlNodeToHtmelo(fmNode: XmlNode) : Node =
  match fmNode with
  | ParentNode((tagName, attributes), children) ->
    attributes
    |> Array.fold
      foldAttributes
      (h(tagName, children |> List.map xmlNodeToHtmelo))
  | HtmlElements.Text text -> Core.Text text
  | VoidElement((tagName, attributes)) ->
    attributes |> Array.fold foldAttributes (h tagName)

let streamHtml(node: XmlNode) : HttpHandler =
  fun (next: HttpFunc) (ctx: HttpContext) -> task {
    let writer = ctx.Response.BodyWriter
    let token = ctx.RequestAborted
    let node = xmlNodeToHtmelo node

    for chunk in Chunked.render node token do
      let bytes = Encoding.UTF8.GetBytes(chunk)
      do! writer.WriteAsync(ReadOnlyMemory(bytes), token) |> ValueTask.ignore

      do! writer.FlushAsync(token) |> ValueTask.ignore

    return! earlyReturn ctx
  }

let streamHtmelo(node: Node) : HttpHandler =
  fun (next: HttpFunc) (ctx: HttpContext) -> task {
    let writer = ctx.Response.BodyWriter
    let token = ctx.RequestAborted

    for chunk in Chunked.render node token do
      let bytes = Encoding.UTF8.GetBytes(chunk)
      do! writer.WriteAsync(ReadOnlyMemory(bytes), token) |> ValueTask.ignore

      do! writer.FlushAsync(token) |> ValueTask.ignore

    return! earlyReturn ctx
  }



let GiraffeView =
  html [ _lang "en" ] [
    head [] [
      title [] [ encodedText "Hello world" ]
      link [
        _rel "stylesheet"
        _media "(prefers-color-scheme:light)"
        _href
          "https://cdn.jsdelivr.net/npm/@shoelace-style/shoelace@2.12.0/cdn/themes/light.css"
      ]
      link [
        _rel "stylesheet"
        _media "(prefers-color-scheme:dark)"
        attr "onload" "document.documentElement.classList.add('sl-theme-dark');"
        _href
          "https://cdn.jsdelivr.net/npm/@shoelace-style/shoelace@2.12.0/cdn/themes/dark.css"
      ]
    ]
    body [] [
      tag "sl-card" [] [
        encodedText
          "This is just a basic card. No image, no header, and no footer. Just your content."
      ]
      script [
        _src
          "https://cdn.jsdelivr.net/npm/@shoelace-style/shoelace@2.12.0/cdn/shoelace-autoloader.js"
        _type "module"
      ] []
    ]
  ]

let HtmeloView =
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

let webApp =
  choose [
    GET
    >=> choose [
      route "/" >=> (streamHtmelo HtmeloView)
      route "/giraffe" >=> (htmlView GiraffeView)
      route "/giraffe2" >=> (streamHtml GiraffeView)
    ]
  ]

let builder = WebApplication.CreateBuilder(Array.empty)

builder.Services.AddGiraffe() |> ignore

let app = builder.Build()

app.UseGiraffe(webApp) |> ignore

app.Run()
