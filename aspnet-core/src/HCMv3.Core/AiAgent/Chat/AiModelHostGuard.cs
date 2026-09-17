using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading.Tasks;
using Abp.Dependency;

namespace HCMv3.AiAgent.Chat;

/// <summary>
/// Keeps employee data on the organisation's own network: the model may only be reached on this machine or a
/// private network address, unless someone deliberately sets <c>AiAgent:AllowExternalModelHost</c> to true.
///
/// <para>
/// <b>Why this exists.</b> Everything the assistant sends the model -- questions, names, leave balances, salary
/// lines -- goes to <c>AiAgent:BaseUrl</c>. Without this check, pointing that one setting at a hosted AI service
/// would silently start sending all of it outside the server. The business requirement is that no data leaves;
/// this makes it a rule the code enforces rather than a convention an appsettings edit can break.
/// </para>
///
/// <para>
/// A hostname is judged by what it RESOLVES to, every address of it, at the moment of the call -- a name like
/// <c>gpu-server</c> is allowed when it is on the private network, and a name that resolves to a public address
/// is refused however local it sounds.
/// </para>
/// </summary>
public static class AiModelHostGuard
{
    /// <exception cref="AiChatClientException">The host is outside this machine and the private network, or its
    /// address could not be established.</exception>
    public static async Task EnsureAllowedAsync(Uri endpoint, bool allowExternalHost, Func<string, Task<IPAddress[]>> resolve = null)
    {
        if (endpoint.Scheme != Uri.UriSchemeHttp && endpoint.Scheme != Uri.UriSchemeHttps)
        {
            throw new AiChatClientException($"The model address '{endpoint}' must use http or https.");
        }

        if (allowExternalHost)
        {
            return;
        }

        IPAddress[] addresses;

        if (endpoint.IsLoopback)
        {
            return;
        }

        if (IPAddress.TryParse(endpoint.DnsSafeHost, out var literal))
        {
            addresses = new[] { literal };
        }
        else
        {
            try
            {
                addresses = await (resolve ?? Dns.GetHostAddressesAsync)(endpoint.DnsSafeHost);
            }
            catch (Exception ex) when (ex is SocketException or ArgumentException)
            {
                throw new AiChatClientException(
                    $"The model host '{endpoint.Host}' could not be resolved, so it cannot be confirmed to be on this server's network. The call was refused.", ex);
            }
        }

        if (addresses.Length == 0 || !addresses.All(IsLocalOrPrivate))
        {
            throw new AiChatClientException(
                $"The model host '{endpoint.Host}' is outside this server and its private network " +
                $"({string.Join(", ", addresses.Select(a => a.ToString()))}). Employee data may not be sent there, so the call was refused. " +
                "If an external model host is genuinely approved, set AiAgent:AllowExternalModelHost to true.");
        }
    }

    /// <summary>Loopback, or a private range: 10/8, 172.16/12, 192.168/16, and IPv6 unique-local fc00::/7.</summary>
    public static bool IsLocalOrPrivate(IPAddress address)
    {
        if (address.IsIPv4MappedToIPv6)
        {
            address = address.MapToIPv4();
        }

        if (IPAddress.IsLoopback(address))
        {
            return true;
        }

        if (address.AddressFamily == AddressFamily.InterNetwork)
        {
            var b = address.GetAddressBytes();
            return b[0] == 10
                   || (b[0] == 172 && b[1] >= 16 && b[1] <= 31)
                   || (b[0] == 192 && b[1] == 168);
        }

        if (address.AddressFamily == AddressFamily.InterNetworkV6)
        {
            return (address.GetAddressBytes()[0] & 0xFE) == 0xFC;
        }

        return false;
    }
}

/// <summary>
/// The one connection handler used to reach the model. Deliberately NOT the application's shared HttpClient
/// setup, because two of its defaults would each undo <see cref="AiModelHostGuard"/>:
/// <list type="bullet">
/// <item><description><b>Redirects are not followed.</b> A local address that answers "moved to" an outside URL
/// would otherwise receive the checked request and forward the same body there.</description></item>
/// <item><description><b>No proxy.</b> A server-wide proxy setting would otherwise route model traffic -- employee
/// data included -- through whatever that proxy is. The model is local or private; it never needs one.</description></item>
/// </list>
/// </summary>
public class AiModelHttpHandlerProvider : ISingletonDependency, IDisposable
{
    private readonly HttpMessageHandler _handler;

    public AiModelHttpHandlerProvider()
        : this(new SocketsHttpHandler
        {
            AllowAutoRedirect = false,
            UseProxy = false,
            PooledConnectionLifetime = TimeSpan.FromMinutes(5)
        })
    {
    }

    /// <summary>For tests, to substitute the network.</summary>
    public AiModelHttpHandlerProvider(HttpMessageHandler handler)
    {
        _handler = handler;
    }

    public HttpClient CreateClient(TimeSpan timeout) => new(_handler, disposeHandler: false) { Timeout = timeout };

    public void Dispose() => _handler.Dispose();
}
