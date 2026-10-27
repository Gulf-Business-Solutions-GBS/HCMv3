using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Extensions;
using Abp.Linq.Extensions;
using Abp.UI;
using HCMv3.Authorization;
using HCMv3.Authorization.Users;
using HCMv3.Licensing;
using HCMv3.PersonnelAdministration.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.PersonnelAdministration;

/// <summary>
/// Administers the <c>Employee.UserId</c> link. See <see cref="Employee.UserId"/> for why this link exists
/// and why it replaces the legacy <c>Employee.LoginID == User.UserName</c> string match.
///
/// The link is stored one-directionally on Employee and is deliberately not a database FK, so this service
/// owns the invariant the database can only half-state: a user may be linked to at most one employee (also
/// backed by a filtered unique index), and both sides must belong to the current tenant (guaranteed by ABP's
/// tenant filter on both repositories).
/// </summary>
[AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_Employees_ManageUserLink)]
public class EmployeeUserLinkAppService : HCMv3AppServiceBase, IEmployeeUserLinkAppService
{
    private readonly IRepository<Employee> _employeeRepository;
    private readonly IRepository<User, long> _userRepository;
    private readonly IUserModuleLicenceManager _licenceManager;

    public EmployeeUserLinkAppService(
        IRepository<Employee> employeeRepository,
        IRepository<User, long> userRepository,
        IUserModuleLicenceManager licenceManager)
    {
        _licenceManager = licenceManager;
        _employeeRepository = employeeRepository;
        _userRepository = userRepository;
    }

    public async Task<EmployeeUserLinkDto> GetEmployeeUserLink(EntityDto input)
    {
        var employee = await _employeeRepository.GetAsync(input.Id);

        var dto = new EmployeeUserLinkDto
        {
            EmployeeId = employee.Id,
            EmployeeCode = employee.Code,
            EmployeeFullNameEn = employee.FullNameEn,
            EmployeeFullNameAr = employee.FullNameAr,
            LoginID = employee.LoginID,
            UserId = employee.UserId
        };

        if (!employee.UserId.HasValue)
        {
            return dto;
        }

        var user = await _userRepository.FirstOrDefaultAsync(employee.UserId.Value);
        if (user == null)
        {
            // The link points at a user that no longer exists -- possible precisely because this is not a
            // database FK. Reported via UserExists rather than silently blanked, so an admin can re-link.
            return dto;
        }

        dto.UserExists = true;
        dto.UserName = user.UserName;
        dto.UserFullName = user.FullName;
        dto.UserEmailAddress = user.EmailAddress;
        dto.UserIsActive = user.IsActive;

        return dto;
    }

    public async Task LinkUser(LinkEmployeeUserInput input)
    {
        var employee = await _employeeRepository.GetAsync(input.EmployeeId);

        var user = await _userRepository.FirstOrDefaultAsync(input.UserId);
        if (user == null)
        {
            throw new UserFriendlyException(L("UserNotFound"));
        }

        var otherEmployee = await _employeeRepository.FirstOrDefaultAsync(
            e => e.UserId == input.UserId && e.Id != employee.Id);

        if (otherEmployee != null)
        {
            throw new UserFriendlyException(L("UserIsAlreadyLinkedToAnotherEmployee", otherEmployee.Code));
        }

        employee.UserId = input.UserId;

        await _employeeRepository.UpdateAsync(employee);

        // Legacy gave a new employee user its ESS and Mobile licences when seats were free. Skips quietly when
        // they are not -- the link itself must never fail over licensing.
        await _licenceManager.AssignOnEmployeeLinkAsync(input.UserId);
    }

    public async Task UnlinkUser(EntityDto input)
    {
        var employee = await _employeeRepository.GetAsync(input.Id);

        employee.UserId = null;

        await _employeeRepository.UpdateAsync(employee);
    }

    [HttpPost]
    public async Task<PagedResultDto<LinkableUserDto>> GetLinkableUsers(GetLinkableUsersInput input)
    {
        input.Normalize();

        // Every user already spoken for, keyed by user id -- used both to exclude them and, for the rows
        // that survive the filter, to explain the exclusion in the picker instead of just omitting them.
        var linkedByUserId = await _employeeRepository.GetAll()
            .Where(e => e.UserId.HasValue)
            .Select(e => new { UserId = e.UserId.Value, EmployeeId = e.Id, e.Code })
            .ToDictionaryAsync(x => x.UserId);

        var takenUserIds = linkedByUserId.Keys.ToList();

        var currentlyLinkedUserId = input.ForEmployeeId.HasValue
            ? await _employeeRepository.GetAll()
                .Where(e => e.Id == input.ForEmployeeId.Value)
                .Select(e => e.UserId)
                .FirstOrDefaultAsync()
            : null;

        var filter = input.Filter;

        var query = _userRepository.GetAll()
            .WhereIf(!filter.IsNullOrWhiteSpace(), u =>
                u.UserName.Contains(filter) ||
                u.Name.Contains(filter) ||
                u.Surname.Contains(filter) ||
                u.EmailAddress.Contains(filter))
            // The employee being edited keeps its own current selection visible, or the edit form would
            // render an empty picker for an employee that is in fact already linked.
            .WhereIf(input.OnlyUnlinked, u =>
                !takenUserIds.Contains(u.Id) ||
                (currentlyLinkedUserId.HasValue && u.Id == currentlyLinkedUserId.Value));

        var totalCount = await query.CountAsync();

        var users = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        var items = users.Select(u =>
        {
            var dto = new LinkableUserDto
            {
                Id = u.Id,
                UserName = u.UserName,
                Name = u.Name,
                Surname = u.Surname,
                EmailAddress = u.EmailAddress,
                IsActive = u.IsActive
            };

            if (linkedByUserId.TryGetValue(u.Id, out var linked))
            {
                dto.LinkedEmployeeId = linked.EmployeeId;
                dto.LinkedEmployeeCode = linked.Code;
            }

            return dto;
        }).ToList();

        return new PagedResultDto<LinkableUserDto>(totalCount, items);
    }

    public async Task<AutoLinkEmployeeUsersResultDto> AutoLinkByLoginId()
    {
        var result = new AutoLinkEmployeeUsersResultDto();

        var unlinkedEmployees = await _employeeRepository.GetAll()
            .Where(e => !e.UserId.HasValue && e.LoginID != null && e.LoginID != "")
            .ToListAsync();

        if (unlinkedEmployees.Count == 0)
        {
            return result;
        }

        // A LoginID shared by two unlinked employees is unresolvable by definition: picking either one would
        // be a guess about whose identity a login represents. Both are reported and neither is linked.
        var ambiguousLoginIds = unlinkedEmployees
            .GroupBy(e => e.LoginID, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        var ambiguousLoginIdSet = new HashSet<string>(ambiguousLoginIds, StringComparer.OrdinalIgnoreCase);

        var takenUserIdSet = new HashSet<long>(
            await _employeeRepository.GetAll()
                .Where(e => e.UserId.HasValue)
                .Select(e => e.UserId.Value)
                .ToListAsync());

        var loginIds = unlinkedEmployees.Select(e => e.LoginID).Distinct().ToList();

        var matchingUsers = await _userRepository.GetAll()
            .Where(u => loginIds.Contains(u.UserName))
            .Select(u => new { u.Id, u.UserName })
            .ToListAsync();

        var userIdByUserName = matchingUsers
            .GroupBy(u => u.UserName, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First().Id, StringComparer.OrdinalIgnoreCase);

        foreach (var employee in unlinkedEmployees)
        {
            if (ambiguousLoginIdSet.Contains(employee.LoginID))
            {
                result.AmbiguousLoginIdCount++;
                continue;
            }

            if (!userIdByUserName.TryGetValue(employee.LoginID, out var userId))
            {
                result.NoMatchingUserCount++;
                continue;
            }

            if (takenUserIdSet.Contains(userId))
            {
                result.UserAlreadyLinkedCount++;
                continue;
            }

            employee.UserId = userId;
            takenUserIdSet.Add(userId);
            result.LinkedCount++;

            await _employeeRepository.UpdateAsync(employee);
            await _licenceManager.AssignOnEmployeeLinkAsync(userId);
        }

        result.AmbiguousLoginIds = ambiguousLoginIds;

        return result;
    }

    public async Task<List<DuplicateLoginIdDto>> GetDuplicateLoginIds()
    {
        var employees = await _employeeRepository.GetAll()
            .Where(e => e.LoginID != null && e.LoginID != "")
            .Select(e => new
            {
                EmployeeId = e.Id,
                e.Code,
                e.FullNameEn,
                e.Status,
                e.UserId,
                e.LoginID
            })
            .ToListAsync();

        // Grouped case-insensitively because SQL Server's default collation is case-insensitive: "Ahmed.H"
        // and "ahmed.h" already resolve to the same login at sign-in, so they are a duplicate here too.
        return employees
            .GroupBy(e => e.LoginID, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .OrderBy(g => g.Key)
            .Select(g => new DuplicateLoginIdDto
            {
                LoginID = g.Key,
                Employees = g
                    .OrderByDescending(e => e.UserId.HasValue)
                    .ThenBy(e => e.Code)
                    .Select(e => new DuplicateLoginIdEmployeeDto
                    {
                        EmployeeId = e.EmployeeId,
                        Code = e.Code,
                        FullNameEn = e.FullNameEn,
                        Status = e.Status,
                        UserId = e.UserId
                    })
                    .ToList()
            })
            .ToList();
    }
}
