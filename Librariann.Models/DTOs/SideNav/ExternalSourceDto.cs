namespace Librariann.Models.DTOs.SideNav;

public sealed record ExternalSourceDto
{
    public required int Id { get; set; } = 0;
    public required string Name { get; set; }
    public required string Host { get; set; }
    public required string ApiKey { get; set; }
    /// <summary>
    /// Short-lived, one-use Librariann URL for opening the linked source without exposing its API key in this DTO.
    /// </summary>
    public string LaunchApiPath { get; set; } = string.Empty;
}
