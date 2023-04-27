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

    let formatLiNode(liNode: HtmlNode) (level : int) (listMarker : string) =
        let listMarkerPadding = String.replicate level "\t" 
        let contentPadding = listMarkerPadding + "\t"
        // get the text head element and the text tail
        let children = liNode.ChildNodes |> List.ofSeq
        let headText = children |> List.takeWhile (fun node -> not (isListNode node)) |> List.map (fun n -> $"{contentPadding}{n.InnerText.Trim()}") |> String.concat $"{System.Environment.NewLine}"
        
        let forTheWin = $"{listMarkerPadding}{listMarker}\t{headText.TrimStart('\t')}"
        forTheWin


    let formatList (node: HtmlNode) =
        
        let getLmProvider (node : HtmlNode) =
            match node.Name with
            | "ul" -> fun i -> "-"
            | "ol" -> fun i -> $"{i+1}."
            | _ -> failwith "Cannot get a list marker for nodes other than ul and ol."
                   
        let rec formatListRec(node: HtmlNode) (level : int) (index : int) (listMarkerProvider :  int -> string) : string =
            
            match node.Name with
            | "li" -> 
                let liText = formatLiNode node level (listMarkerProvider index)
                let children = node.ChildNodes |> List.ofSeq |> List.filter isListNode
                match children with
                | [] -> liText  
                | _ -> 
                    let childrenText = children |> List.mapi (fun idx n  -> formatListRec n (level + 1) idx listMarkerProvider) |> String.concat $"{System.Environment.NewLine}"
                    liText + System.Environment.NewLine + childrenText
            | "ul" | "ol" ->
                let children = node.ChildNodes |> List.ofSeq |> List.filter (fun i -> i.Name = "li") 
                let lmProvider = getLmProvider node
                children |> List.ofSeq |> List.mapi (fun idx li -> formatListRec li (level + 1) idx lmProvider) |> String.concat $"{System.Environment.NewLine}"
            | _ -> ""
        
        formatListRec node 0 1 (getLmProvider(node))

       
  

      
 
    let formatElementAsPlainText (node: HtmlNode) : string = 
        match node.Name.ToLower() with
        | "table" -> formatTableAsPlainText node
        | "ol" | "ul" -> formatList node 
        | "p" | "#text" -> node.InnerText + System.Environment.NewLine
        | _ -> node.InnerText.Trim()
 
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
            

