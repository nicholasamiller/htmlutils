namespace Shoshin.HtmlUtils

open Azure.Data.Tables
open Azure.Storage.Blobs
open Errors

module Storage =
    
    // create an azure storage container if it doesn't exist, async
    let createContainerIfNotExistsAsync (containerName : string) (blobServiceClient : BlobServiceClient) : Async<Result<Azure.Response<bool>,StorageError>> = async {
        let containerClient = blobServiceClient.GetBlobContainerClient(containerName)
        try
            let! existsResponse = containerClient.ExistsAsync() |> Async.AwaitTask
            let existsResponseHasValue = existsResponse.HasValue;
            return 
                match existsResponseHasValue with
                | false -> Error(StorageError.AzureResponseHasNoValue)
                | true -> 
                    let e = existsResponse
                    match e.Value with
                    | true -> Ok (e)
                    | false -> Error(StorageError.FailedToCheckIfContainerExists(containerName))
        with
            | ex -> return Error(StorageError.Exception(ex))
        }
            
    


