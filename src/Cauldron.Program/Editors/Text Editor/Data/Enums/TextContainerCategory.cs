using System.ComponentModel.DataAnnotations;

namespace StudioCore.Editors.TextEditor;

public enum TextContainerCategory
{
    [Display(Name = "None")] None,

    [Display(Name = "Common")] Common,

    // Languages
    [Display(Name = "English (US)")] English,
    [Display(Name = "French")] French,
    [Display(Name = "German")] German,
    [Display(Name = "Italian")] Italian,
    [Display(Name = "Japanese")] Japanese,
    [Display(Name = "Korean")] Korean,
    [Display(Name = "Polish")] Polish,
    [Display(Name = "Russian")] Russian,
    [Display(Name = "Spanish")] Spanish,
    [Display(Name = "Traditional Chinese")] TraditionalChinese,

    [Display(Name = "Spanish - Neutral")] SpanishNeutral,
    [Display(Name = "Portuguese")] Portuguese,

    // These are more like region codes than language codes, but some paths only have this region code
    // While others have an additional language code below
    [Display(Name = "Asia")] Asia, // Seen in ACV region text file paths: /region/as/
    [Display(Name = "Europe")] Europe, // Seen in ACVD region text file paths: /region/eu/
    [Display(Name = "Japan")] Japan, // Seen in ACVD region text file paths: /region/jp/
    [Display(Name = "North America")] NorthAmerica, // Seen in ACVD region text file paths: /region/na/

    [Display(Name = "Spanish - Latin America")] SpanishLatin, // Introduced in BB
    [Display(Name = "Simplified Chinese")] SimplifiedChinese, // Introduced in BB
    [Display(Name = "Danish")] Danish, // Introduced in BB
    [Display(Name = "English (UK)")] EnglishUK, // Introduced in BB
    [Display(Name = "Finish")] Finnish, // Introduced in BB
    [Display(Name = "Dutch")] Dutch, // Introduced in BB
    [Display(Name = "Norwegian")] Norwegian, // Introduced in BB
    [Display(Name = "Portuguese - Latin America")] PortugueseLatin,
    [Display(Name = "Swedish")] Swedish, // Introduced in BB
    [Display(Name = "Turkish")] Turkish, // Introduced in BB

    [Display(Name = "Thai")] Thai, // Introduced in SDT

    [Display(Name = "Arabic")] Arabic, // Intorduced in ER

    // Sell Regions, BB onwards
    [Display(Name = "Sell Region")] SellRegion,
}

