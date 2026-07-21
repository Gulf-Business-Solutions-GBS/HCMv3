using Abp.Auditing;

namespace HCMv3.Web.Models.TokenAuth;

public class PasswordlessAuthenticateModel
{
    public string Provider { get; set; }

    public string ProviderValue { get; set; }

    public string VerificationCode { get; set; }

    public bool? SingleSignIn { get; set; }

    public string ReturnUrl { get; set; }

    [DisableAuditing]
    public string CaptchaResponse { get; set; }
}

