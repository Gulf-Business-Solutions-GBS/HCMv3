using System.Net.Security;
using HCMv3.ApiClient;

namespace HCMv3.Maui.Core.Platforms.Android.HttpClient;

public class AndroidHttpClientHandlerConfigurer
{
    public void Configure(HttpClientHandler handler)
    {
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
