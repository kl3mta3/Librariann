using System.ComponentModel;

namespace Librariann.Models.Entities.Enums.Theme;

public enum ThemeProvider
{
    /// <summary>
    /// Theme is provided by System
    /// </summary>
    [Description("System")]
    System = 1,
    /// <summary>
    /// Theme is provided by the User (ie it's custom) or Downloaded via Themes Repo
    /// </summary>
    [Description("Custom")]
    Custom = 2,
}
