using System.ComponentModel;

namespace Librariann.Models.Entities.Enums;

public enum ReaderMode
{
    [Description("Left and Right")]
    LeftRight = 0,
    [Description("Up and Down")]
    UpDown = 1,
    [Description("Webtoon")]
    Webtoon = 2
}
