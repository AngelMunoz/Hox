module Hox.Markdown

open System
open System.IO

open Markdig

open IcedTasks

type Html =

  static member inline ofMarkdown
    (
      markdown: string,
      ?pipeline: MarkdownPipeline
    ) =
    Markdown.ToHtml(markdown, ?pipeline = pipeline)

  static member inline ofMarkdownFile
    (
      path: string,
      ?pipeline: MarkdownPipeline
    ) =
    cancellableTask {
      let! token = CancellableTask.getCancellationToken()

      if token.IsCancellationRequested then
        return String.Empty
      else
        let! markdown = File.ReadAllTextAsync(path, token)
        return Html.ofMarkdown(markdown, ?pipeline = pipeline)
    }
