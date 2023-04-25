namespace Shoshin.HtmlUtils

open HtmlAgilityPack
open Domain
open System.Text

module Chunking = 
       

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
    
        
    let chunkHtmlByHeadingsH1andH2 (items : HtmlNode list) =
        let isChunkStart (item : HtmlNode) =
            match getHeadingLevelFromElementName item.Name with
            | Some(H1)  -> true
            | Some(H2)  -> true
            | _ -> false
        chunk items isChunkStart
    
   



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

    let isListNode (node: HtmlNode) =
        match node.Name.ToLower() with
        | "ol" | "ul" -> true
        | _ -> false

    let formatLiNode(liNode: HtmlNode) =
        // get the text head element and the text tail
        let children = liNode.ChildNodes
        let headText = children |> Seq.takeWhile (fun node -> not (isListNode node)) |> Seq.map (fun n -> n.InnerText.Trim()) |> String.concat $"{System.Environment.NewLine}"
        headText

    let formatListNode (listNode: HtmlNode) (level: int) =
        let padding = String.replicate level "\t"
        // ul or ol cannot have text directly inside them, so we need to get the li nodes
        let liNodes = listNode.ChildNodes |> List.ofSeq |>  List.filter (fun node -> node.Name.ToLower() = "li") 
        let liText = liNodes |> List.map formatLiNode |> List.mapi  (fun idx text  -> $"{padding}{(idx+1).ToString()}. {text}") 
        let text = liText |> String.concat $"{System.Environment.NewLine}"
        text
    
 
    let formatElementAsPlainText (node: HtmlNode) : string = 
        match node.Name.ToLower() with
        | "table" -> formatTableAsPlainText node
        | "li" -> formatLiNode node
        | "ol" | "ul" -> failwith "not implemented"
        | "p" -> node.InnerText + System.Environment.NewLine
        | _ -> node.InnerText         
 
    let parseNodeListToChunk (nodes : HtmlNode list) : Chunk =
        let heading = nodes.Head 
        let headingLevel = getHeadingLevelFromElementName heading.Name |> Option.get
        let content = nodes |> List.tail |> List.map formatElementAsPlainText |> String.concat System.Environment.NewLine
        { Heading = heading.InnerText; Content = content; HeadingLevel = headingLevel }
                 
    
            
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
            

