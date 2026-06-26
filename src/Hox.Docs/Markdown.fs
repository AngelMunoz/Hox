module Hox.Markdown

open System
open System.IO

open Markdig

open IcedTasks

module Html =
  let private pipeline =
    lazy
      (MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .UsePreciseSourceLocation()
        .Build())

  let ofMarkdownFile(path: string) = cancellableTask {
    let! token = CancellableTask.getCancellationToken()

    if token.IsCancellationRequested then
      return String.Empty
    else
      let! markdown = File.ReadAllTextAsync(path, token)
      return Markdown.ToHtml(markdown, pipeline = pipeline.Value)
  }
