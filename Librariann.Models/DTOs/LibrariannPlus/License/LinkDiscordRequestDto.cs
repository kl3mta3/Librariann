namespace Librariann.Models.DTOs.LibrariannPlus.License;

public sealed record LinkDiscordRequestDto
{
    public required string DiscordId { get; set; }
    public required string DiscordUserName { get; set; }
}
