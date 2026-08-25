using System.ComponentModel.DataAnnotations;
namespace Librariann.Models.DTOs.LibrariannPlus.OAuth;

public sealed record RefreshTokenRequestDto
{
    [EnumDataType(typeof(OAuthUpstream))]
    public required OAuthUpstream Upstream { get; set; }
    public required string RefreshToken { get; set; }
}