using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace HCMv3.ApiClient;

/// <summary>
/// Retries a request whose connection to the server never opened. Some attempts to reach the hosted
/// server hang for 15-20 seconds and then succeed on a fresh try, so waiting out one stuck connection is
/// what made every screen of the mobile app slow.
/// Only failures to connect are retried: the request has not reached the server, so even a POST (a leave
/// request, a punch) cannot be recorded twice. Anything that fails after connecting is passed on unchanged.
/// </summary>
public class ConnectRetryHandler : DelegatingHandler
{
    public const int DefaultMaxAttempts = 3;

    private readonly int _maxAttempts;

    public ConnectRetryHandler(int maxAttempts = DefaultMaxAttempts)
    {
        if (maxAttempts < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(maxAttempts));
        }

        _maxAttempts = maxAttempts;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                return await base.SendAsync(request, cancellationToken);
            }
            catch (Exception ex) when (attempt < _maxAttempts && IsConnectFailure(ex, cancellationToken))
            {
            }
        }
    }

    public static bool IsConnectFailure(Exception exception, CancellationToken cancellationToken)
    {
        // The caller gave up (overall timeout or the user left the screen): do not keep trying behind its back.
        if (cancellationToken.IsCancellationRequested)
        {
            return false;
        }

        // SocketsHttpHandler.ConnectTimeout elapsed.
        if (exception is OperationCanceledException && exception.InnerException is TimeoutException)
        {
            return true;
        }

        // Refused, reset or unreachable while opening the connection.
        return exception is HttpRequestException { HttpRequestError: HttpRequestError.ConnectionError };
    }
}
