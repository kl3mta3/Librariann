using System.Threading.Tasks;

namespace Librariann.API.Services;

public interface IUrlValidationService
{
    /// <summary>
    /// Validates that a URL is safe to fetch (SSRF protection).
    /// Rejects non-HTTPS schemes, private/loopback IPs, and link-local addresses.
    /// </summary>
    /// <param name="url">The URL to validate</param>
    /// <exception cref="Librariann.Common.LibrariannException">Thrown when the URL fails validation</exception>
    Task ValidateUrlAsync(string url);
}
