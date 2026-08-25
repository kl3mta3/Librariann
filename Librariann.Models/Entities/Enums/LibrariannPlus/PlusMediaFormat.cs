using System.ComponentModel;

namespace Librariann.Models.Entities.Enums.LibrariannPlus;

/// <summary>
/// Represents PlusMediaFormat
/// </summary>
public enum PlusMediaFormat
{
    [Description("Manga")]
    Manga = 1,
    [Description("Comic")]
    Comic = 2,
    [Description("LightNovel")]
    LightNovel = 3,
    [Description("Book")]
    Book = 4,
    Unknown = 5
}
