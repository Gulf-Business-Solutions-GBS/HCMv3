namespace HCMv3.Careers.Dto;

/// <summary>
/// Result of an anonymous candidate registration. Mirrors ASP.NET Zero's
/// <c>HCMv3.Authorization.Accounts.Dto.RegisterOutput</c> so the careers front end can share the "you may
/// now sign in" / "check your inbox" branch logic.
///
/// Deliberately carries no user id, user name or applicant id: those would let an anonymous caller confirm
/// which addresses map to accounts.
/// </summary>
public class CareersRegisterOutput
{
    /// <summary>
    /// <c>true</c> when the account is active and either already confirmed or the tenant does not require
    /// email confirmation. <c>false</c> means the candidate must click the activation link first.
    /// </summary>
    public bool CanLogin { get; set; }
}
