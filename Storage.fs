namespace Shoshin.HtmlUtils

open Azure.Data.Tables
open Azure.Storage.Blobs
open Errors

module AzureStorage =
    
    let getAzureResultAsync<'T> (azureStorageSdkCall : unit -> System.Threading.Tasks.Task<Azure.Response<'T>>)  = async {
        try
            let! response = azureStorageSdkCall() |> Async.AwaitTask
            match response with
            | r when r.HasValue = false -> return Error(StorageError.AzureResponseHasNoValue)
            | r when r.GetRawResponse().IsError -> return Error(StorageError.AzureResponseBadStatusCode(r.GetRawResponse()))
            | r -> return Ok(r)
        with
        | ex -> return Error(StorageError.Exception(ex))
     }
    
    let containerAlreadyExistsAsync (containerName: string) (connectionString : string) = async {
        let cc = new BlobContainerClient(connectionString,containerName);
        let azureStorageSdkCall = fun () -> cc.ExistsAsync()
        return! getAzureResultAsync azureStorageSdkCall
    }
    
    let createContainerAsync (containerName : string) (connectionString : string) = async {
        let cc = new BlobContainerClient(connectionString,containerName);
        let azureStorageSdkCall = fun () -> cc.CreateIfNotExistsAsync()
        return! getAzureResultAsync azureStorageSdkCall
    }
    
        
    let uploadBlobStringAsync (containerClient : BlobContainerClient ) (blobName: string) (blobContent: string) = async {
        let bd = System.BinaryData(blobContent)
        let blobClient = containerClient.GetBlobClient(blobName)
        let azureStorageSdkCall = fun () -> blobClient.UploadAsync(bd,overwrite = true)
        return! getAzureResultAsync azureStorageSdkCall    
    }
                
    let createTableServiceClient (connectionString : string) = 
        let tableServiceClient = new TableServiceClient(connectionString)
        tableServiceClient


    let createTableIfNotExistsAsync(tableName : string) (tableServiceClient : TableServiceClient) = async {
        let tableClient = tableServiceClient.GetTableClient(tableName)
        let azureStorageSdkCall = fun () -> tableClient.CreateIfNotExistsAsync()
        return! getAzureResultAsync azureStorageSdkCall
    }



    
        


