namespace Shoshin.HtmlUtils

open HtmlAgilityPack

module MetadataExtraction =

    type Breadcrumbs = {RootToLeaf:  string list}

    let extractLiBreadcrumbs (htmlDoc : HtmlDocument) (olSelectorXPath : string) : Breadcrumbs option =
        let olNode = htmlDoc.DocumentNode.SelectSingleNode(olSelectorXPath)
        match olNode with
        | null -> None
        | n -> 
            let liNodes = n.SelectNodes(".//li")
            let breadcrumbs = liNodes |> Seq.map (fun x -> x.InnerText.Trim()) |> Seq.toList
            Some { RootToLeaf = breadcrumbs }
            
    

