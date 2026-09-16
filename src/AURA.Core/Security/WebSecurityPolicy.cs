using System.Net;
using System.Net.Sockets;

namespace AURA.Core.Security;

/// <summary>
/// Valida URLs antes de serem usadas por WebView, Web AI e ferramentas HTTP.
/// Conteúdo remoto nunca define a política de navegação.
/// </summary>
public static class WebSecurityPolicy
{
    public const int MaxUrlLength = 4096;
    public const int MaxRedirects = 5;

    public static bool TryValidateHttpUrl(
        string? raw,
        out Uri uri,
        bool rejectLocalHost = false)
    {
        uri = null!;
        if (string.IsNullOrWhiteSpace(raw) || raw.Length > MaxUrlLength ||
            !Uri.TryCreate(raw.Trim(), UriKind.Absolute, out Uri? candidate) ||
            (candidate.Scheme != Uri.UriSchemeHttp && candidate.Scheme != Uri.UriSchemeHttps) ||
            string.IsNullOrWhiteSpace(candidate.Host) ||
            !string.IsNullOrEmpty(candidate.UserInfo) ||
            (candidate.Port != 80 && candidate.Port != 443 && candidate.Port != -1))
        {
            return false;
        }

        if (rejectLocalHost && IsLocalHost(candidate.Host))
        {
            return false;
        }

        uri = candidate;
        return true;
    }

    public static async Task<bool> IsPublicHttpUrlAsync(string raw, CancellationToken ct = default)
    {
        if (!TryValidateHttpUrl(raw, out Uri uri, rejectLocalHost: true))
        {
            return false;
        }

        if (IPAddress.TryParse(uri.Host, out IPAddress? literal))
        {
            return IsPublicAddress(literal);
        }

        try
        {
            IPAddress[] addresses = await Dns.GetHostAddressesAsync(uri.Host, ct).ConfigureAwait(false);
            return addresses.Length > 0 && addresses.All(IsPublicAddress);
        }
        catch (SocketException)
        {
            return false;
        }
    }

    public static string RedactForLog(string? raw)
    {
        if (!TryValidateHttpUrl(raw, out Uri uri))
        {
            return "<url inválida>";
        }

        var builder = new UriBuilder(uri)
        {
            UserName = string.Empty,
            Password = string.Empty,
            Query = string.Empty,
            Fragment = string.Empty,
        };
        return builder.Uri.AbsoluteUri;
    }

    public static bool IsLocalHost(string host)
    {
        if (string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase) ||
            host.EndsWith(".localhost", StringComparison.OrdinalIgnoreCase) ||
            host.EndsWith(".local", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return IPAddress.TryParse(host, out IPAddress? address) && !IsPublicAddress(address);
    }

    private static bool IsPublicAddress(IPAddress address)
    {
        if (IPAddress.IsLoopback(address) || address.Equals(IPAddress.Any) ||
            address.Equals(IPAddress.IPv6Any) || address.Equals(IPAddress.None) ||
            address.Equals(IPAddress.IPv6None))
        {
            return false;
        }

        if (address.AddressFamily == AddressFamily.InterNetwork)
        {
            byte[] b = address.GetAddressBytes();
            return b[0] != 10 &&
                   !(b[0] == 172 && b[1] >= 16 && b[1] <= 31) &&
                   !(b[0] == 192 && b[1] == 168) &&
                   !(b[0] == 169 && b[1] == 254) &&
                   !(b[0] == 127);
        }

        byte[] v6 = address.GetAddressBytes();
        bool linkLocal = (v6[0] & 0xFE) == 0xFC || (v6[0] == 0xFE && (v6[1] & 0xC0) == 0x80);
        return !linkLocal && !address.IsIPv6SiteLocal;
    }
}
