namespace Shoshin.HtmlUtils
open HtmlAgilityPack
open System.Linq
open System.Net
open Errors
open System

module HtmlParsing =
 
    let getHtmlDoc (html: string) : Result<HtmlDocument,HtmlParseError> = 
           try 
               let htmlDoc = new HtmlDocument()
               htmlDoc.LoadHtml(html)
               Ok htmlDoc
           with
               | ex -> Error(HtmlParseError.Exception(ex))
    

    let getSingleDocumentNode (htmlDoc : HtmlDocument) (xPath: string) : Result<HtmlNode,HtmlParseError> =
        match htmlDoc.DocumentNode.SelectSingleNode(xPath) with
        | null -> Error(HtmlParseError.XPath({Html = htmlDoc.ParsedText; XPath = xPath}))
        | node -> Ok node
    
    let getMultipleDocumentNodes (htmlDoc : HtmlDocument) (xPath : string) : Result<seq<HtmlNode>,HtmlParseError> =
        match htmlDoc.DocumentNode.SelectNodes(xPath) with
        | null -> Error(HtmlParseError.XPath({Html = htmlDoc.ParsedText; XPath = xPath}))
        | nodes -> Ok(nodes)
    
    let getMultipleDocumentNodesOrEmpty (htmlDoc : HtmlDocument) (xPath : string) : seq<HtmlNode> =
        match htmlDoc.DocumentNode.SelectNodes(xPath) with
        | null -> Seq.empty
        | nodes -> nodes

    let getAttributeValueFromNode (node : HtmlNode) (attributeName : string) =
        match node.Attributes.Contains(attributeName) with
        | false -> Error(HtmlParseError.MissingAttribute({Node = node; Attribute = attributeName}))
        | true -> Ok node.Attributes.[attributeName].Value
    

    let getAttributeValue (nodeXPath : string) (attributeName: string) (html: string) =
        getHtmlDoc html
        |> Result.bind (fun r -> getSingleDocumentNode r nodeXPath)
        |> Result.bind (fun r -> getAttributeValueFromNode r attributeName)
    
    let getLinkNode (anchorId : string) (htmlDoc : HtmlDocument) =
        let xPath = "//a[@id = '" +  anchorId + "']"
        getSingleDocumentNode htmlDoc xPath

    let getLinkTarget (anchorId : string) (html : string) =
        getHtmlDoc html
        |> Result.bind (fun d -> getLinkNode anchorId d)
        |> Result.bind (fun n -> getAttributeValueFromNode n "href")
    
    type LinkInfo = {
        Target: string;
        Text: string;
    }

    let getLinkInfo (anchorId : string) (htmlDoc : HtmlDocument) =
        getLinkNode anchorId htmlDoc
        |> Result.bind (fun n -> 
            let target = getAttributeValueFromNode n "href"
            match target with
            | Ok(t) -> Ok({Target = t; Text = n.InnerText })
            | Error e ->  Error(HtmlParseError.MissingAttribute({Attribute = "href"; Node = n }))
        )
        
    let getAttributeValueFromHtmlDoc (nodeXPath : string) (attributeName: string) (htmlDoc: HtmlDocument) =
        getSingleDocumentNode htmlDoc nodeXPath |> Result.bind (fun n -> getAttributeValueFromNode n attributeName)
    
   
    type TableData = {
        Headings: string seq option;
        Rows: seq<seq<string>>;
    }

    let getDataFromHtmlTable(tableNode : HtmlNode)   =
        
        let rowToSeq(trNode: HtmlNode) : seq<string> = 
            let tdElements = trNode.Elements("td") |> Seq.cast<HtmlNode>
            match tdElements with
            | null -> Seq.empty
            | cells -> cells |> Seq.map (fun cell -> cell.InnerText)

        match tableNode with
        | t when  t.Name <> "table" -> Error(HtmlParseError.Message("Cannot parse table data from an html node that is not a table."))
        | t -> 
            let headerRowXPath = "./thead/th"
            let tableRowXPath = "./tbody/tr"
            let headerRowIfAny = 
                match tableNode.SelectNodes(headerRowXPath) with
                | null -> None
                | thCollection -> Some(thCollection.Elements() |> Seq.map (fun i -> i.InnerText) )
            let tableRowsIfAny =
                match tableNode.SelectNodes(tableRowXPath) with
                | null -> Seq.empty
                | trCollection -> trCollection |> Seq.cast<HtmlNode> |> Seq.map (fun row -> rowToSeq row)
            Ok({Headings = headerRowIfAny;  Rows = tableRowsIfAny })


    let getTableData (tableId: string) (htmlDoc: HtmlDocument) =
        getSingleDocumentNode htmlDoc $"//table[@id='{tableId}']" |> Result.bind (fun n -> getDataFromHtmlTable n)

        
    let getAllLinkTargetStringsFromPage (htmlDoc : HtmlDocument) (whiteSupremacyFilter : string -> bool) =
        let xPath = "//a"
        let anchorNodes = getMultipleDocumentNodesOrEmpty htmlDoc xPath
        let anchorNodesFiltered = anchorNodes |> Seq.filter (fun n -> whiteSupremacyFilter n.InnerText)
        let targetResults = anchorNodesFiltered |> Seq.map (fun n -> getAttributeValueFromNode n "href")
        let targets = targetResults |> Seq.choose (fun r -> match r with | Ok(t) -> Some t | Error _ -> None)
        targets

    let getAbsoluteUrl (baseUrl : string) (url : string) =
        try
            let isAboluteAlready = url.StartsWith("http")
            match isAboluteAlready with
            | true -> Ok(url)
            | false ->
                let baseUri = new Uri(baseUrl)
                let relativeUri = new Uri(url, UriKind.Relative)
                let absoluteUri = new Uri(baseUri, relativeUri)
                Ok(absoluteUri.AbsoluteUri)
        with 
        | ex ->  Error(ScrapeError.Exception(ex))
    



  