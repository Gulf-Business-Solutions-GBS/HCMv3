// Shared SQL fragments for the EmployeeInfo report family.
//
// MULTI-TENANCY: everything here runs through DevExpress SqlDataSource as RAW SQL, so EF Core's
// per-tenant global query filter does NOT apply. Every fragment filters TenantId explicitly via
// @TenantId, supplied by the hidden PrmTenantId report parameter that HcmReportBase adds.
// Dropping an "AND TenantId = @TenantId" silently leaks rows across tenants.

namespace HCMv3.Reports.EmployeeInfo
{
    /// <summary>
    /// Reusable SQL text shared by the ported EmployeeInfo reports. Declared <c>const</c> so the
    /// per-report query constants concatenate them at COMPILE time and the designer file can keep
    /// assigning a plain constant to <c>CustomSqlQuery.Sql</c>.
    ///
    /// CALLING CONVENTION: every query that uses these fragments must alias its
    /// <c>EmployeeOrgAssignments</c> row source as <c>orgAssign</c>.
    ///
    /// ============================ THE ORG-UNIT REWRITE ============================
    /// Every legacy EmployeeInfo report resolves "the Company / Department / Division the employee
    /// sits under" by joining <c>EmployeeOrgAssignment.OrgLevel1..OrgLevel7</c> -- seven flat FK
    /// columns -- to <c>OrganizationUnit</c> seven times, each join filtered on
    /// <c>OrganizationUnitType.NameEn = 'Company'</c> (or 'Department' / 'Division'), then picking
    /// the first non-null with a seven-branch CASE. That is 7 joins plus a 7-branch CASE per unit
    /// type per language; rptEmploymentNotice alone carries 14 such joins and 4 CASE expressions.
    ///
    /// HCMv3 models this properly. <c>EmployeeOrgAssignments.OrganizationUnitId</c> is a SINGLE FK
    /// into <c>AbpOrganizationUnits</c>, an ABP materialised-path tree (<c>Code</c>/<c>ParentId</c>,
    /// Code being dot-separated 5-digit segments such as "00001.00003"). The unit TYPE that ABP
    /// itself has no concept of is carried by HCMv3's own
    /// <c>OrgUnitProfiles.OrganizationUnitTypeId</c> -> <c>OrganizationUnitTypes</c>.
    ///
    /// *** CORRECTION TO THE CORPUS NOTES. *** aspnet-core/tools/HCMv3.ReportMigrator/reports/
    /// PayslipEn/PORTING-NOTES.md records that AbpOrganizationUnits has "no unit-type concept at
    /// all" and that "which ancestor is the Department is NOT expressible". That is out of date:
    /// OrgUnitProfiles supplies exactly the missing unit type, and the ancestor walk below
    /// expresses the legacy intent precisely. PayslipOrgV2Query can be tightened the same way.
    ///
    /// The walk itself: an ancestor is any unit whose Code is a PREFIX of the employee's unit Code
    /// -- a materialised path is a prefix by construction, so no recursive CTE is needed --
    /// and <c>ORDER BY LEN(Code) DESC</c> takes the NEAREST such ancestor, which is what the
    /// legacy "first non-null OrgLevel" CASE effectively did.
    ///
    /// NAME COLUMNS: legacy <c>OrganizationUnit</c> had NameEn/NameAr. ABP's table has only
    /// <c>DisplayName</c>, so English comes from <c>DisplayName</c> and Arabic from
    /// <c>OrgUnitProfiles.NameAr</c>.
    ///
    /// LOGO: legacy <c>OrganizationUnit.logo</c> has no HCMv3 counterpart -- neither
    /// AbpOrganizationUnits nor OrgUnitProfiles carries one. Queries that projected it return
    /// NULL. *** FEATURE LOST, same finding as the payslip port. ***
    ///
    /// KNOWN DATA GAP ON THE CURRENT DATABASE: tenant 1 holds exactly ONE organisation unit
    /// ("Head Office", type "Test Type Level0"), and the org assignment of the verification
    /// employee (3400) has <c>OrganizationUnitId = NULL</c>. Every Company/Department/Division
    /// column therefore renders blank today. That is missing DATA, not a broken rewrite.
    /// </summary>
    internal static class EmployeeInfoSqlFragments
    {
        /// <summary>Nearest ancestor of the employee's unit whose type is 'Company'.</summary>
        internal const string CompanyAncestor = @"
OUTER APPLY (
    SELECT TOP 1 ancestor.DisplayName AS NameEn, ancestorProfile.NameAr AS NameAr
    FROM AbpOrganizationUnits ownUnit
    INNER JOIN AbpOrganizationUnits ancestor
            ON ancestor.TenantId = @TenantId AND ancestor.IsDeleted = 0
           AND ownUnit.Code LIKE ancestor.Code + '%'
    INNER JOIN OrgUnitProfiles ancestorProfile
            ON ancestorProfile.OrganizationUnitId = ancestor.Id
           AND ancestorProfile.TenantId = @TenantId AND ancestorProfile.IsDeleted = 0
    INNER JOIN OrganizationUnitTypes ancestorType
            ON ancestorType.Id = ancestorProfile.OrganizationUnitTypeId
           AND ancestorType.TenantId = @TenantId AND ancestorType.IsDeleted = 0
           AND ancestorType.NameEn = 'Company'
    WHERE ownUnit.Id = orgAssign.OrganizationUnitId
      AND ownUnit.TenantId = @TenantId AND ownUnit.IsDeleted = 0
    ORDER BY LEN(ancestor.Code) DESC
) company
";

        /// <summary>Nearest ancestor of the employee's unit whose type is 'Department'.</summary>
        internal const string DepartmentAncestor = @"
OUTER APPLY (
    SELECT TOP 1 ancestor.DisplayName AS NameEn, ancestorProfile.NameAr AS NameAr
    FROM AbpOrganizationUnits ownUnit
    INNER JOIN AbpOrganizationUnits ancestor
            ON ancestor.TenantId = @TenantId AND ancestor.IsDeleted = 0
           AND ownUnit.Code LIKE ancestor.Code + '%'
    INNER JOIN OrgUnitProfiles ancestorProfile
            ON ancestorProfile.OrganizationUnitId = ancestor.Id
           AND ancestorProfile.TenantId = @TenantId AND ancestorProfile.IsDeleted = 0
    INNER JOIN OrganizationUnitTypes ancestorType
            ON ancestorType.Id = ancestorProfile.OrganizationUnitTypeId
           AND ancestorType.TenantId = @TenantId AND ancestorType.IsDeleted = 0
           AND ancestorType.NameEn = 'Department'
    WHERE ownUnit.Id = orgAssign.OrganizationUnitId
      AND ownUnit.TenantId = @TenantId AND ownUnit.IsDeleted = 0
    ORDER BY LEN(ancestor.Code) DESC
) department
";

        /// <summary>Nearest ancestor of the employee's unit whose type is 'Division'.</summary>
        internal const string DivisionAncestor = @"
OUTER APPLY (
    SELECT TOP 1 ancestor.DisplayName AS NameEn, ancestorProfile.NameAr AS NameAr
    FROM AbpOrganizationUnits ownUnit
    INNER JOIN AbpOrganizationUnits ancestor
            ON ancestor.TenantId = @TenantId AND ancestor.IsDeleted = 0
           AND ownUnit.Code LIKE ancestor.Code + '%'
    INNER JOIN OrgUnitProfiles ancestorProfile
            ON ancestorProfile.OrganizationUnitId = ancestor.Id
           AND ancestorProfile.TenantId = @TenantId AND ancestorProfile.IsDeleted = 0
    INNER JOIN OrganizationUnitTypes ancestorType
            ON ancestorType.Id = ancestorProfile.OrganizationUnitTypeId
           AND ancestorType.TenantId = @TenantId AND ancestorType.IsDeleted = 0
           AND ancestorType.NameEn = 'Division'
    WHERE ownUnit.Id = orgAssign.OrganizationUnitId
      AND ownUnit.TenantId = @TenantId AND ownUnit.IsDeleted = 0
    ORDER BY LEN(ancestor.Code) DESC
) division
";

        /// <summary>
        /// The employee's OWN organisation unit, aliased <c>ownOrgUnit</c> / <c>ownOrgUnitProfile</c>.
        /// Used as the COALESCE fallback when the tree carries no ancestor of the requested type,
        /// mirroring the deviation already taken in <c>PayslipSql.PayslipOrgV2Query</c> so that
        /// both reports agree on what "Department" means for a flat org tree.
        /// </summary>
        internal const string OwnOrgUnit = @"
LEFT JOIN AbpOrganizationUnits ownOrgUnit
       ON ownOrgUnit.Id = orgAssign.OrganizationUnitId
      AND ownOrgUnit.TenantId = @TenantId AND ownOrgUnit.IsDeleted = 0
LEFT JOIN OrgUnitProfiles ownOrgUnitProfile
       ON ownOrgUnitProfile.OrganizationUnitId = ownOrgUnit.Id
      AND ownOrgUnitProfile.TenantId = @TenantId AND ownOrgUnitProfile.IsDeleted = 0
";
    }
}
