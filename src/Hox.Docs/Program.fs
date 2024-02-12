open System
open System.IO
open System.Threading

open Spectre.Console

open FSharp.Control
open IcedTasks

open Hox
open Hox.Rendering

open Hox.Docs
open Hox.Docs.Toc

[<EntryPoint>]
let Main argv =
  task {
    use cts = new CancellationTokenSource()

    let isGhPages =
      argv |> Array.exists(fun arg -> arg.StartsWith("--is-gh-pages"))

    AnsiConsole.Markup("[yellow]Generating documentation...[/]")

    Console.CancelKeyPress.Add(fun _ ->
      Console.WriteLine("Stopping...")
      cts.Cancel())

    let! toc = getMetadata |> Async.AwaitColdTask

    let table =
      Table()
        .AddColumns(
          "Title",
          "File",
          "Author",
          "Contributors",
          "Summary",
          "Updated",
          "Category"
        )

    let! entries = ToC.getContent cts.Token

    do!
      AnsiConsole
        .Status()
        .StartAsync(
          "Generating documentation...",
          fun ctx -> task {
            for (metadata, content) in entries do
              ctx.Status <- $"Preparing directory for {metadata.title}..."

              Directory.CreateDirectory(
                Path.Combine("docs", Path.GetDirectoryName(metadata.file))
              )
              |> ignore

              let layout = Layout.Default(toc, metadata, "/", raw content)

              let path =
                Path.Combine("docs", metadata.file.Replace(".md", ".html"))

              use writer =
                File.Open(
                  path,
                  FileMode.Create,
                  FileAccess.Write,
                  FileShare.Read
                )

              ctx.Status <- $"Rendering {metadata.title}..."
              do! Render.toStream(layout, writer, cts.Token)
          }
        )

    if isGhPages then
      File.WriteAllText(Path.Combine("docs", ".nojekyll"), String.Empty)
      |> ignore

    AnsiConsole.MarkupLine("[green]Documentation generated![/]")
    return 0
  }
  |> Async.AwaitTask
  |> Async.RunSynchronously
