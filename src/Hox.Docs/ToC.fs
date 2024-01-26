module Hox.Docs.Toc


open System
open System.IO

open Thoth.Json.Net

open FSharp.Control
open IcedTasks
open Hox.Markdown
open System.Threading
open System.Collections.Generic

module Decoders =

  let dateOnly: Decoder<DateOnly> =
    fun path value ->
      if Decode.Helpers.isString value then
        let unboxedval = unbox<string> value

        match DateOnly.TryParse(unboxedval) with
        | true, date -> Ok date
        | false, _ -> Error(path, BadPrimitive("DateOnly", value))
      else
        Error(path, BadPrimitive("DateOnly", value))

  let dateOnlyExact(format: string) : Decoder<DateOnly> =
    fun path value ->
      if Decode.Helpers.isString value then
        let unboxedval = unbox<string> value

        match DateOnly.TryParseExact(unboxedval, format) with
        | true, date -> Ok date
        | false, _ -> Error(path, BadPrimitive("DateOnly", value))
      else
        Error(path, BadPrimitive("DateOnly", value))

type EntryMetadata = {
  title: string
  file: string
  author: string
  contributors: string list
  summary: string
  updated: DateOnly option
  category: string option
}

type EntryMetadata with

  static member Decode: Decoder<EntryMetadata> =
    Decode.object(fun ob -> {
      title = ob.Required.Field "title" Decode.string
      file = ob.Required.Field "file" Decode.string
      author = ob.Required.Field "author" Decode.string
      contributors =
        ob.Required.Field "contributors" (Decode.list Decode.string)
      summary = ob.Required.Field "summary" Decode.string
      updated = ob.Optional.Field "updated" Decoders.dateOnly
      category = ob.Optional.Field "category" Decode.string
    })

let getMetadata = coldTask {
  let! content = File.ReadAllTextAsync("markdown/toc.json")

  match Decode.fromString (Decode.list(EntryMetadata.Decode)) content with
  | Ok entries -> return entries
  | Error err -> return failwith $"Error decoding metadata: %A{err}"
}

type ToC =
  static member getContent = cancellableTask {
    let! token = CancellableTask.getCancellationToken()
    let! metadata = getMetadata()

    let operations =
      metadata
      |> List.map(fun entry -> async {
        let created = File.GetCreationTime($"markdown/{entry.file}").Date
        let updated = File.GetLastWriteTime($"markdown/{entry.file}").Date

        let entry =
          if created = updated then
            entry
          else
            {
              entry with
                  updated =
                    File.GetLastWriteTime($"markdown/{entry.file}").Date
                    |> DateOnly.FromDateTime
                    |> Some
            }

        let! content = Html.ofMarkdownFile($"markdown/{entry.file}")

        return entry, content
      })

    if token.IsCancellationRequested then
      return Array.empty
    else
      return! Async.Parallel(operations)
  }
