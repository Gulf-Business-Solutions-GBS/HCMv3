using System.Net;

namespace HCMv3.Maui.Core.Platforms.iOS.HttpClient;

public class IOSHttpClientHandlerConfigurer
{
    public void Configure(HttpClientHandler handler)
    {
        handler.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
#if DEBUG
        TrustLocalDeveloperCert(handler);
#endif
    }

    private static void TrustLocalDeveloperCert(HttpClientHandler messageHandler)
    {
        messageHandler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;
    }
}