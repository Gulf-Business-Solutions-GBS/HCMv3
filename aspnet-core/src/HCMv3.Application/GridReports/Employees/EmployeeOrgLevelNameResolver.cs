using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Dependency;
using Abp.Domain.Repositories;
using Abp.Organizations;
using HCMv3.OrganizationManagement;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.GridReports.Employees;

/// <summary>
/// Resolves the legacy <c>OrgLevel1</c>..<c>OrgLevel7</c> report COLUMNS for a set of grid-report rows.
///
/// MODEL ADAPTATION (the single biggest deviation in this wave): legacy
/// <c>EmployeeOrgAssignment</c> carried seven discrete FK fields <c>OrgLevel1</c>..<c>OrgLevel7</c>, and the
/// reports simply read <c>r.OrgLevelN.NameEn/NameAr</c>. HCMv3's <see cref="EmployeeOrgAssignment"/>
/// collapses all seven into ONE <c>OrganizationUnitId</c> pointing at Zero's real, hierarchical
/// <c>Abp.Organizations.OrganizationUnit</c> tree (see <c>OrgHierarchyService</c> remarks for the
/// rationale). So "OrgLevel3" is no longer a stored field -- it is the ancestor-or-self of the
/// assignment's leaf unit whose <see cref="OrgUnitProfile.OrganizationUnitTypeId"/> is classified
/// <c>Level3</c>.
///
/// Doing that per row inside the LINQ projection would mean seven correlated sub-selects per row, so
/// instead the whole (small) org-unit/profile table is loaded ONCE and the levels are stamped onto the
/// already-materialized rows. Both the paged read and the export call the same stamping method, so the
/// export cannot drift from the grid.
///
/// Legacy also localized the column CAPTION via <c>OrgService.GetOrgUnitTypeColName</c>; in this port the
/// caption is a localization key/level name resolved on the client, and only the VALUES come from here.
/// </summary>
public class EmployeeOrgLevelNameResolver : ITransientDependency
{
    /// <summary>Legacy OrgLevel1..OrgLevel7 map onto these <see cref="OrganizationUnitType"/> codes.</summary>
    public static readonly string[] LevelCodes =
    {
        OrganizationUnitType.Level1Code,
        OrganizationUnitType.Level2Code,
        OrganizationUnitType.Level3Code,
        OrganizationUnitType.Level4Code,
        OrganizationUnitType.Level5Code,
        OrganizationUnitType.Level6Code,
        OrganizationUnitType.Level7Code
    };

    private readonly IRepository<OrgUnitProfile> _orgUnitProfileRepository;
    private readonly IRepository<OrganizationUnit, long> _organizationUnitRepository;

    public EmployeeOrgLevelNameResolver(
        IRepository<OrgUnitProfile> orgUnitProfileRepository,
        IRepository<OrganizationUnit, long> organizationUnitRepository)
    {
        _orgUnitProfileRepository = orgUnitProfileRepository;
        _organizationUnitRepository = organizationUnitRepository;
    }

    public async Task<EmployeeOrgLevelLookup> BuildLookupAsync(bool useArabicNames)
    {
        var profiles = await _orgUnitProfileRepository.GetAll()
            .Select(p => new OrgLevelProfileInfo
            {
                OrganizationUnitCode = p.OrganizationUnit.Code,
                LevelCode = p.OrganizationUnitType.Code,
                Name = useArabicNames ? p.NameAr : p.OrganizationUnit.DisplayName
            })
            .ToListAsync();

        var unitCodes = await _organizationUnitRepository.GetAll()
            .Select(u => new { u.Id, u.Code })
            .ToDictionaryAsync(u => u.Id, u => u.Code);

        return new EmployeeOrgLevelLookup(profiles, unitCodes);
    }

    public class OrgLevelProfileInfo
    {
        public string OrganizationUnitCode { get; set; }

        public string LevelCode { get; set; }

        public string Name { get; set; }
    }
}

/// <summary>
/// In-memory ancestor lookup produced by <see cref="EmployeeOrgLevelNameResolver"/>. Zero's
/// <c>OrganizationUnit.Code</c> is a dot-separated hierarchical path ("00001.00002.00003"), so an
/// ancestor is exactly a string prefix of its descendant -- no recursion needed.
/// </summary>
public class EmployeeOrgLevelLookup
{
    private readonly List<EmployeeOrgLevelNameResolver.OrgLevelProfileInfo> _profiles;
    private readonly Dictionary<long, string> _unitCodes;

    public EmployeeOrgLevelLookup(
        List<EmployeeOrgLevelNameResolver.OrgLevelProfileInfo> profiles,
        Dictionary<long, string> unitCodes)
    {
        _profiles = profiles;
        _unitCodes = unitCodes;
    }

    /// <summary>
    /// Returns the seven legacy OrgLevel1..OrgLevel7 names for the given leaf org unit. Missing levels come
    /// back as <c>""</c> -- legacy also wrote empty string, never null, for an absent OrgLevelN.
    /// </summary>
    public string[] GetLevelNames(long? leafOrganizationUnitId)
    {
        var result = new string[EmployeeOrgLevelNameResolver.LevelCodes.Length];
        for (var i = 0; i < result.Length; i++)
        {
            result[i] = string.Empty;
        }

        if (leafOrganizationUnitId == null || !_unitCodes.TryGetValue(leafOrganizationUnitId.Value, out var leafCode))
        {
            return result;
        }

        for (var i = 0; i < EmployeeOrgLevelNameResolver.LevelCodes.Length; i++)
        {
            var levelCode = EmployeeOrgLevelNameResolver.LevelCodes[i];
            var match = _profiles.FirstOrDefault(p =>
                p.LevelCode == levelCode &&
                p.OrganizationUnitCode != null &&
                leafCode.StartsWith(p.OrganizationUnitCode));

            result[i] = match?.Name ?? string.Empty;
        }

        return result;
    }
}
