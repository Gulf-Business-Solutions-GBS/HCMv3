namespace HCMv3.Authorization.Accounts.Dto;

public class VerifyPasswordlessLoginCodeInput
{
    /// <summary>
    /// Passwordless login code send to user's ProviderValue.
    /// </summary>
    public string Code { get; set; }

    /// <summary>
    /// Passwordless login provider value like user's email address or phone number.
    /// </summary>
    public string ProviderValue { get; set; }
}

