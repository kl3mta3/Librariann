using System.Net;
using System.Net.Sockets;

namespace Librariann.Services.Acquisition;

internal static class IntegrationNetworkPolicy
{
    public static bool IsAllowed(IPAddress address, bool allowPrivateNetwork)
    {
        if (IsAlwaysBlocked(address)) return false;
        return allowPrivateNetwork || !Librariann.Common.Helpers.IpBlocklist.IsBlockedAddress(address);
    }

    public static bool IsPrivateOrLoopback(IPAddress address)
    {
        var ip = Normalize(address);
        if (IPAddress.IsLoopback(ip)) return true;

        var bytes = ip.GetAddressBytes();
        if (ip.AddressFamily == AddressFamily.InterNetwork)
            return bytes[0] == 10
                   || (bytes[0] == 172 && bytes[1] is >= 16 and <= 31)
                   || (bytes[0] == 192 && bytes[1] == 168);

        return ip.AddressFamily == AddressFamily.InterNetworkV6 && (bytes[0] & 0xFE) == 0xFC;
    }

    private static bool IsAlwaysBlocked(IPAddress address)
    {
        var ip = Normalize(address);
        if (ip.Equals(IPAddress.Any) || ip.Equals(IPAddress.IPv6Any) || ip.IsIPv6LinkLocal || IsMulticast(ip)) return true;

        if (ip.AddressFamily != AddressFamily.InterNetwork) return false;
        var bytes = ip.GetAddressBytes();
        return bytes[0] == 169 && bytes[1] == 254;
    }

    private static bool IsMulticast(IPAddress address)
    {
        var bytes = address.GetAddressBytes();
        return address.AddressFamily == AddressFamily.InterNetwork
            ? (bytes[0] & 0xF0) == 0xE0
            : address.AddressFamily == AddressFamily.InterNetworkV6 && bytes[0] == 0xFF;
    }

    private static IPAddress Normalize(IPAddress address) =>
        address.IsIPv4MappedToIPv6 ? address.MapToIPv4() : address;
}

