#r "nuget: Fun.Result, 2.0.9"
#r "nuget: Fun.Build, 1.0.9"

open System.IO
open Fun.Build

let version = "1.0.0"


let build name = stage $"Build {name}" { run $"dotnet build src/{name}" }

let test shouldRebuild = stage "Test" {
  run(fun ctx ->
    let arg = if shouldRebuild then "--no-build" else ""
    ctx.RunCommand $"dotnet test {arg}")
}

let pack name = stage $"Pack {name}" {
  run $"dotnet pack src/{name} -p:Version={version} -o dist"
}

let pushNugets = stage $"Push to NuGet" {

  run(fun ctx -> async {

    let nugetApiKey = ctx.GetEnvVar "NUGET_DEPLOY_KEY"
    let nugets = Directory.GetFiles(__SOURCE_DIRECTORY__ + "/dist", "*.nupkg")

    for nuget in nugets do
      printfn "Pushing %s" nuget

      let! res =
        ctx.RunSensitiveCommand
          $"dotnet nuget push {nuget} --skip-duplicate  -s https://api.nuget.org/v3/index.json -k {nugetApiKey}"

      match res with
      | Ok _ -> return ()
      | Error err -> failwith err
  })
}

pipeline "nuget" {

  build "Hox"
  build "Hox.Feliz"
  test false
  pack "Hox"
  pack "Hox.Feliz"
  pushNugets
  runIfOnlySpecified true
}

pipeline "build" {

  build "Hox"
  build "Hox.Feliz"
  test false
  runIfOnlySpecified false
}

pipeline "nuget:local" {
  build "Hox"
  build "Hox.Feliz"
  test false
  pack "Hox"
  pack "Hox.Feliz"
  runIfOnlySpecified true
}

pipeline "build:docs" {
  stage "Build docs" {
    run $"dotnet run --project src/Hox.Docs -- --is-gh-pages"
  }

  runIfOnlySpecified true
}


tryPrintPipelineCommandHelp()
