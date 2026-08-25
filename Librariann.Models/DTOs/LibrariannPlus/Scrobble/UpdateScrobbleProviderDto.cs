using Librariann.Models.Entities.Enums;
using System.ComponentModel.DataAnnotations;

namespace Librariann.Models.DTOs.LibrariannPlus.Scrobble;

public class UpdateScrobbleProviderDto
{
    [EnumDataType(typeof(ScrobbleProvider))]
    public required ScrobbleProvider Provider { get; set; }
    public string UserName { get; set; }
    public string AuthenticationToken { get; set; }
    public string RefreshToken { get; set; }
}