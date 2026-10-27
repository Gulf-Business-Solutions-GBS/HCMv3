using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Authorization.Users;
using Abp.Domain.Repositories;
using Abp.Extensions;
using Abp.Linq.Extensions;
using Abp.Runtime.Session;
using HCMv3.Authorization;
using HCMv3.Authorization.Roles;
using HCMv3.Authorization.Users;
using HCMv3.Licensing.Dto;
using HCMv3.PersonnelAdministration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.Licensing;

/// <inheritdoc cref="ILicenceAssignmentAppService"/>
/// <remarks>
/// The rules (seat counts, administrators exempt, what is counted) live in <see cref="IUserModuleLicenceManager"/>;
/// this service only lists and forwards. Rows are built by hand rather than with ObjectMapper because each one joins
/// a user with its licence rows, its role and its employee -- there is no single entity to map from.
/// </remarks>
[AbpAuthorize(AppPermissions.Pages_Administration_Tools_LicenceAssignment)]
public class LicenceAssignmentAppService : HCMv3AppServiceBase, ILicenceAssignmentAppService
{
    private readonly IUserModuleLicenceManager _licenceManager;
    private readonly IUserModuleLicenceChecker _licenceChecker;
    private readonly IRepository<UserModuleLicence> _licenceRepository;
    private readonly IRepository<User, long> _userRepository;
    private readonly IRepository<UserRole, long> _userRoleRepository;
    private readonly IRepository<Role> _roleRepository;
    private readonly IRepository<Employee> _employeeRepository;

    public LicenceAssignmentAppService(
        IUserModuleLicenceManager licenceManager,
        IUserModuleLicenceChecker licenceChecker,
        IRepository<UserModuleLicence> licenceRepository,
        IRepository<User, long> userRepository,
        IRepository<UserRole, long> userRoleRepository,
        IRepository<Role> roleRepository,
        IRepository<Employee> employeeRepository)
    {
        _licenceManager = licenceManager;
        _licenceChecker = licenceChecker;
        _licenceRepository = licenceRepository;
        _userRepository = userRepository;
        _userRoleRepository = userRoleRepository;
        _roleRepository = roleRepository;
        _employeeRepository = employeeRepository;
    }

    public async Task<GetLicenceAssignmentSummaryOutput> GetSummary()
    {
        var usage = await _licenceManager.GetUsageAsync();

        return new GetLicenceAssignmentSummaryOutput
        {
            IsEnforced = await _licenceChecker.IsEnforcedAsync(AbpSession.GetTenantId()),
            Modules = usage.Select(ToDto).ToList()
        };
    }

    [HttpPost]
    public async Task<PagedResultDto<LicenceAssignmentUserDto>> GetUsers(GetLicenceAssignmentUsersInput input)
    {
        var query = _userRepository.GetAll()
            .WhereIf(!input.Filter.IsNullOrWhiteSpace(),
                u => u.UserName.Contains(input.Filter)
                     || u.Name.Contains(input.Filter)
                     || u.Surname.Contains(input.Filter)
                     || u.EmailAddress.Contains(input.Filter));

        if (input.Module.HasValue && input.IsAssigned.HasValue)
        {
            var module = input.Module.Value;
            query = input.IsAssigned.Value
                ? query.Where(u => _licenceRepository.GetAll().Any(l => l.UserId == u.Id && l.Module == module))
                : query.Where(u => !_licenceRepository.GetAll().Any(l => l.UserId == u.Id && l.Module == module));
        }

        var totalCount = await query.CountAsync();

        var users = await ApplySorting(query, input.Sorting)
            .PageBy(input)
            .ToListAsync();

        var userIds = users.Select(u => u.Id).ToList();
        var nullableUserIds = userIds.Select(id => (long?)id).ToList();

        var licences = await _licenceRepository.GetAll()
            .Where(l => userIds.Contains(l.UserId))
            .Select(l => new { l.UserId, l.Module })
            .ToListAsync();

        var adminRoleId = await _roleRepository.GetAll()
            .Where(r => r.Name == StaticRoleNames.Tenants.Admin)
            .Select(r => (int?)r.Id)
            .FirstOrDefaultAsync();

        var adminUserIds = adminRoleId.HasValue
            ? await _userRoleRepository.GetAll()
                .Where(ur => ur.RoleId == adminRoleId.Value && userIds.Contains(ur.UserId))
                .Select(ur => ur.UserId)
                .ToListAsync()
            : new List<long>();

        var employeeCodes = await _employeeRepository.GetAll()
            .Where(e => nullableUserIds.Contains(e.UserId))
            .Select(e => new { e.UserId, e.Code })
            .ToListAsync();

        var items = users.Select(u => new LicenceAssignmentUserDto
        {
            Id = u.Id,
            UserName = u.UserName,
            FullName = (u.Name + " " + u.Surname).Trim(),
            EmailAddress = u.EmailAddress,
            IsActive = u.IsActive,
            IsAdministrator = adminUserIds.Contains(u.Id),
            EmployeeCode = employeeCodes.FirstOrDefault(e => e.UserId == u.Id)?.Code,
            Modules = licences.Where(l => l.UserId == u.Id).Select(l => l.Module).Distinct().OrderBy(m => m).ToList()
        }).ToList();

        return new PagedResultDto<LicenceAssignmentUserDto>(totalCount, items);
    }

    public async Task<LicenceModuleUsageDto> SetUserModuleLicence(SetUserModuleLicenceInput input)
    {
        if (input.IsAssigned)
        {
            await _licenceManager.AssignAsync(input.UserId, input.Module);
        }
        else
        {
            await _licenceManager.UnassignAsync(input.UserId, input.Module);
        }

        await CurrentUnitOfWork.SaveChangesAsync();

        return ToDto(await _licenceManager.GetUsageAsync(input.Module));
    }

    private LicenceModuleUsageDto ToDto(LicenceModuleUsage usage)
    {
        return new LicenceModuleUsageDto
        {
            Module = usage.Module,
            ModuleName = L(LicenceModules.Get(usage.Module).DisplayNameKey),
            IsModuleEnabled = usage.IsModuleEnabled,
            SeatCount = usage.SeatCount,
            UsedCount = usage.UsedCount,
            IsUnlimited = usage.IsUnlimited,
            IsOverLimit = !usage.IsUnlimited && usage.UsedCount > usage.SeatCount
        };
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
