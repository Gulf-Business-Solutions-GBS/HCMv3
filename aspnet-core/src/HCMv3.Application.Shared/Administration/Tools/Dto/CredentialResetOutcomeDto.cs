namespace HCMv3.Administration.Tools.Dto;

/// <summary>What happened to one user. Never carries the generated password.</summary>
public class CredentialResetOutcomeDto
{
    public long UserId { get; set; }

    public string UserName { get; set; }

    public bool PasswordReset { get; set; }

    public bool EmailSent { get; set; }

    public string Message { get; set; }
}
