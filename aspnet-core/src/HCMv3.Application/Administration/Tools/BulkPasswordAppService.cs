using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Authorization.Users;
using Abp.Domain.Repositories;
using Abp.Extensions;
using Abp.Linq.Extensions;
using Abp.UI;
using HCMv3.Administration.Tools.Dto;
using HCMv3.Authorization;
using HCMv3.Authorization.Roles;
using HCMv3.Authorization.Users;
using HCMv3.PersonnelAdministration;
using HCMv3.SystemSecurity;
using HCMv3.SystemSecurity.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace HCMv3.Administration.Tools;

/// <inheritdoc cref="IBulkPasswordAppService"/>
/// <remarks>
/// <para><b>SECURITY NOTE — read <see cref="IBulkPasswordAppService"/> first.</b> This service mints and
/// e-mails credentials for other users. Two legacy behaviours were deliberately changed rather than copied:
/// <list type="number">
/// <item><description>Password generation uses ASP.NET Zero's
/// <c>UserManager.CreateRandomPassword()</c> (honours the tenant's configured password-complexity setting)
/// instead of legacy <c>WorkFlowServices.GenerateRandomString</c>, which used
/// <c>System.Random</c> and prefixed the password with the employee's first initial
/// (WorkFlowServices.cs, and uscChangePassword.ascx.cs line 275).</description></item>
/// <item><description>The generated password is <b>never</b> returned in the API response. Legacy's own
/// screens did not display it either — it only ever went out by e-mail — so this is parity, stated
/// explicitly because it would be an obvious "convenience" to add.</description></item>
/// </list>
/// The legacy <c>uscChangePassword.btn_update2_Click</c> variant that set every password in a role to the
/// fully predictable <c>FirstNameEn[0] + EmployeeCode</c> (line 125) is <b>not</b> ported — see the
/// interface docs.</para>
///
/// <para><b>DELIBERATE DEVIATION — "administrative role" test.</b> Legacy skipped users holding a role with
/// XAF's <c>PermissionPolicyRole.IsAdministrative</c> flag. ABP Zero roles have no such flag; the closest
/// faithful equivalent is the static tenant <c>Admin</c> role, which is what is skipped here.</para>
///
/// <para><b>Legacy bug NOT reproduced.</b> Legacy swallowed every exception with an empty
/// <c>catch (Exception ex) { }</c> (WorkFlowServices.cs lines 9235, 9303) and reported a count of 0 with no
/// explanation. Failures are surfaced per user in <see cref="BulkResetPasswordsResultDto.Messages"/>.</para>
/// </remarks>
[AbpAuthorize(AppPermissions.Pages_Administration_Tools, AppPermissions.Pages_Administration_Tools_BulkPasswordReset)]
public class BulkPasswordAppService : HCMv3AppServiceBase, IBulkPasswordAppService
{
    /// <summary>
    /// Legacy looked the notification template up by this exact English name (WorkFlowServices.cs line
    /// 9259, uscSystemConfigPanel.ascx.cs line 147).
    /// </summary>
    private const string RandomPasswordTemplateName = "Random Password Notification";

    /// <summary>
    /// Legacy merge key placed into the <c>objList</c> handed to <c>EmailService.SendEmail</c>
    /// (WorkFlowServices.cs line 9277).
    /// </summary>
    private const string PasswordMergeKey = "Password";

    private readonly IRepository<Employee> _employeeRepository;
    private readonly IRepository<User, long> _userRepository;
    private readonly IRepository<UserRole, long> _userRoleRepository;
    private readonly IRepository<Role> _roleRepository;
    private readonly IRepository<EmailTemplate> _emailTemplateRepository;
    private readonly IEmailService _emailService;
    private readonly IPasswordHasher<User> _passwordHasher;

    public BulkPasswordAppService(
        IRepository<Employee> employeeRepository,
        IRepository<User, long> userRepository,
        IRepository<UserRole, long> userRoleRepository,
        IRepository<Role> roleRepository,
        IRepository<EmailTemplate> emailTemplateRepository,
        IEmailService emailService,
        IPasswordHasher<User> passwordHasher)
    {
        _employeeRepository = employeeRepository;
        _userRepository = userRepository;
        _userRoleRepository = userRoleRepository;
        _roleRepository = roleRepository;
        _emailTemplateRepository = emailTemplateRepository;
        _emailService = emailService;
        _passwordHasher = passwordHasher;
    }

    /// <summary>
    /// Legacy uscRandomPassword <c>Table1()</c> (line 590). Legacy's <c>dsUsers</c> sub-query was
    /// "user has a non-empty name AND at least one role"; the employee side was "Active AND LoginID is in
    /// that user set", ordered by Code.
    /// </summary>
    [HttpPost]
    public async Task<PagedResultDto<BulkPasswordCandidateDto>> GetBulkPasswordCandidates(
        GetBulkPasswordCandidatesInput input)
    {
        var userNamesWithRoles = _userRepository.GetAll()
            .Where(u => u.UserName != "")
            .Where(u => _userRoleRepository.GetAll().Any(ur => ur.UserId == u.Id))
            .Select(u => u.UserName);

        var query = _employeeRepository.GetAll()
            .Where(e => e.Status == EmployeeStatus.Active
                        && e.LoginID != null
                        && e.LoginID != ""
                        && userNamesWithRoles.Contains(e.LoginID))
            .WhereIf(!input.Filter.IsNullOrWhiteSpace(),
                e => e.Code.Contains(input.Filter)
                     || e.LoginID.Contains(input.Filter)
                     || e.FullNameEn.Contains(input.Filter)
                     || e.Email.Contains(input.Filter));

        var totalCount = query.Count();

        var employees = query
            .OrderBy(input.Sorting.IsNullOrWhiteSpace() ? "Code" : input.Sorting)
            .PageBy(input)
            .ToList();

        var items = employees.Select(e => new BulkPasswordCandidateDto
        {
            Id = e.Id,
            UserName = e.LoginID,
            Code = e.Code,
            EmployeeName = e.FullNameEn,
            Email = e.Email
        }).ToList();

        return await Task.FromResult(new PagedResultDto<BulkPasswordCandidateDto>(totalCount, items));
    }

    /// <summary>
    /// Legacy <c>WorkFlowServices.SetAllUsersPassword</c> (WorkFlowServices.cs line 9241).
    /// </summary>
    [HttpPost]
    public async Task<BulkResetPasswordsResultDto> BulkResetPasswords(BulkResetPasswordsInput input)
    {
        var result = new BulkResetPasswordsResultDto();

        if (input.EmployeeIds == null || input.EmployeeIds.Count == 0)
        {
            throw new UserFriendlyException(L("NoUsersSelected"));
        }

        // Legacy lines 9259-9262: the whole operation is a no-op unless the template exists AND has a body.
        // Failing loudly here instead: silently resetting nobody's password looks identical to success.
        var template = _emailTemplateRepository.GetAll()
            .FirstOrDefault(r => r.NameEn == RandomPasswordTemplateName);

        if (template == null || template.EmailBodyText.IsNullOrWhiteSpace())
        {
            throw new UserFriendlyException(L("RandomPasswordNotificationTemplateNotConfigured"));
        }

        // Legacy lines 9254-9257: Active employees with a Code and a LoginID only.
        var employees = _employeeRepository.GetAll()
            .Where(e => input.EmployeeIds.Contains(e.Id)
                        && e.Status == EmployeeStatus.Active
                        && e.Code != ""
                        && e.LoginID != null
                        && e.LoginID != "")
            .OrderBy(e => e.Code)
            .ToList();

        var adminRoleId = _roleRepository.GetAll()
            .Where(r => r.Name == StaticRoleNames.Tenants.Admin)
            .Select(r => (int?)r.Id)
            .FirstOrDefault();

        await UserManager.InitializeOptionsAsync(AbpSession.TenantId);

        foreach (var employee in employees)
        {
            var loginId = employee.LoginID;
            var user = _userRepository.GetAll().FirstOrDefault(u => u.UserName == loginId);

            if (user == null)
            {
                // Legacy silently skipped this case in SetAllUsersPassword; the later ESS variant
                // (WorkFlowServices.cs line 9404) did report it, so it is reported here.
                result.Messages.Add(L("BulkPasswordUserNotFound", employee.Code, loginId));
                continue;
            }

            // Legacy line 9251: users holding an administrative role are excluded. See class remarks for
            // how "administrative" is mapped onto ABP Zero roles.
            if (adminRoleId.HasValue)
            {
                var userId = user.Id;
                var adminRoleIdValue = adminRoleId.Value;
                var isAdmin = _userRoleRepository.GetAll()
                    .Any(ur => ur.UserId == userId && ur.RoleId == adminRoleIdValue);

                if (isAdmin)
                {
                    result.Messages.Add(L("BulkPasswordAdminUserSkipped", employee.Code));
                    continue;
                }
            }

            var password = await UserManager.CreateRandomPassword();
            user.Password = _passwordHasher.HashPassword(user, password);

            // Not in legacy: forcing a change on next login limits the window in which a password that
            // travelled by plaintext e-mail is valid. Flagged as an intentional hardening, not a port.
            user.ShouldChangePasswordOnNextLogin = true;

            await _userRepository.UpdateAsync(user);
            result.UpdatedPasswordCount++;

            // Legacy lines 9287-9294: mail failures do NOT roll back the password change.
            var emailResponse = await _emailService.SendEmailAsync(
                template,
                employee,
                employee,
                new Dictionary<string, object> { { PasswordMergeKey, password } });

            if (emailResponse != null && emailResponse.Status)
            {
                result.EmailedPasswordCount++;
                result.Messages.Add(L("BulkPasswordResetAndEmailed", employee.Code, employee.Email));
            }
            else
            {
                result.Messages.Add(L("BulkPasswordResetButEmailFailed", employee.Code));
            }
        }

        return result;
    }
}
