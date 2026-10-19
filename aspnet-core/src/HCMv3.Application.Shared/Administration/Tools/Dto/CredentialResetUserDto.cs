using Abp.Application.Services.Dto;

namespace HCMv3.Administration.Tools.Dto;

/// <summary>
/// One user the Reset User Passwords tool can reset: an active account in this tenant that is not the
/// caller's own and does not hold the Admin role.
/// </summary>
public class CredentialResetUserDto : EntityDto<long>
{
    public string UserName { get; set; }

    public string FullName { get; set; }

    public string EmailAddress { get; set; }

    /// <summary>The linked employee's code, when the user is an employee.</summary>
    public string EmployeeCode { get; set; }

    /// <summary>False when there is nowhere to send a new password; such a user is never reset.</summary>
    public bool HasEmailAddress { get; set; }
}
