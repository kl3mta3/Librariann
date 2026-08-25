using System.ComponentModel;

namespace Librariann.Models.Entities.Enums;

public enum LayoutMode
{
    [Description("Single")]
    Single = 1,
    [Description("Double")]
    Double = 2,
    [Description("Double (manga)")]
    DoubleReversed = 3
}
