using System.ComponentModel;

namespace Librariann.Models.Entities.Enums.LibrariannPlus;

public enum TagWeight
{
    [Description("Core")]
    Core = 1,
    [Description("Defining")]
    Defining = 2,
    [Description("Recurrent")]
    Recurrent = 3,
    [Description("Incidental")]
    Incidental = 4,
    [Description("Unweighted")]
    Unweighted = 5,
}
