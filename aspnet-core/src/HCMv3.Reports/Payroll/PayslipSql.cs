// SQL for rptPayslipEn and its subreports, rewritten for the HCMv3 schema.
//
// SOURCE OF TRUTH: aspnet-core/tools/HCMv3.ReportMigrator/reports/PayslipEn/queries/*.sql
// These strings are a verbatim copy of those files. If a query needs changing, change it there
// first and re-copy, so the migration pipeline and the ported report never diverge.
//
// MULTI-TENANCY: these run through DevExpress SqlDataSource as RAW SQL, so EF Core's per-tenant
// global query filter does NOT apply. Every query filters TenantId explicitly via @TenantId, which
// is supplied as a report parameter (PrmTenantId) populated from AbpSession.TenantId by the caller.
// Removing an "AND TenantId = @TenantId" silently leaks rows across tenants.

namespace HCMv3.Reports.Payroll
{
    /// <summary>Raw SQL used by rptPayslipEn and its subreports.</summary>
    public static class PayslipSql
    {
        public const string PayrollArea = @"-- rptPayslipEn :: PayrollArea (parameter look-up list feeding the @PrmPayrollArea combo)
-- Legacy: ""select * from PayrollArea""  -- MECHANICAL rewrite, plus the two ABP filters the legacy
-- corpus has no concept of.
SELECT *
FROM PayrollAreas
WHERE TenantId = @TenantId
  AND IsDeleted = 0
ORDER BY Name;";

        public const string PayslipOrgV2Query = @"-- ===================================================================================================
-- rptPayslipEn :: PayslipOrgV2Query  (MASTER query -- one row per employee/period)
-- HCMv3 rewrite of the legacy DevExpress CustomSqlQuery.
--
-- MULTI-TENANCY / SOFT DELETE
--   The legacy corpus contains ZERO references to TenantId or IsDeleted -- HCM10 was single-tenant
--   and hard-deleted. Every table touched below is an ABP FullAuditedEntity with both columns, so
--   @TenantId and ""IsDeleted = 0"" are ADDED to every single join. Omitting either silently leaks
--   rows across tenants / resurrects deleted records, and nothing in the report would show it.
--
-- MECHANICAL RENAMES applied throughout
--   Employee->Employees, SalaryRegister->SalaryRegisters, ...        (table pluralisation)
--   OID -> Id                                                        (PK)
--   <EntityName> -> <EntityName>Id                                   (FK columns)
--
-- NON-MECHANICAL REWRITES -- see PORTING-NOTES.md for the full list. Summary:
--   1. OrgLevel1..7 + OrganizationUnitType. Legacy EmployeeOrgAssignment carried seven flat FK
--      columns and picked the one whose OrganizationUnit had type 'Department' (the OrgUnit column)
--      via a CASE testing OrgLevel1 first. HCMv3 has a SINGLE OrganizationUnitId into
--      AbpOrganizationUnits, an ABP materialised-path tree (Code/ParentId). ABP itself has no
--      unit-type concept, but HCMv3's OrgUnitProfiles.OrganizationUnitTypeId supplies exactly that,
--      so the 14 aliased joins collapse to an ancestor walk (see the OUTER APPLY below).
--      RESOLVED -- an earlier revision of this note claimed ""which ancestor is the Department is
--      not expressible"" and substituted the employee's own unit. That was wrong and is fixed.
--   2. CompanyLogo. Legacy read OrganizationUnit.logo (an image column) of the Company_n unit. HCMv3
--      keeps logos in the file store: CompanyLogo stays a NULL placeholder and CompanyLogoOrgUnitId
--      carries the employee unit, which HcmReportBase resolves through ICompanyLogoResolver.
--   3. PayrollArea join. Legacy joined on a STRING: PayrollArea.Code = EmployeeOrgAssignment.PayrollArea.
--      HCMv3 models it properly as EmployeeOrgAssignments.PayrollAreaId -> PayrollAreas.Id.
--      The @PrmPayrollArea parameter still filters on PayrollAreas.Name for operator parity.
--   4. EmployeeBasicPayDetail.WageType = 1 -- a RAW LEGACY PRIMARY KEY, not a code. HRMS_RCF
--      WageType OID 1 = Code '1000' (Basic Salary). HCMv3 has an unrelated wage type whose *Code*
--      is '1' (Employee PIFSS Ded), so translating the literal 1 mechanically returns a deduction
--      instead of basic pay. Rewritten as WageTypes.Code = '1000'.
--   5. EmployeeVisaInfo / EmployeeGratuity joins dropped: no column from either is projected, and
--      as LEFT JOINs to 1:N tables they could only fan the master row out. Legacy bug, not ported.
-- ===================================================================================================

-- PORT DEVIATION vs queries/PayslipOrgV2Query.sql: the source file opens with
--     DECLARE @PaySlipDate DATE = EOMONTH(DATEFROMPARTS(@PrmYear, @PrmMonth, 1));
-- DevExpress's CustomSqlQuery validator rejects anything that is not purely SELECT
-- (""A custom SQL query should contain only SELECT statements""), so the local variable is
-- inlined at its three use sites below. Semantics are identical.

SELECT
      emp.Code                                  AS [Employee Code]
    , emp.Status
    , emp.CivilID
    , emp.Mobile
    , bank.NameEn                               AS [Bank Name]
    , bank.Code                                 AS BankCode
    , empBank.StartDate
    , empBank.IBAN                              AS [Bank IBAN]
    , empBank.AccountNum
    , deptAnc.DisplayName                       AS OrgUnit          -- see rewrite note 1
    , emp.FullNameEn                            AS [Employee Name]
    , subGroup.NameEn                           AS Grade
    , grp.NameEn                                AS [Salary Profile]
    , jobTitle.NameEn                           AS Jobtitle
    , emp.JoiningDate                           AS [Joining Date]
    , sr.ForPeriod
    , sr.InPeriod
    , sr.PayrollRunType
    , srdNet.Amount                             AS [Net Pay]
    , payrollArea.Name                          AS [Payroll Area]
    , payrollArea.Code                          AS [Payroll Area Code]
    , basicPayCurrency.CurrencyShortFormEn      AS Currency
    , CAST(NULL AS VARBINARY(MAX))              AS CompanyLogo      -- placeholder, see rewrite note 2
    , eoa.OrganizationUnitId                    AS CompanyLogoOrgUnitId -- see rewrite note 2
    , UPPER(CASE
        WHEN sr.PayrollRunType = 0 THEN
            CASE WHEN @PrmEncashment = 1
                 THEN 'LEAVE ENCASHMENT PAYSLIP FOR THE MONTH : ' + DATENAME(Month, sr.ForPeriod) + '-' + CONVERT(VARCHAR(10), @PrmYear)
                 ELSE 'REGULAR PAYSLIP FOR THE MONTH : '          + DATENAME(Month, sr.ForPeriod) + '-' + CONVERT(VARCHAR(10), @PrmYear)
            END
        WHEN sr.PayrollRunType = 1 THEN
            CASE ISNULL(reason.NameEn, '')
                 WHEN 'Advance Leave'  THEN 'LEAVE SALARY PAYSLIP'
                 WHEN 'End Of Service' THEN 'END OF SERVICE SETTLEMENT'
            END
      END)                                      AS HeaderText
    , CASE WHEN sr.PayrollRunType = 1 AND ISNULL(reason.NameEn, '') = 'End Of Service'
           THEN separation.LastDayInService END AS LastDayInService
    , passport.DocumentNum                      AS PassportNo
    , passport.ExpiryDate                       AS PassportExpiryDate
    , civilId.ExpiryDate                        AS CivilIdExpiryDate
    , sponsor.NameEn                            AS SponsorEn

FROM Employees emp

INNER JOIN SalaryRegisters sr
        ON sr.EmployeeId = emp.Id
       AND sr.TenantId   = @TenantId
       AND sr.IsDeleted  = 0

-- Legacy: WageType IN (SELECT OID FROM WageType WHERE Code = 9000). The net-pay carrier row.
INNER JOIN SalaryRegisterDetails srdNet
        ON srdNet.SalaryRegisterId = sr.Id
       AND srdNet.TenantId         = @TenantId
       AND srdNet.IsDeleted        = 0
INNER JOIN WageTypes wtNet
        ON wtNet.Id       = srdNet.WageTypeId
       AND wtNet.Code     = '9000'
       AND wtNet.TenantId = @TenantId
       AND wtNet.IsDeleted = 0

LEFT JOIN EmployeeOrgAssignments eoa
       ON eoa.EmployeeId = emp.Id
      AND eoa.TenantId   = @TenantId
      AND eoa.IsDeleted  = 0
      AND EOMONTH(DATEFROMPARTS(@PrmYear, @PrmMonth, 1)) BETWEEN eoa.StartDate AND eoa.EndDate
LEFT JOIN JobTitles      jobTitle     ON jobTitle.Id     = eoa.JobTitleId        AND jobTitle.TenantId     = @TenantId AND jobTitle.IsDeleted     = 0
LEFT JOIN PayrollAreas   payrollArea  ON payrollArea.Id  = eoa.PayrollAreaId     AND payrollArea.TenantId  = @TenantId AND payrollArea.IsDeleted  = 0
LEFT JOIN EmployeeGroups grp          ON grp.Id          = eoa.EmployeeGroupId   AND grp.TenantId          = @TenantId AND grp.IsDeleted          = 0
LEFT JOIN EmployeeSubGroups subGroup  ON subGroup.Id     = eoa.EmployeeSubGroupId AND subGroup.TenantId    = @TenantId AND subGroup.IsDeleted     = 0
LEFT JOIN AbpOrganizationUnits orgUnit ON orgUnit.Id     = eoa.OrganizationUnitId AND orgUnit.TenantId     = @TenantId AND orgUnit.IsDeleted      = 0

-- The legacy payslip's OrgUnit column is NOT the employee's own unit: it is the DEPARTMENT-typed
-- ancestor, resolved by seven LEFT JOINs on OrgLevel1..OrgLevel7 and a CASE that tests OrgLevel1
-- first (see rptPayslipEn.resx, the End AS OrgUnit block). OrgLevel1 is the top of the tree, so
-- yields the SHALLOWEST Department-typed ancestor -- hence ORDER BY LEN(anc.Code) ascending.
-- Reference implementation: Bank/BankSql.cs.
OUTER APPLY (
    SELECT TOP 1 anc.DisplayName
    FROM AbpOrganizationUnits anc
    INNER JOIN OrgUnitProfiles ancp       ON ancp.OrganizationUnitId = anc.Id      AND ancp.TenantId = @TenantId AND ancp.IsDeleted = 0
    INNER JOIN OrganizationUnitTypes anct ON anct.Id = ancp.OrganizationUnitTypeId AND anct.TenantId = @TenantId AND anct.IsDeleted = 0
    WHERE anc.TenantId  = @TenantId
      AND anc.IsDeleted = 0
      AND orgUnit.Code LIKE anc.Code + '%'
      AND anct.NameEn = 'Department'
    ORDER BY LEN(anc.Code)
) deptAnc

LEFT JOIN EmployeeSponsors empSponsor
       ON empSponsor.EmployeeId = emp.Id
      AND empSponsor.TenantId   = @TenantId
      AND empSponsor.IsDeleted  = 0
      AND EOMONTH(DATEFROMPARTS(@PrmYear, @PrmMonth, 1)) BETWEEN empSponsor.StartDate AND empSponsor.EndDate
LEFT JOIN Sponsors sponsor ON sponsor.Id = empSponsor.SponsorId AND sponsor.TenantId = @TenantId AND sponsor.IsDeleted = 0

LEFT JOIN EmployeeBanks empBank
       ON empBank.EmployeeId = emp.Id
      AND empBank.TenantId   = @TenantId
      AND empBank.IsDeleted  = 0
      AND EOMONTH(DATEFROMPARTS(@PrmYear, @PrmMonth, 1)) BETWEEN empBank.StartDate AND empBank.EndDate
LEFT JOIN Banks bank ON bank.Id = empBank.BankId AND bank.TenantId = @TenantId AND bank.IsDeleted = 0

-- Latest passport / civil-id document. Legacy used a correlated ""OID IN (SELECT TOP 1 ...)""; kept as
-- an OUTER APPLY, which expresses the same ""one most-recently-expiring document"" intent and lets the
-- optimiser see the correlation.
OUTER APPLY (
    SELECT TOP 1 ed.DocumentNum, ed.ExpiryDate
    FROM EmployeeDocuments ed
    JOIN DocumentTypes dt ON dt.Id = ed.DocumentTypeId AND dt.TenantId = @TenantId AND dt.IsDeleted = 0
    WHERE ed.EmployeeId = emp.Id AND ed.TenantId = @TenantId AND ed.IsDeleted = 0
      AND dt.NameEn LIKE '%passport%'
    ORDER BY ed.ExpiryDate DESC
) passport
OUTER APPLY (
    SELECT TOP 1 ed.DocumentNum, ed.ExpiryDate
    FROM EmployeeDocuments ed
    JOIN DocumentTypes dt ON dt.Id = ed.DocumentTypeId AND dt.TenantId = @TenantId AND dt.IsDeleted = 0
    WHERE ed.EmployeeId = emp.Id AND ed.TenantId = @TenantId AND ed.IsDeleted = 0
      AND dt.Code = '2'
    ORDER BY ed.ExpiryDate DESC
) civilId

-- Currency of the employee's most recent BASIC PAY line -- see rewrite note 4.
OUTER APPLY (
    SELECT TOP 1 cur.CurrencyShortFormEn
    FROM EmployeeBasicPays ebp
    JOIN EmployeeBasicPayDetails ebpd ON ebpd.EmployeeBasicPayId = ebp.Id AND ebpd.TenantId = @TenantId AND ebpd.IsDeleted = 0
    JOIN WageTypes wtBasic ON wtBasic.Id = ebpd.WageTypeId AND wtBasic.Code = '1000' AND wtBasic.TenantId = @TenantId AND wtBasic.IsDeleted = 0
    LEFT JOIN Currencies cur ON cur.Id = ebpd.CurrencyId AND cur.TenantId = @TenantId AND cur.IsDeleted = 0
    WHERE ebp.EmployeeId = emp.Id AND ebp.TenantId = @TenantId AND ebp.IsDeleted = 0
    ORDER BY ebp.StartDate DESC
) basicPayCurrency

LEFT JOIN PayrollReasonTypes reason
       ON reason.Id = sr.PayrollReasonTypeId AND reason.TenantId = @TenantId AND reason.IsDeleted = 0

OUTER APPLY (
    SELECT TOP 1 es.LastDayInService
    FROM EmployeeSeparations es
    WHERE es.EmployeeId = emp.Id AND es.TenantId = @TenantId AND es.IsDeleted = 0
    ORDER BY es.LastDayInService DESC
) separation

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
ORDER BY
    -- Legacy sort key preserved verbatim: numeric codes right-aligned zero-padded to 20,
    -- non-numeric left-aligned. (The legacy Replicate('',21) is a no-op; kept for byte parity.)
    CASE WHEN ISNUMERIC(emp.Code) = 1 THEN RIGHT(REPLICATE('0', 21) + emp.Code, 20)
         WHEN ISNUMERIC(emp.Code) = 0 THEN LEFT(emp.Code + REPLICATE('', 21), 20)
         ELSE emp.Code
    END;";

        public const string PayslipEarningQuery = @"-- rptPayslipEn :: PayslipEarningQuery (srptPaySlipEarningEn subreport -- the earnings block)
--
-- MECHANICAL rewrite, except:
--   * ""wagetype.code <> 9000"" compares an nvarchar Code to a numeric literal in the legacy schema.
--     HCMv3 WageTypes.Code is nvarchar, so it is quoted: <> '9000'. Left unquoted SQL Server would
--     implicitly convert the WHOLE column to int and throw on any non-numeric code.
--   * EmployeeBasicPayDetail.WageType = 1 was a RAW LEGACY PRIMARY KEY meaning Code '1000'
--     (Basic Salary). HCMv3 has an unrelated wage type whose Code is literally '1'
--     (Employee PIFSS Ded), so the literal must NOT be carried over. See PORTING-NOTES.md.
SELECT
      emp.Code
    , emp.FullNameEn
    , sr.ForPeriod
    , sr.InPeriod
    , sr.PayrollRunType
    , srd.Amount
    , ROUND(srd.Number, 3)     AS Number
    , ROUND(srd.Rate,   3)     AS Rate
    , wt.NameEn                AS WageType
    , wt.BillingType
    , basicPayCurrency.CurrencyShortFormEn AS Currency
FROM Employees emp
INNER JOIN SalaryRegisters sr
        ON sr.EmployeeId = emp.Id AND sr.TenantId = @TenantId AND sr.IsDeleted = 0
INNER JOIN SalaryRegisterDetails srd
        ON srd.SalaryRegisterId = sr.Id AND srd.TenantId = @TenantId AND srd.IsDeleted = 0
       AND srd.Amount <> 0
INNER JOIN WageTypes wt
        ON wt.Id = srd.WageTypeId AND wt.TenantId = @TenantId AND wt.IsDeleted = 0
       AND wt.BillingType = 0
       AND ISNULL(wt.DontImpactNetPay, 0) = 0
       AND wt.Code <> '9000'
OUTER APPLY (
    SELECT TOP 1 cur.CurrencyShortFormEn
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

        public const string PayslipDeductionQuery = @"-- rptPayslipEn :: PayslipDeductionQuery (srptPaySlipDeductionEn subreport -- the deductions block)
--
-- NON-MECHANICAL: legacy carried ""SRdetail.WageType <> 65"". 65 is a RAW LEGACY PRIMARY KEY, and in
-- HRMS_RCF WageType OID 65 = Code '9000' (Net Pay) -- i.e. the clause means ""do not print the net-pay
-- carrier row as a deduction"", exactly mirroring the earnings query's ""code <> 9000"". It is a real
-- rule, not dead legacy noise, and is ported as a Code comparison.
-- (The FastReport pilot in HCMv3.Application/Reports/ReportsAppService.cs dropped this filter on the
-- grounds that 65 had ""no stable meaning in HCMv3"" -- worth revisiting there.)
SELECT
      emp.Code
    , emp.FullNameEn
    , sr.ForPeriod
    , sr.InPeriod
    , sr.PayrollRunType
    , srd.Amount
    , ROUND(srd.Number, 3)     AS Number
    , ROUND(srd.Rate,   3)     AS Rate
    , wt.NameEn                AS WageType
    , wt.BillingType
    , basicPayCurrency.CurrencyShortFormEn AS Currency
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
    SELECT TOP 1 cur.CurrencyShortFormEn
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

        public const string BasicPayQuery = @"-- rptPayslipEn :: BasicPayQuery (srptPayslipBasicPay subreport)
-- MECHANICAL rewrite. Note EmployeeBasicPayDetail.Currency -> EmployeeBasicPayDetails.CurrencyId.
SELECT
      emp.Code
    , emp.FullNameEn
    , wt.NameEn                    AS WageType
    , ebpd.Amount
    , ebp.StartDate
    , ebp.EndDate
    , cur.CurrencyShortFormEn      AS Currency
FROM EmployeeBasicPays ebp
LEFT JOIN EmployeeBasicPayDetails ebpd ON ebpd.EmployeeBasicPayId = ebp.Id  AND ebpd.TenantId = @TenantId AND ebpd.IsDeleted = 0
LEFT JOIN WageTypes  wt  ON wt.Id  = ebpd.WageTypeId  AND wt.TenantId  = @TenantId AND wt.IsDeleted  = 0
LEFT JOIN Employees  emp ON emp.Id = ebp.EmployeeId   AND emp.TenantId = @TenantId AND emp.IsDeleted = 0
LEFT JOIN Currencies cur ON cur.Id = ebpd.CurrencyId  AND cur.TenantId = @TenantId AND cur.IsDeleted = 0
WHERE ebp.TenantId  = @TenantId
  AND ebp.IsDeleted = 0
  AND emp.Code = @empcode
  AND ebp.StartDate <= @ForPeriod
  AND ebp.EndDate   >= @ForPeriod;";

        public const string OvertimeQuery = @"-- rptPayslipEn :: OvertimeQuery (srptPayslipOvertime subreport)
-- MECHANICAL rewrite: table pluralisation + OID/FK renames + TenantId/IsDeleted.
-- EOMonth(EffectiveDate) = @ForPeriod is kept verbatim: @ForPeriod is always a month-end date.
SELECT
      emp.Code
    , emp.FullNameEn
    , ot.NameEn                              AS Type
    , SUM(eot.ApprovedOvertimeHours)         AS ActualHrs
    , EOMONTH(eot.EffectiveDate)             AS EndOfMonth
FROM EmployeeOvertimes eot
LEFT JOIN OvertimeTypes ot ON ot.Id  = eot.OvertimeTypeId AND ot.TenantId  = @TenantId AND ot.IsDeleted  = 0
LEFT JOIN Employees     emp ON emp.Id = eot.EmployeeId     AND emp.TenantId = @TenantId AND emp.IsDeleted = 0
WHERE eot.TenantId  = @TenantId
  AND eot.IsDeleted = 0
  AND emp.Code = @empcode
  AND EOMONTH(eot.EffectiveDate) = @ForPeriod
GROUP BY emp.Code, emp.FullNameEn, ot.NameEn, EOMONTH(eot.EffectiveDate);";

        public const string PayslipAttendanceQuery = @"-- rptPayslipEn :: PayslipAttendanceQuery (srptPayslipAttendance subreport)
-- MECHANICAL rewrite. Legacy listed code 4022 twice in the IN list -- harmless, de-duplicated here.
SELECT
      emp.Code    AS [Employee Code]
    , sr.ForPeriod
    , sr.InPeriod
    , sr.PayrollRunType
    , srd.Number
    , wt.NameEn   AS [Wage Type]
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

        public const string PayslipWorkingDayQuery = @"-- rptPayslipEn :: PayslipWorkingDayQuery (srptPayslipWorkingDays subreport)
-- MECHANICAL rewrite; see PayslipIndemnityQuery for the Code-quoting note.
SELECT
      emp.Code    AS [Employee Code]
    , sr.ForPeriod
    , sr.InPeriod
    , sr.PayrollRunType
    , srd.Number
    , wt.NameEn   AS [Wage Type]
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

        public const string PayslipIndemnityQuery = @"-- rptPayslipEn :: PayslipIndemnityQuery (srptPayslipIndemnity subreport)
-- MECHANICAL rewrite. Wage-type Codes stay as the legacy business codes (4004..4018) -- these ARE
-- codes, not row ids -- but must be quoted now that Code is nvarchar (see PayslipEarningQuery note).
SELECT
      emp.Code    AS [Employee Code]
    , sr.ForPeriod
    , sr.InPeriod
    , sr.PayrollRunType
    , srd.Number
    , wt.NameEn   AS [Wage Type]
    , wt.Id       AS WageTypeId
FROM Employees emp
INNER JOIN SalaryRegisters sr        ON sr.EmployeeId = emp.Id           AND sr.TenantId  = @TenantId AND sr.IsDeleted  = 0
INNER JOIN SalaryRegisterDetails srd ON srd.SalaryRegisterId = sr.Id     AND srd.TenantId = @TenantId AND srd.IsDeleted = 0
INNER JOIN WageTypes wt              ON wt.Id = srd.WageTypeId           AND wt.TenantId  = @TenantId AND wt.IsDeleted  = 0
WHERE emp.TenantId = @TenantId AND emp.IsDeleted = 0
  AND wt.Code IN ('4004', '4005', '4006', '4017', '4018')
  AND emp.Code          = @empcode
  AND sr.ForPeriod      = @ForPeriod
  AND sr.InPeriod       = @InPeriod
  AND sr.PayrollRunType = @RunType;";

        public const string LeaveInfoQuery = @"-- rptPayslipEn :: LeaveInfoQuery (srptPayslipLeaveInfo subreport; off-cycle runs only)
-- MECHANICAL rewrite. The ""AND @PrmPayrollRunType = 1"" guard at the end is legacy's way of making the
-- subreport return nothing on a regular run; kept verbatim rather than moved into report logic.
SELECT
      emp.Code
    , emp.FullNameEn
    , emp.FullNameAr
    , at.NameEn AS AbsenceTypeEn
    , at.NameAr AS AbsenceTypeAr
    , ea.StartDate
    , ea.EndDate
    , ea.StartTime
    , ea.EndTime
    , ea.AbsenceHours
    , ea.TotalLeaveDays
    , ea.CalendarDays
    , ea.PaymentDate
    , ea.ReturnDate
    , CASE ea.LeavePaymentType
        WHEN 0 THEN 'Pay With Payroll'
        WHEN 1 THEN 'Pay In Advance'
        ELSE ''
      END AS LeavePaymentType
FROM EmployeeAbsences ea
LEFT JOIN Employees    emp ON emp.Id = ea.EmployeeId    AND emp.TenantId = @TenantId AND emp.IsDeleted = 0
LEFT JOIN AbsenceTypes at  ON at.Id  = ea.AbsenceTypeId AND at.TenantId  = @TenantId AND at.IsDeleted  = 0
WHERE ea.TenantId = @TenantId AND ea.IsDeleted = 0
  AND ea.LeavePaymentType = 2
  AND emp.Code = @PrmEmpCode
  AND ea.PaymentDate >= DATEFROMPARTS(@PrmYear, @PrmMonth, 1)
  AND ea.PaymentDate <= EOMONTH(DATEFROMPARTS(@PrmYear, @PrmMonth, 1))
  AND @PrmPayrollRunType = 1;";

        public const string LeaveEncashQuery = @"-- rptPayslipEn :: LeaveEncashQuery (srptPayslipLeaveEncashment subreport)
-- MECHANICAL rewrite + the Code-quoting and basic-pay-currency notes from PayslipEarningQuery.
-- Wage type '1052' is a business code and carries over unchanged.
SELECT
      emp.Code        AS [Employee Code]
    , emp.FullNameEn  AS [Employee Name]
    , sr.ForPeriod
    , sr.InPeriod
    , sr.PayrollRunType
    , srd.Amount
    , srd.Number
    , srd.Rate
    , wt.NameEn       AS [Wage Type]
    , wt.BillingType
    , basicPayCurrency.CurrencyShortFormEn AS Currency
FROM Employees emp
INNER JOIN SalaryRegisters sr        ON sr.EmployeeId = emp.Id       AND sr.TenantId  = @TenantId AND sr.IsDeleted  = 0
INNER JOIN SalaryRegisterDetails srd ON srd.SalaryRegisterId = sr.Id AND srd.TenantId = @TenantId AND srd.IsDeleted = 0 AND srd.Amount <> 0
INNER JOIN WageTypes wt              ON wt.Id = srd.WageTypeId       AND wt.TenantId  = @TenantId AND wt.IsDeleted  = 0
                                    AND wt.BillingType = 0 AND wt.Code <> '9000'
OUTER APPLY (
    SELECT TOP 1 cur.CurrencyShortFormEn
    FROM EmployeeBasicPays ebp
    JOIN EmployeeBasicPayDetails ebpd ON ebpd.EmployeeBasicPayId = ebp.Id AND ebpd.TenantId = @TenantId AND ebpd.IsDeleted = 0
    JOIN WageTypes wtBasic ON wtBasic.Id = ebpd.WageTypeId AND wtBasic.Code = '1000' AND wtBasic.TenantId = @TenantId AND wtBasic.IsDeleted = 0
    LEFT JOIN Currencies cur ON cur.Id = ebpd.CurrencyId AND cur.TenantId = @TenantId AND cur.IsDeleted = 0
    WHERE ebp.EmployeeId = emp.Id AND ebp.TenantId = @TenantId AND ebp.IsDeleted = 0
    ORDER BY ebp.StartDate DESC
) basicPayCurrency
WHERE emp.TenantId = @TenantId AND emp.IsDeleted = 0
  AND emp.Code = @PrmEmpCode
  AND sr.InPeriod = sr.ForPeriod
  AND DATEPART(Month, sr.InPeriod) = @PrmMonth
  AND DATEPART(Year,  sr.InPeriod) = @PrmYear
  AND sr.PayrollRunType = @PrmPayrollRunType
  AND wt.Code IN ('1052');";

    }
}
