// SQL for the payslip VARIANT reports -- rptSubordPayslipEn / rptSubordPayslipAr (manager view of
// subordinates' payslips), rptPayslip_wsp_En / rptPayslip_wosp_En (payslip with / without salary
// particulars) -- and for the PIFSS statutory report rptSocialSecurity.
//
// It is deliberately a SEPARATE file from PayslipSql.cs / PayslipArSql.cs: those two are committed
// and shared, and the variants must not force an edit into them. Where a variant needs exactly the
// same query as the base payslip it does NOT get a copy here -- the variant instantiates the already
// ported subreport class, which brings its own SQL with it. Only genuinely new SQL lives here.
//
// MULTI-TENANCY: these run through DevExpress SqlDataSource as RAW SQL, so EF Core's per-tenant
// global query filter does NOT apply. Every query filters TenantId explicitly via @TenantId, which
// HcmReportBase.EnsureHcmParameters wires as a QueryParameter keyed on the literal text "@TenantId"
// appearing in the SQL. Removing an "AND TenantId = @TenantId" silently leaks rows across tenants.
//
// *** THE WAGE-TYPE TRAP -- read before touching any wage-type literal in this file ***
//   Legacy "EmployeeBasicPayDetail.WageType = 1" is a RAW LEGACY PRIMARY KEY. HRMS_RCF WageType
//   OID 1 = Code '1000' (Basic Salary). HCMv3 has an unrelated wage type whose *Code* is '1', and
//   that one is the Employee PIFSS deduction. Translating the literal 1 mechanically therefore
//   swaps basic pay for a social-security deduction. Every such site below is rewritten as
//   WageTypes.Code = '1000'.
//   Conversely, rptSocialSecurity's legacy predicate is "WageType.Code IN ('1')" -- that one really
//   IS the code, and really IS the Employee PIFSS deduction, so it stays as Code = '1'. Getting
//   these two backwards in a PIFSS return would be especially bad; they are marked at each site.

namespace HCMv3.Reports.Payroll
{
    /// <summary>Raw SQL used by the payslip variant reports and by rptSocialSecurity.</summary>
    public static class PayslipVariantSql
    {
        // ===========================================================================================
        // Shared
        // ===========================================================================================

        /// <summary>
        /// Company logo. Legacy: <c>select logo from OrganizationUnit where OrgUnitType = 1</c>.
        ///
        /// (Legacy enum OrgUnitType 1 = Company.) HCMv3 keeps logos in the file store, so this query only
        /// keeps the layout's shape: HcmReportBase removes the picture box's <c>QueryLogo.logo</c> binding
        /// and loads the logo through ICompanyLogoResolver with CompanyLogoSource.FirstCompanyUnit; this
        /// NULL column is no longer read.
        /// </summary>
        public const string LogoQuery = @"-- payslip variants :: QueryLogo
SELECT TOP 1 CAST(NULL AS VARBINARY(MAX)) AS logo   -- FEATURE LOST: no Logo column in AbpOrganizationUnits
FROM AbpOrganizationUnits orgUnit
WHERE orgUnit.TenantId  = @TenantId
  AND orgUnit.IsDeleted = 0;";

        // ===========================================================================================
        // rptSubordPayslipEn / rptSubordPayslipAr -- the manager's view of subordinates' payslips
        // ===========================================================================================

        // WHY THESE TWO QUERIES KEEP THE UNION ROW
        //   The report's "Subordinates" parameter is a DynamicListLookUp bound to this very query
        //   (DisplayMember = subordinate, ValueMember = SubordinateOid). The legacy query appends a
        //   synthetic UNION row (-1 / 'All') so the combo offers an "All my subordinates" entry, and
        //   the report's FilterString then excludes that row from the printed output via
        //   "[PayrollAreaCode] <> -1". Dropping the union would silently remove the All option, so it
        //   is preserved verbatim in intent.
        //
        // PayrollAreaCode IS NOW AN ID, NOT A CODE
        //   Legacy PayrollArea.Code was an int and the union row used -1 as its "not a real row"
        //   sentinel. HCMv3 PayrollAreas.Code is nvarchar, so a literal -1 would force SQL Server to
        //   convert the whole column to int and throw on the first non-numeric code. The column is
        //   therefore projected from PayrollAreas.Id, which is an int and preserves exactly what the
        //   FilterString actually tests: "is this a real row or the All placeholder".
        //
        // ORG UNIT -- TYPE-BASED ANCESTOR WALK (the Bank/BankSql.cs pattern, NOT the PayslipSql one)
        //   Legacy walked two blind ParentOrgUnit hops (orgunit -> orgunit2 -> orgunit3) and printed
        //   the grandparent's name, on the assumption that that level is the department. HCMv3's
        //   AbpOrganizationUnits is an ABP materialised-path tree, so a fixed two-hop climb is
        //   meaningless -- it prints blank for anyone attached nearer the root and prints the wrong
        //   level for anyone attached deeper.
        //   The org-unit TYPE *is* expressible in HCMv3: it lives on
        //   OrgUnitProfiles.OrganizationUnitTypeId. So the fixed climb becomes a type-based ancestor
        //   walk -- of the employee's unit and all its ancestors (ancestor <=> the employee unit's
        //   Code starts with the ancestor's Code), take the shallowest one whose profile type is
        //   named 'Department'. COALESCE back to the employee's own unit so nothing goes blank where
        //   the legacy report printed something.
        //   NOTE this deliberately DIVERGES from PayslipSql.cs / SalaryCertificateSql.cs, which
        //   concluded unit type was "not expressible" and fell back to the employee's own unit
        //   unconditionally. That conclusion is wrong; Bank/BankSql.cs had it right. Flagged in
        //   PORTING-NOTES-PayslipVariants.md so the two do not silently disagree -- correcting the
        //   committed files is the orchestrator's call, not this batch's.
        //
        // JOINS DROPPED
        //   "left outer join EmployeeSponsor empsponsor on empsponsor.Employee = emp.OID" projects
        //   nothing and, being 1:N, can only fan the row out. Legacy bug; not ported. Same call
        //   PayslipSql made for its EmployeeVisaInfo / EmployeeGratuity joins -- except here
        //   EmployeeVisaInfos and EmployeeGratuities ARE projected (PassportExpiryDate,
        //   GradPaidTill), so those two are kept but narrowed to TOP 1 via OUTER APPLY rather than
        //   left as row-multiplying LEFT JOINs.

        /// <summary>rptSubordPayslipEn :: Query (MASTER -- one row per manager/subordinate/period).</summary>
        public const string SubordPayslipEnQuery = @"-- rptSubordPayslipEn :: Query (MASTER)
SELECT
      mgr.Id                                AS OID
    , mgr.Code                              AS [manager Code]
    , mgr.FullNameEn                        AS EmployeeName
    , sub.Id                                AS SubordinateOid
    , sub.Code                              AS SubordinateCode
    , sub.FullNameEn                        AS subordinate
    , ISNULL(department.DisplayName, orgUnit.DisplayName) AS OrgUnit   -- see ORG UNIT note above
    , subGroup.NameEn                       AS Grade
    , grp.NameEn                            AS SalaryProfile
    , jobTitle.NameEn                       AS Jobtitle
    , mgr.JoiningDate                       AS JoiningDate
    , visa.PassportExpiryDate               AS PassportExpiryDate
    , gratuity.GratuityPaidDate             AS GradPaidTill
    , bank.Code                             AS BankName         -- legacy printed the bank CODE here
    , empBank.IBAN                          AS BankIBAN
    , sr.ForPeriod
    , sr.InPeriod
    , srdNet.Amount                         AS NetPay
    , wtNet.NameEn                          AS NameEn
    , payrollArea.Name                      AS PayrollArea
    , payrollArea.Id                        AS PayrollAreaCode  -- see PayrollAreaCode note above
    , basicPayCurrency.CurrencyShortFormEn  AS Currency

FROM EmployeeManagers empManager

INNER JOIN Employees mgr
        ON mgr.Id       = empManager.ManagerId
       AND mgr.TenantId = @TenantId
       AND mgr.IsDeleted = 0
INNER JOIN Employees sub
        ON sub.Id       = empManager.EmployeeId
       AND sub.TenantId = @TenantId
       AND sub.IsDeleted = 0

LEFT JOIN EmployeeOrgAssignments eoa
       ON eoa.EmployeeId = sub.Id AND eoa.TenantId = @TenantId AND eoa.IsDeleted = 0
LEFT JOIN JobTitles         jobTitle    ON jobTitle.Id    = eoa.JobTitleId         AND jobTitle.TenantId    = @TenantId AND jobTitle.IsDeleted    = 0
LEFT JOIN EmployeeSubGroups subGroup    ON subGroup.Id    = eoa.EmployeeSubGroupId AND subGroup.TenantId    = @TenantId AND subGroup.IsDeleted    = 0
LEFT JOIN EmployeeGroups    grp         ON grp.Id         = eoa.EmployeeGroupId    AND grp.TenantId         = @TenantId AND grp.IsDeleted         = 0
LEFT JOIN AbpOrganizationUnits orgUnit  ON orgUnit.Id     = eoa.OrganizationUnitId AND orgUnit.TenantId     = @TenantId AND orgUnit.IsDeleted     = 0

-- The legacy fixed ParentOrgUnit climb, replaced by a type-based ancestor walk
-- (Bank/BankSql.cs pattern; the org-unit type lives on OrgUnitProfiles.OrganizationUnitTypeId).
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
) department
LEFT JOIN PayrollAreas      payrollArea ON payrollArea.Id = eoa.PayrollAreaId      AND payrollArea.TenantId = @TenantId AND payrollArea.IsDeleted = 0

OUTER APPLY (
    SELECT TOP 1 v.PassportExpiryDate
    FROM EmployeeVisaInfos v
    WHERE v.EmployeeId = sub.Id AND v.TenantId = @TenantId AND v.IsDeleted = 0
    ORDER BY v.StartDate DESC
) visa
OUTER APPLY (
    SELECT TOP 1 g.GratuityPaidDate
    FROM EmployeeGratuities g
    WHERE g.EmployeeId = sub.Id AND g.TenantId = @TenantId AND g.IsDeleted = 0
    ORDER BY g.GratuityPaidDate DESC
) gratuity
OUTER APPLY (
    SELECT TOP 1 b.IBAN, b.BankId
    FROM EmployeeBanks b
    WHERE b.EmployeeId = sub.Id AND b.TenantId = @TenantId AND b.IsDeleted = 0
    ORDER BY b.StartDate DESC
) empBank
LEFT JOIN Banks bank ON bank.Id = empBank.BankId AND bank.TenantId = @TenantId AND bank.IsDeleted = 0

INNER JOIN SalaryRegisters sr
        ON sr.EmployeeId = sub.Id AND sr.TenantId = @TenantId AND sr.IsDeleted = 0
INNER JOIN SalaryRegisterDetails srdNet
        ON srdNet.SalaryRegisterId = sr.Id AND srdNet.TenantId = @TenantId AND srdNet.IsDeleted = 0
       AND srdNet.Amount <> 0
-- Legacy: wagetype.Code = 9000 (unquoted). Code is nvarchar in HCMv3, so the literal MUST stay
-- quoted or SQL Server converts the whole column to int and throws on the first non-numeric code.
INNER JOIN WageTypes wtNet
        ON wtNet.Id = srdNet.WageTypeId AND wtNet.Code = '9000'
       AND wtNet.TenantId = @TenantId AND wtNet.IsDeleted = 0

-- Currency of the MANAGER's most recent BASIC PAY line. Legacy keyed this derived table on
-- emp.OID (the manager), not the subordinate -- preserved, see 'Defects carried over' in the notes.
-- WAGE-TYPE TRAP: legacy 'B.WageType = 1' is a PK meaning Code '1000' (Basic Salary), NOT Code '1'.
OUTER APPLY (
    SELECT TOP 1 cur.CurrencyShortFormEn
    FROM EmployeeBasicPays ebp
    JOIN EmployeeBasicPayDetails ebpd ON ebpd.EmployeeBasicPayId = ebp.Id AND ebpd.TenantId = @TenantId AND ebpd.IsDeleted = 0
    JOIN WageTypes wtBasic ON wtBasic.Id = ebpd.WageTypeId AND wtBasic.Code = '1000' AND wtBasic.TenantId = @TenantId AND wtBasic.IsDeleted = 0
    LEFT JOIN Currencies cur ON cur.Id = ebpd.CurrencyId AND cur.TenantId = @TenantId AND cur.IsDeleted = 0
    WHERE ebp.EmployeeId = mgr.Id AND ebp.TenantId = @TenantId AND ebp.IsDeleted = 0
    ORDER BY ebp.StartDate DESC
) basicPayCurrency

WHERE empManager.TenantId = @TenantId
  AND empManager.IsDeleted = 0

UNION ALL

-- The synthetic 'All subordinates' row that feeds the parameter combo. Excluded from the printed
-- output by the report FilterString ([PayrollAreaCode] <> -1).
SELECT -1, '-1', 'All', -1, '-1', 'All',
       NULL, NULL, NULL, NULL,
       CAST(NULL AS DATETIME2), CAST(NULL AS DATETIME2), CAST(NULL AS DATETIME2),
       NULL, NULL,
       CAST(NULL AS DATETIME2), CAST(NULL AS DATETIME2),
       CAST(NULL AS FLOAT), NULL, NULL, -1, NULL;";

        /// <summary>
        /// rptSubordPayslipAr :: Query (MASTER). Arabic sibling of <see cref="SubordPayslipEnQuery"/>.
        ///
        /// LEGACY DIVERGENCES vs the English query, preserved:
        ///   * no Currency column at all (the Arabic layout has no currency label);
        ///   * the net-pay wage-type name is projected as NameAr, not NameEn.
        ///
        /// ORG UNIT: AbpOrganizationUnits carries only DisplayName, so the Arabic report prints the
        /// same org-unit string as the English one. *** ARABIC TEXT LOST -- schema gap, not a layout
        /// gap. *** Identical to the conclusion rptPayslipAr reached.
        /// </summary>
        public const string SubordPayslipArQuery = @"-- rptSubordPayslipAr :: Query (MASTER)
SELECT
      mgr.Id                                AS OID
    , mgr.Code                              AS [manager Code]
    , mgr.FullNameAr                        AS EmployeeName
    , sub.Id                                AS SubordinateOid
    , sub.Code                              AS SubordinateCode
    , sub.FullNameAr                        AS subordinate
    , ISNULL(department.DisplayName, orgUnit.DisplayName) AS OrgUnit   -- ARABIC TEXT LOST: DisplayName only
                                                                       -- see ORG UNIT note above
    , subGroup.NameAr                       AS Grade
    , grp.NameAr                            AS SalaryProfile
    , jobTitle.NameAr                       AS Jobtitle
    , mgr.JoiningDate                       AS JoiningDate
    , visa.PassportExpiryDate               AS PassportExpiryDate
    , gratuity.GratuityPaidDate             AS GradPaidTill
    , bank.Code                             AS BankName
    , empBank.IBAN                          AS BankIBAN
    , sr.ForPeriod
    , sr.InPeriod
    , srdNet.Amount                         AS NetPay
    , wtNet.NameAr                          AS NameAr
    , payrollArea.Name                      AS PayrollArea
    , payrollArea.Id                        AS PayrollAreaCode

FROM EmployeeManagers empManager

INNER JOIN Employees mgr
        ON mgr.Id       = empManager.ManagerId
       AND mgr.TenantId = @TenantId
       AND mgr.IsDeleted = 0
INNER JOIN Employees sub
        ON sub.Id       = empManager.EmployeeId
       AND sub.TenantId = @TenantId
       AND sub.IsDeleted = 0

LEFT JOIN EmployeeOrgAssignments eoa
       ON eoa.EmployeeId = sub.Id AND eoa.TenantId = @TenantId AND eoa.IsDeleted = 0
LEFT JOIN JobTitles         jobTitle    ON jobTitle.Id    = eoa.JobTitleId         AND jobTitle.TenantId    = @TenantId AND jobTitle.IsDeleted    = 0
LEFT JOIN EmployeeSubGroups subGroup    ON subGroup.Id    = eoa.EmployeeSubGroupId AND subGroup.TenantId    = @TenantId AND subGroup.IsDeleted    = 0
LEFT JOIN EmployeeGroups    grp         ON grp.Id         = eoa.EmployeeGroupId    AND grp.TenantId         = @TenantId AND grp.IsDeleted         = 0
LEFT JOIN AbpOrganizationUnits orgUnit  ON orgUnit.Id     = eoa.OrganizationUnitId AND orgUnit.TenantId     = @TenantId AND orgUnit.IsDeleted     = 0

-- The legacy fixed ParentOrgUnit climb, replaced by a type-based ancestor walk
-- (Bank/BankSql.cs pattern; the org-unit type lives on OrgUnitProfiles.OrganizationUnitTypeId).
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
) department
LEFT JOIN PayrollAreas      payrollArea ON payrollArea.Id = eoa.PayrollAreaId      AND payrollArea.TenantId = @TenantId AND payrollArea.IsDeleted = 0

OUTER APPLY (
    SELECT TOP 1 v.PassportExpiryDate
    FROM EmployeeVisaInfos v
    WHERE v.EmployeeId = sub.Id AND v.TenantId = @TenantId AND v.IsDeleted = 0
    ORDER BY v.StartDate DESC
) visa
OUTER APPLY (
    SELECT TOP 1 g.GratuityPaidDate
    FROM EmployeeGratuities g
    WHERE g.EmployeeId = sub.Id AND g.TenantId = @TenantId AND g.IsDeleted = 0
    ORDER BY g.GratuityPaidDate DESC
) gratuity
OUTER APPLY (
    SELECT TOP 1 b.IBAN, b.BankId
    FROM EmployeeBanks b
    WHERE b.EmployeeId = sub.Id AND b.TenantId = @TenantId AND b.IsDeleted = 0
    ORDER BY b.StartDate DESC
) empBank
LEFT JOIN Banks bank ON bank.Id = empBank.BankId AND bank.TenantId = @TenantId AND bank.IsDeleted = 0

INNER JOIN SalaryRegisters sr
        ON sr.EmployeeId = sub.Id AND sr.TenantId = @TenantId AND sr.IsDeleted = 0
INNER JOIN SalaryRegisterDetails srdNet
        ON srdNet.SalaryRegisterId = sr.Id AND srdNet.TenantId = @TenantId AND srdNet.IsDeleted = 0
       AND srdNet.Amount <> 0
INNER JOIN WageTypes wtNet
        ON wtNet.Id = srdNet.WageTypeId AND wtNet.Code = '9000'
       AND wtNet.TenantId = @TenantId AND wtNet.IsDeleted = 0

WHERE empManager.TenantId = @TenantId
  AND empManager.IsDeleted = 0

UNION ALL

-- DELIBERATE FIX, not a port slip: the legacy Arabic union row used the INTEGER literal -1 for
-- [manager Code] and SubordinateCode, whose first-branch counterparts are nvarchar. UNION type
-- resolution promotes the whole column to int, so the legacy query throws
-- ""Conversion failed when converting the nvarchar value ... to data type int"" for any tenant with a
-- non-numeric employee code. The English sibling got this right ('-1'); the Arabic one did not.
SELECT -1, N'-1', N'الكل', -1, N'-1', N'الكل',
       NULL, NULL, NULL, NULL,
       CAST(NULL AS DATETIME2), CAST(NULL AS DATETIME2), CAST(NULL AS DATETIME2),
       NULL, NULL,
       CAST(NULL AS DATETIME2), CAST(NULL AS DATETIME2),
       CAST(NULL AS FLOAT), NULL, NULL, -1;";

        // -------------------------------------------------------------------------------------------
        // FilterStrings. Not SQL, but they lived in the legacy .resx and the .resx is not ported
        // (a resx entry that carries a Bitmap breaks the multi-targeted build, so this project does
        // not use resx at all). They are DevExpress criteria strings, kept verbatim.
        // -------------------------------------------------------------------------------------------

        /// <summary>rptSubordPayslipEn: the one-subordinate filter (legacy "$this.FilterString").</summary>
        public const string SubordPayslipEnFilter =
            "[CalForPeriodMonth] = ?PeriodMonth And [CalcInPeriodMonth] = ?PeriodMonth " +
            "And [CalForPeriodYear] = ?PeriodYear And [calcInPeriodYear] = ?PeriodYear " +
            "And [SubordinateOid] = ?subordinates And [PayrollAreaCode] <> -1";

        /// <summary>rptSubordPayslipEn: the "All subordinates" filter, applied when ?subordinates = -1.</summary>
        public const string SubordPayslipEnAllFilter =
            "[CalForPeriodMonth] = ?PeriodMonth And [CalcInPeriodMonth] = ?PeriodMonth " +
            "And [CalForPeriodYear] = ?PeriodYear And [calcInPeriodYear] = ?PeriodYear " +
            "And [PayrollAreaCode] <> -1 And [manager Code] = ?EmployeeCode";

        /// <summary>rptSubordPayslipAr: the one-subordinate filter (legacy "$this.FilterString").</summary>
        public const string SubordPayslipArFilter =
            "[SubordinateOid] = ?Subordinates And [CalForPeriodMonth] = ?PeriodMonth " +
            "And [CalcInPeriodMonth] = ?PeriodMonth And [CalForPeriodYear] = ?PeriodYear " +
            "And [calcInPeriodYear] = ?PeriodYear And [PayrollAreaCode] <> -1";

        /// <summary>rptSubordPayslipAr: the "All subordinates" filter, applied when ?Subordinates = -1.</summary>
        public const string SubordPayslipArAllFilter =
            "[CalForPeriodMonth] = ?PeriodMonth And [CalcInPeriodMonth] = ?PeriodMonth " +
            "And [CalForPeriodYear] = ?PeriodYear And [calcInPeriodYear] = ?PeriodYear " +
            "And [PayrollAreaCode] <> -1 And [manager Code] = ?EmployeeCode";

        // ===========================================================================================
        // rptSocialSecurity -- the PIFSS statutory return
        // ===========================================================================================
        //
        // *** WAGE-TYPE MAPPING -- VERIFIED, NOT ASSUMED ***
        //   This report is the one place in the payslip family where the legacy wage-type literals
        //   are genuine CODES rather than raw primary keys, so they must NOT be rewritten the way
        //   PayslipSql rewrote "WageType = 1" into Code = '1000'.
        //     legacy "WageType.Code IN ('1')" -> HCMv3 WageTypes.Code = '1' = Employee PIFSS Ded
        //     legacy "WageType.Code IN ('3')" -> HCMv3 WageTypes.Code = '3' = Employer PIFSS Ded
        //   Confirmed against HCMv3.Core/PayrollManagement/Services/EmployeePifssDeductionWageTypeCalculator.cs
        //   ("Wage code 1 -- Employee PIFSS Ded") and EmployerPifssDeductionWageTypeCalculator.cs
        //   ("Wage code 3 -- Employer PIFSS Ded"). Inverting these with the '1000' Basic Salary
        //   rewrite would put gross basic pay into a statutory contribution return.
        //   Code is nvarchar in HCMv3, so both literals MUST stay quoted.
        //
        // ARREARS
        //   Legacy also swept in arrear lines via
        //   "WageType.ArrearSourceWageType IN (SELECT OID FROM WageType WHERE Code IN ('1'))".
        //   HCMv3 models this as WageTypes.ArrearSourceWageTypeId, so the sub-select becomes a plain
        //   self-join on the wage-type table. Preserved exactly -- dropping it would understate the
        //   month's contributions whenever a retro run posted arrears.
        //
        // COMPANY / DEPARTMENT -- SEMANTIC DEVIATION (the one BankSql already documents)
        //   Legacy EmployeeOrgAssignment carried seven flat FK columns OrgLevel1..OrgLevel7 and took
        //   whichever pointed at an OrganizationUnit whose OrganizationUnitType was 'Company'
        //   (resp. 'Department') -- fourteen aliased joins. HCMv3 has ONE
        //   EmployeeOrgAssignments.OrganizationUnitId into AbpOrganizationUnits, an ABP materialised
        //   path tree, and the unit TYPE lives on the HCMv3-side OrgUnitProfiles.OrganizationUnitTypeId.
        //   The fourteen joins collapse to two ancestor walks: of the employee's unit and all its
        //   ancestors (ancestor <=> the employee unit's Code starts with the ancestor's Code), take
        //   the shallowest one whose profile type is named 'Company' / 'Department'. This is the
        //   pattern BankSql.cs already established and is strictly better than the flat-column
        //   version. NOTE PayslipSql concluded unit type was "not expressible" and fell back to the
        //   employee's own unit; it IS expressible, and that port is worth revisiting.

        /// <summary>rptSocialSecurity :: PayrollSheetQuery (MASTER -- one row per employee/period).</summary>
        public const string SocialSecurityQuery = @"-- rptSocialSecurity :: PayrollSheetQuery (MASTER)
-- PORT DEVIATION vs the legacy SQL: the legacy text opens with six commented-out DECLARE lines.
-- DevExpress's CustomSqlQuery validator rejects anything that is not purely SELECT, so a leading
-- DECLARE block (even commented) is not worth the risk and is dropped. Semantics are identical.
SELECT
      A.Code
    , A.PaymentType
    , A.PayrollRunType
    , A.JoiningDate
    , A.ForPeriod
    , A.FullNameEn
    , A.FullNameAr
    , A.JobTitleEn
    , A.JobTitleAr
    , A.PayrollArea
    , A.CompanyEn
    , A.DepartmentEn

    , A.EmployeePIFSSDed
    , A.EmployeeClaimAmount
    , (A.EmployeePIFSSDed - A.EmployeeClaimAmount)          AS DiffEmployeeContribution

    , A.EmployerPIFSSDed
    , A.EmployerClaimAmount
    , (A.EmployerPIFSSDed - A.EmployerClaimAmount)          AS DiffEmployerContribution

    , (A.EmployeePIFSSDed    + A.EmployerPIFSSDed)          AS TotalPIFSSDed
    , (A.EmployeeClaimAmount + A.EmployerClaimAmount)       AS TotalClaimAmount

    , (A.EmployeePIFSSDed    + A.EmployerPIFSSDed)
    - (A.EmployeeClaimAmount + A.EmployerClaimAmount)       AS DiffInTotalDed

FROM (
    SELECT
          emp.Code
        , sr.ForPeriod
        , sr.PayrollRunType
        , emp.JoiningDate
        , CASE WHEN empBank.PaymentType = 0 THEN 'BANK' ELSE 'CASH' END AS PaymentType
        , emp.FullNameEn
        , emp.FullNameAr
        , jobTitle.NameEn                                   AS JobTitleEn
        , jobTitle.NameAr                                   AS JobTitleAr
        , payrollArea.Name                                  AS PayrollArea
        , company.DisplayName                               AS CompanyEn
        , department.DisplayName                            AS DepartmentEn

        , ISNULL(employeeDed.Amount, 0)                     AS EmployeePIFSSDed
        , ISNULL(claim.EmployeeContributionAmount, 0)       AS EmployeeClaimAmount
        , ISNULL(employerDed.Amount, 0)                     AS EmployerPIFSSDed
        , ISNULL(claim.EmployerContributionAmount, 0)       AS EmployerClaimAmount

    FROM SalaryRegisters sr

    INNER JOIN Employees emp
            ON emp.Id        = sr.EmployeeId
           AND emp.TenantId  = @TenantId
           AND emp.IsDeleted = 0

    LEFT JOIN EmployeeOrgAssignments eoa
           ON eoa.EmployeeId = emp.Id
          AND eoa.TenantId   = @TenantId
          AND eoa.IsDeleted  = 0
          AND eoa.StartDate <= sr.ForPeriod
          AND eoa.EndDate   >= sr.ForPeriod

    LEFT JOIN JobTitles    jobTitle    ON jobTitle.Id    = eoa.JobTitleId    AND jobTitle.TenantId    = @TenantId AND jobTitle.IsDeleted    = 0
    LEFT JOIN PayrollAreas payrollArea ON payrollArea.Id = eoa.PayrollAreaId AND payrollArea.TenantId = @TenantId AND payrollArea.IsDeleted = 0

    LEFT JOIN EmployeeBanks empBank
           ON empBank.EmployeeId = emp.Id
          AND empBank.TenantId   = @TenantId
          AND empBank.IsDeleted  = 0
          AND empBank.StartDate <= sr.ForPeriod
          AND empBank.EndDate   >= sr.ForPeriod

    -- DROPPED: 'LEFT JOIN EmployeeBasicPay ... BETWEEN StartDate AND EndDate'. Nothing is projected
    -- from it and, being 1:N, it could only fan the row out and double every contribution figure.
    -- Legacy bug, not ported -- the same call BankSql and PayslipSql made.

    -- The fourteen OrgLevel1..7 'Company'/'Department' joins, collapsed to two ancestor walks.
    LEFT JOIN AbpOrganizationUnits ou
           ON ou.Id        = eoa.OrganizationUnitId
          AND ou.TenantId  = @TenantId
          AND ou.IsDeleted = 0

    OUTER APPLY (
        SELECT TOP 1 anc.DisplayName
        FROM AbpOrganizationUnits anc
        INNER JOIN OrgUnitProfiles ancp       ON ancp.OrganizationUnitId = anc.Id      AND ancp.TenantId = @TenantId AND ancp.IsDeleted = 0
        INNER JOIN OrganizationUnitTypes anct ON anct.Id = ancp.OrganizationUnitTypeId AND anct.TenantId = @TenantId AND anct.IsDeleted = 0
        WHERE anc.TenantId  = @TenantId
          AND anc.IsDeleted = 0
          AND ou.Code LIKE anc.Code + '%'
          AND anct.NameEn = 'Company'
        ORDER BY LEN(anc.Code)
    ) company

    OUTER APPLY (
        SELECT TOP 1 anc.DisplayName
        FROM AbpOrganizationUnits anc
        INNER JOIN OrgUnitProfiles ancp       ON ancp.OrganizationUnitId = anc.Id      AND ancp.TenantId = @TenantId AND ancp.IsDeleted = 0
        INNER JOIN OrganizationUnitTypes anct ON anct.Id = ancp.OrganizationUnitTypeId AND anct.TenantId = @TenantId AND anct.IsDeleted = 0
        WHERE anc.TenantId  = @TenantId
          AND anc.IsDeleted = 0
          AND ou.Code LIKE anc.Code + '%'
          AND anct.NameEn = 'Department'
        ORDER BY LEN(anc.Code)
    ) department

    ---------------------------- Employee PIFSS Ded (wage CODE '1') ----------------------------
    LEFT JOIN (
        SELECT srd.SalaryRegisterId
             , SUM(srd.Rate)   AS Rate
             , SUM(srd.Number) AS Number
             , SUM(srd.Amount) AS Amount
        FROM SalaryRegisterDetails srd
        INNER JOIN WageTypes wt
                ON wt.Id        = srd.WageTypeId
               AND wt.TenantId  = @TenantId
               AND wt.IsDeleted = 0
        LEFT JOIN WageTypes wtSrc
               ON wtSrc.Id        = wt.ArrearSourceWageTypeId
              AND wtSrc.TenantId  = @TenantId
              AND wtSrc.IsDeleted = 0
        WHERE srd.TenantId  = @TenantId
          AND srd.IsDeleted = 0
          AND (wt.Code = '1' OR wtSrc.Code = '1')     -- Employee PIFSS Ded; see wage-type note above
        GROUP BY srd.SalaryRegisterId
    ) employeeDed ON employeeDed.SalaryRegisterId = sr.Id

    ---------------------------- Employer PIFSS Ded (wage CODE '3') ----------------------------
    LEFT JOIN (
        SELECT srd.SalaryRegisterId
             , SUM(srd.Rate)   AS Rate
             , SUM(srd.Number) AS Number
             , SUM(srd.Amount) AS Amount
        FROM SalaryRegisterDetails srd
        INNER JOIN WageTypes wt
                ON wt.Id        = srd.WageTypeId
               AND wt.TenantId  = @TenantId
               AND wt.IsDeleted = 0
        LEFT JOIN WageTypes wtSrc
               ON wtSrc.Id        = wt.ArrearSourceWageTypeId
              AND wtSrc.TenantId  = @TenantId
              AND wtSrc.IsDeleted = 0
        WHERE srd.TenantId  = @TenantId
          AND srd.IsDeleted = 0
          AND (wt.Code = '3' OR wtSrc.Code = '3')     -- Employer PIFSS Ded; see wage-type note above
        GROUP BY srd.SalaryRegisterId
    ) employerDed ON employerDed.SalaryRegisterId = sr.Id

    ---------------------------- PIFSS claims raised in the period -----------------------------
    -- Legacy re-derived the salary-register period predicates inside both deduction sub-queries;
    -- the join is on SalaryRegisterId, which already carries them from the outer WHERE, so they are
    -- not repeated. Same rows, one pass.
    LEFT JOIN (
        SELECT c.EmployeeId
             , SUM(c.EmployeeContributionAmount) AS EmployeeContributionAmount
             , SUM(c.EmployerContributionAmount) AS EmployerContributionAmount
        FROM EmployeeSocialSecurityClaims c
        WHERE c.TenantId  = @TenantId
          AND c.IsDeleted = 0
          AND DATEPART(Month, c.ClaimDate) = @PrmMonth
          AND DATEPART(Year,  c.ClaimDate) = @PrmYear
        GROUP BY c.EmployeeId
    ) claim ON claim.EmployeeId = sr.EmployeeId

    WHERE sr.TenantId  = @TenantId
      AND sr.IsDeleted = 0
      AND sr.InPeriod  = sr.ForPeriod
      AND DATEPART(Month, sr.ForPeriod) = @PrmMonth
      AND DATEPART(Year,  sr.ForPeriod) = @PrmYear
      AND ((@PrmPayrollRunType IS NOT NULL AND sr.PayrollRunType = @PrmPayrollRunType) OR @PrmPayrollRunType IS NULL)
      -- Legacy: 'LEFT JOIN EmployeeSocialSecurity ... WHERE EmployeeSocialSecurity.OID IS NOT NULL'.
      -- DELIBERATE CHANGE to EXISTS: as a LEFT JOIN, two overlapping social-security records for one
      -- employee duplicate the salary-register row and DOUBLE that employee's reported contribution.
      -- In a statutory PIFSS return that is not a defect worth reproducing faithfully. The row set is
      -- otherwise identical. See 'Defects carried over' in PORTING-NOTES-PayslipVariants.md.
      AND EXISTS (
            SELECT 1
            FROM EmployeeSocialSecurities ess
            WHERE ess.EmployeeId = emp.Id
              AND ess.TenantId   = @TenantId
              AND ess.IsDeleted  = 0
              AND ess.StartDate <= sr.ForPeriod
              AND ess.EndDate   >= sr.ForPeriod
          )
) A
WHERE ((ISNULL(@PrmPayrollArea, '') <> '' AND A.PayrollArea  = @PrmPayrollArea) OR ISNULL(@PrmPayrollArea, '') = '')
  AND ((ISNULL(@PrmCompany,     '') <> '' AND A.CompanyEn    = @PrmCompany)     OR ISNULL(@PrmCompany,     '') = '')
  AND ((ISNULL(@PrmDepartment,  '') <> '' AND A.DepartmentEn = @PrmDepartment)  OR ISNULL(@PrmDepartment,  '') = '')
ORDER BY
    -- Legacy sort key preserved verbatim: numeric codes right-aligned zero-padded to 20,
    -- non-numeric left-aligned. (The legacy Replicate('',21) is a no-op; kept for byte parity.)
    CASE WHEN ISNUMERIC(A.Code) = 1 THEN RIGHT(REPLICATE('0', 21) + A.Code, 20)
         WHEN ISNUMERIC(A.Code) = 0 THEN LEFT(A.Code + REPLICATE('', 21), 20)
         ELSE A.Code
    END;";

        /// <summary>rptSocialSecurity :: dsCompany (the PrmCompany combo).</summary>
        public const string SocialSecurityCompanyList = @"-- rptSocialSecurity :: dsCompany
-- Legacy: OrganizationUnit LEFT JOIN OrganizationUnitType WHERE OrganizationUnitType.NameEn='Company'.
-- HCMv3: the unit type moved off the ABP table onto OrgUnitProfiles.
SELECT ou.DisplayName AS CompanyEn
FROM AbpOrganizationUnits ou
INNER JOIN OrgUnitProfiles p       ON p.OrganizationUnitId    = ou.Id AND p.TenantId = @TenantId AND p.IsDeleted = 0
INNER JOIN OrganizationUnitTypes t ON t.Id = p.OrganizationUnitTypeId AND t.TenantId = @TenantId AND t.IsDeleted = 0
WHERE ou.TenantId  = @TenantId
  AND ou.IsDeleted = 0
  AND t.NameEn = 'Company'
ORDER BY ou.DisplayName;";

        /// <summary>rptSocialSecurity :: dsDepartment (the PrmDepartment combo).</summary>
        public const string SocialSecurityDepartmentList = @"-- rptSocialSecurity :: dsDepartment
SELECT ou.DisplayName AS DepartmentEn
FROM AbpOrganizationUnits ou
INNER JOIN OrgUnitProfiles p       ON p.OrganizationUnitId    = ou.Id AND p.TenantId = @TenantId AND p.IsDeleted = 0
INNER JOIN OrganizationUnitTypes t ON t.Id = p.OrganizationUnitTypeId AND t.TenantId = @TenantId AND t.IsDeleted = 0
WHERE ou.TenantId  = @TenantId
  AND ou.IsDeleted = 0
  AND t.NameEn = 'Department'
ORDER BY ou.DisplayName;";

        /// <summary>rptSocialSecurity / rptPayslip_wsp_En :: dsPayrollArea (the PrmPayrollArea combo).</summary>
        public const string PayrollAreaList = @"-- payslip variants :: dsPayrollArea
-- Legacy: ""Select Name From PayrollArea ORDER By Name"" -- MECHANICAL rewrite plus the two ABP
-- filters the legacy corpus has no concept of.
SELECT pa.Name
FROM PayrollAreas pa
WHERE pa.TenantId  = @TenantId
  AND pa.IsDeleted = 0
ORDER BY pa.Name;";

        // ===========================================================================================
        // rptPayslip_wosp_En -- "payslip WITHOUT salary particulars"
        // ===========================================================================================
        //
        // WHAT THIS REPORT IS
        //   The same employee header block as rptPayslipEn, then ONLY the earnings and deductions
        //   columns -- no basic-pay breakdown, no overtime, no leave, no indemnity, no working-day or
        //   attendance summaries. Two subreports instead of nine, and both are the ALREADY PORTED
        //   srptPaySlipEarningEn / srptPaySlipDeductionEn, instantiated (see rptPayslip_wosp_En.cs).
        //
        // THE DATA SOURCE HAD TO BE REPLACED WHOLESALE
        //   Unlike every other report in this batch, the legacy layout had NO SQL at all. It bound to
        //   a XAF <c>DevExpress.Persistent.Base.ReportsV2.CollectionDataSource</c> over the XPO class
        //   HCM10.Module.BusinessObjects.PayrollManagement.SalaryRegister, and every control walked an
        //   XPO association path ("Employee.Bank.Bank.NameEn",
        //   "Employee.OrgAssignment.OrganizationUnit.ParentOrgUnit.ParentOrgUnit.ParentOrgUnit.NameEn",
        //   ...). CollectionDataSource lives in DevExpress.Persistent.Base, an XAF assembly that is
        //   not, and must not become, a dependency of HCMv3.Reports. The paths are therefore flattened
        //   into the single query below and each XRBinding is re-pointed at a query-qualified column
        //   ("PayslipQuery.EmpCode" etc). This is the ONE report in the batch whose bindings had to be
        //   rewritten; every other one already carried a SqlDataSource.
        //
        // ORG UNIT -- SEMANTIC DEVIATION
        //   The legacy path walked THREE ParentOrgUnit hops from the employee's unit and printed the
        //   great-grandparent's name. HCMv3's AbpOrganizationUnits is an ABP materialised-path tree
        //   and three blind hops upward is not a meaningful concept -- it prints blank for anyone
        //   attached nearer the root. The employee's OWN unit is used instead, matching PayslipSql
        //   (rewrite note 1). *** Needs the same product decision as the base payslip. ***
        //
        // *** COLUMN LOST: CalendarDays ***
        //   Legacy bound "Employee.Attendance.CalendarDays". HCMv3's EmployeeAttendances has no
        //   CalendarDays column at all (it carries AttendanceHours / TotalAttendDays / AttendanceUnit).
        //   Rather than silently substitute a different measure into a payslip, the column is returned
        //   as NULL and the cell prints blank. Restore it when the attendance model grows the column.
        //
        // GOVERNMENT JOB TITLE -- SIMPLIFIED, DELIBERATELY
        //   Legacy path: Employee.Sponsor -> Sponsor -> GovtJobTitle -> GovtJobTitle -> NameEn, i.e.
        //   the employee's SPONSOR's job-title row, not the employee's. HCMv3 puts
        //   GovtJobTitleId directly on EmployeeSponsors, so the two indirection hops collapse to one
        //   join and the value is now the employee's own government job title. That is what the label
        //   ("Govt. Job Title") always claimed to show.

        /// <summary>rptPayslip_wosp_En :: PayslipQuery (MASTER -- one row per employee/period).</summary>
        public const string PayslipWospQuery = @"-- rptPayslip_wosp_En :: PayslipQuery (MASTER)
-- Replaces the XAF CollectionDataSource over the XPO SalaryRegister class; see the notes above.
SELECT
      emp.Code                              AS EmpCode
    , emp.FullNameEn                        AS EmpName
    , emp.JoiningDate                       AS JoiningDate
    , ISNULL(department.DisplayName, orgUnit.DisplayName) AS OrgUnit
    , subGroup.NameEn                       AS Grade
    , grp.NameEn                            AS SalaryProfile
    , govtJobTitle.NameEn                   AS GovtJobTitle
    , bank.NameEn                           AS BankName
    , empBank.AccountNum                    AS AccountNum
    , sr.ForPeriod
    , sr.InPeriod
    , sr.PayrollRunType
    , srdNet.Amount                         AS NetPay
    , ISNULL(absence.OffDays, 0)            AS OffDays
    , ISNULL(absence.SickLeaveDays, 0)      AS SickLeaveDays
    , CAST(NULL AS INT)                     AS CalendarDays     -- COLUMN LOST: see note above

FROM Employees emp

INNER JOIN SalaryRegisters sr
        ON sr.EmployeeId = emp.Id
       AND sr.TenantId   = @TenantId
       AND sr.IsDeleted  = 0

-- The net-pay carrier row. Code is nvarchar in HCMv3 so the literal MUST stay quoted.
INNER JOIN SalaryRegisterDetails srdNet
        ON srdNet.SalaryRegisterId = sr.Id
       AND srdNet.TenantId         = @TenantId
       AND srdNet.IsDeleted        = 0
INNER JOIN WageTypes wtNet
        ON wtNet.Id        = srdNet.WageTypeId
       AND wtNet.Code      = '9000'
       AND wtNet.TenantId  = @TenantId
       AND wtNet.IsDeleted = 0

LEFT JOIN EmployeeOrgAssignments eoa
       ON eoa.EmployeeId = emp.Id
      AND eoa.TenantId   = @TenantId
      AND eoa.IsDeleted  = 0
      AND EOMONTH(DATEFROMPARTS(@PrmYear, @PrmMonth, 1)) BETWEEN eoa.StartDate AND eoa.EndDate
LEFT JOIN EmployeeGroups    grp      ON grp.Id      = eoa.EmployeeGroupId    AND grp.TenantId      = @TenantId AND grp.IsDeleted      = 0
LEFT JOIN EmployeeSubGroups subGroup ON subGroup.Id = eoa.EmployeeSubGroupId AND subGroup.TenantId = @TenantId AND subGroup.IsDeleted = 0
LEFT JOIN AbpOrganizationUnits orgUnit
       ON orgUnit.Id        = eoa.OrganizationUnitId
      AND orgUnit.TenantId  = @TenantId
      AND orgUnit.IsDeleted = 0

-- The legacy fixed ParentOrgUnit climb, replaced by a type-based ancestor walk
-- (Bank/BankSql.cs pattern; the org-unit type lives on OrgUnitProfiles.OrganizationUnitTypeId).
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
) department

LEFT JOIN EmployeeSponsors empSponsor
       ON empSponsor.EmployeeId = emp.Id
      AND empSponsor.TenantId   = @TenantId
      AND empSponsor.IsDeleted  = 0
      AND EOMONTH(DATEFROMPARTS(@PrmYear, @PrmMonth, 1)) BETWEEN empSponsor.StartDate AND empSponsor.EndDate
LEFT JOIN GovtJobTitles govtJobTitle
       ON govtJobTitle.Id        = empSponsor.GovtJobTitleId
      AND govtJobTitle.TenantId  = @TenantId
      AND govtJobTitle.IsDeleted = 0

LEFT JOIN EmployeeBanks empBank
       ON empBank.EmployeeId = emp.Id
      AND empBank.TenantId   = @TenantId
      AND empBank.IsDeleted  = 0
      AND EOMONTH(DATEFROMPARTS(@PrmYear, @PrmMonth, 1)) BETWEEN empBank.StartDate AND empBank.EndDate
LEFT JOIN Banks bank ON bank.Id = empBank.BankId AND bank.TenantId = @TenantId AND bank.IsDeleted = 0

-- Replaces the two XPO CalculatedFields OFFCurrentMonthCal and SLCal, which aggregated over the
-- Employee.Absence child collection. OFFCurrentMonthCal summed EVERY absence in the period (its name
-- notwithstanding); SLCal summed only those whose AbsenceType is named 'Sick Leave'. Both semantics
-- are preserved exactly, including OFFCurrentMonthCal's failure to exclude sick leave from its own
-- total -- see 'Defects carried over' in PORTING-NOTES-PayslipVariants.md.
OUTER APPLY (
    SELECT SUM(DATEDIFF(DAY, ea.StartDate, ea.EndDate)) AS OffDays
         , SUM(CASE WHEN at.NameEn = 'Sick Leave'
                    THEN DATEDIFF(DAY, ea.StartDate, ea.EndDate) ELSE 0 END) AS SickLeaveDays
    FROM EmployeeAbsences ea
    LEFT JOIN AbsenceTypes at
           ON at.Id        = ea.AbsenceTypeId
          AND at.TenantId  = @TenantId
          AND at.IsDeleted = 0
    WHERE ea.EmployeeId = emp.Id
      AND ea.TenantId   = @TenantId
      AND ea.IsDeleted  = 0
      AND DATEPART(Month, ea.StartDate) = @PrmMonth
      AND DATEPART(Year,  ea.StartDate) = @PrmYear
) absence

WHERE emp.TenantId  = @TenantId
  AND emp.IsDeleted = 0
  AND sr.InPeriod   = sr.ForPeriod
  AND DATEPART(Month, sr.ForPeriod) = @PrmMonth
  AND DATEPART(Year,  sr.ForPeriod) = @PrmYear
ORDER BY
    CASE WHEN ISNUMERIC(emp.Code) = 1 THEN RIGHT(REPLICATE('0', 21) + emp.Code, 20)
         WHEN ISNUMERIC(emp.Code) = 0 THEN LEFT(emp.Code + REPLICATE('', 21), 20)
         ELSE emp.Code
    END;";

        /// <summary>
        /// rptPayslip_wosp_En: the report FilterString, retargeted at the new column names.
        ///
        /// Legacy read
        /// <c>[Employee.Code] = ?EmployeeCode And [CalForPeriodMonth] = ?PeriodMonth And [CalForPeriodYear] = ?PeriodYear</c>.
        /// The period half is now also enforced in SQL (@PrmMonth / @PrmYear), so the client-side
        /// half is redundant but kept: the two CalculatedFields it names are still in the layout, and
        /// removing predicates from a filter is exactly the kind of "improvement" that quietly widens
        /// a payslip's row set.
        /// </summary>
        public const string PayslipWospFilter =
            "[EmpCode] = ?EmployeeCode And [CalForPeriodMonth] = ?PeriodMonth " +
            "And [CalForPeriodYear] = ?PeriodYear";
    }
}
