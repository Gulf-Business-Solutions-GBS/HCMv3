using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Organizations;
using Abp.Runtime.Session;
using Abp.UI;
using HCMv3.Configuration;
using HCMv3.Navigation.Dto;
using HCMv3.OrganizationManagement;
using HCMv3.PersonnelAdministration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.Navigation;

/// <summary>
/// People lookup for the header search bar.
///
/// AUTHORIZATION is "any signed-in user of this tenant" -- <c>[AbpAuthorize]</c> with no permission named.
/// This is the same reasoning as <c>EmployeePhotoController</c>: an internal staff directory is ordinary
/// information, and gating it behind the Employees permission would leave most people with a search that
/// finds screens but never colleagues. The repository's tenant filter is what keeps one tenant's staff out
/// of another's results.
///
/// Note what is NOT returned: no salary, no personal contact details, no national ids. Name, code, job
/// title, department and work email are what identify a colleague in a dropdown, and nothing beyond that is needed to
/// pick the right one.
/// </summary>
[AbpAuthorize]
public class GlobalSearchAppService : HCMv3AppServiceBase, IGlobalSearchAppService
{
    private readonly IRepository<Employee> _employeeRepository;
    private readonly IRepository<EmployeeOrgAssignment> _employeeOrgAssignmentRepository;
    private readonly IRepository<JobTitle> _jobTitleRepository;
    private readonly IRepository<OrganizationUnit, long> _organizationUnitRepository;
    private readonly IRepository<OrgUnitProfile> _orgUnitProfileRepository;

    public GlobalSearchAppService(
        IRepository<Employee> employeeRepository,
        IRepository<EmployeeOrgAssignment> employeeOrgAssignmentRepository,
        IRepository<JobTitle> jobTitleRepository,
        IRepository<OrganizationUnit, long> organizationUnitRepository,
        IRepository<OrgUnitProfile> orgUnitProfileRepository)
    {
        _employeeRepository = employeeRepository;
        _employeeOrgAssignmentRepository = employeeOrgAssignmentRepository;
        _jobTitleRepository = jobTitleRepository;
        _organizationUnitRepository = organizationUnitRepository;
        _orgUnitProfileRepository = orgUnitProfileRepository;
    }

    /// <summary>
    /// ABP maps <c>Search*</c> to POST already, but the attribute is explicit so the intent survives a
    /// rename -- and NSwag needs the input generated as a class, not as query-string parameters.
    /// </summary>
    [HttpPost]
    public async Task<GlobalSearchPeopleResultDto> SearchPeople(SearchPeopleInput input)
    {
        var filter = (input.Filter ?? string.Empty).Trim();

        // One or two characters match most of the company: slow to answer, useless to read, and it would
        // push the action results out of a shared dropdown.
        if (filter.Length < SearchPeopleInput.MinimumFilterLength)
        {
            return new GlobalSearchPeopleResultDto();
        }

        var maxResultCount = input.MaxResultCount <= 0
            ? SearchPeopleInput.DefaultResultCount
            : Math.Min(input.MaxResultCount, SearchPeopleInput.MaxAllowedResultCount);

        // One row over the cap, purely to learn whether more exist without a second COUNT query.
        var matches = await _employeeRepository.GetAll()
            .Where(x =>
                x.FullNameEn.Contains(filter) ||
                x.FullNameAr.Contains(filter) ||
                x.Code.Contains(filter))
            .OrderBy(x => x.FullNameEn)
            .Take(maxResultCount + 1)
            .Select(x => new
            {
                x.Id,
                x.Code,
                x.FullNameEn,
                x.FullNameAr,
                x.Email,
                HasPhoto = x.PhotoPath != null || x.LegacyPhoto != null
            })
            .ToListAsync();

        var hasMore = matches.Count > maxResultCount;
        var page = matches.Take(maxResultCount).ToList();

        if (page.Count == 0)
        {
            return new GlobalSearchPeopleResultDto();
        }

        var employeeIds = page.Select(x => x.Id).ToList();
        var placements = await LoadPlacementsAsync(employeeIds);

        return new GlobalSearchPeopleResultDto
        {
            HasMore = hasMore,
            People = page
                .Select(employee =>
                {
                    placements.TryGetValue(employee.Id, out var placement);

                    return new GlobalSearchPersonDto
                    {
                        Id = employee.Id,
                        Code = employee.Code,
                        FullNameEn = employee.FullNameEn,
                        FullNameAr = employee.FullNameAr,
                        Email = employee.Email,
                        HasPhoto = employee.HasPhoto,
                        JobTitleEn = placement?.JobTitleEn,
                        JobTitleAr = placement?.JobTitleAr,
                        DepartmentEn = placement?.DepartmentEn,
                        DepartmentAr = placement?.DepartmentAr
                    };
                })
                .ToList()
        };
    }

    public async Task<GlobalSearchRecentResultDto> GetRecentSearches()
    {
        var stored = await ReadRecentAsync();
        var employeeIds = stored
            .Where(x => x.Kind == GlobalSearchRecentKind.Person)
            .Select(x => x.EmployeeId.Value)
            .ToList();

        var people = new Dictionary<int, GlobalSearchPersonDto>();
        if (employeeIds.Count > 0)
        {
            var found = await _employeeRepository.GetAll()
                .Where(x => employeeIds.Contains(x.Id))
                .Select(x => new GlobalSearchPersonDto
                {
                    Id = x.Id,
                    Code = x.Code,
                    FullNameEn = x.FullNameEn,
                    FullNameAr = x.FullNameAr,
                    Email = x.Email,
                    HasPhoto = x.PhotoPath != null || x.LegacyPhoto != null
                })
                .ToListAsync();

            var placements = await LoadPlacementsAsync(found.Select(x => x.Id).ToList());
            foreach (var person in found)
            {
                if (placements.TryGetValue(person.Id, out var placement))
                {
                    person.JobTitleEn = placement.JobTitleEn;
                    person.JobTitleAr = placement.JobTitleAr;
                    person.DepartmentEn = placement.DepartmentEn;
                    person.DepartmentAr = placement.DepartmentAr;
                }

                people[person.Id] = person;
            }
        }

        var result = new GlobalSearchRecentResultDto();
        foreach (var item in stored)
        {
            if (item.Kind == GlobalSearchRecentKind.Action)
            {
                // Whether the user may still open the screen is the client's call: it only shows routes in
                // the menu it built from the user's permissions.
                result.Items.Add(new GlobalSearchRecentItemDto { Kind = item.Kind, Route = item.Route });
            }
            else if (people.TryGetValue(item.EmployeeId.Value, out var person))
            {
                // A deleted employee is simply not found, and so not shown.
                result.Items.Add(new GlobalSearchRecentItemDto { Kind = item.Kind, EmployeeId = item.EmployeeId, Person = person });
            }
        }

        return result;
    }

    public async Task AddRecentSearch(GlobalSearchRecentItemInput input)
    {
        var entry = ToStoredItem(input);
        if (entry.Kind == GlobalSearchRecentKind.Person
            && !await _employeeRepository.GetAll().AnyAsync(x => x.Id == entry.EmployeeId.Value))
        {
            return;
        }

        var items = (await ReadRecentAsync()).Where(x => !IsSameEntry(x, entry)).ToList();
        items.Insert(0, entry);
        await WriteRecentAsync(items.Take(GlobalSearchRecentItemDto.MaxItems).ToList());
    }

    /// <summary>POST, not ABP's default DELETE for "Remove", so the input travels as a body like the others.</summary>
    [HttpPost]
    public async Task RemoveRecentSearch(GlobalSearchRecentItemInput input)
    {
        var entry = ToStoredItem(input);
        var items = await ReadRecentAsync();
        var kept = items.Where(x => !IsSameEntry(x, entry)).ToList();
        if (kept.Count != items.Count)
        {
            await WriteRecentAsync(kept);
        }
    }

    public async Task ClearRecentSearches()
    {
        await WriteRecentAsync(new List<StoredRecentItem>());
    }

    private StoredRecentItem ToStoredItem(GlobalSearchRecentItemInput input)
    {
        var kind = (input.Kind ?? string.Empty).Trim().ToLowerInvariant();
        if (kind == GlobalSearchRecentKind.Person && input.EmployeeId > 0)
        {
            return new StoredRecentItem { Kind = kind, EmployeeId = input.EmployeeId };
        }

        var route = (input.Route ?? string.Empty).Trim();
        if (kind == GlobalSearchRecentKind.Action
            && route.StartsWith('/')
            && route.Length <= GlobalSearchRecentItemDto.MaxRouteLength)
        {
            return new StoredRecentItem { Kind = kind, Route = route };
        }

        throw new UserFriendlyException(L("InvalidRecentSearch"));
    }

    private static bool IsSameEntry(StoredRecentItem a, StoredRecentItem b) =>
        a.Kind == b.Kind
        && (a.Kind == GlobalSearchRecentKind.Person
            ? a.EmployeeId == b.EmployeeId
            : string.Equals(a.Route, b.Route, StringComparison.OrdinalIgnoreCase));

    /// <summary>A missing or unreadable value reads as an empty list -- never an error in the header.</summary>
    private async Task<List<StoredRecentItem>> ReadRecentAsync()
    {
        var json = await SettingManager.GetSettingValueForUserAsync(
            AppSettings.UserManagement.RecentGlobalSearches, AbpSession.ToUserIdentifier());

        List<StoredRecentItem> items;
        try
        {
            items = JsonSerializer.Deserialize<List<StoredRecentItem>>(string.IsNullOrWhiteSpace(json) ? "[]" : json);
        }
        catch (JsonException)
        {
            return new List<StoredRecentItem>();
        }

        return (items ?? new List<StoredRecentItem>())
            .Where(x => x != null
                        && ((x.Kind == GlobalSearchRecentKind.Person && x.EmployeeId > 0)
                            || (x.Kind == GlobalSearchRecentKind.Action && !string.IsNullOrEmpty(x.Route))))
            .Take(GlobalSearchRecentItemDto.MaxItems)
            .ToList();
    }

    private Task WriteRecentAsync(List<StoredRecentItem> items) =>
        SettingManager.ChangeSettingForUserAsync(
            AbpSession.ToUserIdentifier(),
            AppSettings.UserManagement.RecentGlobalSearches,
            JsonSerializer.Serialize(items, RecentJsonOptions));

    private static readonly JsonSerializerOptions RecentJsonOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>What is kept in the setting: only enough to find the result again.</summary>
    private class StoredRecentItem
    {
        public string Kind { get; set; }

        public string Route { get; set; }

        public int? EmployeeId { get; set; }
    }

    /// <summary>
    /// Job title and department for the people on this page, from each one's LATEST org assignment.
    ///
    /// Not filtered to a date, matching the org charts: the search says where somebody sits now, and a
    /// date filter would blank the subtitle for anyone whose assignment rows had lapsed. Every lookup is
    /// optional, because an employee with no assignment, no job title or no unit must still be findable by
    /// name -- they simply appear without a subtitle.
    /// </summary>
    private async Task<Dictionary<int, Placement>> LoadPlacementsAsync(List<int> employeeIds)
    {
        // Deliberately three small queries rather than one chain of left joins. Joining the unit and then
        // its profile off a possibly-null unit is the kind of expression EF translates badly or not at
        // all, and the page is at most a couple of dozen rows -- there is nothing to win by being clever.
        var assignments = await _employeeOrgAssignmentRepository.GetAll()
            .Where(x => employeeIds.Contains(x.EmployeeId))
            .Select(x => new { x.EmployeeId, x.Id, x.StartDate, x.JobTitleId, x.OrganizationUnitId })
            .ToListAsync();

        var latest = assignments
            .GroupBy(x => x.EmployeeId)
            .Select(group => group
                .OrderByDescending(x => x.StartDate)
                .ThenByDescending(x => x.Id)
                .First())
            .ToList();

        if (latest.Count == 0)
        {
            return new Dictionary<int, Placement>();
        }

        var jobTitleIds = latest.Where(x => x.JobTitleId.HasValue).Select(x => x.JobTitleId.Value).Distinct().ToList();
        var jobTitles = jobTitleIds.Count == 0
            ? new List<JobTitle>()
            : await _jobTitleRepository.GetAll().Where(x => jobTitleIds.Contains(x.Id)).ToListAsync();

        var unitIds = latest.Where(x => x.OrganizationUnitId.HasValue).Select(x => x.OrganizationUnitId.Value).Distinct().ToList();
        var units = unitIds.Count == 0
            ? new List<UnitName>()
            : await (from unit in _organizationUnitRepository.GetAll()
                     join profile in _orgUnitProfileRepository.GetAll()
                         on unit.Id equals profile.OrganizationUnitId into profiles
                     from profile in profiles.DefaultIfEmpty()
                     where unitIds.Contains(unit.Id)
                     select new UnitName
                     {
                         Id = unit.Id,
                         NameEn = unit.DisplayName,
                         NameAr = profile != null ? profile.NameAr : null
                     })
                    .ToListAsync();

        return latest.ToDictionary(
            x => x.EmployeeId,
            x =>
            {
                var jobTitle = jobTitles.FirstOrDefault(t => t.Id == x.JobTitleId);
                var unit = units.FirstOrDefault(u => u.Id == x.OrganizationUnitId);

                return new Placement
                {
                    JobTitleEn = jobTitle?.NameEn,
                    JobTitleAr = jobTitle?.NameAr,
                    DepartmentEn = unit?.NameEn,
                    DepartmentAr = unit?.NameAr
                };
            });
    }

    private class UnitName
    {
        public long Id { get; set; }

        public string NameEn { get; set; }

        public string NameAr { get; set; }
    }

    private class Placement
    {
        public string JobTitleEn { get; set; }

        public string JobTitleAr { get; set; }

        public string DepartmentEn { get; set; }

        public string DepartmentAr { get; set; }
    }
}
