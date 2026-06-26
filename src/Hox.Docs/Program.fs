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
  asyncEx {
    use cts = new CancellationTokenSource()
    let outputDir = Directory.CreateDirectory "./docs"
    let sources = DirectoryInfo "./markdown"

    let isGhPages =
      argv |> Array.exists(fun arg -> arg.StartsWith "--is-gh-pages")

    AnsiConsole.MarkupLine "[yellow]Generating documentation...[/]"

    Console.CancelKeyPress.Add(fun _ ->
      Console.WriteLine "Stopping..."
      cts.Cancel())

    let! toc = getMetadata(Path.Combine(sources.FullName, "toc.json"))

    let table = Table().AddColumns("Title", "Author", "Updated", "File")

    let! entries = ToC.GetContent toc

    for entry, _ in entries do
      let updatedStr =
        match entry.updated with
        | Some date -> date.ToString()
        | None -> ""

      table.AddRow(entry.title, entry.author, updatedStr, entry.file) |> ignore

    AnsiConsole.Write table

    do!
      AnsiConsole
        .Status()
        .StartAsync(
          "Generating documentation...",
          fun ctx -> task {
            for metadata, content in entries do
              ctx.Status <- $"Preparing directory for {metadata.title}..."

              match Path.GetDirectoryName metadata.file with
              | null
              | "" -> ()
              | path -> outputDir.CreateSubdirectory path |> ignore

              let layout =
                Layout.Default(
                  toc,
                  metadata,
                  (if isGhPages then "/Hox/" else "/"),
                  raw content
                )

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
              do! Render.toStream(layout, writer, cancellationToken = cts.Token)
          }
        )

    AnsiConsole
      .Status()
      .Start(
        "Copy Static Assets",
        fun _ ->
          let assetsDir = Path.Combine(AppContext.BaseDirectory, "assets")
          let assets = DirectoryInfo assetsDir
          let files = assets.EnumerateFiles("*", SearchOption.AllDirectories)

          for file in files do
            let finalPath =
              Path.Combine(
                outputDir.FullName,
                "assets",
                file.FullName.Replace(assetsDir, "").TrimStart
                  Path.DirectorySeparatorChar
              )

            Path.GetDirectoryName finalPath
            |> nonNull
            |> Directory.CreateDirectory
            |> ignore

            file.CopyTo(finalPath, overwrite = true) |> ignore
      )

    use nojekyll =
      File.CreateText(Path.Combine(outputDir.FullName, ".nojekyll"))

    nojekyll.Write String.Empty
    nojekyll.Flush()
    AnsiConsole.MarkupLine "[green]Documentation generated![/]"
    return 0
  }
  |> Async.RunSynchronously
