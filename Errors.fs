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

 
