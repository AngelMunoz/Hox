module Hox.Markdown

open System
open System.IO

open Markdig

open IcedTasks

module private Pipeline =
  let pipeline =
    lazy
      (MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .UseYamlFrontMatter()
        .Build())

type Html =

  static member inline ofMarkdown
    (
      markdown: string,
      ?pipeline: MarkdownPipeline
    ) =
    Markdown.ToHtml(markdown, ?pipeline = pipeline)

  static member ofMarkdownFile(path: string) = cancellableTask {
    let! token = CancellableTask.getCancellationToken()

    if token.IsCancellationRequested then
      return String.Empty
    else
      let! markdown = File.ReadAllTextAsync(path, token)
      return Html.ofMarkdown(markdown, Pipeline.pipeline.Value)
  }
