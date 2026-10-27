using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.Administration.Tools.Dto;

namespace HCMv3.Administration.Tools;

/// <summary>
/// Reset User Passwords (Administration &gt; Tools): pick users, or a role, give each a new temporary password
/// and e-mail it to them. The port of legacy <c>uscRandomPassword</c> and the "by role" half of
/// <c>uscChangePassword</c>, built on ASP.NET Zero rather than beside it:
/// <list type="bullet">
/// <item><description>the password follows the tenant's password complexity settings and is checked by
/// Zero's password validators and password history (<c>UserManager.ChangePasswordAsync</c>);</description></item>
/// <item><description>the e-mail is Zero's own credentials mail, <c>IUserEmailer.SendEmailActivationLinkAsync</c>
/// with the plain password -- the same mail Zero's Users screen sends for "set random password";</description></item>
/// <item><description>every reset sets <c>ShouldChangePasswordOnNextLogin</c>, so the e-mailed password
/// works once;</description></item>
/// <item><description>ABP auditing records each call; the password is never part of an input or an output.</description></item>
/// </list>
/// Zero's Users screen and <c>UserAppService</c> are not touched.
/// </summary>
public interface IUserCredentialResetAppService : IApplicationService
{
    Task<PagedResultDto<CredentialResetUserDto>> GetUsers(GetCredentialResetUsersInput input);

    Task<ListResultDto<CredentialResetRoleDto>> GetRoles();

    Task<ResetUserCredentialsResultDto> ResetCredentials(ResetUserCredentialsInput input);
}
