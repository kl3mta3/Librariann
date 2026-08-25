namespace Librariann.Models.DTOs.Settings;

/// <summary>
/// Status of the Kokoro process Librariann itself started and supervises - backs the Start/Stop buttons in
/// Settings -> Media. Librariann only ever reports/controls a process it launched itself (see
/// IKokoroProcessService's doc comment) - a Kokoro server the admin points KokoroEndpointUrl at manually always
/// shows IsManaged: false here, regardless of whether it's actually running somewhere.
/// </summary>
public sealed record KokoroProcessStatusDto
{
    public bool IsManaged { get; set; }
    public bool IsRunning { get; set; }
    public int? ProcessId { get; set; }
    public string? Error { get; set; }
    /// <summary>Whether LKS.Server(.exe) actually exists at the configured KokoroExecutablePath folder -
    /// independent of whether it's currently running. Drives whether Settings shows an "Install" button
    /// (nothing there yet) or Start/Stop controls (something to manage).</summary>
    public bool IsInstalled { get; set; }
}
