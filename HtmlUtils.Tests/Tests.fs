namespace HtmlUtilsTests
open Microsoft.VisualStudio.TestTools.UnitTesting
open System.Net.Http
open Shoshin.HtmlUtils
open System.Collections.Generic
open HtmlAgilityPack



[<TestClass>]
type TestClass () =
    
    let idOfDocX = "ctl00_MainContent_AttachmentsRepeater_ctl00_ArtifactVersionRenderer_Repeater1_ctl00_ArtifactFormatTableRenderer1_RadGridNonHtml_ctl00_ctl04_hlPrimaryDoc"
    let testHtml = System.IO.File.ReadAllText("TestData/testHtml1.html")
    let testHtmlToChunk = System.IO.File.ReadAllText("TestData/testHtmlToChunk.html")
    
    let passed result =
       match result with 
       | Ok res -> true
       | Error e -> false

    let  XpathForDocxId = "//a[@id = '" +  idOfDocX + "']/@href"
    
    [<TestMethod>]
    member this.TestDocumentLoadSuccess () =
        
         let goodResult = Shoshin.HtmlUtils.HtmlParsing.getHtmlDoc(testHtml)
         Assert.IsTrue(passed goodResult)

    [<TestMethod>]
    member this.TestGetHrefValue () =
        let result = Shoshin.HtmlUtils.HtmlParsing.getAttributeValue XpathForDocxId "href" testHtml
        let linkMatches =
            match result with
            | Ok l -> l = "https://www.legislation.gov.au/Details/F2021C00349/0040fe4e-c964-498d-b282-bd37647b4cd3"
            | Error e -> false
        Assert.IsTrue(linkMatches)


    [<TestMethod>]
    member this.TestChunker () =
        let testList = [1;2;3;4;5;6]
        let testPredicate = fun i -> i % 2 = 1 // is odd
        let result = Shoshin.HtmlUtils.Chunking.chunk testList testPredicate
        let expected = [[1;2];[3;4];[5;6]]
        let passed = result = expected
        Assert.IsTrue(passed)

    [<TestMethod>]
    member this.TestHtmlChunker () =
        let testHtml = """
            <html>
            <body>
            <h1>h1</h1>
            <p>p1</p>
            <p>p2</p>
            <h2>h2-1</h2>
            <p>p3</p>
            <p>p4</p>
            <h2>h2-2</h2>
            <p>p5</p>
            </body>
            </html>
        """

        let htmlDoc = new HtmlDocument();
        htmlDoc.LoadHtml(testHtml) |> ignore
        let bodyNode = htmlDoc.DocumentNode.SelectSingleNode("//body")
        let whiteFilter = fun (node: HtmlNode) -> node.Name = "p" || node.Name.StartsWith("h")
        let items = bodyNode.Descendants() |> Seq.toList |> List.filter(whiteFilter)
        let result = Shoshin.HtmlUtils.Chunking.chunkHtmlByHeadingsH1andH2 items
        let getInnerText (nodeList : HtmlNode list) = nodeList |> List.map(fun n -> n.InnerText) |> String.concat ""
        let result = result |> List.map(getInnerText) |> String.concat ""
        let expected = "h1p1p2h2-1p3p4h2-2p5"
        let passed = result = expected
        Assert.IsTrue(passed)
            
    [<TestMethod>]
    member this.TestUlListFormatter() =
        let testHtml = """
        <ol>
        <li>1</li>
        <li>2</li>
        <li>3
            <ul>
                <li>3.1</li>
                <li>3.2</li>
            </ul>
        </li>
        <li>4</li>
        </ol>
        """
        
        let testNode = HtmlNode.CreateNode(testHtml);
        let result = Shoshin.HtmlUtils.Chunking.formatListAsPlainText testNode
        let expected = ""
        let passed = result = expected
        Assert.IsTrue(passed)

    [<TestMethod>]
    member this.TestChunkingOnRealData() = 
        // first select the main body of text from the webpage, excluding nav bars, side bars etc
        // it is the div with id 'main-wrapper'
        let htmlDoc = new HtmlDocument()
        htmlDoc.LoadHtml(testHtmlToChunk) |> ignore
        let xPath = "//main//*[self::h1 or self::h2 or self::h3[not(parent::div[@class='toc toc-tree'])] or self::h4 or self::h5 or self::h6 or self::p or self::ul or self::ol[not(parent::div[@class='toc toc-tree'])]  ]";
        let contentNodes = htmlDoc.DocumentNode.SelectNodes(xPath) |> Seq.toList
        let result = Shoshin.HtmlUtils.Chunking.chunkHtmlByHeadingsH1andH2 contentNodes 
        let prettyPrintChunks (chunks: HtmlNode list list) =
            chunks |> List.map(fun chunk -> chunk |> List.map(fun node -> Chunking.formatElementAsPlainText node) |> String.concat System.Environment.NewLine) |> String.concat $"{System.Environment.NewLine}{System.Environment.NewLine}"
        printfn "%s" (prettyPrintChunks result)
        
        Assert.IsNotNull(result)
        
        


