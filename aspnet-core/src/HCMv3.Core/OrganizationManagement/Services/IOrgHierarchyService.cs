using System.Threading.Tasks;

namespace HCMv3.OrganizationManagement.Services;

/// <summary>
/// Org-unit-by-level resolution helpers (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.Services.OrgService</c>). See <see cref="OrgHierarchyService"/> remarks
/// for the significant model adaptation this required.
/// </summary>
public interface IOrgHierarchyService
{
    /// <summary>
    /// Ported from legacy <c>OrgService.GetParentOrgUnitTypeCode</c>: returns the org-level code one level
    /// above <paramref name="orgUnitTypeCode"/> (e.g. "Level3" -&gt; "Level2"), or <c>""</c> for "Level0" or
    /// an unrecognized code. Adapted to <see cref="OrganizationUnitType.AllowedCodes"/>'s 10 levels
    /// (Level0..Level9) instead of legacy's 8 (OrgLevel0..OrgLevel7). Pure -- no DB dependency.
    /// </summary>
    string GetParentOrgUnitTypeCode(string orgUnitTypeCode);

    /// <summary>
    /// Resolves the <c>Abp.Organizations.OrganizationUnit.Id</c> of the ancestor-or-self (of the employee's
    /// current/latest <see cref="EmployeeOrgAssignment"/>'s org unit) whose <see cref="OrgUnitProfile"/> is
    /// classified at <paramref name="orgUnitTypeCode"/> (e.g. "Level2"). Returns <c>null</c> if the
    /// employee has no current org assignment or no ancestor at that level. Ported from legacy
    /// <c>OrgService.GetCurrentOrgUnit(string, Employee, Session)</c>/<c>GetOrgUnit</c> -- see
    /// <see cref="OrgHierarchyService"/> remarks.
    /// </summary>
    Task<long?> GetOrgUnitAtLevelForEmployeeAsync(int employeeId, string orgUnitTypeCode);

    /// <summary>
    /// Same as <see cref="GetOrgUnitAtLevelForEmployeeAsync"/> but resolved from a specific
    /// <see cref="EmployeeOrgAssignment"/> row rather than the employee's latest one. Ported from legacy
    /// <c>OrgService.GetCurrentOrgUnit(string, OrgAssignmentInfo, Session)</c>/<c>GetOrgUnitFromOrgAssignment</c>.
    /// </summary>
    Task<long?> GetOrgUnitAtLevelForOrgAssignmentAsync(int employeeOrgAssignmentId, string orgUnitTypeCode);

    /// <summary>
    /// Returns the employee's current/latest org unit id directly (the leaf of the (now single-FK) org
    /// assignment). Ported from legacy <c>OrgService.GetOrgUnitForUC</c>, which walked
    /// <c>OrgLevel7..OrgLevel1</c> looking for the first non-null level -- collapsed to a single FK in this
    /// port's <see cref="EmployeeOrgAssignment"/>, so this is now a direct lookup. Returns <c>null</c> if
    /// the employee has no current org assignment.
    /// </summary>
    Task<long?> GetCurrentOrgUnitIdForEmployeeAsync(int employeeId);

    /// <summary>
    /// Resolves the display name (NameAr if <paramref name="useArabic"/>, else NameEn) of the
    /// <see cref="OrganizationUnitType"/> with the given <paramref name="orgUnitTypeCode"/>. Ported from
    /// legacy <c>OrgService.GetOrgUnitTypeColName</c>. Falls back to <paramref name="orgUnitTypeCode"/>
    /// itself if no matching type is configured.
    /// </summary>
    Task<string> GetOrgUnitTypeDisplayNameAsync(string orgUnitTypeCode, bool useArabic);
}
