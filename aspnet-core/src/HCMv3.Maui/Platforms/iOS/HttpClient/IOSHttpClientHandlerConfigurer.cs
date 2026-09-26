using System.Net;
using HCMv3.ApiClient;

namespace HCMv3.Maui.Core.Platforms.iOS.HttpClient;

public class IOSHttpClientHandlerConfigurer
{
    public void Configure(HttpClientHandler handler)
    {
        handler.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
        handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) =>
        {
#if DEBUG
            if (cert != null && cert.Issuer.Equals("CN=localhost"))
            {
                return true;
            }
#endif
            return TrustedServerCertificates.IsTrusted(cert, errors);
        };
    }
}
