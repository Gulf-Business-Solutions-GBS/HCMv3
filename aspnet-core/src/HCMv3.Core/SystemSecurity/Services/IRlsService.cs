using System.Collections.Generic;
using System.Threading.Tasks;

namespace HCMv3.SystemSecurity.Services;

/// <summary>
/// Row-Level-Security enforcement engine (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.Services.RLSService</c>, complementing the already-ported
/// <see cref="SysUserOrgUnitData"/> data model). See <see cref="RlsService"/> remarks for the simplification
/// around dynamic criteria building.
/// </summary>
public interface IRlsService
{
    /// <summary>
    /// Resolves the set of Employee ids the current session user is allowed to see: the current user's own
    /// employee-manager subtree (<see cref="GetEmployeeIdsUnderManagerAsync"/>) unioned with the
    /// org-unit-scoped set implied by their roles' <see cref="SysUserOrgUnitData"/> rows
    /// (<see cref="GetEmployeeIdsUnderOrgUnitForCurrentUserAsync"/>). Returns an empty list when Row Level
    /// Security is disabled (<c>ManageSecuritySettings.EnableRowLevelSecurity</c>) -- ported from legacy
    /// <c>RLSService.GetRLSEmployeeOidList</c>.
    /// </summary>
    Task<List<int>> GetRlsEmployeeIdsAsync();

    /// <summary>Same as <see cref="GetRlsEmployeeIdsAsync"/> but returning Employee <c>Code</c>s. Ported
    /// from legacy <c>RLSService.GetRLSEmployeeCodeList</c>.</summary>
    Task<List<string>> GetRlsEmployeeCodesAsync();

    /// <summary>
    /// Recursively walks the <see cref="PersonnelAdministration.EmployeeManager"/> hierarchy under
    /// <paramref name="employeeId"/> (current-dated rows only), returning <paramref name="employeeId"/>
    /// itself plus every subordinate at any depth. Ported near-verbatim from legacy
    /// <c>RLSService.GetListOfEmployeeCode_UnderManager</c>.
    /// </summary>
    Task<List<int>> GetEmployeeIdsUnderManagerAsync(int employeeId);

    /// <summary>
    /// Resolves the Employee ids visible via the current session user's roles' org-unit scoping
    /// (<see cref="SysUserOrgUnitData"/>): expands each scoped org unit to its full descendant subtree
    /// (<see cref="GetOrgUnitDescendantIdsAsync"/>), matches employees whose current
    /// <see cref="PersonnelAdministration.EmployeeOrgAssignment.OrganizationUnitId"/> falls in that set, and
    /// -- if any scoped row is at the root level (<see cref="OrganizationManagement.OrganizationUnitType.Level0Code"/>)
    /// -- also includes employees with no org assignment at all. ADAPTED from legacy
    /// <c>RLSService.GetListOfEmployeeCode_UnderOrgUnit</c>: legacy matched any of 7 discrete
    /// <c>EmployeeOrgAssignment.OrgLevel1</c>..<c>OrgLevel7</c> FK columns against the scoped set; this
    /// port's <see cref="PersonnelAdministration.EmployeeOrgAssignment"/> has a single
    /// <c>OrganizationUnitId</c> FK instead (see its class remarks), so only that one column needs
    /// checking.
    /// </summary>
    Task<List<int>> GetEmployeeIdsUnderOrgUnitForCurrentUserAsync();

    /// <summary>
    /// Returns <paramref name="organizationUnitId"/> and all of its descendants, using Zero's own
    /// hierarchical dot-separated <c>OrganizationUnit.Code</c> (a descendant's code always starts with its
    /// ancestor's code) rather than hand-rolled parent-pointer recursion. Ported from legacy
    /// <c>RLSService.GetOrgUnitHierarchy</c> (which recursed via the legacy's own bespoke
    /// <c>OrganizationUnit.ParentOrgUnit</c> pointer -- superseded here by Zero's native
    /// <c>Abp.Organizations.OrganizationUnit</c> hierarchy, per dispatch instructions).
    /// </summary>
    Task<List<long>> GetOrgUnitDescendantIdsAsync(long organizationUnitId);
}

// DROPPED (DevExpress-specific part only): legacy RLSService.BuildInOrRangeCriteria built a DevExpress
// CriteriaOperator object graph (In/Between operators) for XAF grid data-access-mode filtering -- there is
// no ABP/EF Core equivalent, and none is needed: callers of this port's GetRlsEmployeeIdsAsync get a plain
// List<int> they can pass straight into a normal EF Core .Where(e => allowedIds.Contains(e.Id)), which
// achieves the same practical filtering purpose without ever needing dynamic criteria strings.
//
// PORTED (the framework-agnostic part): the pure id-compression algorithm at the heart of legacy's nested
// RLSRangeStore.Build (dedupe + sort + contiguous-run compression into "singles"/"ranges") IS ported,
// framework-agnostic, as HCMv3.Common.IdRangeCompressor -- available for the rare case a caller needs a
// literal SQL WHERE-fragment (e.g. for FromSqlRaw/FromSqlInterpolated) rather than an in-memory Contains
// filter.
