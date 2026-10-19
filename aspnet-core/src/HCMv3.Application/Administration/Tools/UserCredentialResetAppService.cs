using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;
using Abp.Configuration;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Authorization.Users;
using Abp.Domain.Repositories;
using Abp.Extensions;
using Abp.Linq.Extensions;
using Abp.Runtime.Session;
using Abp.UI;
using HCMv3.Administration.Tools.Dto;
using HCMv3.Authorization;
using HCMv3.Authorization.Roles;
using HCMv3.Authorization.Users;
using HCMv3.Configuration;
using HCMv3.PersonnelAdministration;
using HCMv3.Url;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.Administration.Tools;

/// <inheritdoc cref="IUserCredentialResetAppService"/>
/// <remarks>
/// <para><b>Who can be reset.</b> Active users of this tenant, other than the caller, who do not hold the static
/// Admin role. Legacy <c>WorkFlowServices.SetAllUsersPassword</c> skipped users holding an administrative role;
/// Zero roles have no such flag, and the static Admin role is the faithful equivalent. A user with no e-mail
/// address is listed but never reset -- there would be nowhere to send the new password.</para>
///
/// <para><b>Order of work for each user, and why.</b> The password is checked against Zero's validators first,
/// then e-mailed, and only then saved. An e-mail that fails therefore leaves the old password working instead
/// of locking the user out with a password nobody received -- legacy did it the other way round and swallowed
/// the mail error. Each user runs in a unit of work of their own, so one failure does not undo the others.</para>
///
/// <para><b>Outbound e-mail switched off</b> (Integration Center) is refused up front: the sender would drop
/// every message silently and every selected user would be left with a password they never received.</para>
/// </remarks>
[AbpAuthorize(AppPermissions.Pages_Administration_Tools_ResetUserCredentials)]
public class UserCredentialResetAppService : HCMv3AppServiceBase, IUserCredentialResetAppService
{
    /// <summary>
    /// Each reset sends one e-mail while the request waits, so a run is capped to keep it inside a request
    /// timeout. A larger role is done in several runs.
    /// </summary>
    public const int MaxUsersPerRun = 200;

    private readonly IRepository<User, long> _userRepository;
    private readonly IRepository<UserRole, long> _userRoleRepository;
    private readonly IRepository<Role> _roleRepository;
    private readonly IRepository<Employee> _employeeRepository;
    private readonly IUserEmailer _userEmailer;
    private readonly ICredentialPasswordGenerator _passwordGenerator;

    /// <summary>Property-injected exactly as in Zero's <c>UserAppService</c>; Web.Host registers the real one.</summary>
    public IAppUrlService AppUrlService { get; set; }

    public UserCredentialResetAppService(
        IRepository<User, long> userRepository,
        IRepository<UserRole, long> userRoleRepository,
        IRepository<Role> roleRepository,
        IRepository<Employee> employeeRepository,
        IUserEmailer userEmailer,
        ICredentialPasswordGenerator passwordGenerator)
    {
        _userRepository = userRepository;
        _userRoleRepository = userRoleRepository;
        _roleRepository = roleRepository;
        _employeeRepository = employeeRepository;
        _userEmailer = userEmailer;
        _passwordGenerator = passwordGenerator;

        AppUrlService = NullAppUrlService.Instance;
    }

    [HttpPost]
    public async Task<PagedResultDto<CredentialResetUserDto>> GetUsers(GetCredentialResetUsersInput input)
    {
        var query = (await EligibleUsersQueryAsync())
            .WhereIf(input.RoleId.HasValue,
                u => _userRoleRepository.GetAll().Any(ur => ur.UserId == u.Id && ur.RoleId == input.RoleId.Value))
            .WhereIf(!input.Filter.IsNullOrWhiteSpace(),
                u => u.UserName.Contains(input.Filter)
                     || u.Name.Contains(input.Filter)
                     || u.Surname.Contains(input.Filter)
                     || u.EmailAddress.Contains(input.Filter));

        var totalCount = await query.CountAsync();

        var users = await ApplySorting(query, input.Sorting)
            .PageBy(input)
            .ToListAsync();

        var userIds = users.Select(u => (long?)u.Id).ToList();
        var employeeCodes = await _employeeRepository.GetAll()
            .Where(e => userIds.Contains(e.UserId))
            .Select(e => new { e.UserId, e.Code })
            .ToListAsync();

        var items = users.Select(u => new CredentialResetUserDto
        {
            Id = u.Id,
            UserName = u.UserName,
            FullName = (u.Name + " " + u.Surname).Trim(),
            EmailAddress = u.EmailAddress,
            EmployeeCode = employeeCodes.FirstOrDefault(e => e.UserId == u.Id)?.Code,
            HasEmailAddress = !u.EmailAddress.IsNullOrWhiteSpace()
        }).ToList();

        return new PagedResultDto<CredentialResetUserDto>(totalCount, items);
    }

    public async Task<ListResultDto<CredentialResetRoleDto>> GetRoles()
    {
        var roles = await _roleRepository.GetAll()
            .Where(r => r.Name != StaticRoleNames.Tenants.Admin)
            .OrderBy(r => r.DisplayName)
            .Select(r => new CredentialResetRoleDto { Id = r.Id, DisplayName = r.DisplayName })
            .ToListAsync();

        return new ListResultDto<CredentialResetRoleDto>(roles);
    }

    public async Task<ResetUserCredentialsResultDto> ResetCredentials(ResetUserCredentialsInput input)
    {
        var requestedIds = (input.UserIds ?? new List<long>()).Distinct().ToList();

        if (requestedIds.Count == 0 && !input.RoleId.HasValue)
        {
            throw new UserFriendlyException(L("NoUsersSelected"));
        }

        if (!await SettingManager.GetSettingValueAsync<bool>(AppSettings.Email.IsOutboundEmailEnabled))
        {
            throw new UserFriendlyException(L("OutboundEmailIsOffCannotResetPasswords"));
        }

        var eligible = await EligibleUsersQueryAsync();

        var roleUserIds = new List<long>();
        if (input.RoleId.HasValue)
        {
            var roleId = input.RoleId.Value;
            roleUserIds = await eligible
                .Where(u => _userRoleRepository.GetAll().Any(ur => ur.UserId == u.Id && ur.RoleId == roleId))
                .Select(u => u.Id)
                .ToListAsync();
        }

        var targetIds = requestedIds.Union(roleUserIds).ToList();

        if (targetIds.Count == 0)
        {
            throw new UserFriendlyException(L("NoUsersSelected"));
        }

        if (targetIds.Count > MaxUsersPerRun)
        {
            throw new UserFriendlyException(L("TooManyUsersForOneCredentialReset", MaxUsersPerRun));
        }

        var eligibleUsers = await eligible
            .Where(u => targetIds.Contains(u.Id))
            .Select(u => new { u.Id, u.UserName, u.EmailAddress })
            .ToListAsync();

        // Built once, before anybody is touched: a host without a client URL configured fails here, loudly,
        // instead of half-way through the list.
        var activationUrlFormat = AppUrlService.CreateEmailActivationUrlFormat(AbpSession.TenantId);

        await UserManager.InitializeOptionsAsync(AbpSession.TenantId);

        var result = new ResetUserCredentialsResultDto();

        foreach (var userId in targetIds)
        {
            var candidate = eligibleUsers.FirstOrDefault(u => u.Id == userId);

            CredentialResetOutcomeDto outcome;
            if (candidate == null)
            {
                // Asked for by id but not resettable here: inactive, an administrator, the caller themselves,
                // or not a user of this tenant at all. Said so rather than silently dropped.
                outcome = new CredentialResetOutcomeDto
                {
                    UserId = userId,
                    Message = L("CredentialResetSkippedNotEligible", userId)
                };
            }
            else if (candidate.EmailAddress.IsNullOrWhiteSpace())
            {
                outcome = new CredentialResetOutcomeDto
                {
                    UserId = userId,
                    UserName = candidate.UserName,
                    Message = L("CredentialResetSkippedNoEmail", candidate.UserName)
                };
            }
            else
            {
                outcome = await ResetOneAsync(userId, activationUrlFormat);
            }

            result.Users.Add(outcome);

            if (outcome.PasswordReset)
            {
                result.PasswordsResetCount++;
            }

            if (outcome.EmailSent)
            {
                result.EmailsSentCount++;
            }
        }

        return result;
    }

    /// <summary>One user, in a unit of work of their own -- see class remarks for the order of the steps.</summary>
    private async Task<CredentialResetOutcomeDto> ResetOneAsync(long userId, string activationUrlFormat)
    {
        using var uow = UnitOfWorkManager.Begin(TransactionScopeOption.RequiresNew);

        var user = await UserManager.GetUserByIdAsync(userId);
        var outcome = new CredentialResetOutcomeDto { UserId = user.Id, UserName = user.UserName };

        var password = await _passwordGenerator.CreateAsync();

        var refusals = new List<string>();
        foreach (var validator in UserManager.PasswordValidators)
        {
            var validation = await validator.ValidateAsync(UserManager, user, password);
            if (!validation.Succeeded)
            {
                refusals.AddRange(validation.Errors.Select(e => e.Description));
            }
        }

        if (refusals.Count > 0)
        {
            outcome.Message = L("CredentialResetPasswordRejected", user.UserName, string.Join(" ", refusals));
            return outcome;
        }

        // Zero's own credentials mail: user name, tenancy and the plain password, plus the e-mail
        // confirmation link it always carries -- which needs a fresh confirmation code to point at.
        user.SetNewEmailConfirmationCode();

        try
        {
            await _userEmailer.SendEmailActivationLinkAsync(user, activationUrlFormat, password);
        }
        catch (Exception ex)
        {
            Logger.Warn("Reset User Passwords: e-mail to user " + user.Id + " failed; password left unchanged.", ex);
            outcome.Message = L("CredentialResetEmailFailedPasswordUnchanged", user.UserName);
            return outcome;
        }

        outcome.EmailSent = true;

        try
        {
            CheckErrors(await UserManager.ChangePasswordAsync(user, password));

            // The e-mailed password is good for one sign-in: Zero's login flow then makes them choose their own.
            user.ShouldChangePasswordOnNextLogin = true;

            CheckErrors(await UserManager.UpdateAsync(user));

            await uow.CompleteAsync();
        }
        catch (Exception ex)
        {
            Logger.Error("Reset User Passwords: password for user " + user.Id + " was e-mailed but could not be saved.", ex);
            outcome.Message = L("CredentialResetEmailSentButNotSaved", user.UserName);
            return outcome;
        }

        outcome.PasswordReset = true;
        outcome.Message = L("CredentialResetDone", user.UserName, user.EmailAddress);
        return outcome;
    }

    /// <summary>
    /// The users this tool may touch. Applied to every id the client sends, however it arrived, so a crafted
    /// request cannot reach an administrator, the caller, an inactive account or another tenant's user (the
    /// tenant filter sees to the last).
    /// </summary>
    private async Task<IQueryable<User>> EligibleUsersQueryAsync()
    {
        var callerId = AbpSession.GetUserId();

        var adminRoleId = await _roleRepository.GetAll()
            .Where(r => r.Name == StaticRoleNames.Tenants.Admin)
            .Select(r => (int?)r.Id)
            .FirstOrDefaultAsync();

        return _userRepository.GetAll()
            .Where(u => u.IsActive && u.Id != callerId)
            .WhereIf(adminRoleId.HasValue,
                u => !_userRoleRepository.GetAll().Any(ur => ur.UserId == u.Id && ur.RoleId == adminRoleId.Value));
    }

    /// <summary>Only the columns the list can sort by; anything else falls back to the user name.</summary>
    private static IQueryable<User> ApplySorting(IQueryable<User> query, string sorting)
    {
        var descending = !sorting.IsNullOrWhiteSpace() && sorting.Trim().EndsWith(" desc", StringComparison.OrdinalIgnoreCase);
        var field = sorting.IsNullOrWhiteSpace() ? "" : sorting.Trim().Split(' ')[0].ToLowerInvariant();

        return field switch
        {
            "fullname" or "name" => descending
                ? query.OrderByDescending(u => u.Name).ThenByDescending(u => u.Surname)
                : query.OrderBy(u => u.Name).ThenBy(u => u.Surname),
            "emailaddress" => descending ? query.OrderByDescending(u => u.EmailAddress) : query.OrderBy(u => u.EmailAddress),
            _ => descending ? query.OrderByDescending(u => u.UserName) : query.OrderBy(u => u.UserName)
        };
    }
}
