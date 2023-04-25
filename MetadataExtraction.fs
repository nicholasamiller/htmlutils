namespace Shoshin.HtmlUtils

open HtmlAgilityPack
open Domain

module DrupalMetadataExtraction =
    
    let getMetaContentAttribute (propertyName: string) (doc: HtmlDocument) : string option =
        doc.DocumentNode.SelectSingleNode(sprintf "//meta[@property='%s']/@content" propertyName)
        |> Option.ofObj
        |> Option.bind (fun x -> x.Attributes.["content"].Value |> Option.ofObj)
    
    let extractLiBreadcrumbs (htmlDoc : HtmlDocument) (olSelectorXPath : string) : string list =
        let olNode = htmlDoc.DocumentNode.SelectSingleNode(olSelectorXPath)
        match olNode with
        | null -> []
        | n -> 
            let liNodes = n.SelectNodes(".//li")
            let breadcrumbs = liNodes |> Seq.map (fun x -> x.InnerText.Trim()) |> Seq.toList
            breadcrumbs

    let extractDrupalMetadata (doc: HtmlDocument) : Domain.DrupalPageMetadata =
        let url = getMetaContentAttribute "og:url" doc
        let title = getMetaContentAttribute "og:title" doc
        let breadcrumbs = extractLiBreadcrumbs doc "//ol[@class='breadcrumb']"
        { Url = url; Title = title; Breadcrumbs = breadcrumbs }
        
