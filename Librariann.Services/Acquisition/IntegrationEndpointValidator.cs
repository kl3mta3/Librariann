using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Librariann.API.Services.Acquisition;
using Librariann.Common;

namespace Librariann.Services.Acquisition;

public sealed class IntegrationEndpointValidator : IIntegrationEndpointValidator
{
    public async Task<Uri> ValidateAsync(string url, bool allowPrivateNetwork, CancellationToken cancellationToken = default)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp))
            throw new LibrariannException("integration-endpoint-invalid");

        if (!string.IsNullOrEmpty(uri.UserInfo) || !string.IsNullOrEmpty(uri.Fragment))
            throw new LibrariannException("integration-endpoint-credentials-or-fragment");

        IPAddress[] addresses;
        try
        {
            addresses = await Dns.GetHostAddressesAsync(uri.Host, cancellationToken);
        }
        catch (SocketException)
        {
            throw new LibrariannException("integration-endpoint-unable-to-resolve");
        }

        if (addresses.Length == 0) throw new LibrariannException("integration-endpoint-unable-to-resolve");
        if (System.Linq.Enumerable.Any(addresses, address => !IntegrationNetworkPolicy.IsAllowed(address, allowPrivateNetwork)))
            throw new LibrariannException("integration-endpoint-private-network-opt-in-required");

        if (uri.Scheme == Uri.UriSchemeHttp && (!allowPrivateNetwork || System.Linq.Enumerable.Any(addresses, address => !IntegrationNetworkPolicy.IsPrivateOrLoopback(address))))
            throw new LibrariannException("integration-endpoint-https-required");

        return new Uri(uri.GetLeftPart(UriPartial.Path).TrimEnd('/'));
    }

}
