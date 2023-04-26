namespace Shoshin.HtmlUtils

module Domain =
    
        
    type DrupalPageMetadata = {
        Url: string option;
        Title: string option;
        Breadcrumbs: string list
    }

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
    
    type DrupalContentDocument = {
        Metadata : DrupalPageMetadata
        Chunks: Chunk list;
    } 
        


