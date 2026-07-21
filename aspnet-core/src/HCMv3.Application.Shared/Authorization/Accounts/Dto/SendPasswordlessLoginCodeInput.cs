using HCMv3.Authentication.PasswordlessLogin;

namespace HCMv3.Authorization.Accounts.Dto;

public class SendPasswordlessLoginCodeInput
{
    /// <summary>
    /// Passwordless login provider value like email address or phone number.
    /// </summary>
    public string ProviderValue { get; set; }

    /// <summary>
    /// Passwordless login provider like Email or Sms.
    /// </summary>
    public PasswordlessLoginProviderType ProviderType { get; set; }
}

