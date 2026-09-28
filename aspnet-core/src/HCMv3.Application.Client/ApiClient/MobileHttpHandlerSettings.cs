using System;
using System.Net;
using System.Net.Http;

namespace HCMv3.ApiClient;

/// <summary>
/// How the mobile app opens and keeps connections to the server, the same on Android and iOS.
/// </summary>
public static class MobileHttpHandlerSettings
{
    /// <summary>A stuck connection to the hosted server hangs 15-20 seconds; giving up sooner lets
    /// <see cref="ConnectRetryHandler"/> try again. Not much sooner: a phone's first connection after start
    /// (radio wake-up, DNS, TLS handshake) can take several seconds, and 5 seconds cut those off too.</summary>
    public static readonly TimeSpan ConnectTimeout = TimeSpan.FromSeconds(10);

    /// <summary>Keep an open connection for reuse, so moving between screens does not open a new one each time.</summary>
    public static readonly TimeSpan PooledConnectionIdleTimeout = TimeSpan.FromMinutes(5);

    /// <summary>Upper bound for a whole request once connected, so a screen reports a problem instead of hanging.</summary>
    public static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(60);

    public static void Apply(SocketsHttpHandler handler, bool trustLocalDeveloperCertificate = false)
    {
        handler.ConnectTimeout = ConnectTimeout;
        handler.PooledConnectionIdleTimeout = PooledConnectionIdleTimeout;
        handler.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate | DecompressionMethods.Brotli;
        handler.SslOptions.RemoteCertificateValidationCallback = (sender, certificate, chain, errors) =>
        {
            if (trustLocalDeveloperCertificate && certificate != null && certificate.Issuer.Equals("CN=localhost"))
            {
                return true;
            }

            return TrustedServerCertificates.IsTrusted(certificate, errors);
        };
    }
}
