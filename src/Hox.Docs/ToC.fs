module Hox.Docs.Toc


open System
open System.IO
open System.Text.Json


open FSharp.Control
open IcedTasks
open Hox.Markdown

open JDeck

module Decoders =

  let dateOnly: Decoder<DateOnly> =
    Required.dateTime >> Result.map DateOnly.FromDateTime

  let dateOnlyExact(format: string) : Decoder<DateOnly> =
    Required.dateTimeExact format >> Result.map DateOnly.FromDateTime

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
    fun metadata -> decode {
      let! title =
        Decode.Required.Property.get ("title", Required.string) metadata

      and! file =
        Decode.Required.Property.get ("file", Required.string) metadata

      and! author =
        Decode.Required.Property.get ("author", Required.string) metadata

      and! summary =
        Decode.Required.Property.get ("summary", Required.string) metadata

      and! contributors =
        Decode.Required.Property.list ("contributors", Required.string) metadata

      and! updated =
        Decode.Optional.Property.get ("updated", Decoders.dateOnly) metadata

      and! category =
        Decode.Optional.Property.get ("category", Required.string) metadata

      return {
        title = title
        file = file
        author = author
        contributors = contributors
        summary = summary
        updated = updated
        category = category
      }
    }

let getMetadata tocPath = cancellableTask {
  use content = File.OpenRead tocPath

  match!
    Decoding.fromStream(
      content,
      Decode.array(fun _ el -> EntryMetadata.Decode el)
    )
  with
  | Ok entries -> return entries
  | Error err -> return failwith $"Error decoding metadata: %A{err}"
}

type ToC =
  static member GetContent(metadata) = async {
    let operations =
      metadata
      |> Array.map(fun entry -> async {
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

        let! content = Html.ofMarkdownFile $"markdown/{entry.file}"

        return entry, content
      })

    return! Async.Parallel operations
  }
