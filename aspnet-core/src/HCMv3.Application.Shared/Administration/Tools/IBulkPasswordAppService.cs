using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.Administration.Tools.Dto;

namespace HCMv3.Administration.Tools;

/// <summary>
/// Ported from legacy <c>uscRandomPassword.ascx.cs</c> (the grid + <c>SetAllUsersPassword</c> call at line
/// 408) and the "reset every user in a role" halves of <c>uscChangePassword.ascx.cs</c>.
///
/// <para><b>SCOPE — what was NOT ported, and why.</b> The self-service half of uscChangePassword
/// (<c>btn_update_Click</c>, line 74: verify current password, check the new one against
/// <c>PasswordPolicy</c>, set it, append a <c>PasswordHistory</c> row) is a duplicate of ASP.NET Zero's own
/// <c>ProfileAppService.ChangePassword</c> + the <c>PasswordComplexity</c> setting + this project's
/// <c>IPasswordExpirationService</c>. Porting it would mean maintaining a second, weaker password path.
/// Deliberately closed out. Only the bulk operator-driven reset — which Zero has no equivalent for — is
/// ported here.</para>
///
/// <para><b>SECURITY WARNING — faithful port of a practice that would not be shipped new today.</b> The
/// legacy generator (<c>WorkFlowServices.GenerateRandomString</c>) builds a password as
/// <c>FirstNameEn[0]</c> + 7 characters from <c>System.Random</c> (not a CSPRNG), and e-mails the
/// plaintext to the employee. The legacy uscChangePassword "reset all users in a role" button
/// (<c>btn_update2_Click</c>, line 125) was worse still: it set every password to the fully predictable
/// <c>FirstNameEn[0] + EmployeeCode</c>. <b>That deterministic variant is deliberately NOT ported</b> — it
/// is not a behaviour with a legitimate use, and <c>btn_update3_Click</c> (line 189) already provided the
/// same "reset a whole role" reach via the random+email path, which IS ported. The random path itself is
/// ported but hardened to <c>UserManager.CreateRandomPassword()</c> (Zero's own generator, which honours
/// the tenant's configured password complexity) rather than legacy's <c>System.Random</c>. Callers are
/// forced through <c>AppPermissions.Pages_Administration_Tools_BulkPasswordReset</c>, and the generated
/// password is never returned to the caller — only mailed.</para>
/// </summary>
public interface IBulkPasswordAppService : IApplicationService
{
    /// <summary>
    /// Legacy uscRandomPassword <c>Table1()</c> (line 590): active employees whose <c>LoginID</c> matches a
    /// user account that has at least one role.
    /// </summary>
    Task<PagedResultDto<BulkPasswordCandidateDto>> GetBulkPasswordCandidates(GetBulkPasswordCandidatesInput input);

    /// <summary>
    /// Legacy <c>WorkFlowServices.SetAllUsersPassword</c> (WorkFlowServices.cs line 9241). Generates a new
    /// random password for each selected employee's user account and e-mails it using the
    /// "Random Password Notification" e-mail template. Accounts holding an admin role are skipped, exactly
    /// as legacy did.
    /// </summary>
    Task<BulkResetPasswordsResultDto> BulkResetPasswords(BulkResetPasswordsInput input);
}
