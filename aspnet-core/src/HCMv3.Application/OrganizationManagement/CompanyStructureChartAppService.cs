using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Extensions;
using Abp.Linq.Extensions;
using Abp.Organizations;
using HCMv3.Authorization;
using HCMv3.OrganizationManagement.Dto;
using HCMv3.PersonnelAdministration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.OrganizationManagement;

/// <summary>
/// Read-only feed for the Company Structure Overview chart -- the first of the three org-chart screens
/// (the Employee and Position charts reuse the same Angular renderer against their own services).
///
/// The screen is a drill-down explorer, not a whole-tree render: it shows one focus node with its
/// immediate children laid out as a card grid, and descends when the user clicks a child. So every method
/// here is scoped to a single level, and the response size is bounded by one node's fan-out rather than by
/// the size of the tenant's tree.
///
/// HEADCOUNTS: cards show where employees sit NOW, from each employee's latest
/// <see cref="EmployeeOrgAssignment"/> -- the row that names their <c>OrganizationUnit</c>. Rather than one
/// aggregate query per card, <see cref="GetNode"/> reads the assignments once and folds the direct/total
/// figures out of that single result in memory (see <see cref="LoadSubtreeHeadcountsAsync"/>).
///
/// NO DATE FILTER: an earlier version took an "as of" date and counted the assignments in force then. It
/// was removed because the chart answers "who sits where now", and dating it zeroed every card as soon as
/// the underlying rows had lapsed.
///
/// Descendant and ancestor resolution both use Zero's own hierarchical dot-separated
/// <c>OrganizationUnit.Code</c> (ancestor "00001.00002" is a string-prefix of descendant
/// "00001.00002.00003") rather than hand-rolled parent-pointer recursion -- the same technique
/// <c>OrgHierarchyService</c> already relies on.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_OrganizationManagement_CompanyStructureChart)]
public class CompanyStructureChartAppService : HCMv3AppServiceBase, ICompanyStructureChartAppService
{
    /// <summary>Separator between segments of Zero's hierarchical <c>OrganizationUnit.Code</c>.</summary>
    private const string TreeCodeSeparator = ".";

    private readonly IRepository<OrganizationUnit, long> _organizationUnitRepository;
    private readonly IRepository<OrgUnitProfile> _profileRepository;
    private readonly IRepository<OrganizationUnitType> _organizationUnitTypeRepository;
    private readonly IRepository<EmployeeOrgAssignment> _employeeOrgAssignmentRepository;

    public CompanyStructureChartAppService(
        IRepository<OrganizationUnit, long> organizationUnitRepository,
        IRepository<OrgUnitProfile> profileRepository,
        IRepository<OrganizationUnitType> organizationUnitTypeRepository,
        IRepository<EmployeeOrgAssignment> employeeOrgAssignmentRepository)
    {
        _organizationUnitRepository = organizationUnitRepository;
        _profileRepository = profileRepository;
        _organizationUnitTypeRepository = organizationUnitTypeRepository;
        _employeeOrgAssignmentRepository = employeeOrgAssignmentRepository;
    }

    /// <summary>
    /// ABP maps <c>Get*</c> to HTTP GET by default, which would make NSwag emit this custom input DTO as
    /// loose query-string parameters and never generate the class for the Angular client -- hence the
    /// explicit <c>[HttpPost]</c> (see the repository's application-layer rules).
    /// </summary>
    [HttpPost]
    public async Task<CompanyStructureNodeDto> GetNode(GetCompanyStructureNodeInput input)
    {
        var focusUnit = await ResolveFocusUnitAsync(input.OrganizationUnitId);
        if (focusUnit == null)
        {
            // Tenant has no organization units at all -- an empty result, not an error: the screen draws
            // its own "nothing configured yet" state rather than showing a failure.
            return new CompanyStructureNodeDto();
        }

        var childUnits = await _organizationUnitRepository.GetAll()
            .Where(x => x.ParentId == focusUnit.Id)
            .OrderBy(x => x.Code)
            .ToListAsync();

        var cardUnits = new List<OrganizationUnit> { focusUnit };
        cardUnits.AddRange(childUnits);

        var cardUnitIds = cardUnits.Select(x => x.Id).ToList();
        var profiles = await LoadProfilesAsync(cardUnitIds);
        var organizationUnitTypes = await _organizationUnitTypeRepository.GetAllListAsync();
        var headcounts = await LoadSubtreeHeadcountsAsync(focusUnit.Code);
        var childUnitCounts = await LoadChildUnitCountsAsync(cardUnitIds);

        return new CompanyStructureNodeDto
        {
            Focus = ToNodeDto(focusUnit, profiles, organizationUnitTypes, headcounts, childUnitCounts),
            Children = childUnits
                .Select(x => ToNodeDto(x, profiles, organizationUnitTypes, headcounts, childUnitCounts))
                .ToList(),
            Ancestors = await LoadAncestorsAsync(focusUnit)
        };
    }

    public async Task<CompanyStructureChartFilterOptionsDto> GetFilterOptions()
    {
        var rootUnits = await _organizationUnitRepository.GetAll()
            .Where(x => x.ParentId == null)
            .OrderBy(x => x.Code)
            .ToListAsync();

        var profiles = await LoadProfilesAsync(rootUnits.Select(x => x.Id).ToList());
        var organizationUnitTypes = await _organizationUnitTypeRepository.GetAllListAsync();

        return new CompanyStructureChartFilterOptionsDto
        {
            Roots = rootUnits.Select(x => ToSummaryDto(x, profiles, organizationUnitTypes)).ToList(),
            OrganizationUnitTypes = organizationUnitTypes
                .OrderBy(x => x.Code)
                .Select(x => new OrgChartTypeOptionDto
                {
                    Id = x.Id,
                    Code = x.Code,
                    NameEn = x.NameEn,
                    NameAr = x.NameAr
                })
                .ToList()
        };
    }

    [HttpPost]
    public async Task<List<OrgChartNodeSummaryDto>> SearchNodes(SearchCompanyStructureNodesInput input)
    {
        var maxResultCount = input.MaxResultCount <= 0
            ? SearchCompanyStructureNodesInput.DefaultResultCount
            : Math.Min(input.MaxResultCount, SearchCompanyStructureNodesInput.MaxAllowedResultCount);

        var filter = input.Filter;

        var query = from unit in _organizationUnitRepository.GetAll()
                    join profile in _profileRepository.GetAll() on unit.Id equals profile.OrganizationUnitId into joined
                    from profile in joined.DefaultIfEmpty()
                    select new { unit, profile };

        query = query
            .WhereIf(!filter.IsNullOrWhiteSpace(), x =>
                x.unit.DisplayName.Contains(filter) ||
                (x.profile != null && x.profile.NameAr.Contains(filter)) ||
                (x.profile != null && x.profile.Code.Contains(filter)))
            .WhereIf(input.OrganizationUnitTypeId.HasValue, x =>
                x.profile != null && x.profile.OrganizationUnitTypeId == input.OrganizationUnitTypeId.Value);

        if (input.RootOrganizationUnitId.HasValue)
        {
            // Confine the search to the company structure the user currently has selected.
            var rootUnit = await _organizationUnitRepository.FirstOrDefaultAsync(input.RootOrganizationUnitId.Value);
            if (rootUnit == null)
            {
                return new List<OrgChartNodeSummaryDto>();
            }

            var rootTreeCode = rootUnit.Code;
            var rootDescendantPrefix = rootTreeCode + TreeCodeSeparator;
            query = query.Where(x => x.unit.Code == rootTreeCode || x.unit.Code.StartsWith(rootDescendantPrefix));
        }

        var matches = await query
            .OrderBy(x => x.unit.Code)
            .Take(maxResultCount)
            .ToListAsync();

        var organizationUnitTypes = await _organizationUnitTypeRepository.GetAllListAsync();
        var matchedProfiles = matches.Where(x => x.profile != null).Select(x => x.profile).ToList();

        return matches
            .Select(x => ToSummaryDto(x.unit, matchedProfiles, organizationUnitTypes))
            .ToList();
    }

    /// <summary>
    /// Resolves the node to draw: the requested one, or -- when the caller sent nothing, as the screen
    /// does on first load -- the first root unit in tree order.
    /// </summary>
    private async Task<OrganizationUnit> ResolveFocusUnitAsync(long? organizationUnitId)
    {
        if (organizationUnitId.HasValue)
        {
            return await _organizationUnitRepository.FirstOrDefaultAsync(organizationUnitId.Value);
        }

        return await _organizationUnitRepository.GetAll()
            .Where(x => x.ParentId == null)
            .OrderBy(x => x.Code)
            .FirstOrDefaultAsync();
    }

    private async Task<List<OrgUnitProfile>> LoadProfilesAsync(List<long> organizationUnitIds)
    {
        if (organizationUnitIds.Count == 0)
        {
            return new List<OrgUnitProfile>();
        }

        return await _profileRepository.GetAll()
            .Where(x => organizationUnitIds.Contains(x.OrganizationUnitId))
            .ToListAsync();
    }

    /// <summary>
    /// Headcount per organization unit tree code, from each employee's CURRENT org assignment. The focus
    /// card and all of its child cards are folded out of this one result in memory -- the direct figure by
    /// exact code match, the total by code prefix -- rather than two aggregate queries per card.
    ///
    /// NOT FILTERED TO A DATE. An earlier version took an "as of" date and counted the assignments in force
    /// then; that was dropped because the chart answers "who sits where now", and dating it zeroed every
    /// card the moment the underlying rows had lapsed.
    ///
    /// The latest row per employee has to be picked before the subtree is narrowed, not after: an employee
    /// whose newest assignment sits elsewhere in the company must not still be counted here on the strength
    /// of an older one. That reduction cannot be expressed as a grouped query, so the assignments come back
    /// projected to four columns -- a couple of thousand rows at this tenant's scale -- and are reduced
    /// here. Ties on StartDate fall to the highest Id, the later-entered row.
    /// </summary>
    private async Task<List<SubtreeHeadcount>> LoadSubtreeHeadcountsAsync(string focusTreeCode)
    {
        var descendantPrefix = focusTreeCode + TreeCodeSeparator;

        var assignments = await (from assignment in _employeeOrgAssignmentRepository.GetAll()
                                 // EmployeeOrgAssignment.OrganizationUnitId is nullable, OrganizationUnit.Id
                                 // is not -- the cast keeps the join key types matched; null-keyed rows
                                 // simply never match.
                                 join unit in _organizationUnitRepository.GetAll()
                                     on assignment.OrganizationUnitId equals (long?)unit.Id
                                 select new
                                 {
                                     assignment.EmployeeId,
                                     assignment.Id,
                                     assignment.StartDate,
                                     UnitCode = unit.Code
                                 })
                                .ToListAsync();

        return assignments
            .GroupBy(x => x.EmployeeId)
            .Select(group => group
                .OrderByDescending(x => x.StartDate)
                .ThenByDescending(x => x.Id)
                .First())
            .Where(x => x.UnitCode == focusTreeCode || x.UnitCode.StartsWith(descendantPrefix))
            .GroupBy(x => x.UnitCode)
            .Select(grouped => new SubtreeHeadcount
            {
                TreeCode = grouped.Key,
                // One row per employee survives the reduction above, so this is already a headcount.
                EmployeeCount = grouped.Count()
            })
            .ToList();
    }

    private async Task<Dictionary<long, int>> LoadChildUnitCountsAsync(List<long> parentIds)
    {
        if (parentIds.Count == 0)
        {
            return new Dictionary<long, int>();
        }

        var counts = await _organizationUnitRepository.GetAll()
            .Where(x => x.ParentId.HasValue && parentIds.Contains(x.ParentId.Value))
            .GroupBy(x => x.ParentId.Value)
            .Select(grouped => new { ParentId = grouped.Key, Count = grouped.Count() })
            .ToListAsync();

        return counts.ToDictionary(x => x.ParentId, x => x.Count);
    }

    /// <summary>
    /// Root-first breadcrumb above the focus node, resolved in one query by string-prefix on Zero's
    /// hierarchical code -- every ancestor's code is a prefix of the focus node's own.
    /// </summary>
    private async Task<List<OrgChartNodeSummaryDto>> LoadAncestorsAsync(OrganizationUnit focusUnit)
    {
        var focusTreeCode = focusUnit.Code;

        var ancestorUnits = await _organizationUnitRepository.GetAll()
            .Where(x => x.Id != focusUnit.Id && focusTreeCode.StartsWith(x.Code))
            .OrderBy(x => x.Code)
            .ToListAsync();

        if (ancestorUnits.Count == 0)
        {
            return new List<OrgChartNodeSummaryDto>();
        }

        var profiles = await LoadProfilesAsync(ancestorUnits.Select(x => x.Id).ToList());
        var organizationUnitTypes = await _organizationUnitTypeRepository.GetAllListAsync();

        return ancestorUnits.Select(x => ToSummaryDto(x, profiles, organizationUnitTypes)).ToList();
    }

    private OrgChartNodeDto ToNodeDto(
        OrganizationUnit unit,
        List<OrgUnitProfile> profiles,
        List<OrganizationUnitType> organizationUnitTypes,
        List<SubtreeHeadcount> headcounts,
        Dictionary<long, int> childUnitCounts)
    {
        var profile = profiles.FirstOrDefault(x => x.OrganizationUnitId == unit.Id);
        var organizationUnitType = profile == null
            ? null
            : organizationUnitTypes.FirstOrDefault(x => x.Id == profile.OrganizationUnitTypeId);

        var descendantPrefix = unit.Code + TreeCodeSeparator;

        return new OrgChartNodeDto
        {
            Id = unit.Id,
            TreeCode = unit.Code,
            Code = profile?.Code,
            NameEn = unit.DisplayName,
            NameAr = profile?.NameAr,
            ParentId = unit.ParentId,
            OrganizationUnitTypeId = profile?.OrganizationUnitTypeId,
            OrganizationUnitTypeCode = organizationUnitType?.Code,
            OrganizationUnitTypeNameEn = organizationUnitType?.NameEn,
            OrganizationUnitTypeNameAr = organizationUnitType?.NameAr,
            DirectEmployeeCount = headcounts
                .Where(x => x.TreeCode == unit.Code)
                .Sum(x => x.EmployeeCount),
            TotalEmployeeCount = headcounts
                .Where(x => x.TreeCode == unit.Code || x.TreeCode.StartsWith(descendantPrefix))
                .Sum(x => x.EmployeeCount),
            ChildUnitCount = childUnitCounts.TryGetValue(unit.Id, out var childUnitCount) ? childUnitCount : 0
        };
    }

    private OrgChartNodeSummaryDto ToSummaryDto(
        OrganizationUnit unit,
        List<OrgUnitProfile> profiles,
        List<OrganizationUnitType> organizationUnitTypes)
    {
        var profile = profiles.FirstOrDefault(x => x.OrganizationUnitId == unit.Id);
        var organizationUnitType = profile == null
            ? null
            : organizationUnitTypes.FirstOrDefault(x => x.Id == profile.OrganizationUnitTypeId);

        return new OrgChartNodeSummaryDto
        {
            Id = unit.Id,
            TreeCode = unit.Code,
            Code = profile?.Code,
            NameEn = unit.DisplayName,
            NameAr = profile?.NameAr,
            ParentId = unit.ParentId,
            OrganizationUnitTypeNameEn = organizationUnitType?.NameEn,
            OrganizationUnitTypeNameAr = organizationUnitType?.NameAr
        };
    }

    /// <summary>Per-organization-unit as-of headcount, the shape returned by the single grouped query.</summary>
    private class SubtreeHeadcount
    {
        public string TreeCode { get; set; }

        public int EmployeeCount { get; set; }
    }
}
