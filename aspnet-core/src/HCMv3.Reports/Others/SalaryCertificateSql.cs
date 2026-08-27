// SQL for rptSalaryCertificateEn / rptSalaryCertificateAr, rewritten for the HCMv3 schema.
//
// Held here (rather than in a .resx, where the legacy reports kept it) for exactly the reason
// PayslipSql.cs gives: the SQL is the expensive, reviewable part of the port and must be diffable.
//
// MULTI-TENANCY: these run through DevExpress SqlDataSource as RAW SQL, so EF Core's per-tenant
// global query filter does NOT apply. Every query filters TenantId explicitly via @TenantId, which
// is bound to the hidden PrmTenantId report parameter by HcmReportBase.EnsureHcmParameters().
// Removing an "AND TenantId = @TenantId" silently leaks rows across tenants.

namespace HCMv3.Reports.Others
{
    /// <summary>Raw SQL used by the two salary-certificate reports.</summary>
    public static class SalaryCertificateSql
    {
        // ===================================================================================================
        // Shared rewrite notes (apply to BOTH certificate queries)
        //
        // MECHANICAL RENAMES
        //   Employee->Employees, Nationality->Nationalities, JobTitle->JobTitles,
        //   GovtJobTitle->GovtJobTitles, EmployeeOrgAssignment->EmployeeOrgAssignments,
        //   EmployeeSponsor->EmployeeSponsors, EmployeeBasicPay(Detail)->EmployeeBasicPay(Detail)s,
        //   OrganizationUnit->AbpOrganizationUnits
        //   OID -> Id ; <EntityName> -> <EntityName>Id (FK columns)
        //   + "AND TenantId = @TenantId" and "AND IsDeleted = 0" on EVERY join (zero occurrences in
        //     the legacy corpus -- HCM10 was single-tenant and hard-deleted).
        //
        // NON-MECHANICAL REWRITES
        //   1. OrgLevel1. Legacy EmployeeOrgAssignment carried seven flat FK columns (OrgLevel1..7)
        //      into OrganizationUnit and this report took OrgLevel1 -- by convention the Company.
        //      HCMv3 has a SINGLE EmployeeOrgAssignments.OrganizationUnitId into AbpOrganizationUnits,
        //      an ABP materialised-path tree (Code/ParentId) with NO unit-type concept, so "the
        //      OrgLevel1 ancestor" is not expressible. The employee's own unit is used instead.
        //      *** SEMANTIC DEVIATION -- same one PayslipSql.PayslipOrgV2Query carries; needs the same
        //      product decision. ***
        //   2. The legacy "orgUnit.OID = (SELECT TOP 1 OrgLevel1 FROM EmployeeOrgAssignment WHERE
        //      Employee = emp.OID ORDER BY EndDate DESC)" correlated scalar subqueries are rewritten
        //      as OUTER APPLY: same "most recent assignment" intent, correlation visible to the
        //      optimiser, and -- unlike the legacy form -- the row actually stays tied to the
        //      employee (the legacy form joined on the value alone, so any org unit sharing that id
        //      matched).
        //   3. EMPLOYEE FILTER. The legacy reports pulled EVERY employee in the database and then
        //      narrowed to one with a client-side report FilterString, "[Code] In (?PrmEmpCode, ?)".
        //      That is replaced by an "AND emp.Code = @PrmEmpCode" predicate in SQL. This is not
        //      optional tidying: a client-side filter over a raw SqlDataSource would have fetched
        //      every tenant's payroll to the report engine before discarding it.
        // ===================================================================================================

        /// <summary>
        /// rptSalaryCertificateEn :: SalaryCertificateQuery.
        ///
        /// Additions over the legacy column list -- all four exist to replace XAF code-behind that ran
        /// an XPObjectSpaceProvider from inside the report (see rptSalaryCertificateEn.cs):
        ///   SalaryAmount / CurrencyCode / CurrencyName -- the legacy BeforePrint read
        ///     emp.BasicPay.OrderByDescending(StartDate).First().Amount and the first detail's
        ///     Currency through XPO. A DevExpress-constructed report has no DI container, so the
        ///     values are projected here and the label is composed in Detail_BeforePrint.
        ///   HasRegularPayroll -- the legacy guard "effectiveDate != default(DateTime)", i.e. print
        ///     the salary sentence only if the employee has at least one regular payroll run.
        ///
        /// LEGACY QUIRK KEPT: Nationality is projected from NameAr even in the English certificate.
        /// No label binds it, so it is dead either way; changing it would be an unforced deviation.
        /// </summary>
        public const string SalaryCertificateEnQuery = @"-- rptSalaryCertificateEn :: SalaryCertificateQuery
SELECT
      emp.Code
    , emp.FullNameEn
    , emp.CivilID
    , emp.JoiningDate
    , nationality.NameAr           AS Nationality        -- legacy quirk: NameAr in the English report
    , orgUnit.DisplayName          AS [Org Unit]         -- see rewrite note 1
    , jobTitle.NameEn              AS [Jobtitle]
    , basicPay.Amount              AS Total_Salary
    , basicPay.Amount              AS SalaryAmount
    , ISNULL(currency.Code,   'KWD')          AS CurrencyCode
    , ISNULL(currency.NameEn, 'Kuwaiti Dinar') AS CurrencyName
    , CASE WHEN regularRun.ForPeriod IS NULL THEN 0 ELSE 1 END AS HasRegularPayroll
FROM Employees emp

LEFT JOIN Nationalities nationality
       ON nationality.Id       = emp.NationalityId
      AND nationality.TenantId = @TenantId
      AND nationality.IsDeleted = 0

-- see rewrite note 2
OUTER APPLY (
    SELECT TOP 1 eoa.OrganizationUnitId, eoa.JobTitleId
    FROM EmployeeOrgAssignments eoa
    WHERE eoa.EmployeeId = emp.Id AND eoa.TenantId = @TenantId AND eoa.IsDeleted = 0
    ORDER BY eoa.EndDate DESC
) orgAssignment

LEFT JOIN AbpOrganizationUnits orgUnit
       ON orgUnit.Id        = orgAssignment.OrganizationUnitId
      AND orgUnit.TenantId  = @TenantId
      AND orgUnit.IsDeleted = 0

LEFT JOIN JobTitles jobTitle
       ON jobTitle.Id        = orgAssignment.JobTitleId
      AND jobTitle.TenantId  = @TenantId
      AND jobTitle.IsDeleted = 0

-- Latest basic-pay header. The legacy SQL ordered by EndDate DESC and the legacy code-behind by
-- StartDate DESC; both pick the open-ended (EndDate 9999-12-31) row on real data. EndDate DESC is
-- kept, matching the SQL, so the projected amount and the sentence below it cannot disagree.
OUTER APPLY (
    SELECT TOP 1 bp.Id, bp.Amount
    FROM EmployeeBasicPays bp
    WHERE bp.EmployeeId = emp.Id AND bp.TenantId = @TenantId AND bp.IsDeleted = 0
    ORDER BY bp.EndDate DESC
) basicPay

-- Currency of that header's basic-salary line. The legacy code-behind took BasicPayDetail.First(),
-- which is order-undefined across a header that has several wage-type lines (3400 has two: Code
-- '1000' Basic Salary and '1058' Additional Duties Allowance). Pinned to Code '1000' so the
-- certificate is reproducible.
--
-- '1000' is a business Code, not a row id -- contrast EmployeeBasicPayDetail.WageType = 1 in the
-- payslip corpus, which is a raw legacy PRIMARY KEY that happens to mean Code '1000'. Codes are
-- quoted because HCMv3 WageTypes.Code is nvarchar; unquoted, SQL Server would implicitly convert
-- the whole column to int and throw on the first non-numeric code.
OUTER APPLY (
    SELECT TOP 1 cur.Code, cur.NameEn
    FROM EmployeeBasicPayDetails ebpd
    JOIN WageTypes wt  ON wt.Id  = ebpd.WageTypeId AND wt.TenantId  = @TenantId AND wt.IsDeleted  = 0
                      AND wt.Code = '1000'
    LEFT JOIN Currencies cur ON cur.Id = ebpd.CurrencyId AND cur.TenantId = @TenantId AND cur.IsDeleted = 0
    WHERE ebpd.EmployeeBasicPayId = basicPay.Id AND ebpd.TenantId = @TenantId AND ebpd.IsDeleted = 0
) currency

-- Legacy guard: the salary sentence prints only when a regular payroll run exists.
OUTER APPLY (
    SELECT TOP 1 sr.ForPeriod
    FROM SalaryRegisters sr
    WHERE sr.EmployeeId = emp.Id AND sr.TenantId = @TenantId AND sr.IsDeleted = 0
      AND sr.ForPeriod = sr.InPeriod
      AND sr.PayrollRunType = 0            -- PayrollRunType.Regular
    ORDER BY sr.ForPeriod DESC
) regularRun

WHERE emp.TenantId  = @TenantId
  AND emp.IsDeleted = 0
  AND emp.Code      = @PrmEmpCode;";

        /// <summary>
        /// rptSalaryCertificateAr :: SalaryCertificateQuery.
        ///
        /// Differences from the English certificate are the legacy ones, not port artefacts:
        ///   * Arabic name / job-title / org-unit columns.
        ///   * Job title comes from the employee's SPONSOR record (GovtJobTitle -- the title on the
        ///     residency), not from the org assignment. That is deliberate in the legacy report: an
        ///     Arabic salary certificate is a government-facing document.
        ///   * Basic_Salary is the wage-type '1000' DETAIL amount, not the basic-pay header total.
        ///     The English certificate's sentence uses the header total. Kept as-is on both sides.
        /// </summary>
        public const string SalaryCertificateArQuery = @"-- rptSalaryCertificateAr :: SalaryCertificateQuery
SELECT
      emp.Code
    , emp.FullNameAr
    , emp.CivilID
    , emp.JoiningDate
    , nationality.NameAr           AS Nationality
    , orgUnit.DisplayName          AS [Org Unit]         -- see rewrite note 1
    , govtJobTitle.NameAr          AS [Jobtitle]
    , basicPayDetail.Amount        AS Basic_Salary
FROM Employees emp

LEFT JOIN Nationalities nationality
       ON nationality.Id        = emp.NationalityId
      AND nationality.TenantId  = @TenantId
      AND nationality.IsDeleted = 0

-- see rewrite note 2
OUTER APPLY (
    SELECT TOP 1 eoa.OrganizationUnitId
    FROM EmployeeOrgAssignments eoa
    WHERE eoa.EmployeeId = emp.Id AND eoa.TenantId = @TenantId AND eoa.IsDeleted = 0
    ORDER BY eoa.EndDate DESC
) orgAssignment

LEFT JOIN AbpOrganizationUnits orgUnit
       ON orgUnit.Id        = orgAssignment.OrganizationUnitId
      AND orgUnit.TenantId  = @TenantId
      AND orgUnit.IsDeleted = 0

OUTER APPLY (
    SELECT TOP 1 es.GovtJobTitleId
    FROM EmployeeSponsors es
    WHERE es.EmployeeId = emp.Id AND es.TenantId = @TenantId AND es.IsDeleted = 0
    ORDER BY es.EndDate DESC
) sponsorAssignment

LEFT JOIN GovtJobTitles govtJobTitle
       ON govtJobTitle.Id        = sponsorAssignment.GovtJobTitleId
      AND govtJobTitle.TenantId  = @TenantId
      AND govtJobTitle.IsDeleted = 0

-- Wage-type '1000' (Basic Salary) line of the most recent basic-pay header. See the Code-quoting
-- note on SalaryCertificateEnQuery.
OUTER APPLY (
    SELECT TOP 1 ebpd.Amount
    FROM EmployeeBasicPays bp
    JOIN EmployeeBasicPayDetails ebpd
      ON ebpd.EmployeeBasicPayId = bp.Id AND ebpd.TenantId = @TenantId AND ebpd.IsDeleted = 0
    JOIN WageTypes wt
      ON wt.Id = ebpd.WageTypeId AND wt.TenantId = @TenantId AND wt.IsDeleted = 0 AND wt.Code = '1000'
    WHERE bp.EmployeeId = emp.Id AND bp.TenantId = @TenantId AND bp.IsDeleted = 0
    ORDER BY bp.EndDate DESC
) basicPayDetail

WHERE emp.TenantId  = @TenantId
  AND emp.IsDeleted = 0
  AND emp.Code      = @PrmEmpCode;";

        /// <summary>
        /// Look-up list behind the (hidden) PrmShowLogo parameter, on both certificates.
        ///
        /// The legacy query was <c>SELECT logo, NameEn, NameAr, OID FROM OrganizationUnit WHERE
        /// OrganizationUnitType IN (SELECT OID FROM OrganizationUnitType WHERE Code = 'OrgLevel1')</c>.
        ///
        /// FEATURE LOST: AbpOrganizationUnits has no <c>Logo</c> column and HCMv3 has no Company
        /// entity carrying one, so <c>logo</c> is returned as NULL -- the same gap PayslipSql records
        /// for CompanyLogo. The unit-type filter is likewise not expressible (ABP org units have no
        /// type), so every unit is listed.
        ///
        /// The legacy column aliases (NameAr / NameEn / OID) are preserved so the parameter's
        /// DynamicListLookUpSettings in the designer file need no edit. Nothing renders this list --
        /// PrmShowLogo is Visible = false on both reports -- but it is part of the layout and is kept.
        /// </summary>
        public const string OrgUnitLogoQuery = @"-- rptSalaryCertificate* :: OrgUnitLogo (hidden PrmShowLogo look-up)
SELECT
      CAST(NULL AS VARBINARY(MAX)) AS logo   -- FEATURE LOST: no Logo column in AbpOrganizationUnits
    , orgUnit.DisplayName          AS NameEn
    , orgUnit.DisplayName          AS NameAr
    , orgUnit.Id                   AS OID
FROM AbpOrganizationUnits orgUnit
WHERE orgUnit.TenantId  = @TenantId
  AND orgUnit.IsDeleted = 0
ORDER BY orgUnit.DisplayName;";
    }
}
