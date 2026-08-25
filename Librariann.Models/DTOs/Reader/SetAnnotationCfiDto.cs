namespace Librariann.Models.DTOs.Reader;

/// <summary>
/// Body for <c>POST annotation/{annotationId}/cfi</c> - see <see cref="AnnotationDto.Cfi"/>.
/// </summary>
public sealed record SetAnnotationCfiDto
{
    public required string Cfi { get; init; }
}
