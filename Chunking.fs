namespace Shoshin.HtmlUtils

open HtmlAgilityPack
open System.Text

module Chunking = 
    
    type HeadingLevel =
    | H1
    | H2
    | H3
    | H4
    | H5
    | H6

    let getNextHeadingLevel (currentLevel: HeadingLevel) =
        match currentLevel with
        | H1 -> H2
        | H2 -> H3
        | H3 -> H4
        | H4 -> H5
        | H5 -> H6
        | H6 -> H6      
    
    let getHeadingLevelFromElementName (elementName : string) : HeadingLevel option =
        match elementName with
        | "h1" -> Some H1
        | "h2" -> Some H2
        | "h3" -> Some H3
        | "h4" -> Some H4
        | "h5" -> Some H5
        | "h6" -> Some H6
        | _ -> None

    type Chunk = {
        Heading: string;
        Content: string;
        HeadingLevel: HeadingLevel;
    }

    let chunk (items: 'a list) (isChunkStart: 'a -> bool) : 'a list list =
        let folder (chunksAccumulator: 'a list list, currentChunk: 'a list) (item: 'a) =
            if isChunkStart item then
                // last chunk gets prepeneded, start a new current chunk list with item
                (currentChunk :: chunksAccumulator, [item])
            else
                // part way through chunk
                (chunksAccumulator, item :: currentChunk)
                // but this does not get added to the accumulator until the next item is processed
        let (chunkedItems, lastChunk) = List.fold folder ([], []) items
        let allChunks = lastChunk :: chunkedItems
        List.rev allChunks |> List.map List.rev |> List.skip 1

    // first step is to split to chunks
    // then we will convert the chunks to a tree


    let chunkHtmlByHeadingsH1andH2 (items : HtmlNode list) =
        let isChunkStart (item : HtmlNode) =
            match getHeadingLevelFromElementName item.Name with
            | Some(H1)  -> true
            | Some(H2)  -> true
            | _ -> false
        chunk items isChunkStart
    
    let extractText (node: HtmlNode) =
        match node.NodeType with
        | HtmlNodeType.Element ->
            match node.Name.ToLower() with
            | "p" | "h1" | "h2" | "h3" | "h4" | "h5" | "h6" -> Some node.InnerText
            | "table" -> Some node.OuterHtml
            | _ -> None
        | _ -> None
    

    let formatTableAsPlainText (tableNode: HtmlNode) : string =
        let stringBuilder = StringBuilder()

        for row in tableNode.SelectNodes(".//tr") do
            let cells = row.SelectNodes(".//td|.//th")
            if cells <> null then
                for cell in cells do
                    stringBuilder.Append(cell.InnerText.Trim()) |> ignore
                    stringBuilder.Append("\t") |> ignore
                stringBuilder.AppendLine() |> ignore
        stringBuilder.ToString()


    type ListType = 
        | Ordered
        | Unordered

    let formatListAsPlainText (listNode : HtmlNode) : string =
        
        let formatItem (liNode: HtmlNode) (listType : ListType) (index: int)  (level : int) =
            let spacerFromLeft = String.replicate level "\t"
            match listType with
            | Ordered -> $"{spacerFromLeft}{index.ToString()}. {liNode.InnerText}" 
            | Unordered -> $"{spacerFromLeft}- {liNode.InnerText}"

        let sb = StringBuilder()
        let rec build (listNode : HtmlNode) (level : int) =
            let listType = if listNode.Name = "ol" then Ordered else Unordered
            let items = listNode.ChildNodes
            let mutable index = 1
            for item in items do
                match item with
                | item when item.Name = "li" -> 
                    let line = formatItem item listType index level
                    sb.AppendLine(line) |> ignore
                    index <- index + 1
                | item when item.Name = "ol" || item.Name = "ul" ->
                    build item (level + 1)
                | _ -> ()
        build listNode 1 |> ignore
        sb.ToString()
    
    let formatElementAsPlainText (node: HtmlNode) : string = 
        match node.Name.ToLower() with
        | "table" -> formatTableAsPlainText node
        | "ol" | "ul" -> formatListAsPlainText node
        | _ -> node.InnerText
                

            
    //type Tree<'a> = Node of 'a * Tree<'a> list

    // assume the first item is root
    
    //let convertListToTree (items: 'a list) (getLevel: 'a -> int option) : Tree<'a> =
    //    let root = Node[items.Head, []]
    //    let startLevel = Some(1)
    //    let rec convertItems (items.Tail) (getLevel: 'a -> int option) (treeAcc : Tree<'a>) (currentLevel: int option) (currentNode: Node) =
    //         get the level of the item
    //         if it none, it means item should be part of the current node
    //         so prepend it to the current node content
    //         if it is some, it means it is a new node
    //         if it's a sibling, get the parent of the 
            

