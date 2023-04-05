namespace Shoshin.HtmlUtils

open System.Net.Http
open System
open System.Net
open Errors
open System.IO

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
    
    



