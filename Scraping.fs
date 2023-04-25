namespace Shoshin.HtmlUtils

open System.Net.Http
open System
open System.Net
open Errors
open System.IO
open System
open System.IO
open System.Net.Http
open System.Threading.Tasks
open System.Threading
open FSharp.Data
open Azure.Data.Tables
open Azure.Storage.Blobs
open DrupalMetadataExtraction

module Scraping =
    
    let getBaseUri (uri : Uri)  = sprintf "%s://%s" uri.Scheme uri.Host
    
    let createFetcher (httpClient: HttpClient) =
        fun (url: Uri) -> async {
            try
                let! response = httpClient.GetAsync(url.ToString()) |> Async.AwaitTask
                match response with
                | r when r.StatusCode = HttpStatusCode.Redirect -> 
                    let target = r.Headers.Location
                    let! targetResponse = httpClient.GetAsync(getBaseUri(url).ToString() + target.ToString()) |> Async.AwaitTask
                    let! targetStream = targetResponse.Content.ReadAsStreamAsync() |> Async.AwaitTask
                    return Ok targetStream
                | r when not (r.StatusCode = HttpStatusCode.OK) -> return Error(ScrapeError.UnexpectedHttpStatusCode(r.StatusCode))
                | _ -> 
                    let! contentStream = response.Content.ReadAsStreamAsync() |> Async.AwaitTask
                    return Ok contentStream
            with
            | ex -> return Error(ScrapeError.Exception(ex))
        }
    
    
    let readStreamAsHtml (stream : Stream) : Result<string,ScrapeError> = 
         use sr = new StreamReader(stream,true)
         try
            let text = sr.ReadToEnd()
            Ok text
         with
         | ex -> Error(ScrapeError.Exception(ex))
   
   
    let scrapeDrupalContent(rawHtml : string) : Result<Domain.DrupalContentDocument,ScrapeError> =
        let htmlDocResult = HtmlParsing.getHtmlDoc rawHtml
        // get chunks
        let getChunksResult (htmlDoc : HtmlAgilityPack.HtmlDocument) = 
            let contentSelector = "//main//*[self::h1 or self::h2 or self::h3[not(parent::div[@class='toc toc-tree'])] or self::h4 or self::h5 or self::h6 or self::p or self::ul or self::ol[not(parent::div[@class='toc toc-tree'])]  ]";
            let contentNodesResult =  HtmlParsing.getMultipleDocumentNodes htmlDoc contentSelector |> Result.map List.ofSeq
            let elementLists = contentNodesResult |> Result.map (fun nodeList -> Chunking.chunkHtmlByHeadingsH1andH2 nodeList)
            let chunks = elementLists |> Result.map (fun elementList -> elementList |> List.map (fun l -> Chunking.parseNodeListToChunk l))
            chunks
        let chunks = htmlDocResult |> Result.bind getChunksResult
        let metadata = htmlDocResult |> Result.map (fun d -> DrupalMetadataExtraction.extractDrupalMetadata d)
        match (chunks,metadata) with
        | (Ok chunks,Ok metadata) -> Ok {Chunks = chunks; Metadata = metadata}
        | (Ok _,Error e) -> Error(ScrapeError.HtmlParseError(e))
        | (Error e,Ok _) -> Error(ScrapeError.HtmlParseError(e))
        | (Error e1,Error e2) -> Error(ScrapeError.CompositeScrapeError([ScrapeError.HtmlParseError(e1);ScrapeError.HtmlParseError(e2)]))
    
