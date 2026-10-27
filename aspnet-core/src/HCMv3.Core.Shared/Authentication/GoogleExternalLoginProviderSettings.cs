using Abp.Extensions;

namespace HCMv3.Authentication;

public class GoogleExternalLoginProviderSettings
{
    public string ClientId { get; set; }
    public string ClientSecret { get; set; }
    public string UserInfoEndpoint { get; set; }

    public bool IsValid()
    {
        return !ClientId.IsNullOrWhiteSpace() && !ClientSecret.IsNullOrWhiteSpace();
    }
}

