using System.Collections.Generic;

namespace HCMv3.Authorization.Users.Profile.Dto;

public class UpdateGoogleAuthenticatorKeyInput
{
    public string GoogleAuthenticatorKey { get; set; }
    public string AuthenticatorCode { get; set; }
}

