module Hox.Markdown

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
      let! markdown = File.ReadAllTextAsync(path, token)
      return Html.ofMarkdown(markdown, ?pipeline = pipeline)
    }
