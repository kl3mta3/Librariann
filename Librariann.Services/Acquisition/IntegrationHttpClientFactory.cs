using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading.Tasks;
using Librariann.API.Services.Acquisition;
using Librariann.Models.Entities.Acquisition;

namespace Librariann.Services.Acquisition;

/// <summary>
/// Builds clients that re-check DNS at connection time and connect to an approved resolved address.
/// Redirects are disabled so a provider cannot bounce a request into a different network zone.
/// </summary>
public sealed class IntegrationHttpClientFactory : IIntegrationHttpClientFactory
{
    public HttpClient Create(IntegrationProviderConfiguration configuration)
    {
        var handler = new SocketsHttpHandler
        {
            AllowAutoRedirect = false,
            AutomaticDecompression = DecompressionMethods.All,
            ConnectTimeout = TimeSpan.FromSeconds(10),
            PooledConnectionLifetime = TimeSpan.FromMinutes(5),
            UseProxy = false,
            ConnectCallback = async (context, cancellationToken) =>
            {
                var addresses = await Dns.GetHostAddressesAsync(context.DnsEndPoint.Host, cancellationToken);
                var address = addresses.FirstOrDefault(candidate =>
                    IntegrationNetworkPolicy.IsAllowed(candidate, configuration.AllowPrivateNetwork));
                if (address is null) throw new SocketException((int) SocketError.AccessDenied);

                var socket = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                try
                {
                    await socket.ConnectAsync(new IPEndPoint(address, context.DnsEndPoint.Port), cancellationToken);
                    return new NetworkStream(socket, ownsSocket: true);
                }
                catch
                {
                    socket.Dispose();
                    throw;
                }
            },
        };

        return new HttpClient(handler, disposeHandler: true)
        {
            BaseAddress = new Uri(configuration.BaseUrl.TrimEnd('/') + "/"),
            Timeout = TimeSpan.FromSeconds(30),
        };
    }
}

