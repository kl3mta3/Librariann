using System.ComponentModel.DataAnnotations;

namespace Librariann.Models.DTOs.Uploads;

public sealed record UploadUrlDto
{
    /// <summary>
    /// External url
    /// </summary>
    [Required]
    public required string Url { get; set; }
}
