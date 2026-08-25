namespace Librariann.Models.DTOs.Metadata;

public record GenreTagDto
{
    public int Id { get; set; }
    public required string Title { get; set; }
}
