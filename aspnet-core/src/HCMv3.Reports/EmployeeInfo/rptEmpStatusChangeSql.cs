namespace HCMv3.Reports.EmployeeInfo
{
    /// <summary>Raw SQL used by rptEmpStatusChange (data source <c>StatusChangeDs</c>, query <c>StatusChangeQuery</c>).</summary>
    internal static class rptEmpStatusChangeSql
    {
        /// <summary>
        /// HCMv3 rewrite of the legacy 139-line query. This is the genuine rewrite of the batch:
        /// it pairs each employee's CURRENT org assignment / basic pay against the record that
        /// ended the day before, and pivots five wage-type codes across both.
        ///
        /// MECHANICAL RENAMES plus TenantId/IsDeleted everywhere, as elsewhere.
        ///
        /// NON-MECHANICAL:
        ///   1. <c>OrganizationUnit AS CurDept ON CurDept.OID = curOrgAssignment.OrgLevel2</c>.
        ///      Legacy took OrgLevel2 as "the department" by POSITION, without even checking
        ///      OrganizationUnitType -- unlike the sibling reports, which check the type. HCMv3 has
        ///      no positional levels, so this uses the typed Department ancestor and falls back to
        ///      the employee's own unit. *** SEMANTIC DEVIATION: for a tenant whose OrgLevel2 was
        ///      not actually a Department this changes which unit prints. ***
        ///      NOTE the fragments are keyed on the alias <c>orgAssign</c>, so the CURRENT
        ///      assignment is aliased that way and the PREVIOUS one gets its unit resolved by a
        ///      second, inlined copy of the same walk (aliased prevDepartment / prevOwnOrgUnit).
        ///   2. The PIVOT sub-selects were unfiltered -- no tenant, no soft-delete -- and were
        ///      evaluated over the whole EmployeeBasicPayDetails table twice. Both copies now
        ///      filter TenantId/IsDeleted; without that the pivot happily returns another tenant's
        ///      salary next to this tenant's employee.
        ///   3. Wage type <c>1070</c> is in the legacy pivot column list but DOES NOT EXIST in
        ///      HCMv3 (tenant 1 has 1000, 1002, 1011, 1031 of the five). The column is kept so the
        ///      layout's bindings resolve; it simply pivots to NULL -> COALESCE 0.
        ///   4. Wage-type codes are QUOTED. HCMv3 WageTypes.Code is nvarchar; the legacy PIVOT
        ///      column list used bare <c>[1000]</c> etc., which is a quoted identifier in PIVOT and
        ///      is fine, but the underlying WageTypeCode column must stay nvarchar or SQL Server
        ///      converts the whole column to int and throws on the first non-numeric code.
        ///   5. <c>curOrgAssignment.EndDate = '9999-12-31'</c> survives as-is: HCMv3 keeps the same
        ///      open-ended-record sentinel.
        /// </summary>
        /// <summary>
        /// The filtered wage-type pivot, written out twice below (once for current basic pay, once
        /// for previous) rather than hoisted into a CTE.
        ///
        /// PORT NOTE -- NEW v23.2 CONSTRAINT: DevExpress's <c>CustomSqlQuery</c> validator rejects
        /// a query that opens with <c>WITH</c>, exactly as it rejects a leading <c>DECLARE</c>
        /// (the constraint PayslipSql already documents: "A custom SQL query should contain only
        /// SELECT statements"). A CTE is not a SELECT to that validator, and the failure surfaces
        /// only at CreateDocument() time as "Query ... failed to execute / failed to validate",
        /// never at build time. Derived tables are accepted; semantics are identical.
        /// </summary>
        private const string BasicPayPivot = @"(
    SELECT
          EmployeeBasicPayId
        , [1000] AS WageType_1000
        , [1002] AS WageType_1002
        , [1031] AS WageType_1031
        , [1011] AS WageType_1011
        , [1070] AS WageType_1070      -- see rewrite note 3: no such wage type in HCMv3
    FROM (
        SELECT
              detail.EmployeeBasicPayId
            , wageType.Code AS WageTypeCode
            , detail.Amount
        FROM EmployeeBasicPayDetails detail
        INNER JOIN WageTypes wageType
                ON wageType.Id        = detail.WageTypeId
               AND wageType.TenantId  = @TenantId
               AND wageType.IsDeleted = 0
        WHERE detail.TenantId  = @TenantId
          AND detail.IsDeleted = 0
    ) src
    PIVOT (MAX(Amount) FOR WageTypeCode IN ([1000], [1002], [1031], [1011], [1070])) pvt
)";

        internal const string Query1 = @"-- rptEmpStatusChange :: StatusChangeQuery
SELECT
      emp.Code
    , emp.FullNameEn
    , emp.FullNameAr
    , emp.JoiningDate
    , curJobTitle.NameEn AS Current_JobTitleEn
    , curJobTitle.NameAr AS Current_JobTitleAr

    -- Latest of the four change dates (verbatim legacy logic).
    , CASE
        WHEN orgAssign.StartDate    >= prevOrgAssign.StartDate
         AND orgAssign.StartDate    >= prevBasicPay.StartDate
         AND orgAssign.StartDate    >= curBasicPay.StartDate     THEN orgAssign.StartDate
        WHEN prevOrgAssign.StartDate >= orgAssign.StartDate
         AND prevOrgAssign.StartDate >= prevBasicPay.StartDate
         AND prevOrgAssign.StartDate >= curBasicPay.StartDate     THEN prevOrgAssign.StartDate
        WHEN prevBasicPay.StartDate >= prevOrgAssign.StartDate
         AND prevBasicPay.StartDate >= curBasicPay.StartDate
         AND prevBasicPay.StartDate >= orgAssign.StartDate        THEN prevBasicPay.StartDate
        ELSE curBasicPay.StartDate
      END AS Current_StartDate

    , CASE
        WHEN orgAssign.StartDate     <= prevOrgAssign.StartDate
         AND curBasicPay.StartDate   <= prevBasicPay.StartDate    THEN orgAssign.StartDate
        WHEN prevOrgAssign.StartDate <= orgAssign.StartDate
         AND curBasicPay.StartDate   <= prevBasicPay.StartDate    THEN prevOrgAssign.StartDate
        WHEN orgAssign.StartDate     <= prevOrgAssign.StartDate
         AND prevBasicPay.StartDate  <= curBasicPay.StartDate     THEN orgAssign.StartDate
        ELSE prevBasicPay.StartDate
      END AS Previous_StartDate

    -- see rewrite note 1: legacy took OrgLevel2 positionally; HCMv3 uses the typed ancestor.
    , COALESCE(department.NameEn, ownOrgUnit.DisplayName)               AS Current_OrgLevel2_En
    , COALESCE(department.NameAr, ownOrgUnitProfile.NameAr)             AS Current_OrgLevel2_Ar
    , COALESCE(prevJobTitle.NameEn, curJobTitle.NameEn)                 AS Previous_JobTitleEn
    , COALESCE(prevJobTitle.NameAr, curJobTitle.NameAr)                 AS Previous_JobTitleAr
    , COALESCE(prevDepartment.NameEn, prevOwnOrgUnit.DisplayName,
               department.NameEn, ownOrgUnit.DisplayName)               AS Previous_OrgLevel2_En
    , COALESCE(prevDepartment.NameAr, prevOwnOrgUnitProfile.NameAr,
               department.NameAr, ownOrgUnitProfile.NameAr)             AS Previous_OrgLevel2_Ar

    , COALESCE(curPay.WageType_1000, 0)  AS curWageType_1000
    , COALESCE(curPay.WageType_1002, 0)  AS curWageType_1002
    , COALESCE(curPay.WageType_1031, 0)  AS curWageType_1031
    , COALESCE(curPay.WageType_1011, 0)  AS curWageType_1011
    , COALESCE(curPay.WageType_1070, 0)  AS curWageType_1070
    , COALESCE(prevPay.WageType_1000, 0) AS RevWageType_1000
    , COALESCE(prevPay.WageType_1002, 0) AS RevWageType_1002
    , COALESCE(prevPay.WageType_1031, 0) AS RevWageType_1031
    , COALESCE(prevPay.WageType_1011, 0) AS RevWageType_1011
    , COALESCE(prevPay.WageType_1070, 0) AS RevWageType_1070

    , COALESCE(curPay.WageType_1000, 0) + COALESCE(curPay.WageType_1002, 0)
    + COALESCE(curPay.WageType_1031, 0) + COALESCE(curPay.WageType_1011, 0)
    + COALESCE(curPay.WageType_1070, 0)  AS Current_Total

    , COALESCE(prevPay.WageType_1000, 0) + COALESCE(prevPay.WageType_1002, 0)
    + COALESCE(prevPay.WageType_1031, 0) + COALESCE(prevPay.WageType_1011, 0)
    + COALESCE(prevPay.WageType_1070, 0) AS Previous_Total

FROM Employees emp

-- CURRENT org assignment (aliased orgAssign: the shared org-unit fragments key on that name)
LEFT JOIN EmployeeOrgAssignments orgAssign
       ON orgAssign.EmployeeId = emp.Id
      AND orgAssign.TenantId   = @TenantId
      AND orgAssign.IsDeleted  = 0
      AND orgAssign.EndDate    = '9999-12-31'
LEFT JOIN JobTitles curJobTitle
       ON curJobTitle.Id        = orgAssign.JobTitleId
      AND curJobTitle.TenantId  = @TenantId
      AND curJobTitle.IsDeleted = 0

-- PREVIOUS org assignment: the record that ended the day before the current one started
LEFT JOIN EmployeeOrgAssignments prevOrgAssign
       ON prevOrgAssign.EmployeeId = emp.Id
      AND prevOrgAssign.TenantId   = @TenantId
      AND prevOrgAssign.IsDeleted  = 0
      AND prevOrgAssign.EndDate    = DATEADD(DAY, -1, orgAssign.StartDate)
LEFT JOIN JobTitles prevJobTitle
       ON prevJobTitle.Id        = prevOrgAssign.JobTitleId
      AND prevJobTitle.TenantId  = @TenantId
      AND prevJobTitle.IsDeleted = 0
LEFT JOIN AbpOrganizationUnits prevOwnOrgUnit
       ON prevOwnOrgUnit.Id = prevOrgAssign.OrganizationUnitId
      AND prevOwnOrgUnit.TenantId = @TenantId AND prevOwnOrgUnit.IsDeleted = 0
LEFT JOIN OrgUnitProfiles prevOwnOrgUnitProfile
       ON prevOwnOrgUnitProfile.OrganizationUnitId = prevOwnOrgUnit.Id
      AND prevOwnOrgUnitProfile.TenantId = @TenantId AND prevOwnOrgUnitProfile.IsDeleted = 0
OUTER APPLY (
    SELECT TOP 1 ancestor.DisplayName AS NameEn, ancestorProfile.NameAr AS NameAr
    FROM AbpOrganizationUnits prevUnit
    INNER JOIN AbpOrganizationUnits ancestor
            ON ancestor.TenantId = @TenantId AND ancestor.IsDeleted = 0
           AND prevUnit.Code LIKE ancestor.Code + '%'
    INNER JOIN OrgUnitProfiles ancestorProfile
            ON ancestorProfile.OrganizationUnitId = ancestor.Id
           AND ancestorProfile.TenantId = @TenantId AND ancestorProfile.IsDeleted = 0
    INNER JOIN OrganizationUnitTypes ancestorType
            ON ancestorType.Id = ancestorProfile.OrganizationUnitTypeId
           AND ancestorType.TenantId = @TenantId AND ancestorType.IsDeleted = 0
           AND ancestorType.NameEn = 'Department'
    WHERE prevUnit.Id = prevOrgAssign.OrganizationUnitId
      AND prevUnit.TenantId = @TenantId AND prevUnit.IsDeleted = 0
    ORDER BY LEN(ancestor.Code)
) prevDepartment

-- CURRENT basic pay
LEFT JOIN EmployeeBasicPays curBasicPay
       ON curBasicPay.EmployeeId = emp.Id
      AND curBasicPay.TenantId   = @TenantId
      AND curBasicPay.IsDeleted  = 0
      AND curBasicPay.EndDate    = '9999-12-31'
LEFT JOIN " + BasicPayPivot + @" curPay
       ON curPay.EmployeeBasicPayId = curBasicPay.Id

-- PREVIOUS basic pay
LEFT JOIN EmployeeBasicPays prevBasicPay
       ON prevBasicPay.EmployeeId = emp.Id
      AND prevBasicPay.TenantId   = @TenantId
      AND prevBasicPay.IsDeleted  = 0
      AND prevBasicPay.EndDate    = DATEADD(DAY, -1, curBasicPay.StartDate)
LEFT JOIN " + BasicPayPivot + @" prevPay
       ON prevPay.EmployeeBasicPayId = prevBasicPay.Id
" + EmployeeInfoSqlFragments.OwnOrgUnit
  + EmployeeInfoSqlFragments.DepartmentAncestor + @"
WHERE emp.TenantId  = @TenantId
  AND emp.IsDeleted = 0
  AND emp.Status    = 1
  AND (@PrmEmpCode IS NULL OR @PrmEmpCode = '' OR emp.Code = @PrmEmpCode);";
    }
}
