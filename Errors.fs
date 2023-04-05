namespace Shoshin.HtmlUtils

open HtmlAgilityPack
open System.Net

module Errors =

    type XPathFoundNoNodes = {Html : string; XPath : string }
    type MissingAttribute = {Node: HtmlNode; Attribute: string }

    type HtmlParseError =
       | XPath of XPathFoundNoNodes
       | MissingAttribute of MissingAttribute
       | Exception of System.Exception
       | Message of string
    
    type ScrapeError =
       | HtmlParseError of HtmlParseError
       | NotFound of string
       | UnexpectedHttpStatusCode of HttpStatusCode
       | Exception of System.Exception
       | Message of string
       | XPath of XPathFoundNoNodes
       | MissingAttribute of MissingAttribute
       | CompositeScrapeError of ScrapeError list

 
