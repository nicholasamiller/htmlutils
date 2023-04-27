open System
open System.IO
open System.Text.RegularExpressions
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open Newtonsoft.Json

type Config() =
    member val Root = "" with get, set
    member val Output = "" with get, set
    member val Errors = "" with get, set
    member val DirectoryIgnore : string seq  = [] with get, set
    member val FileIgnore : string seq = [] with get, set



let createHost args =
    Host.CreateDefaultBuilder(args).Build()

let readConfiguration() =
    ConfigurationBuilder()
        .AddJsonFile("appsettings.json", optional=true, reloadOnChange=true)
        .Build()

let bindConfiguration (config: IConfiguration) =
    let settings = new Config()
    config.Bind(settings)
    settings

let createDirectories (settings : Config) =
    Directory.CreateDirectory(settings.Output) |> ignore
    Directory.CreateDirectory(settings.Errors) |> ignore

let harvest argv =
    let host = createHost argv
    let config = readConfiguration()
    let settings = bindConfiguration config
    let directoriesToIgnore = 
        settings.DirectoryIgnore
        |> Seq.map (fun x -> DirectoryInfo(Path.Combine(settings.Root, x)))
        |> List.ofSeq
    let filesToIgnore = 
        settings.FileIgnore
        |> Seq.map (fun f -> Regex(f))
        |> List.ofSeq

    createDirectories settings

    let wd = Directory.GetCurrentDirectory()
    printfn "Working directory: %s" wd

    let rootContentDir = Path.Combine(wd, settings.Root)
    printfn "Root content dir: %s" rootContentDir
    printfn "Root dir exists: %b" (Directory.Exists rootContentDir)

    let isExcluded (fullFileName : string) =
        if directoriesToIgnore |>  List.exists (fun d -> fullFileName.StartsWith(d.FullName, StringComparison.OrdinalIgnoreCase)) then
            true
        elif filesToIgnore |> List.exists (fun regex -> regex.IsMatch (Path.GetFileName fullFileName)) then
            true
        else false

    let files =
        Directory.EnumerateFiles(settings.Root, "*.html", SearchOption.AllDirectories)
        |> Seq.filter (fun f -> not (isExcluded f))
        |> Seq.truncate 10
        |> Seq.toList

    printfn "Number of files to scrape: %d." (List.length files)

    let mutable remaining = List.length files

    for file in files do
        let fileText = File.ReadAllText(file)
        let documentResult = Shoshin.HtmlUtils.Scraping.scrapeDrupalContent fileText
        let guid = Guid.NewGuid().ToString("N")
        let outputFileName = Path.Combine(settings.Output, $"{Path.GetFileNameWithoutExtension(file)}.{guid}.json")
        match documentResult with
        | Ok document ->
            let docAsJson = JsonConvert.SerializeObject(document, Formatting.Indented)
            File.WriteAllText(outputFileName, docAsJson)
            remaining <- remaining - 1
            printfn "Success.  Wrote: %s.  Remaining: %d" outputFileName remaining
        | Error errorValue ->
            let errorFileName = Path.Combine(settings.Errors, $"{Path.GetFileNameWithoutExtension(file)}.{guid}.error.json")
            let errorAsJson = JsonConvert.SerializeObject(errorValue, Formatting.Indented)
            printfn "Error.  Wrote: %s." errorFileName
            File.WriteAllText(errorFileName, errorAsJson)

    host.RunAsync().Wait() 

[<EntryPoint>]
let main argv = harvest argv; 0    

