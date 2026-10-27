namespace HCMv3.Reports.EmployeeInfo
{
    /// <summary>
    /// Raw SQL used by rptEmploymentNotice (data source <c>EmpJobOfferDS</c>, query
    /// <c>EmployementNotice_Query</c> -- legacy spelling kept, the layout binds to it).
    /// </summary>
    internal static class rptEmploymentNoticeSql
    {
        /// <summary>
        /// HCMv3 rewrite of the legacy 131-line query.
        ///
        /// MECHANICAL RENAMES: Employee->Employees, Title->Titles, JobTitle->JobTitles,
        /// EmployeeOrgAssignment->EmployeeOrgAssignments, EmployeeBasicPay->EmployeeBasicPays,
        /// EmployeeBasicPayDetail->EmployeeBasicPayDetails, WageType->WageTypes; OID->Id;
        /// &lt;Entity&gt; FK column -> &lt;Entity&gt;Id. Plus "AND TenantId = @TenantId" and
        /// "AND IsDeleted = 0" on EVERY join -- neither appears anywhere in the legacy corpus.
        ///
        /// NON-MECHANICAL:
        ///   1. The 14 OrgLevel1..7 joins for Company and Department collapse to two OUTER APPLY
        ///      ancestor walks -- see EmployeeInfoSqlFragments. COALESCEd onto the employee's own
        ///      unit so a flat org tree still prints something.
        ///   2. Basic salary. Legacy: "WageType IN (select OID from WageType Where Code = '1000')".
        ///      This one is already code-based, so it is NOT the raw-legacy-PK trap that
        ///      PayslipSql documents; it survives as WageTypes.Code = '1000' unchanged. The Code
        ///      literal stays QUOTED because HCMv3 WageTypes.Code is nvarchar -- an unquoted
        ///      numeric literal makes SQL Server convert the whole column to int and throw on the
        ///      first non-numeric code.
        ///   3. Legacy joined EmployeeBasicPay with INNER JOIN, so an employee with no basic-pay
        ///      record valid at @PrmDate produced no employment notice at all. Kept INNER: that is
        ///      the report's actual precondition (it prints a salary figure), not an accident.
        ///
        /// @PrmDays is declared as a query parameter by the layout but the legacy SQL never
        /// references it. Left unreferenced here too rather than inventing a meaning; the label
        /// that shows the notice period binds the report parameter directly.
        /// </summary>
        internal const string Query1 = @"-- rptEmploymentNotice :: EmployementNotice_Query
SELECT
      emp.Code                                          AS [Employe Code]   -- legacy typo kept: the layout binds this name
    , emp.FullNameEn
    , emp.FullNameAr
    , title.NameEn                                      AS TitleEn
    , title.NameAr                                      AS TitleAr
    , basicPay.StartDate                                AS EffectiveDate
    , ISNULL(basicPayDetail.Amount, 0)                  AS [Basic Salary]
    , jobTitle.NameEn                                   AS JobtitleEn
    , jobTitle.NameAr                                   AS JobtitleAr
    , COALESCE(department.NameEn, ownOrgUnit.DisplayName)      AS DepartmentEn
    , COALESCE(department.NameAr, ownOrgUnitProfile.NameAr)    AS DepartmentAr
    , COALESCE(company.NameEn, ownOrgUnit.DisplayName)         AS CompanyEn
    , COALESCE(company.NameAr, ownOrgUnitProfile.NameAr)       AS CompanyAr

FROM Employees emp

INNER JOIN Titles title
        ON title.Id        = emp.TitleId
       AND title.TenantId  = @TenantId
       AND title.IsDeleted = 0

LEFT JOIN EmployeeOrgAssignments orgAssign
       ON orgAssign.EmployeeId = emp.Id
      AND orgAssign.TenantId   = @TenantId
      AND orgAssign.IsDeleted  = 0
      AND orgAssign.StartDate <= @PrmDate
      AND orgAssign.EndDate   >= @PrmDate

LEFT JOIN JobTitles jobTitle
       ON jobTitle.Id        = orgAssign.JobTitleId
      AND jobTitle.TenantId  = @TenantId
      AND jobTitle.IsDeleted = 0

INNER JOIN EmployeeBasicPays basicPay
        ON basicPay.EmployeeId = emp.Id
       AND basicPay.TenantId   = @TenantId
       AND basicPay.IsDeleted  = 0
       AND basicPay.StartDate <= @PrmDate
       AND basicPay.EndDate   >= @PrmDate

LEFT JOIN EmployeeBasicPayDetails basicPayDetail
       ON basicPayDetail.EmployeeBasicPayId = basicPay.Id
      AND basicPayDetail.TenantId  = @TenantId
      AND basicPayDetail.IsDeleted = 0
      AND basicPayDetail.WageTypeId IN (
              SELECT wt.Id FROM WageTypes wt
              WHERE wt.TenantId = @TenantId AND wt.IsDeleted = 0 AND wt.Code = '1000')
" + EmployeeInfoSqlFragments.OwnOrgUnit
  + EmployeeInfoSqlFragments.CompanyAncestor
  + EmployeeInfoSqlFragments.DepartmentAncestor + @"
WHERE emp.TenantId  = @TenantId
  AND emp.IsDeleted = 0
  AND emp.Code      = @PrmEmpCode;";
    }
}
