namespace HtmlUtilsTests
open Microsoft.VisualStudio.TestTools.UnitTesting
open System.Net.Http
open Shoshin.HtmlUtils



[<TestClass>]
type TestClass () =
    
    let idOfDocX = "ctl00_MainContent_AttachmentsRepeater_ctl00_ArtifactVersionRenderer_Repeater1_ctl00_ArtifactFormatTableRenderer1_RadGridNonHtml_ctl00_ctl04_hlPrimaryDoc"
    let testHtml = System.IO.File.ReadAllText("TestData/testHtml1.html")

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