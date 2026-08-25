namespace Librariann.Models.DTOs.Scrobbling;
#nullable enable

/// <summary>
/// Response from Librariann+ Scrobble API
/// </summary>
public sealed record ScrobbleResponseDto
{
    public bool Successful { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ExtraInformation  {get; set;}
    public int RateLeft { get; set; }
}
