using System.Collections.Generic;

namespace HCMv3.Administration.Tools.Dto;

/// <summary>Outcome of one run of the Reset User Passwords tool.</summary>
public class ResetUserCredentialsResultDto
{
    public int PasswordsResetCount { get; set; }

    public int EmailsSentCount { get; set; }

    public List<CredentialResetOutcomeDto> Users { get; set; } = new List<CredentialResetOutcomeDto>();
}
