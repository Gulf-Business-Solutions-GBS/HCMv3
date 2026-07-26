using System.Linq;
using System.Threading.Tasks;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.Organizations;
using HCMv3.PersonnelAdministration;

namespace HCMv3.OrganizationManagement.Services;

/// <summary>
/// Ported from legacy XAF <c>HCM10.Module.BusinessObjects.Services.OrgService</c>.
///
/// MAJOR ADAPTATION: legacy resolved an org level via 7 discrete <c>Employee.OrgLevel1</c>..<c>OrgLevel7</c>
/// / <c>EmployeeOrgAssignment.OrgLevel1</c>..<c>OrgLevel7</c> fields, each a direct FK to the legacy's own
/// standalone <c>OrganizationUnit</c> business object at that fixed depth. This port's
/// <see cref="EmployeeOrgAssignment"/> collapses all 7 into a single <c>OrganizationUnitId</c> FK to Zero's
/// own, already-hierarchical <c>Abp.Organizations.OrganizationUnit</c> tree (see that entity's class
/// remarks). Consequently "the employee's OrgLevel-N unit" is no longer a stored field lookup -- it is
/// resolved dynamically by walking up the real <c>OrganizationUnit</c> tree from the assignment's leaf
/// unit (using Zero's own hierarchical dot-separated <c>Code</c> column, e.g. ancestor "00001.00002" is a
/// string-prefix of descendant "00001.00002.00003" -- no hand-rolled parent-pointer recursion needed) and
/// checking each ancestor's <see cref="OrgUnitProfile.OrganizationUnitTypeId"/> against the requested
/// level's <see cref="OrganizationUnitType.Code"/> (Level0..Level9, replacing legacy's OrgLevel0..OrgLevel7
/// -- see <see cref="OrganizationUnitType"/> remarks).
///
/// Legacy's <c>OrgUnitTypeList</c> static cache and the by-<c>NameEn</c> lookup methods
/// (<c>GetOrgUnit</c>/<c>GetOrgUnitFromOrgAssignment</c>) are folded into the by-<c>Code</c> methods below
/// -- callers already have the level <em>code</em> (e.g. from <see cref="OrganizationUnitType.AllowedCodes"/>)
/// in this port, so the extra NameEn indirection is unnecessary.
///
/// NOTE: <c>HCMv3.Core</c> has no reference to EntityFrameworkCore (per architecture rules), so all
/// queries here use the plain synchronous <c>IQueryable</c> returned by <c>IRepository&lt;T&gt;.GetAll()</c>,
/// wrapped in <c>Task.FromResult</c>/direct <c>await</c> of repository-level async methods for interface
/// consistency (same pattern as <see cref="GeneralMasterInfo.Services.CurrencyService"/>).
/// </summary>
[UnitOfWork]
public class OrgHierarchyService : HCMv3DomainServiceBase, IOrgHierarchyService
{
    private readonly IRepository<EmployeeOrgAssignment> _employeeOrgAssignmentRepository;
    private readonly IRepository<OrgUnitProfile> _orgUnitProfileRepository;
    private readonly IRepository<OrganizationUnitType> _organizationUnitTypeRepository;
    private readonly IRepository<OrganizationUnit, long> _organizationUnitRepository;

    public OrgHierarchyService(
        IRepository<EmployeeOrgAssignment> employeeOrgAssignmentRepository,
        IRepository<OrgUnitProfile> orgUnitProfileRepository,
        IRepository<OrganizationUnitType> organizationUnitTypeRepository,
        IRepository<OrganizationUnit, long> organizationUnitRepository)
    {
        _employeeOrgAssignmentRepository = employeeOrgAssignmentRepository;
        _orgUnitProfileRepository = orgUnitProfileRepository;
        _organizationUnitTypeRepository = organizationUnitTypeRepository;
        _organizationUnitRepository = organizationUnitRepository;
    }

    public string GetParentOrgUnitTypeCode(string orgUnitTypeCode)
    {
        var index = System.Array.IndexOf(OrganizationUnitType.AllowedCodes, orgUnitTypeCode);
        return index <= 0 ? string.Empty : OrganizationUnitType.AllowedCodes[index - 1];
    }

    public Task<long?> GetOrgUnitAtLevelForEmployeeAsync(int employeeId, string orgUnitTypeCode)
    {
        var latestAssignment = _employeeOrgAssignmentRepository.GetAll()
            .Where(a => a.EmployeeId == employeeId && a.OrganizationUnitId != null)
            .OrderByDescending(a => a.EndDate)
            .FirstOrDefault();

        if (latestAssignment?.OrganizationUnitId == null)
        {
            return Task.FromResult((long?)null);
        }

        return Task.FromResult(ResolveAncestorAtLevel(latestAssignment.OrganizationUnitId.Value, orgUnitTypeCode));
    }

    public async Task<long?> GetOrgUnitAtLevelForOrgAssignmentAsync(int employeeOrgAssignmentId, string orgUnitTypeCode)
    {
        var assignment = await _employeeOrgAssignmentRepository.FirstOrDefaultAsync(employeeOrgAssignmentId);
        if (assignment?.OrganizationUnitId == null)
        {
            return null;
        }

        return ResolveAncestorAtLevel(assignment.OrganizationUnitId.Value, orgUnitTypeCode);
    }

    public Task<long?> GetCurrentOrgUnitIdForEmployeeAsync(int employeeId)
    {
        var latestAssignment = _employeeOrgAssignmentRepository.GetAll()
            .Where(a => a.EmployeeId == employeeId && a.OrganizationUnitId != null)
            .OrderByDescending(a => a.EndDate)
            .FirstOrDefault();

        return Task.FromResult(latestAssignment?.OrganizationUnitId);
    }

    public Task<string> GetOrgUnitTypeDisplayNameAsync(string orgUnitTypeCode, bool useArabic)
    {
        var orgUnitType = _organizationUnitTypeRepository.GetAll()
            .FirstOrDefault(t => t.Code == orgUnitTypeCode);

        if (orgUnitType == null)
        {
            return Task.FromResult(orgUnitTypeCode);
        }

        return Task.FromResult(useArabic ? orgUnitType.NameAr : orgUnitType.NameEn);
    }

    /// <summary>
    /// Walks up the real <c>OrganizationUnit</c> tree from <paramref name="leafOrganizationUnitId"/>
    /// (ancestor-or-self, via Zero's hierarchical dot-separated <c>Code</c> prefix match) looking for the
    /// ancestor whose <see cref="OrgUnitProfile"/> is classified at <paramref name="orgUnitTypeCode"/>.
    /// </summary>
    private long? ResolveAncestorAtLevel(long leafOrganizationUnitId, string orgUnitTypeCode)
    {
        var leafUnit = _organizationUnitRepository.Get(leafOrganizationUnitId);

        var match = _orgUnitProfileRepository
            .GetAllIncluding(p => p.OrganizationUnit, p => p.OrganizationUnitType)
            .Where(p => p.OrganizationUnitType.Code == orgUnitTypeCode)
            .Where(p => leafUnit.Code.StartsWith(p.OrganizationUnit.Code))
            .Select(p => (long?)p.OrganizationUnitId)
            .FirstOrDefault();

        return match;
    }
}
