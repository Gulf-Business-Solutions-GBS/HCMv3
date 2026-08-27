// SQL for rptPayslipAr and its five Arabic subreports, rewritten for the HCMv3 schema.
//
// Sibling of PayslipSql.cs (the English payslip). Read that file's header first -- the mechanical
// rename rules, the WageTypes.Code quoting rule and the raw-legacy-primary-key traps are identical
// and are not repeated here.
//
// MULTI-TENANCY: these run through DevExpress SqlDataSource as RAW SQL, so EF Core's per-tenant
// global query filter does NOT apply. Every query filters TenantId explicitly via @TenantId, bound
// to the hidden PrmTenantId report parameter by HcmReportBase.EnsureHcmParameters().
//
// WHAT THE ARABIC FAMILY CHANGES ABOUT THE PORT, beyond swapping NameEn for NameAr:
//
//   THE LEGACY ARABIC REPORTS DID NOT FILTER IN SQL AT ALL. Every query -- master and all five
//   subreports -- selected the entire table set unfiltered, and the report then narrowed it with a
//   client-side DevExpress FilterString such as
//       "[Employee Code] In (?PrmEmployeeCode) And [ForPeriod] = ?ForPeriod And ..."
//   The single exception was an employee-code list that rptPayslipAr's DataSourceDemanded handler
//   CONCATENATED into the SQL text as string literals (a SQL-injection vector, and it mutated
//   Queries[0] -- which is not even the query the report binds).
//
//   That whole arrangement is replaced by real SqlDataSource query parameters. It is not tidying:
//   a client-side filter over a raw SqlDataSource pulls EVERY tenant's payroll into the report
//   engine's memory before discarding it, which is precisely the multi-tenancy hole @TenantId
//   exists to close. The English payslip was already ported this way; the Arabic one now matches.

namespace HCMv3.Reports.Payroll
{
    /// <summary>Raw SQL used by rptPayslipAr and its subreports.</summary>
    public static class PayslipArSql
    {
        /// <summary>
        /// Look-up list feeding the PrmPayrollArea combo.
        ///
        /// The legacy version was a designer-built <c>SelectQuery</c> over the <c>PayrollArea</c>
        /// table (columns Code, Name). It is rewritten as a CustomSqlQuery for one reason: a
        /// SelectQuery has nowhere to put <c>@TenantId</c>, so as a SelectQuery it would list every
        /// tenant's payroll areas in the parameter drop-down.
        /// </summary>
        public const string PayrollArea = @"-- rptPayslipAr :: PayrollArea (PrmPayrollArea look-up list)
SELECT Code, Name
FROM PayrollAreas
WHERE TenantId = @TenantId
  AND IsDeleted = 0
ORDER BY Name;";

        /// <summary>
        /// MASTER query -- the report's DataMember. One row per employee/period.
        ///
        /// HCMv3 rewrite notes specific to this query (the shared ones live in PayslipSql.cs):
        ///
        ///   1. OrgLevel1..7 -&gt; a single OrganizationUnitId into AbpOrganizationUnits. The legacy
        ///      Arabic master joined OrgLevel2 to <c>orgunit</c>, then joined <c>orgunit2</c> to
        ///      <c>orgunit.OID</c> -- a self-join to the same row -- and projected orgunit2.NameAr.
        ///      HCMv3 has no unit-type concept, so the employee's own unit is used.
        ///      *** SEMANTIC DEVIATION -- same one PayslipSql.PayslipOrgV2Query carries. ***
        ///      AbpOrganizationUnits has only <c>DisplayName</c>: there is no NameAr, so the Arabic
        ///      payslip shows the same org-unit string as the English one. *** ARABIC TEXT LOST. ***
        ///
        ///   2. "Bank Name" is projected from <c>bank.Code</c>, not from a name column. That is what
        ///      the legacy Arabic query does (the English one uses <c>bank.NameEn</c>). Kept: the
        ///      column feeds a label captioned as the bank, and changing it would silently alter a
        ///      printed payslip.
        ///
        ///   3. EmployeeVisaInfos.PassportExpiryDate is used directly, as in the legacy report. The
        ///      English port had to reconstruct the passport from EmployeeDocuments; it did not need
        ///      to -- EmployeeVisaInfos survives into HCMv3 intact. Worth revisiting in PayslipSql.
        ///
        ///   4. EmployeeSponsor / EmployeeGratuity joins dropped: no column from either is projected
        ///      and, as LEFT JOINs to 1:N tables, they could only fan the master row out. Same legacy
        ///      bug PayslipSql records.
        ///
        ///   5. Org assignment is picked with the legacy Arabic rule -- the assignment that is open
        ///      TODAY (<c>EndDate &gt; GETDATE()</c>), not the one covering the payslip period. Kept
        ///      verbatim; it is a real (if arguable) legacy behaviour and the English payslip's
        ///      period-based rule is the deviation, not this.
        ///
        ///   6. PayrollArea join. Legacy joined on a STRING (<c>PayrollArea.Code =
        ///      EmployeeOrgAssignment.PayrollArea</c>); HCMv3 models it as
        ///      <c>EmployeeOrgAssignments.PayrollAreaId</c>. The @PrmPayrollArea parameter now
        ///      compares <c>PayrollAreas.Name</c> -- matching rptPayslipEn, whose caller contract is
        ///      already "Admin. Office"-style names. The legacy Arabic report compared Code; its
        ///      parameter look-up ValueMember is changed from Code to Name to suit, so the two
        ///      payslips take the same argument.
        /// </summary>
        public const string PayslipOrgV2Query = @"-- rptPayslipAr :: PayslipOrgV2Query (MASTER)
SELECT
      emp.Code                          AS [Employee Code]
    , emp.Status
    , bank.Code                         AS [Bank Name]      -- see rewrite note 2
    , empBank.StartDate
    , empBank.IBAN                      AS [Bank IBAN]
    , orgUnit.DisplayName               AS OrgUnit          -- see rewrite note 1
    , emp.FullNameAr                    AS [Employee Name]
    , subGroup.NameAr                   AS Grade
    , grp.NameAr                        AS [Salary Profile]
    , jobTitle.NameAr                   AS Jobtitle
    , emp.JoiningDate                   AS [Joining Date]
    , visaInfo.PassportExpiryDate                           -- see rewrite note 3
    , sr.ForPeriod
    , sr.InPeriod
    , sr.PayrollRunType
    , srdNet.Amount                     AS [Net Pay]
    , wtNet.NameAr                      AS [Wage Type]
    , payrollArea.Name                  AS [Payroll Area]
    , payrollArea.Code                  AS [Payroll Area Code]

FROM Employees emp

INNER JOIN SalaryRegisters sr
        ON sr.EmployeeId = emp.Id
       AND sr.TenantId   = @TenantId
       AND sr.IsDeleted  = 0

-- Legacy: wagetype.Code = 9000. The net-pay carrier row. Code is nvarchar in HCMv3, hence quoted.
INNER JOIN SalaryRegisterDetails srdNet
        ON srdNet.SalaryRegisterId = sr.Id
       AND srdNet.TenantId         = @TenantId
       AND srdNet.IsDeleted        = 0
INNER JOIN WageTypes wtNet
        ON wtNet.Id        = srdNet.WageTypeId
       AND wtNet.Code      = '9000'
       AND wtNet.TenantId  = @TenantId
       AND wtNet.IsDeleted = 0

-- see rewrite note 5
LEFT JOIN EmployeeOrgAssignments eoa
       ON eoa.EmployeeId = emp.Id
      AND eoa.TenantId   = @TenantId
      AND eoa.IsDeleted  = 0
      AND eoa.EndDate    > GETDATE()
LEFT JOIN JobTitles         jobTitle    ON jobTitle.Id    = eoa.JobTitleId         AND jobTitle.TenantId    = @TenantId AND jobTitle.IsDeleted    = 0
LEFT JOIN PayrollAreas      payrollArea ON payrollArea.Id = eoa.PayrollAreaId      AND payrollArea.TenantId = @TenantId AND payrollArea.IsDeleted = 0
LEFT JOIN EmployeeGroups    grp         ON grp.Id         = eoa.EmployeeGroupId    AND grp.TenantId         = @TenantId AND grp.IsDeleted         = 0
LEFT JOIN EmployeeSubGroups subGroup    ON subGroup.Id    = eoa.EmployeeSubGroupId AND subGroup.TenantId    = @TenantId AND subGroup.IsDeleted    = 0
LEFT JOIN AbpOrganizationUnits orgUnit  ON orgUnit.Id     = eoa.OrganizationUnitId AND orgUnit.TenantId     = @TenantId AND orgUnit.IsDeleted     = 0

LEFT JOIN EmployeeBanks empBank
       ON empBank.EmployeeId = emp.Id
      AND empBank.TenantId   = @TenantId
      AND empBank.IsDeleted  = 0
      AND GETDATE() BETWEEN empBank.StartDate AND empBank.EndDate
LEFT JOIN Banks bank ON bank.Id = empBank.BankId AND bank.TenantId = @TenantId AND bank.IsDeleted = 0

-- Legacy joined EmployeeVisaInfo 1:N with no tie-breaker, which could fan the master row out.
-- OUTER APPLY keeps one row -- the latest-expiring passport -- and cannot duplicate the payslip.
OUTER APPLY (
    SELECT TOP 1 evi.PassportExpiryDate
    FROM EmployeeVisaInfos evi
    WHERE evi.EmployeeId = emp.Id AND evi.TenantId = @TenantId AND evi.IsDeleted = 0
    ORDER BY evi.PassportExpiryDate DESC
) visaInfo

WHERE emp.TenantId  = @TenantId
  AND emp.IsDeleted = 0
  AND ((@PrmEmployeeStatus <> -1 AND emp.Status = @PrmEmployeeStatus) OR @PrmEmployeeStatus = -1)
  AND sr.InPeriod = sr.ForPeriod
  AND DATEPART(Month, sr.InPeriod) = @PrmMonth
  AND DATEPART(Year,  sr.InPeriod) = @PrmYear
  AND sr.PayrollRunType = @PrmPayrollRunType
  AND (
        (ISNULL(@PrmEmpCode, '') <> '' AND emp.Code = @PrmEmpCode)
     OR (ISNULL(@PrmEmpCode, '') =  '' AND ISNULL(@PrmPayrollArea, '') <> '' AND payrollArea.Name = @PrmPayrollArea)
      )
ORDER BY emp.Code;";

        /// <summary>
        /// Secondary master. The layout binds exactly ONE control to it -- the designation label,
        /// through the cross-data-member expression <c>[PayslipQuery].[Designation]</c>.
        ///
        /// LEGACY DEFECT FIXED HERE, deliberately. A cross-data-member reference resolves against
        /// the FIRST row of the named query, and the legacy report's client-side FilterString applied
        /// to the report's own DataMember only -- never to PayslipQuery. So the legacy Arabic payslip
        /// printed whichever employee happened to sort first in an unfiltered, whole-database query
        /// as the designation on EVERY payslip. Giving this query the same @PrmEmpCode / @TenantId
        /// predicates as the master both closes the tenant hole and makes the label correct.
        ///
        /// Kept as a separate query rather than folded into the master because removing it would mean
        /// re-pointing a layout binding, and the layout is the part of a report that must stay
        /// recognisable to whoever maintained the legacy one.
        /// </summary>
        public const string PayslipQuery = @"-- rptPayslipAr :: PayslipQuery (feeds only [PayslipQuery].[Designation])
SELECT
      emp.Code                          AS [Employee Code]
    , emp.Status
    , orgUnit.DisplayName               AS OrgUnit
    , emp.FullNameAr                    AS [Employee Name]
    , subGroup.NameAr                   AS Grade
    , grp.NameAr                        AS [Salary Profile]
    , jobTitle.NameAr                   AS Designation
    , emp.JoiningDate                   AS [Joining Date]
    , visaInfo.PassportExpiryDate
    , bank.Code                         AS [Bank Name]
    , empBank.IBAN                      AS [Bank IBAN]
    , sr.ForPeriod
    , sr.InPeriod
    , srdNet.Amount                     AS [Net Pay]
    , sr.PayrollRunType
    , wtNet.NameAr
    , payrollArea.Name                  AS [Payroll Area]
    , payrollArea.Code                  AS [Payroll Area Code]

FROM Employees emp

LEFT JOIN EmployeeOrgAssignments eoa
       ON eoa.EmployeeId = emp.Id
      AND eoa.TenantId   = @TenantId
      AND eoa.IsDeleted  = 0
      AND GETDATE() BETWEEN eoa.StartDate AND eoa.EndDate
LEFT JOIN JobTitles         jobTitle    ON jobTitle.Id    = eoa.JobTitleId         AND jobTitle.TenantId    = @TenantId AND jobTitle.IsDeleted    = 0
LEFT JOIN PayrollAreas      payrollArea ON payrollArea.Id = eoa.PayrollAreaId      AND payrollArea.TenantId = @TenantId AND payrollArea.IsDeleted = 0
LEFT JOIN EmployeeSubGroups subGroup    ON subGroup.Id    = eoa.EmployeeSubGroupId AND subGroup.TenantId    = @TenantId AND subGroup.IsDeleted    = 0
LEFT JOIN EmployeeGroups    grp         ON grp.Id         = eoa.EmployeeGroupId    AND grp.TenantId         = @TenantId AND grp.IsDeleted         = 0
LEFT JOIN AbpOrganizationUnits orgUnit  ON orgUnit.Id     = eoa.OrganizationUnitId AND orgUnit.TenantId     = @TenantId AND orgUnit.IsDeleted     = 0

LEFT JOIN EmployeeBanks empBank
       ON empBank.EmployeeId = emp.Id
      AND empBank.TenantId   = @TenantId
      AND empBank.IsDeleted  = 0
      AND GETDATE() BETWEEN empBank.StartDate AND empBank.EndDate
LEFT JOIN Banks bank ON bank.Id = empBank.BankId AND bank.TenantId = @TenantId AND bank.IsDeleted = 0

OUTER APPLY (
    SELECT TOP 1 evi.PassportExpiryDate
    FROM EmployeeVisaInfos evi
    WHERE evi.EmployeeId = emp.Id AND evi.TenantId = @TenantId AND evi.IsDeleted = 0
    ORDER BY evi.PassportExpiryDate DESC
) visaInfo

INNER JOIN SalaryRegisters sr
        ON sr.EmployeeId = emp.Id AND sr.TenantId = @TenantId AND sr.IsDeleted = 0
INNER JOIN SalaryRegisterDetails srdNet
        ON srdNet.SalaryRegisterId = sr.Id AND srdNet.TenantId = @TenantId AND srdNet.IsDeleted = 0
INNER JOIN WageTypes wtNet
        ON wtNet.Id = srdNet.WageTypeId AND wtNet.Code = '9000'
       AND wtNet.TenantId = @TenantId AND wtNet.IsDeleted = 0

WHERE emp.TenantId  = @TenantId
  AND emp.IsDeleted = 0
  AND sr.InPeriod = sr.ForPeriod
  AND DATEPART(Month, sr.InPeriod) = @PrmMonth
  AND DATEPART(Year,  sr.InPeriod) = @PrmYear
  AND sr.PayrollRunType = @PrmPayrollRunType
  AND (
        (ISNULL(@PrmEmpCode, '') <> '' AND emp.Code = @PrmEmpCode)
     OR (ISNULL(@PrmEmpCode, '') =  '' AND ISNULL(@PrmPayrollArea, '') <> '' AND payrollArea.Name = @PrmPayrollArea)
      )
ORDER BY emp.Code;";

        /// <summary>
        /// Feeds the company-logo picture box (<c>[PayslipLogoQuery].[logo]</c>).
        ///
        /// FEATURE LOST: the legacy query read <c>OrganizationUnit.logo</c> for the unit whose
        /// OrganizationUnitType was 'Company'. AbpOrganizationUnits has no Logo column and HCMv3 has
        /// no Company entity carrying one, so NULL is returned and the picture box prints empty --
        /// the same gap PayslipSql records for the English payslip's CompanyLogo column.
        ///
        /// The query still selects FROM the org-unit table, tenant-filtered, rather than being a
        /// bare "SELECT NULL": that keeps the shape (and the @TenantId predicate) in place for
        /// whoever restores the logo column.
        /// </summary>
        public const string PayslipLogoQuery = @"-- rptPayslipAr :: PayslipLogoQuery
SELECT TOP 1 CAST(NULL AS VARBINARY(MAX)) AS logo   -- FEATURE LOST: no Logo column in AbpOrganizationUnits
FROM AbpOrganizationUnits orgUnit
WHERE orgUnit.TenantId  = @TenantId
  AND orgUnit.IsDeleted = 0;";

        /// <summary>
        /// srptPaySlipEarningAr -- the earnings block.
        ///
        /// FAITHFUL DIVERGENCE FROM THE ENGLISH SIBLING: the legacy Arabic earnings query filters on
        /// <c>BillingType = 0 AND Code &lt;&gt; 9000</c> only, whereas the English one also excludes
        /// <c>DontImpactNetPay</c> wage types. That difference is legacy, not a port artefact, and is
        /// preserved. On the reconciliation case (employee 3400, June 2024) the two agree, because
        /// every earning wage type there has DontImpactNetPay = 0 -- so the divergence is latent, and
        /// worth a product decision rather than a silent fix.
        ///
        /// The legacy currency sub-select was a correlated ROW_NUMBER() derived table keyed on
        /// <c>B.WageType = 1</c> -- a RAW LEGACY PRIMARY KEY meaning Code '1000' (Basic Salary), NOT
        /// a wage type whose Code is '1' (which in HCMv3 is Employee PIFSS Ded, a deduction). See
        /// PayslipSql.cs. Rewritten as an OUTER APPLY on Code = '1000'.
        /// </summary>
        public const string PayslipEarningQuery = @"-- rptPayslipAr :: earnings (srptPaySlipEarningAr)
SELECT
      emp.Code       AS [Employee Code]
    , emp.FullNameAr AS [Employee Name]
    , sr.ForPeriod
    , sr.InPeriod
    , sr.PayrollRunType
    , srd.Amount
    , ROUND(srd.Number, 3) AS Number
    , ROUND(srd.Rate,   3) AS Rate
    , wt.NameAr      AS [Wage Type]
    , wt.BillingType
    , basicPayCurrency.CurrencyShortFormAr AS Currency
FROM Employees emp
INNER JOIN SalaryRegisters sr
        ON sr.EmployeeId = emp.Id AND sr.TenantId = @TenantId AND sr.IsDeleted = 0
INNER JOIN SalaryRegisterDetails srd
        ON srd.SalaryRegisterId = sr.Id AND srd.TenantId = @TenantId AND srd.IsDeleted = 0
       AND srd.Amount <> 0
INNER JOIN WageTypes wt
        ON wt.Id = srd.WageTypeId AND wt.TenantId = @TenantId AND wt.IsDeleted = 0
       AND wt.BillingType = 0
       AND wt.Code <> '9000'
OUTER APPLY (
    SELECT TOP 1 cur.CurrencyShortFormAr
    FROM EmployeeBasicPays ebp
    JOIN EmployeeBasicPayDetails ebpd ON ebpd.EmployeeBasicPayId = ebp.Id AND ebpd.TenantId = @TenantId AND ebpd.IsDeleted = 0
    JOIN WageTypes wtBasic ON wtBasic.Id = ebpd.WageTypeId AND wtBasic.Code = '1000' AND wtBasic.TenantId = @TenantId AND wtBasic.IsDeleted = 0
    LEFT JOIN Currencies cur ON cur.Id = ebpd.CurrencyId AND cur.TenantId = @TenantId AND cur.IsDeleted = 0
    WHERE ebp.EmployeeId = emp.Id AND ebp.TenantId = @TenantId AND ebp.IsDeleted = 0
    ORDER BY ebp.StartDate DESC
) basicPayCurrency
WHERE emp.TenantId = @TenantId AND emp.IsDeleted = 0
  AND emp.Code = @empcode
  AND sr.ForPeriod      = @ForPeriod
  AND sr.InPeriod       = @InPeriod
  AND sr.PayrollRunType = @RunType;";

        /// <summary>
        /// srptPaySlipDeductionAr -- the deductions block.
        ///
        /// The legacy clause <c>SRdetail.WageType &lt;&gt; 65</c> is a RAW LEGACY PRIMARY KEY:
        /// HRMS_RCF WageType OID 65 = Code '9000' (Net Pay). It means "do not print the net-pay
        /// carrier row as a deduction", mirroring the earnings query's <c>Code &lt;&gt; 9000</c>.
        /// Ported as a Code comparison, exactly as PayslipSql does for the English payslip.
        /// </summary>
        public const string PayslipDeductionQuery = @"-- rptPayslipAr :: deductions (srptPaySlipDeductionAr)
SELECT
      emp.Code       AS [Employee Code]
    , emp.FullNameAr AS [Employee Name]
    , sr.ForPeriod
    , sr.InPeriod
    , sr.PayrollRunType
    , srd.Amount
    , ROUND(srd.Number, 3) AS Number
    , ROUND(srd.Rate,   3) AS Rate
    , wt.NameAr      AS [Wage Type]
    , wt.BillingType
    , basicPayCurrency.CurrencyShortFormAr AS Currency
FROM Employees emp
INNER JOIN SalaryRegisters sr
        ON sr.EmployeeId = emp.Id AND sr.TenantId = @TenantId AND sr.IsDeleted = 0
INNER JOIN SalaryRegisterDetails srd
        ON srd.SalaryRegisterId = sr.Id AND srd.TenantId = @TenantId AND srd.IsDeleted = 0
       AND srd.Amount <> 0
INNER JOIN WageTypes wt
        ON wt.Id = srd.WageTypeId AND wt.TenantId = @TenantId AND wt.IsDeleted = 0
       AND wt.BillingType = 1
       AND ISNULL(wt.DontImpactNetPay, 0) = 0
       AND wt.Code <> '9000'
OUTER APPLY (
    SELECT TOP 1 cur.CurrencyShortFormAr
    FROM EmployeeBasicPays ebp
    JOIN EmployeeBasicPayDetails ebpd ON ebpd.EmployeeBasicPayId = ebp.Id AND ebpd.TenantId = @TenantId AND ebpd.IsDeleted = 0
    JOIN WageTypes wtBasic ON wtBasic.Id = ebpd.WageTypeId AND wtBasic.Code = '1000' AND wtBasic.TenantId = @TenantId AND wtBasic.IsDeleted = 0
    LEFT JOIN Currencies cur ON cur.Id = ebpd.CurrencyId AND cur.TenantId = @TenantId AND cur.IsDeleted = 0
    WHERE ebp.EmployeeId = emp.Id AND ebp.TenantId = @TenantId AND ebp.IsDeleted = 0
    ORDER BY ebp.StartDate DESC
) basicPayCurrency
WHERE emp.TenantId = @TenantId AND emp.IsDeleted = 0
  AND emp.Code = @empcode
  AND sr.ForPeriod      = @ForPeriod
  AND sr.InPeriod       = @InPeriod
  AND sr.PayrollRunType = @RunType;";

        /// <summary>
        /// srptPayslipAttendenceSummaryAr. MECHANICAL rewrite; wage-type Codes 4021/4022/4000/4002
        /// are real business codes and carry over, quoted because Code is nvarchar in HCMv3.
        /// </summary>
        public const string PayslipAttendanceQuery = @"-- rptPayslipAr :: attendance summary (srptPayslipAttendenceSummaryAr)
SELECT
      emp.Code    AS [Employee Code]
    , sr.ForPeriod
    , sr.InPeriod
    , sr.PayrollRunType
    , srd.Number
    , wt.NameAr   AS [Wage Type]
    , wt.Id       AS WageTypeId
    , wt.Code
FROM SalaryRegisters sr
INNER JOIN Employees emp             ON emp.Id = sr.EmployeeId       AND emp.TenantId = @TenantId AND emp.IsDeleted = 0
INNER JOIN SalaryRegisterDetails srd ON srd.SalaryRegisterId = sr.Id AND srd.TenantId = @TenantId AND srd.IsDeleted = 0
INNER JOIN WageTypes wt              ON wt.Id = srd.WageTypeId       AND wt.TenantId  = @TenantId AND wt.IsDeleted  = 0
WHERE sr.TenantId = @TenantId AND sr.IsDeleted = 0
  AND wt.Code IN ('4021', '4022', '4000', '4002')
  AND emp.Code          = @empcode
  AND sr.ForPeriod      = @ForPeriod
  AND sr.InPeriod       = @InPeriod
  AND sr.PayrollRunType = @RunType;";

        /// <summary>srptPayslipIndemnityDaysSummaryAr. MECHANICAL rewrite.</summary>
        public const string PayslipIndemnityQuery = @"-- rptPayslipAr :: indemnity days (srptPayslipIndemnityDaysSummaryAr)
SELECT
      emp.Code    AS [Employee Code]
    , sr.ForPeriod
    , sr.InPeriod
    , sr.PayrollRunType
    , srd.Number
    , wt.NameAr   AS [Wage Type]
    , wt.Id       AS WageTypeId
FROM Employees emp
INNER JOIN SalaryRegisters sr        ON sr.EmployeeId = emp.Id       AND sr.TenantId  = @TenantId AND sr.IsDeleted  = 0
INNER JOIN SalaryRegisterDetails srd ON srd.SalaryRegisterId = sr.Id AND srd.TenantId = @TenantId AND srd.IsDeleted = 0
INNER JOIN WageTypes wt              ON wt.Id = srd.WageTypeId       AND wt.TenantId  = @TenantId AND wt.IsDeleted  = 0
WHERE emp.TenantId = @TenantId AND emp.IsDeleted = 0
  AND wt.Code IN ('4004', '4005', '4006', '4017', '4018')
  AND emp.Code          = @empcode
  AND sr.ForPeriod      = @ForPeriod
  AND sr.InPeriod       = @InPeriod
  AND sr.PayrollRunType = @RunType;";

        /// <summary>srptPayslipWorkingDaysSummaryAr. MECHANICAL rewrite.</summary>
        public const string PayslipWorkingDayQuery = @"-- rptPayslipAr :: working days (srptPayslipWorkingDaysSummaryAr)
SELECT
      emp.Code    AS [Employee Code]
    , sr.ForPeriod
    , sr.InPeriod
    , sr.PayrollRunType
    , srd.Number
    , wt.NameAr   AS [Wage Type]
FROM Employees emp
INNER JOIN SalaryRegisters sr        ON sr.EmployeeId = emp.Id       AND sr.TenantId  = @TenantId AND sr.IsDeleted  = 0
INNER JOIN SalaryRegisterDetails srd ON srd.SalaryRegisterId = sr.Id AND srd.TenantId = @TenantId AND srd.IsDeleted = 0
INNER JOIN WageTypes wt              ON wt.Id = srd.WageTypeId       AND wt.TenantId  = @TenantId AND wt.IsDeleted  = 0
WHERE emp.TenantId = @TenantId AND emp.IsDeleted = 0
  AND wt.Code IN ('4007', '4008', '4009', '4011', '4014')
  AND emp.Code          = @empcode
  AND sr.ForPeriod      = @ForPeriod
  AND sr.InPeriod       = @InPeriod
  AND sr.PayrollRunType = @RunType;";

        #region Calculated-field expressions lifted out of the legacy .resx

        // These are DevExpress EXPRESSION strings, not SQL. They lived in rptPayslipAr.resx and were
        // fetched with resources.GetString(...); they are inlined here so the whole report is
        // readable from source and the .resx does not have to be carried over.

        /// <summary>
        /// CalcArMonth. Arabic month name of the payslip period.
        ///
        /// PORTED WITH ITS BUG INTACT: the expression reads <c>[CalForPeriodMonth]</c> -- one letter
        /// short of the calculated field that actually exists, <c>CalcForPeriodMonth</c>. Nothing in
        /// the layout binds CalcArMonth, so the typo has never been reachable. Left as-is rather than
        /// "fixed": correcting a dead expression changes nothing visible and would misrepresent the
        /// legacy report to the next reader. It is listed here so it is not mistaken for a port slip.
        /// </summary>
        public const string CalcArMonthExpression =
            "Iif([CalForPeriodMonth] == 1 ,'يناير',Iif([CalForPeriodMonth] == 2, 'فبراير' , Iif([CalForPeriodMonth] == 3, 'مارس' ,Iif([CalForPeriodMonth] = 4, 'أبريل' ,Iif([CalForPeriodMonth] == 5, 'مايو' ,Iif([CalForPeriodMonth] == 6, 'يونيو' ,Iif([CalForPeriodMonth] == 7, 'يوليو'  , Iif([CalForPeriodMonth] == 8, 'أغسطس' ,Iif([CalForPeriodMonth] == 9, 'سبتمبر' , Iif([CalForPeriodMonth] == 10, 'أكتوبر' , Iif([CalForPeriodMonth] == 11, 'نوفمبر' , Iif([CalForPeriodMonth] == 12, 'ديسمبر' ,' ' )))) )) ) ) ) )))";

        /// <summary>CalcJoiningDateMonth. Arabic month name of the employee's joining date.</summary>
        public const string CalcJoiningDateMonthExpression =
            "Iif([CalcJoingDateMonthNumber] == 1 ,'يناير',Iif([CalcJoingDateMonthNumber] == 2, 'فبراير' , Iif([CalcJoingDateMonthNumber] == 3, 'مارس' ,Iif([CalcJoingDateMonthNumber] = 4, 'أبريل' ,Iif([CalcJoingDateMonthNumber] == 5, 'مايو' ,Iif([CalcJoingDateMonthNumber] == 6, 'يونيو' ,Iif([CalcJoingDateMonthNumber] == 7, 'يوليو'  , Iif([CalcJoingDateMonthNumber] == 8, 'أغسطس' ,Iif([CalcJoingDateMonthNumber] == 9, 'سبتمبر' , Iif([CalcJoingDateMonthNumber] == 10, 'أكتوبر' , Iif([CalcJoingDateMonthNumber] == 11, 'نوفمبر' , Iif([CalcJoingDateMonthNumber] == 12, 'ديسمبر' ,' ' ))))))))))))";

        /// <summary>CalcPassportMonthAr. Arabic month name of the passport expiry date.</summary>
        public const string CalcPassportMonthArExpression =
            "Iif([CalcPassportMonthNum] == 1 ,'يناير',Iif([CalcPassportMonthNum] == 2, 'فبراير' , Iif([CalcPassportMonthNum] == 3, 'مارس' ,Iif([CalcPassportMonthNum] = 4, 'أبريل' ,Iif([CalcPassportMonthNum] == 5, 'مايو' ,Iif([CalcPassportMonthNum] == 6, 'يونيو' ,Iif([CalcPassportMonthNum] == 7, 'يوليو'  , Iif([CalcPassportMonthNum] == 8, 'أغسطس' ,Iif([CalcPassportMonthNum] == 9, 'سبتمبر' , Iif([CalcPassportMonthNum] == 10, 'أكتوبر' , Iif([CalcPassportMonthNum] == 11, 'نوفمبر' , Iif([CalcPassportMonthNum] == 12, 'ديسمبر' ,' ' ))))))))))))";

        /// <summary>
        /// CalcHeader. The Arabic title line: "regular payslip for the month: &lt;month&gt; - &lt;year&gt;"
        /// for a regular run, or the leave-salary wording for an off-cycle run.
        /// </summary>
        public const string CalcHeaderExpression =
            "Iif([Parameters].[PrmPayrollRunType] == 0,'كشوف الراتب العادي للشهر: ' + " +
            "Iif([Parameters].[PrmPeriodMonth] = 1,'يناير'," +
            "Iif([Parameters].[PrmPeriodMonth] = 2,'فبراير'," +
            "Iif([Parameters].[PrmPeriodMonth] = 3,'مارس'," +
            "Iif([Parameters].[PrmPeriodMonth] = 4,'أبريل'," +
            "Iif([Parameters].[PrmPeriodMonth] = 5,'مايو'," +
            "Iif([Parameters].[PrmPeriodMonth] = 6,'يونيو'," +
            "Iif([Parameters].[PrmPeriodMonth] = 7,'يوليو'," +
            "Iif([Parameters].[PrmPeriodMonth] = 8,'أغسطس'," +
            "Iif([Parameters].[PrmPeriodMonth] = 9,'سبتمبر'," +
            "Iif([Parameters].[PrmPeriodMonth] = 10,'أكتوبر'," +
            "Iif([Parameters].[PrmPeriodMonth] = 11,'نوفمبر'," +
            "Iif([Parameters].[PrmPeriodMonth] = 12,'ديسمبر'," +
            "''))))))))))))" +
            " + ' - ' + [Parameters].[PrmPeriodYear]" +
            ", Iif([Parameters].[PrmPayrollRunType] == 1, " +
            "'ترك الراتب الراتب - ' + [Parameters].[PrmPeriodYear]" +
            ", ''))";

        #endregion
    }
}
