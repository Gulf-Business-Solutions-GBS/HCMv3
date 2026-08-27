// SQL for the HCMv3.Reports/Bank family, rewritten for the HCMv3 schema.
//
// MULTI-TENANCY: these run through DevExpress SqlDataSource as RAW SQL, so EF Core's per-tenant
// global query filter does NOT apply. Every query filters TenantId explicitly via @TenantId, which
// arrives as the hidden PrmTenantId report parameter (see HcmReportBase.EnsureHcmParameters).
// Removing an "AND TenantId = @TenantId" silently leaks rows across tenants.
//
// ===================================================================================================
// REWRITES SHARED BY EVERY QUERY IN THIS FILE
// ===================================================================================================
//
// MECHANICAL RENAMES
//   Employee -> Employees, SalaryRegister -> SalaryRegisters, SalaryRegisterDetail ->
//   SalaryRegisterDetails, EmployeeBank -> EmployeeBanks, Bank -> Banks, EmployeeOrgAssignment ->
//   EmployeeOrgAssignments, EmployeeOnHold -> EmployeeOnHolds, EmployeeSponsor -> EmployeeSponsors,
//   Sponsor -> Sponsors, SponsorBank -> SponsorBanks, PayeeBankDetail -> PayeeBankDetails,
//   OrganizationBankInfo -> OrganizationBankInfos, WageType -> WageTypes  (pluralisation)
//   OID -> Id                                                            (PK)
//   <EntityName> -> <EntityName>Id                                       (FK columns)
//   OrganizationBankInfo.OrganisationUnit -> OrganizationBankInfos.OrganizationUnitId  (note the
//     legacy column really was spelled "Organisation"; HCMv3 spells it "Organization")
//   OrganizationBankInfo.OrganisationBank -> OrganizationBankInfos.BankId
//   SponsorBank.PaymentBank -> SponsorBanks.PaymentBankId
//   PayeeBankDetail.Bank / .PayeeBank -> PayeeBankDetails.BankId / .PayeeBankId
//   + "AND TenantId = @TenantId" and "AND IsDeleted = 0" on EVERY join (zero occurrences in legacy).
//
// PERIOD-OVERLAP PREDICATE
//   Legacy wrote the same 2-line disjunction on every effective-dated join:
//       (Start <= DATEADD(month,@PrmMonth-1,DATEADD(year,@PrmYear-1900,0))                 -- 1st of month
//        AND End >= DATEADD(day,-1,DATEADD(month,@PrmMonth,DATEADD(year,@PrmYear-1900,0))))-- last of month
//       OR (Start <= <last of month> AND End >= <last of month>)
//   The first disjunct is a strict subset of the second (Start <= 1st implies Start <= last), so the
//   whole thing reduces to "the row in force on the LAST day of the month". Written here as
//       Start <= EOMONTH(DATEFROMPARTS(@PrmYear, @PrmMonth, 1)) AND End >= EOMONTH(...)
//   Provably equivalent, and readable.
//
// WAGE-TYPE LITERALS
//   Legacy "WageType IN (SELECT TOP 1 OID FROM WageType WHERE Code = '9000')" is already code-based
//   (not a raw PK), so it survives as WageTypes.Code = '9000' (Net Pay). Code is nvarchar in HCMv3,
//   so the literal MUST stay quoted -- an unquoted 9000 makes SQL Server implicitly convert the whole
//   Code column to int and throw on the first non-numeric code.
//
// *** SEMANTIC DEVIATION: "Company" / "Division" org levels ***
//   Legacy EmployeeOrgAssignment carried seven flat FK columns OrgLevel1..OrgLevel7 and picked
//   whichever one pointed at an OrganizationUnit whose OrganizationUnitType was 'Company'
//   (resp. 'Division'). HCMv3 has a SINGLE EmployeeOrgAssignments.OrganizationUnitId into
//   AbpOrganizationUnits, an ABP materialised-path tree (Code / ParentId). The unit *type* is not on
//   the ABP table -- it lives on the HCMv3-side OrgUnitProfiles.OrganizationUnitTypeId.
//   So the 7 aliased joins become ONE ancestor walk: of the employee's unit and all its ancestors
//   (ancestor <=> the employee unit's Code starts with the ancestor's Code), take the shallowest one
//   whose profile type is named 'Company'. That is a faithful translation of the legacy intent and
//   strictly better than the flat-column version.
//   NOTE the payslip port (PayslipSql.cs, rewrite note 1) concluded unit type was "not expressible"
//   and fell back to the employee's own unit. It IS expressible via OrgUnitProfiles; that port is
//   worth revisiting.
//
// *** FEATURE LOST: CompanyLogo ***
//   Legacy sourced OrganizationUnit.logo. AbpOrganizationUnits has no Logo column and OrgUnitProfiles
//   does not carry one either. Returned as NULL. Same conclusion the payslip port reached.
//
// *** DROPPED UNUSED JOINS ***
//   Every legacy query in this folder LEFT JOINs JobTitle, PayrollArea and EmployeeBasicPay and then
//   projects nothing from any of them. EmployeeBasicPay in particular is 1:N per employee, so the
//   join can only fan the row out and double the money. Dropped -- same call the payslip port made
//   for its EmployeeVisaInfo / EmployeeGratuity joins. This is a legacy bug, not a feature.
//
// *** KNOWN HCMv3 DATA GAP (not a porting defect) ***
//   EmployeeBanks, OrganizationBankInfos, Sponsors, EmployeeSponsors and SponsorBanks are all EMPTY
//   in the HCMv3 database. Bank name / account / IBAN / sponsor columns therefore render blank, and
//   the four sponsor-keyed reports (rptBankFile, rptBankFileAr, rptBankSupporting, rptCashFile with a
//   sponsor selected) return NO ROWS. That is the data migration, not the SQL.

namespace HCMv3.Reports.Bank
{
    /// <summary>Raw SQL used by the HCMv3.Reports.Bank reports.</summary>
    public static class BankSql
    {
        // -------------------------------------------------------------------------------------------
        // Parameter look-up lists
        // -------------------------------------------------------------------------------------------

        /// <summary>Company combo for rptBankTransfer / rptBankAndCashTransfer.</summary>
        public const string CompanyList = @"-- Legacy: OrganizationUnit LEFT JOIN OrganizationUnitType WHERE OrganizationUnitType.NameEn='Company'.
-- HCMv3: the unit type moved off the ABP table onto OrgUnitProfiles.
SELECT ou.DisplayName AS Company
FROM AbpOrganizationUnits ou
INNER JOIN OrgUnitProfiles p       ON p.OrganizationUnitId    = ou.Id AND p.TenantId  = @TenantId AND p.IsDeleted  = 0
INNER JOIN OrganizationUnitTypes t ON t.Id = p.OrganizationUnitTypeId AND t.TenantId  = @TenantId AND t.IsDeleted  = 0
WHERE ou.TenantId = @TenantId
  AND ou.IsDeleted = 0
  AND t.NameEn = 'Company'
ORDER BY ou.DisplayName;";

        /// <summary>Sponsor combo for rptBankTransferBySponsor.</summary>
        public const string SponsorList = @"SELECT s.NameEn
FROM Sponsors s
WHERE s.TenantId = @TenantId
  AND s.IsDeleted = 0
ORDER BY s.NameEn;";

        /// <summary>Sponsor combo for rptBankAndCashTransferBySponsor (code-prefixed display).</summary>
        public const string SponsorCodeList = @"SELECT s.Code + ' - ' + s.NameEn AS SponsorCode
     , s.NameEn                    AS SponsorEn
FROM Sponsors s
WHERE s.TenantId = @TenantId
  AND s.IsDeleted = 0
ORDER BY s.Code + '-' + s.NameEn;";

        /// <summary>
        /// Sponsor combo for the four sponsor-keyed reports. NOTE the column is deliberately aliased
        /// back to <c>OID</c>: the parameter's DynamicListLookUpSettings.ValueMember in every one of
        /// those layouts is the literal string "OID", and renaming the column would silently break
        /// the look-up rather than fail the build.
        /// </summary>
        public const string SponsorIdList = @"-- Legacy inline designer SQL: ""Select OID, NameEn From Sponsor""
SELECT s.Id AS OID
     , s.NameEn
FROM Sponsors s
WHERE s.TenantId = @TenantId
  AND s.IsDeleted = 0
ORDER BY s.NameEn;";

        /// <summary>Employee-bank combo ("CODE - Name") for the sponsor-keyed reports.</summary>
        public const string BankCodeList = @"-- Legacy inline designer SQL: ""Select Code, Code+'-'+NameEn As NameEn From Bank ORDER BY Code+'-'+NameEn""
SELECT b.Code
     , b.Code + '-' + b.NameEn AS NameEn
FROM Banks b
WHERE b.TenantId = @TenantId
  AND b.IsDeleted = 0
ORDER BY b.Code + '-' + b.NameEn;";

        /// <summary>
        /// Organisation account numbers, used by rptBankFile's unbound "Query" data member.
        /// DATA GAP: OrganizationBankInfos is empty in HCMv3.
        /// </summary>
        public const string OrganizationAccountList = @"-- Legacy inline designer SQL: ""select AccountNo FROM OrganizationBankInfo""
SELECT o.AccountNo
FROM OrganizationBankInfos o
WHERE o.TenantId = @TenantId
  AND o.IsDeleted = 0;";

        /// <summary>Org-unit-type combo (the "break up by" selector) for rptBankAndCashTransferBySponsor.</summary>
        public const string OrgUnitTypeList = @"SELECT t.NameEn AS OrgUnitTypeEn
     , t.Code
FROM OrganizationUnitTypes t
WHERE t.TenantId = @TenantId
  AND t.IsDeleted = 0
ORDER BY t.Code;";

        // -------------------------------------------------------------------------------------------
        // rptBankTransfer -- one row per employee, grouped by company / bank in the layout.
        // -------------------------------------------------------------------------------------------
        public const string BankTransferQuery = @"-- rptBankTransfer :: master query. Bank transfer letter -- FINANCIAL OUTPUT.
-- Totals are summed in the report layout from [NetPay]; do not change the grain (one row per
-- employee / period) or the printed totals move.
SELECT A.Year
     , A.Month
     , A.ForPeriod
     , A.PayrollRunType
     , A.CompanyEn
     , A.Companylogo
     , OrgBank.Code   AS OrgBankCode
     , OrgBank.NameEn AS OrgBankNameEn
     , A.Code
     , A.FullNameEn
     , A.EmpBankCode
     , A.EmpBankNameEn
     , A.EmpBankIBAN
     , A.EmpBankBranch
     , A.PaymentType
     , A.NetPay
FROM (
    SELECT emp.Id                                   AS EmployeeId
         , emp.Code
         , emp.FullNameEn
         , emp.FullNameAr
         , DATEPART(YEAR,  sr.ForPeriod)            AS Year
         , DATEPART(MONTH, sr.ForPeriod)            AS Month
         , sr.ForPeriod
         , sr.PayrollRunType
         , CASE WHEN empBank.PaymentType = 0 OR empBank.PaymentType IS NULL THEN 'BANK' ELSE 'CASH' END AS PaymentType
         , bank.Code                                AS EmpBankCode
         , bank.NameEn                              AS EmpBankNameEn
         , empBank.IBAN                             AS EmpBankIBAN
         , empBank.BankBranch                       AS EmpBankBranch
         , company.DisplayName                      AS CompanyEn      -- see 'Company org level' note
         , CAST(NULL AS VARBINARY(MAX))             AS Companylogo    -- see 'FEATURE LOST' note
         , ISNULL(srdNet.Amount, 0)                 AS NetPay
    FROM Employees emp

    LEFT JOIN SalaryRegisters sr
           ON sr.EmployeeId = emp.Id
          AND sr.TenantId   = @TenantId
          AND sr.IsDeleted  = 0

    LEFT JOIN SalaryRegisterDetails srdNet
           ON srdNet.SalaryRegisterId = sr.Id
          AND srdNet.TenantId         = @TenantId
          AND srdNet.IsDeleted        = 0
          AND srdNet.WageTypeId IN (SELECT TOP 1 wt.Id FROM WageTypes wt
                                    WHERE wt.Code = '9000' AND wt.TenantId = @TenantId AND wt.IsDeleted = 0)

    LEFT JOIN EmployeeOrgAssignments orgAsg
           ON orgAsg.EmployeeId = emp.Id
          AND orgAsg.TenantId   = @TenantId
          AND orgAsg.IsDeleted  = 0
          AND orgAsg.StartDate <= EOMONTH(DATEFROMPARTS(@PrmYear, @PrmMonth, 1))
          AND orgAsg.EndDate   >= EOMONTH(DATEFROMPARTS(@PrmYear, @PrmMonth, 1))

    LEFT JOIN EmployeeBanks empBank
           ON empBank.EmployeeId = emp.Id
          AND empBank.TenantId   = @TenantId
          AND empBank.IsDeleted  = 0
          AND empBank.StartDate <= EOMONTH(DATEFROMPARTS(@PrmYear, @PrmMonth, 1))
          AND empBank.EndDate   >= EOMONTH(DATEFROMPARTS(@PrmYear, @PrmMonth, 1))

    LEFT JOIN Banks bank
           ON bank.Id       = empBank.BankId
          AND bank.TenantId = @TenantId
          AND bank.IsDeleted = 0

    -- the seven OrgLevel1..7 'Company' joins, collapsed to one ancestor walk
    LEFT JOIN AbpOrganizationUnits ou
           ON ou.Id        = orgAsg.OrganizationUnitId
          AND ou.TenantId  = @TenantId
          AND ou.IsDeleted = 0

    OUTER APPLY (
        SELECT TOP 1 anc.Id, anc.DisplayName
        FROM AbpOrganizationUnits anc
        INNER JOIN OrgUnitProfiles ancp       ON ancp.OrganizationUnitId = anc.Id       AND ancp.TenantId = @TenantId AND ancp.IsDeleted = 0
        INNER JOIN OrganizationUnitTypes anct ON anct.Id = ancp.OrganizationUnitTypeId  AND anct.TenantId = @TenantId AND anct.IsDeleted = 0
        WHERE anc.TenantId  = @TenantId
          AND anc.IsDeleted = 0
          AND ou.Code LIKE anc.Code + '%'
          AND anct.NameEn = 'Company'
        ORDER BY LEN(anc.Code)
    ) company

    WHERE emp.TenantId  = @TenantId
      AND emp.IsDeleted = 0
      AND sr.InPeriod   = sr.ForPeriod
) A

-- Legacy joined the org bank through a NAME lookup: OrganizationBankInfo.OrganisationUnit =
-- (SELECT OID FROM OrganizationUnit WHERE NameEn = A.CompanyEn). Kept as a name lookup so that a
-- duplicate-named unit behaves exactly as it did, but scoped to the tenant.
LEFT JOIN OrganizationBankInfos orgBankInfo
       ON orgBankInfo.TenantId  = @TenantId
      AND orgBankInfo.IsDeleted = 0
      AND orgBankInfo.OrganizationUnitId = (SELECT TOP 1 ou2.Id FROM AbpOrganizationUnits ou2
                                            WHERE ou2.DisplayName = A.CompanyEn
                                              AND ou2.TenantId = @TenantId AND ou2.IsDeleted = 0)
LEFT JOIN Banks OrgBank
       ON OrgBank.Id        = orgBankInfo.BankId
      AND OrgBank.TenantId  = @TenantId
      AND OrgBank.IsDeleted = 0

WHERE A.NetPay <> 0
  AND A.Year           = @PrmYear
  AND A.Month          = @PrmMonth
  AND A.PayrollRunType = @PrmPayrollRunType
  AND ((ISNULL(@PrmCompany, '') <> '' AND A.CompanyEn = @PrmCompany) OR ISNULL(@PrmCompany, '') = '')
  -- employees whose bank transfer is on hold for the period are excluded from the transfer letter
  AND A.EmployeeId NOT IN (SELECT DISTINCT oh.EmployeeId FROM EmployeeOnHolds oh
                           WHERE oh.TenantId = @TenantId AND oh.IsDeleted = 0
                             AND oh.StartDate <= A.ForPeriod AND oh.EndDate >= A.ForPeriod
                             AND oh.BankTransfer = 1);";

        // -------------------------------------------------------------------------------------------
        // rptBankTransferBySponsor -- rptBankTransfer grouped by sponsor instead of filtered by company.
        // -------------------------------------------------------------------------------------------
        public const string BankTransferBySponsorQuery = @"-- rptBankTransferBySponsor :: master query. Same shape as BankTransferQuery, plus the sponsor.
-- DATA GAP: Sponsors / EmployeeSponsors are empty in HCMv3, so SponsorEn/SponsorAr render blank.
SELECT A.Year
     , A.Month
     , A.ForPeriod
     , A.PayrollRunType
     , A.CompanyEn
     , A.Companylogo
     , OrgBank.Code   AS OrgBankCode
     , OrgBank.NameEn AS OrgBankNameEn
     , A.Code
     , A.FullNameEn
     , A.EmpBankCode
     , A.EmpBankNameEn
     , A.EmpBankIBAN
     , A.EmpBankBranch
     , A.PaymentType
     , A.SponsorEn
     , A.SponsorAr
     , A.NetPay
FROM (
    SELECT emp.Id                                   AS EmployeeId
         , emp.Code
         , emp.FullNameEn
         , emp.FullNameAr
         , DATEPART(YEAR,  sr.ForPeriod)            AS Year
         , DATEPART(MONTH, sr.ForPeriod)            AS Month
         , sr.ForPeriod
         , sr.PayrollRunType
         , CASE WHEN empBank.PaymentType = 0 OR empBank.PaymentType IS NULL THEN 'BANK' ELSE 'CASH' END AS PaymentType
         , bank.Code                                AS EmpBankCode
         , bank.NameEn                              AS EmpBankNameEn
         , empBank.IBAN                             AS EmpBankIBAN
         , empBank.BankBranch                       AS EmpBankBranch
         , company.DisplayName                      AS CompanyEn
         , CAST(NULL AS VARBINARY(MAX))             AS Companylogo
         , sponsor.NameEn                           AS SponsorEn
         , sponsor.NameAr                           AS SponsorAr
         , ISNULL(srdNet.Amount, 0)                 AS NetPay
    FROM Employees emp

    LEFT JOIN SalaryRegisters sr
           ON sr.EmployeeId = emp.Id AND sr.TenantId = @TenantId AND sr.IsDeleted = 0

    LEFT JOIN SalaryRegisterDetails srdNet
           ON srdNet.SalaryRegisterId = sr.Id AND srdNet.TenantId = @TenantId AND srdNet.IsDeleted = 0
          AND srdNet.WageTypeId IN (SELECT TOP 1 wt.Id FROM WageTypes wt
                                    WHERE wt.Code = '9000' AND wt.TenantId = @TenantId AND wt.IsDeleted = 0)

    LEFT JOIN EmployeeOrgAssignments orgAsg
           ON orgAsg.EmployeeId = emp.Id AND orgAsg.TenantId = @TenantId AND orgAsg.IsDeleted = 0
          AND orgAsg.StartDate <= EOMONTH(DATEFROMPARTS(@PrmYear, @PrmMonth, 1))
          AND orgAsg.EndDate   >= EOMONTH(DATEFROMPARTS(@PrmYear, @PrmMonth, 1))

    LEFT JOIN EmployeeBanks empBank
           ON empBank.EmployeeId = emp.Id AND empBank.TenantId = @TenantId AND empBank.IsDeleted = 0
          AND empBank.StartDate <= EOMONTH(DATEFROMPARTS(@PrmYear, @PrmMonth, 1))
          AND empBank.EndDate   >= EOMONTH(DATEFROMPARTS(@PrmYear, @PrmMonth, 1))

    LEFT JOIN Banks bank
           ON bank.Id = empBank.BankId AND bank.TenantId = @TenantId AND bank.IsDeleted = 0

    LEFT JOIN EmployeeSponsors empSponsor
           ON empSponsor.EmployeeId = emp.Id AND empSponsor.TenantId = @TenantId AND empSponsor.IsDeleted = 0
          AND empSponsor.StartDate <= EOMONTH(DATEFROMPARTS(@PrmYear, @PrmMonth, 1))
          AND empSponsor.EndDate   >= EOMONTH(DATEFROMPARTS(@PrmYear, @PrmMonth, 1))

    LEFT JOIN Sponsors sponsor
           ON sponsor.Id = empSponsor.SponsorId AND sponsor.TenantId = @TenantId AND sponsor.IsDeleted = 0

    LEFT JOIN AbpOrganizationUnits ou
           ON ou.Id = orgAsg.OrganizationUnitId AND ou.TenantId = @TenantId AND ou.IsDeleted = 0

    OUTER APPLY (
        SELECT TOP 1 anc.DisplayName
        FROM AbpOrganizationUnits anc
        INNER JOIN OrgUnitProfiles ancp       ON ancp.OrganizationUnitId = anc.Id      AND ancp.TenantId = @TenantId AND ancp.IsDeleted = 0
        INNER JOIN OrganizationUnitTypes anct ON anct.Id = ancp.OrganizationUnitTypeId AND anct.TenantId = @TenantId AND anct.IsDeleted = 0
        WHERE anc.TenantId = @TenantId AND anc.IsDeleted = 0
          AND ou.Code LIKE anc.Code + '%'
          AND anct.NameEn = 'Company'
        ORDER BY LEN(anc.Code)
    ) company

    WHERE emp.TenantId = @TenantId
      AND emp.IsDeleted = 0
      AND sr.InPeriod = sr.ForPeriod
) A

LEFT JOIN OrganizationBankInfos orgBankInfo
       ON orgBankInfo.TenantId = @TenantId AND orgBankInfo.IsDeleted = 0
      AND orgBankInfo.OrganizationUnitId = (SELECT TOP 1 ou2.Id FROM AbpOrganizationUnits ou2
                                            WHERE ou2.DisplayName = A.CompanyEn
                                              AND ou2.TenantId = @TenantId AND ou2.IsDeleted = 0)
LEFT JOIN Banks OrgBank
       ON OrgBank.Id = orgBankInfo.BankId AND OrgBank.TenantId = @TenantId AND OrgBank.IsDeleted = 0

WHERE A.NetPay <> 0
  AND A.Year           = @PrmYear
  AND A.Month          = @PrmMonth
  AND A.PayrollRunType = @PrmPayrollRunType
  AND ((ISNULL(@PrmSponsor, '') <> '' AND A.SponsorEn = @PrmSponsor) OR ISNULL(@PrmSponsor, '') = '')
  AND A.EmployeeId NOT IN (SELECT DISTINCT oh.EmployeeId FROM EmployeeOnHolds oh
                           WHERE oh.TenantId = @TenantId AND oh.IsDeleted = 0
                             AND oh.StartDate <= A.ForPeriod AND oh.EndDate >= A.ForPeriod
                             AND oh.BankTransfer = 1);";

        // -------------------------------------------------------------------------------------------
        // rptBankAndCashTransfer -- SUMMARY. Aggregates net pay per payment-breakup bucket.
        // -------------------------------------------------------------------------------------------
        public const string BankAndCashTransferQuery = @"-- rptBankAndCashTransfer :: master query. FINANCIAL SUMMARY -- one row per
-- (period, company, payment breakup, breakup detail) with SUM(NetPay).
--
-- Note the legacy inner query, unlike rptBankTransfer, keeps EVERY employee including those whose
-- bank transfer is on hold (the exclusion is commented out at the bottom of the legacy source) and
-- instead classifies them into their own 'TRANSFER ON HOLD' bucket. Preserved exactly: this report
-- is a reconciliation of the whole payroll, so dropping held employees would break the total.
--
-- The EmployeeBank join is a TOP-1-by-EndDate correlated pick here (not a plain join as in
-- rptBankTransfer), so an employee with two overlapping bank records is not double counted.
-- Rewritten as OUTER APPLY: same intent, and the correlation is visible to the optimiser.
SELECT B.Year
     , B.Month
     , B.ForPeriod
     , B.PayrollRunType
     , B.PaymentType
     , B.PaymentBreakup
     , B.CompanyEn
     , B.Companylogo
     , B.BreakupDetail
     , B.OrgBankCode
     , B.OrgBankNameEn
     , SUM(B.NetPay) AS NetPay
FROM (
    SELECT A.Code
         , A.FullNameEn
         , A.Year
         , A.Month
         , A.ForPeriod
         , A.PayrollRunType
         , A.PaymentType
         , CASE WHEN A.PaymentType = 'CASH'           THEN 'CASH TRANSFER'
                WHEN A.BankTransferOnHold = 'YES'     THEN 'TRANSFER ON HOLD'
                WHEN A.EmpBankCode = OrgBank.Code     THEN 'BANK TRANSFER TO ' + OrgBank.Code
                ELSE 'OTHER BANK TRANSFER'
           END AS PaymentBreakup
         , CASE WHEN A.PaymentType = 'CASH'                                    THEN 'CASH TRANSFER'
                WHEN A.EmpBankCode IS NULL OR A.EmpBankCode = OrgBank.Code     THEN A.DivisionEn
                ELSE A.EmpBankNameEn
           END AS BreakupDetail
         , A.EmpBankCode
         , A.EmpBankNameEn
         , A.CompanyEn
         , A.Companylogo
         , A.DivisionEn
         , OrgBank.Code   AS OrgBankCode
         , OrgBank.NameEn AS OrgBankNameEn
         , A.NetPay
    FROM (
        SELECT emp.Id                               AS EmployeeId
             , emp.Code
             , emp.FullNameEn
             , emp.FullNameAr
             , DATEPART(YEAR,  sr.ForPeriod)        AS Year
             , DATEPART(MONTH, sr.ForPeriod)        AS Month
             , sr.ForPeriod
             , sr.PayrollRunType
             , CASE WHEN onHold.Id IS NOT NULL THEN 'YES' ELSE 'NO' END AS BankTransferOnHold
             , CASE WHEN empBank.PaymentType = 0 OR empBank.PaymentType IS NULL THEN 'BANK' ELSE 'CASH' END AS PaymentType
             , bank.Code                            AS EmpBankCode
             , bank.NameEn                          AS EmpBankNameEn
             , company.DisplayName                  AS CompanyEn
             , CAST(NULL AS VARBINARY(MAX))         AS Companylogo
             , division.DisplayName                 AS DivisionEn
             , ISNULL(srdNet.Amount, 0)             AS NetPay
        FROM Employees emp

        LEFT JOIN SalaryRegisters sr
               ON sr.EmployeeId = emp.Id AND sr.TenantId = @TenantId AND sr.IsDeleted = 0

        LEFT JOIN SalaryRegisterDetails srdNet
               ON srdNet.SalaryRegisterId = sr.Id AND srdNet.TenantId = @TenantId AND srdNet.IsDeleted = 0
              AND srdNet.WageTypeId IN (SELECT TOP 1 wt.Id FROM WageTypes wt
                                        WHERE wt.Code = '9000' AND wt.TenantId = @TenantId AND wt.IsDeleted = 0)

        LEFT JOIN EmployeeOrgAssignments orgAsg
               ON orgAsg.EmployeeId = emp.Id AND orgAsg.TenantId = @TenantId AND orgAsg.IsDeleted = 0
              AND orgAsg.StartDate <= EOMONTH(DATEFROMPARTS(@PrmYear, @PrmMonth, 1))
              AND orgAsg.EndDate   >= EOMONTH(DATEFROMPARTS(@PrmYear, @PrmMonth, 1))

        OUTER APPLY (
            SELECT TOP 1 eb.*
            FROM EmployeeBanks eb
            WHERE eb.EmployeeId = emp.Id AND eb.TenantId = @TenantId AND eb.IsDeleted = 0
              AND eb.StartDate <= EOMONTH(DATEFROMPARTS(@PrmYear, @PrmMonth, 1))
              AND eb.EndDate   >= EOMONTH(DATEFROMPARTS(@PrmYear, @PrmMonth, 1))
            ORDER BY eb.EndDate DESC
        ) empBank

        LEFT JOIN Banks bank
               ON bank.Id = empBank.BankId AND bank.TenantId = @TenantId AND bank.IsDeleted = 0

        LEFT JOIN EmployeeOnHolds onHold
               ON onHold.EmployeeId = emp.Id AND onHold.TenantId = @TenantId AND onHold.IsDeleted = 0
              AND onHold.StartDate <= EOMONTH(DATEFROMPARTS(@PrmYear, @PrmMonth, 1))
              AND onHold.EndDate   >= EOMONTH(DATEFROMPARTS(@PrmYear, @PrmMonth, 1))

        LEFT JOIN AbpOrganizationUnits ou
               ON ou.Id = orgAsg.OrganizationUnitId AND ou.TenantId = @TenantId AND ou.IsDeleted = 0

        OUTER APPLY (
            SELECT TOP 1 anc.DisplayName
            FROM AbpOrganizationUnits anc
            INNER JOIN OrgUnitProfiles ancp       ON ancp.OrganizationUnitId = anc.Id      AND ancp.TenantId = @TenantId AND ancp.IsDeleted = 0
            INNER JOIN OrganizationUnitTypes anct ON anct.Id = ancp.OrganizationUnitTypeId AND anct.TenantId = @TenantId AND anct.IsDeleted = 0
            WHERE anc.TenantId = @TenantId AND anc.IsDeleted = 0
              AND ou.Code LIKE anc.Code + '%'
              AND anct.NameEn = 'Company'
            ORDER BY LEN(anc.Code)
        ) company

        OUTER APPLY (
            SELECT TOP 1 anc.DisplayName
            FROM AbpOrganizationUnits anc
            INNER JOIN OrgUnitProfiles ancp       ON ancp.OrganizationUnitId = anc.Id      AND ancp.TenantId = @TenantId AND ancp.IsDeleted = 0
            INNER JOIN OrganizationUnitTypes anct ON anct.Id = ancp.OrganizationUnitTypeId AND anct.TenantId = @TenantId AND anct.IsDeleted = 0
            WHERE anc.TenantId = @TenantId AND anc.IsDeleted = 0
              AND ou.Code LIKE anc.Code + '%'
              AND anct.NameEn = 'Division'
            ORDER BY LEN(anc.Code)
        ) division

        WHERE emp.TenantId = @TenantId
          AND emp.IsDeleted = 0
          AND sr.InPeriod = sr.ForPeriod
    ) A

    LEFT JOIN OrganizationBankInfos orgBankInfo
           ON orgBankInfo.TenantId = @TenantId AND orgBankInfo.IsDeleted = 0
          AND orgBankInfo.OrganizationUnitId = (SELECT TOP 1 ou2.Id FROM AbpOrganizationUnits ou2
                                                WHERE ou2.DisplayName = A.CompanyEn
                                                  AND ou2.TenantId = @TenantId AND ou2.IsDeleted = 0)
    LEFT JOIN Banks OrgBank
           ON OrgBank.Id = orgBankInfo.BankId AND OrgBank.TenantId = @TenantId AND OrgBank.IsDeleted = 0

    WHERE A.NetPay <> 0
      AND A.Year           = @PrmYear
      AND A.Month          = @PrmMonth
      AND A.PayrollRunType = @PrmPayrollRunType
      AND ((ISNULL(@PrmCompany, '') <> '' AND A.CompanyEn = @PrmCompany) OR ISNULL(@PrmCompany, '') = '')
) B
WHERE ((ISNULL(@PrmPaymentType, 'ALL') = 'ALL' OR ISNULL(@PrmPaymentType, '') = '')
       OR (ISNULL(@PrmPaymentType, '') <> '' AND B.PaymentType = @PrmPaymentType))
GROUP BY B.Year
       , B.Month
       , B.ForPeriod
       , B.PayrollRunType
       , B.PaymentType
       , B.PaymentBreakup
       , B.CompanyEn
       , B.Companylogo
       , B.BreakupDetail
       , B.OrgBankCode
       , B.OrgBankNameEn;";

        // -------------------------------------------------------------------------------------------
        // rptBankAndCashTransferBySponsor -- as above, but keyed by sponsor and with a user-chosen
        // org-unit type driving the breakup detail.
        // -------------------------------------------------------------------------------------------
        public const string BankAndCashTransferBySponsorQuery = @"-- rptBankAndCashTransferBySponsor :: master query. FINANCIAL SUMMARY, keyed by sponsor.
--
-- @PrmBreakupBy is the NAME of an org-unit type ('Division', 'Department', ...) chosen by the
-- operator; the legacy version hard-joined seven OrgLevel columns against it. Here the ancestor walk
-- takes the type name straight from the parameter, so it needs no code change to support new types.
--
-- Note this variant does NOT carry the 'TRANSFER ON HOLD' bucket -- the legacy source omits both the
-- BankTransferOnHold column and the EmployeeOnHold join. Preserved as-is rather than 'improved':
-- adding a bucket would change printed totals against the sponsor-keyed sibling report.
--
-- DATA GAP: Sponsors / SponsorBanks are empty in HCMv3 -- this report returns no sponsor and, with a
-- sponsor selected, no rows at all.
SELECT B.Year
     , B.Month
     , B.ForPeriod
     , B.PayrollRunType
     , B.SponsorCode
     , B.SponsorEn
     , B.SponsorAr
     , B.SponsorBankCode
     , B.SponsorBankNameEn
     , B.PaymentType
     , B.PaymentBreakup
     , B.BreakupDetail
     , SUM(B.NetPay) AS NetPay
FROM (
    SELECT A.Code
         , A.FullNameEn
         , A.Year
         , A.Month
         , A.ForPeriod
         , A.PayrollRunType
         , A.PaymentType
         , CASE WHEN A.PaymentType = 'CASH'       THEN 'CASH TRANSFER'
                WHEN A.EmpBankCode = OrgBank.Code THEN 'BANK TRANSFER TO ' + OrgBank.Code
                ELSE 'OTHER BANK TRANSFER'
           END AS PaymentBreakup
         , CASE WHEN A.PaymentType = 'CASH'                                THEN 'CASH TRANSFER'
                WHEN A.EmpBankCode IS NULL OR A.EmpBankCode = OrgBank.Code THEN A.BreakupByEn
                ELSE A.EmpBankNameEn
           END AS BreakupDetail
         , A.EmpBankCode
         , A.EmpBankNameEn
         , A.CompanyEn
         , A.Companylogo
         , A.BreakupByEn
         , A.SponsorCode
         , A.SponsorEn
         , A.SponsorAr
         , A.SponsorBankCode
         , A.SponsorBankNameEn
         , A.NetPay
    FROM (
        SELECT emp.Id                               AS EmployeeId
             , emp.Code
             , emp.FullNameEn
             , emp.FullNameAr
             , DATEPART(YEAR,  sr.ForPeriod)        AS Year
             , DATEPART(MONTH, sr.ForPeriod)        AS Month
             , sr.ForPeriod
             , sr.PayrollRunType
             , CASE WHEN empBank.PaymentType = 0 OR empBank.PaymentType IS NULL THEN 'BANK' ELSE 'CASH' END AS PaymentType
             , bank.Code                            AS EmpBankCode
             , bank.NameEn                          AS EmpBankNameEn
             , company.DisplayName                  AS CompanyEn
             , CAST(NULL AS VARBINARY(MAX))         AS Companylogo
             , breakup.DisplayName                  AS BreakupByEn
             , sponsor.Code                         AS SponsorCode
             , sponsor.NameEn                       AS SponsorEn
             , sponsor.NameAr                       AS SponsorAr
             , sponsorBank.Code                     AS SponsorBankCode
             , sponsorBank.NameEn                   AS SponsorBankNameEn
             , ISNULL(srdNet.Amount, 0)             AS NetPay
        FROM Employees emp

        LEFT JOIN SalaryRegisters sr
               ON sr.EmployeeId = emp.Id AND sr.TenantId = @TenantId AND sr.IsDeleted = 0

        LEFT JOIN SalaryRegisterDetails srdNet
               ON srdNet.SalaryRegisterId = sr.Id AND srdNet.TenantId = @TenantId AND srdNet.IsDeleted = 0
              AND srdNet.WageTypeId IN (SELECT TOP 1 wt.Id FROM WageTypes wt
                                        WHERE wt.Code = '9000' AND wt.TenantId = @TenantId AND wt.IsDeleted = 0)

        LEFT JOIN EmployeeOrgAssignments orgAsg
               ON orgAsg.EmployeeId = emp.Id AND orgAsg.TenantId = @TenantId AND orgAsg.IsDeleted = 0
              AND orgAsg.StartDate <= EOMONTH(DATEFROMPARTS(@PrmYear, @PrmMonth, 1))
              AND orgAsg.EndDate   >= EOMONTH(DATEFROMPARTS(@PrmYear, @PrmMonth, 1))

        OUTER APPLY (
            SELECT TOP 1 eb.*
            FROM EmployeeBanks eb
            WHERE eb.EmployeeId = emp.Id AND eb.TenantId = @TenantId AND eb.IsDeleted = 0
              AND eb.StartDate <= EOMONTH(DATEFROMPARTS(@PrmYear, @PrmMonth, 1))
              AND eb.EndDate   >= EOMONTH(DATEFROMPARTS(@PrmYear, @PrmMonth, 1))
            ORDER BY eb.EndDate DESC
        ) empBank

        LEFT JOIN Banks bank
               ON bank.Id = empBank.BankId AND bank.TenantId = @TenantId AND bank.IsDeleted = 0

        LEFT JOIN EmployeeSponsors empSponsor
               ON empSponsor.EmployeeId = emp.Id AND empSponsor.TenantId = @TenantId AND empSponsor.IsDeleted = 0
              AND empSponsor.StartDate <= EOMONTH(DATEFROMPARTS(@PrmYear, @PrmMonth, 1))
              AND empSponsor.EndDate   >= EOMONTH(DATEFROMPARTS(@PrmYear, @PrmMonth, 1))

        LEFT JOIN Sponsors sponsor
               ON sponsor.Id = empSponsor.SponsorId AND sponsor.TenantId = @TenantId AND sponsor.IsDeleted = 0

        -- legacy aliased 'SponsorBank' onto the Bank table here (not the SponsorBank table); it is
        -- the sponsor's own payment bank, reached through SponsorBanks.PaymentBankId.
        OUTER APPLY (
            SELECT TOP 1 sb.PaymentBankId
            FROM SponsorBanks sb
            WHERE sb.SponsorId = sponsor.Id AND sb.TenantId = @TenantId AND sb.IsDeleted = 0
            ORDER BY sb.EndDate DESC
        ) sponsorBankPick

        LEFT JOIN Banks sponsorBank
               ON sponsorBank.Id = sponsorBankPick.PaymentBankId AND sponsorBank.TenantId = @TenantId AND sponsorBank.IsDeleted = 0

        LEFT JOIN AbpOrganizationUnits ou
               ON ou.Id = orgAsg.OrganizationUnitId AND ou.TenantId = @TenantId AND ou.IsDeleted = 0

        OUTER APPLY (
            SELECT TOP 1 anc.DisplayName
            FROM AbpOrganizationUnits anc
            INNER JOIN OrgUnitProfiles ancp       ON ancp.OrganizationUnitId = anc.Id      AND ancp.TenantId = @TenantId AND ancp.IsDeleted = 0
            INNER JOIN OrganizationUnitTypes anct ON anct.Id = ancp.OrganizationUnitTypeId AND anct.TenantId = @TenantId AND anct.IsDeleted = 0
            WHERE anc.TenantId = @TenantId AND anc.IsDeleted = 0
              AND ou.Code LIKE anc.Code + '%'
              AND anct.NameEn = 'Company'
            ORDER BY LEN(anc.Code)
        ) company

        OUTER APPLY (
            SELECT TOP 1 anc.DisplayName
            FROM AbpOrganizationUnits anc
            INNER JOIN OrgUnitProfiles ancp       ON ancp.OrganizationUnitId = anc.Id      AND ancp.TenantId = @TenantId AND ancp.IsDeleted = 0
            INNER JOIN OrganizationUnitTypes anct ON anct.Id = ancp.OrganizationUnitTypeId AND anct.TenantId = @TenantId AND anct.IsDeleted = 0
            WHERE anc.TenantId = @TenantId AND anc.IsDeleted = 0
              AND ou.Code LIKE anc.Code + '%'
              AND anct.NameEn = @PrmBreakupBy
            ORDER BY LEN(anc.Code)
        ) breakup

        WHERE emp.TenantId = @TenantId
          AND emp.IsDeleted = 0
          AND sr.InPeriod = sr.ForPeriod
    ) A

    LEFT JOIN OrganizationBankInfos orgBankInfo
           ON orgBankInfo.TenantId = @TenantId AND orgBankInfo.IsDeleted = 0
          AND orgBankInfo.OrganizationUnitId = (SELECT TOP 1 ou2.Id FROM AbpOrganizationUnits ou2
                                                WHERE ou2.DisplayName = A.CompanyEn
                                                  AND ou2.TenantId = @TenantId AND ou2.IsDeleted = 0)
    LEFT JOIN Banks OrgBank
           ON OrgBank.Id = orgBankInfo.BankId AND OrgBank.TenantId = @TenantId AND OrgBank.IsDeleted = 0

    WHERE A.NetPay <> 0
      AND A.Year           = @PrmYear
      AND A.Month          = @PrmMonth
      AND A.PayrollRunType = @PrmPayrollRunType
      AND ((ISNULL(@PrmSponsor, '') <> '' AND A.SponsorEn = @PrmSponsor) OR ISNULL(@PrmSponsor, '') = '')
) B
WHERE ((ISNULL(@PrmPaymentType, 'ALL') = 'ALL' OR ISNULL(@PrmPaymentType, '') = '')
       OR (ISNULL(@PrmPaymentType, '') <> '' AND B.PaymentType = @PrmPaymentType))
GROUP BY B.Year
       , B.Month
       , B.ForPeriod
       , B.PayrollRunType
       , B.SponsorCode
       , B.SponsorEn
       , B.SponsorAr
       , B.SponsorBankCode
       , B.SponsorBankNameEn
       , B.PaymentType
       , B.PaymentBreakup
       , B.BreakupDetail;";

        // -------------------------------------------------------------------------------------------
        // rptBankFile -- the Arabic covering letter to the sponsor's bank, one line per employee.
        // -------------------------------------------------------------------------------------------
        public const string BankFileQuery = @"-- rptBankFile :: master query. FINANCIAL OUTPUT -- the salary-transfer file sent to the bank.
-- TotalNetPay is a window SUM over the whole result set; the report prints it, in figures and in
-- Arabic words, in the covering paragraph. Keep it a window function -- moving it to the layout
-- would change what the letter claims to transfer.
--
-- DATA GAP: this query is keyed on Sponsors.Id = @PrmSponsor and Sponsors is EMPTY in HCMv3, so the
-- report returns NO ROWS until the sponsor data is migrated. Not a porting defect.
SELECT SUM(ISNULL(srdNet.Amount, 0)) OVER ()          AS TotalNetPay
     , ROW_NUMBER() OVER (ORDER BY emp.Code)          AS SrNo
     , sponsor.NameEn                                 AS SponsorEn
     , sponsor.NameAr                                 AS SponsorAr
     , sponsor.MOSALID
     , sponsorBankPick.AccountNumber                  AS SponsorAccountNumber
     , bankOfSponsor.Code                             AS BankofSponsorCode
     , bankOfSponsor.NameEn                           AS BankofSponsorEn
     , bankOfSponsor.NameAr                           AS BankofSponsorAr
     , emp.Code
     , emp.FullNameEn
     , emp.FullNameAr
     , sr.ForPeriod
     , CASE WHEN empBank.PaymentType = 0 OR empBank.PaymentType IS NULL THEN 'BANK' ELSE 'CASH' END AS PaymentType
     , bank.Code                                      AS BankCode
     , bank.NameEn                                    AS BankEn
     , bank.NameAr                                    AS BankAr
     -- same bank as the sponsor -> internal account number; different bank -> IBAN
     , CASE WHEN bank.Code = bankOfSponsor.Code THEN empBank.AccountNum ELSE empBank.IBAN END AS AccountNum
     , CASE DATEPART(MONTH, sr.ForPeriod)
            WHEN 1  THEN N'يناير'   WHEN 2  THEN N'فبراير' WHEN 3  THEN N'مارس'
            WHEN 4  THEN N'ابريل'   WHEN 5  THEN N'مايو'   WHEN 6  THEN N'يونيو'
            WHEN 7  THEN N'يوليو'   WHEN 8  THEN N'أغسطس'  WHEN 9  THEN N'سبتمبر'
            WHEN 10 THEN N'أكتوبر'  WHEN 11 THEN N'نوفمبر' WHEN 12 THEN N'ديسمبر'
       END                                            AS MonthAr
     , DATEPART(YEAR, sr.ForPeriod)                   AS Year
     , ISNULL(srdNet.Amount, 0)                       AS NetPay
FROM Employees emp

LEFT JOIN SalaryRegisters sr
       ON sr.EmployeeId = emp.Id AND sr.TenantId = @TenantId AND sr.IsDeleted = 0

LEFT JOIN SalaryRegisterDetails srdNet
       ON srdNet.SalaryRegisterId = sr.Id AND srdNet.TenantId = @TenantId AND srdNet.IsDeleted = 0
      AND srdNet.WageTypeId IN (SELECT TOP 1 wt.Id FROM WageTypes wt
                                WHERE wt.Code = '9000' AND wt.TenantId = @TenantId AND wt.IsDeleted = 0)

LEFT JOIN EmployeeBanks empBank
       ON empBank.EmployeeId = emp.Id AND empBank.TenantId = @TenantId AND empBank.IsDeleted = 0
      AND empBank.StartDate <= EOMONTH(DATEFROMPARTS(@PrmYear, @PrmMonth, 1))
      AND empBank.EndDate   >= EOMONTH(DATEFROMPARTS(@PrmYear, @PrmMonth, 1))

LEFT JOIN Banks bank
       ON bank.Id = empBank.BankId AND bank.TenantId = @TenantId AND bank.IsDeleted = 0

-- legacy: EmployeeSponsor.OID IN (SELECT TOP 1 OID ... ORDER BY EndDate DESC) -- the latest sponsorship
OUTER APPLY (
    SELECT TOP 1 es.SponsorId
    FROM EmployeeSponsors es
    WHERE es.EmployeeId = emp.Id AND es.TenantId = @TenantId AND es.IsDeleted = 0
    ORDER BY es.EndDate DESC
) empSponsor

LEFT JOIN Sponsors sponsor
       ON sponsor.Id = empSponsor.SponsorId AND sponsor.TenantId = @TenantId AND sponsor.IsDeleted = 0

OUTER APPLY (
    SELECT TOP 1 sb.AccountNumber, sb.IBAN, sb.PaymentBankId
    FROM SponsorBanks sb
    WHERE sb.SponsorId = sponsor.Id AND sb.TenantId = @TenantId AND sb.IsDeleted = 0
    ORDER BY sb.EndDate DESC
) sponsorBankPick

LEFT JOIN Banks bankOfSponsor
       ON bankOfSponsor.Id = sponsorBankPick.PaymentBankId AND bankOfSponsor.TenantId = @TenantId AND bankOfSponsor.IsDeleted = 0

WHERE emp.TenantId = @TenantId
  AND emp.IsDeleted = 0
  AND sr.InPeriod = sr.ForPeriod
  AND DATEPART(YEAR,  sr.ForPeriod) = @PrmYear
  AND DATEPART(MONTH, sr.ForPeriod) = @PrmMonth
  AND sr.PayrollRunType = @PrmPayrollRunType
  AND sponsor.Id = @PrmSponsor
  AND ((ISNULL(@PrmEmpBank, '') <> '' AND bank.Code = @PrmEmpBank) OR ISNULL(@PrmEmpBank, '') = '');";

        // -------------------------------------------------------------------------------------------
        // rptBankFileAr -- despite the name this is the fuller, sponsor-optional variant.
        // -------------------------------------------------------------------------------------------
        public const string BankFileArQuery = @"-- rptBankFileAr :: master query. FINANCIAL OUTPUT.
-- Structurally the same as BankFileQuery but: the sponsor filter is OPTIONAL, AccountNum and IBAN are
-- projected separately rather than picked between, PayeeBankDetails supplies the clearing code the
-- sponsor's bank uses for the employee's bank, and a payment-type filter wraps the whole thing.
--
-- DATA GAP: Sponsors / SponsorBanks / EmployeeBanks are empty in HCMv3, so the sponsor and bank
-- columns render blank. With no sponsor selected the report DOES return the payroll rows.
SELECT A.SrNo, A.SponsorEn, A.SponsorAr, A.MOSALID, A.SponsorAccountNumber, A.SponsorIBAN
     , A.BankofSponsorCode, A.BankofSponsorEn, A.BankofSponsorAr
     , A.Code, A.FullNameEn, A.FullNameAr, A.CivilID, A.ForPeriod, A.PaymentType
     , A.EmpBankCode, A.EmpBankEn, A.EmpBankAr, A.EmpBankBranch, A.PayeeBankCode
     , A.AccountNum, A.IBAN, A.MonthAr, A.MonthEn, A.Year, A.NetPay
FROM (
    SELECT ROW_NUMBER() OVER (ORDER BY emp.Code)      AS SrNo
         , sponsor.NameEn                             AS SponsorEn
         , sponsor.NameAr                             AS SponsorAr
         , sponsor.MOSALID
         , sponsorBankPick.AccountNumber              AS SponsorAccountNumber
         , sponsorBankPick.IBAN                       AS SponsorIBAN
         , bankOfSponsor.Code                         AS BankofSponsorCode
         , bankOfSponsor.NameEn                       AS BankofSponsorEn
         , bankOfSponsor.NameAr                       AS BankofSponsorAr
         , emp.Code
         , emp.FullNameEn
         , emp.FullNameAr
         , emp.CivilID
         , sr.ForPeriod
         , CASE WHEN empBank.PaymentType = 0 OR empBank.PaymentType IS NULL THEN 'BANK' ELSE 'CASH' END AS PaymentType
         , bankOfEmployee.Code                        AS EmpBankCode
         , bankOfEmployee.NameEn                      AS EmpBankEn
         , bankOfEmployee.NameAr                      AS EmpBankAr
         , empBank.BankBranch                         AS EmpBankBranch
         , payeeBank.BankCode                         AS PayeeBankCode
         , empBank.AccountNum
         , empBank.IBAN
         , CASE DATEPART(MONTH, sr.ForPeriod)
                WHEN 1  THEN N'يناير'   WHEN 2  THEN N'فبراير' WHEN 3  THEN N'مارس'
                WHEN 4  THEN N'ابريل'   WHEN 5  THEN N'مايو'   WHEN 6  THEN N'يونيو'
                WHEN 7  THEN N'يوليو'   WHEN 8  THEN N'أغسطس'  WHEN 9  THEN N'سبتمبر'
                WHEN 10 THEN N'أكتوبر'  WHEN 11 THEN N'نوفمبر' WHEN 12 THEN N'ديسمبر'
           END                                        AS MonthAr
         , UPPER(DATENAME(MONTH, sr.ForPeriod))       AS MonthEn
         , DATEPART(YEAR, sr.ForPeriod)               AS Year
         , ISNULL(srdNet.Amount, 0)                   AS NetPay
    FROM Employees emp

    LEFT JOIN SalaryRegisters sr
           ON sr.EmployeeId = emp.Id AND sr.TenantId = @TenantId AND sr.IsDeleted = 0

    LEFT JOIN SalaryRegisterDetails srdNet
           ON srdNet.SalaryRegisterId = sr.Id AND srdNet.TenantId = @TenantId AND srdNet.IsDeleted = 0
          AND srdNet.WageTypeId IN (SELECT TOP 1 wt.Id FROM WageTypes wt
                                    WHERE wt.Code = '9000' AND wt.TenantId = @TenantId AND wt.IsDeleted = 0)

    LEFT JOIN EmployeeBanks empBank
           ON empBank.EmployeeId = emp.Id AND empBank.TenantId = @TenantId AND empBank.IsDeleted = 0
          AND empBank.StartDate <= EOMONTH(DATEFROMPARTS(@PrmYear, @PrmMonth, 1))
          AND empBank.EndDate   >= EOMONTH(DATEFROMPARTS(@PrmYear, @PrmMonth, 1))

    LEFT JOIN Banks bankOfEmployee
           ON bankOfEmployee.Id = empBank.BankId AND bankOfEmployee.TenantId = @TenantId AND bankOfEmployee.IsDeleted = 0

    OUTER APPLY (
        SELECT TOP 1 es.SponsorId
        FROM EmployeeSponsors es
        WHERE es.EmployeeId = emp.Id AND es.TenantId = @TenantId AND es.IsDeleted = 0
        ORDER BY es.EndDate DESC
    ) empSponsor

    LEFT JOIN Sponsors sponsor
           ON sponsor.Id = empSponsor.SponsorId AND sponsor.TenantId = @TenantId AND sponsor.IsDeleted = 0

    OUTER APPLY (
        SELECT TOP 1 sb.AccountNumber, sb.IBAN, sb.PaymentBankId
        FROM SponsorBanks sb
        WHERE sb.SponsorId = sponsor.Id AND sb.TenantId = @TenantId AND sb.IsDeleted = 0
        ORDER BY sb.EndDate DESC
    ) sponsorBankPick

    LEFT JOIN Banks bankOfSponsor
           ON bankOfSponsor.Id = sponsorBankPick.PaymentBankId AND bankOfSponsor.TenantId = @TenantId AND bankOfSponsor.IsDeleted = 0

    LEFT JOIN PayeeBankDetails payeeBank
           ON payeeBank.BankId      = bankOfSponsor.Id
          AND payeeBank.PayeeBankId = empBank.BankId
          AND payeeBank.TenantId    = @TenantId
          AND payeeBank.IsDeleted   = 0

    WHERE emp.TenantId = @TenantId
      AND emp.IsDeleted = 0
      AND sr.InPeriod = sr.ForPeriod
      AND DATEPART(YEAR,  sr.ForPeriod) = @PrmYear
      AND DATEPART(MONTH, sr.ForPeriod) = @PrmMonth
      AND sr.PayrollRunType = @PrmPayrollRunType
      AND ((ISNULL(@PrmSponsor, '') <> '' AND sponsor.Id = @PrmSponsor) OR ISNULL(@PrmSponsor, '') = '')
      AND ((ISNULL(@PrmEmpBank, '') <> '' AND bankOfEmployee.Code = @PrmEmpBank) OR ISNULL(@PrmEmpBank, '') = '')
) A
WHERE ((ISNULL(@PrmPaymentType, 'ALL') = 'ALL' OR ISNULL(@PrmPaymentType, '') = '')
       OR (ISNULL(@PrmPaymentType, '') <> '' AND A.PaymentType = @PrmPaymentType));";

        // -------------------------------------------------------------------------------------------
        // rptBankSupporting -- the schedule that accompanies the bank file.
        // -------------------------------------------------------------------------------------------
        public const string BankSupportingQuery = @"-- rptBankSupporting :: master query. FINANCIAL OUTPUT -- the supporting schedule for the bank file.
-- Identical to BankFileQuery except: no TotalNetPay window sum, CivilID is projected, and employees
-- whose bank transfer is on hold for the period are excluded (the bank file itself has no such
-- exclusion -- a legacy inconsistency, preserved deliberately).
--
-- DATA GAP: keyed on Sponsors.Id = @PrmSponsor; Sponsors is EMPTY in HCMv3 -> returns NO ROWS.
SELECT ROW_NUMBER() OVER (ORDER BY emp.Code)          AS SrNo
     , sponsor.NameEn                                 AS SponsorEn
     , sponsor.NameAr                                 AS SponsorAr
     , sponsor.MOSALID
     , sponsorBankPick.AccountNumber                  AS SponsorAccountNumber
     , bankOfSponsor.Code                             AS BankofSponsorCode
     , bankOfSponsor.NameEn                           AS BankofSponsorEn
     , bankOfSponsor.NameAr                           AS BankofSponsorAr
     , emp.Code
     , emp.FullNameEn
     , emp.FullNameAr
     , emp.CivilID
     , sr.ForPeriod
     , CASE WHEN empBank.PaymentType = 0 OR empBank.PaymentType IS NULL THEN 'BANK' ELSE 'CASH' END AS PaymentType
     , bank.Code                                      AS BankCode
     , bank.NameEn                                    AS BankEn
     , bank.NameAr                                    AS BankAr
     , CASE WHEN bank.Code = bankOfSponsor.Code THEN empBank.AccountNum ELSE empBank.IBAN END AS AccountNum
     , CASE DATEPART(MONTH, sr.ForPeriod)
            WHEN 1  THEN N'يناير'   WHEN 2  THEN N'فبراير' WHEN 3  THEN N'مارس'
            WHEN 4  THEN N'ابريل'   WHEN 5  THEN N'مايو'   WHEN 6  THEN N'يونيو'
            WHEN 7  THEN N'يوليو'   WHEN 8  THEN N'أغسطس'  WHEN 9  THEN N'سبتمبر'
            WHEN 10 THEN N'أكتوبر'  WHEN 11 THEN N'نوفمبر' WHEN 12 THEN N'ديسمبر'
       END                                            AS MonthAr
     , DATEPART(YEAR, sr.ForPeriod)                   AS Year
     , ISNULL(srdNet.Amount, 0)                       AS NetPay
FROM Employees emp

LEFT JOIN SalaryRegisters sr
       ON sr.EmployeeId = emp.Id AND sr.TenantId = @TenantId AND sr.IsDeleted = 0

LEFT JOIN SalaryRegisterDetails srdNet
       ON srdNet.SalaryRegisterId = sr.Id AND srdNet.TenantId = @TenantId AND srdNet.IsDeleted = 0
      AND srdNet.WageTypeId IN (SELECT TOP 1 wt.Id FROM WageTypes wt
                                WHERE wt.Code = '9000' AND wt.TenantId = @TenantId AND wt.IsDeleted = 0)

LEFT JOIN EmployeeBanks empBank
       ON empBank.EmployeeId = emp.Id AND empBank.TenantId = @TenantId AND empBank.IsDeleted = 0
      AND empBank.StartDate <= EOMONTH(DATEFROMPARTS(@PrmYear, @PrmMonth, 1))
      AND empBank.EndDate   >= EOMONTH(DATEFROMPARTS(@PrmYear, @PrmMonth, 1))

LEFT JOIN Banks bank
       ON bank.Id = empBank.BankId AND bank.TenantId = @TenantId AND bank.IsDeleted = 0

OUTER APPLY (
    SELECT TOP 1 es.SponsorId
    FROM EmployeeSponsors es
    WHERE es.EmployeeId = emp.Id AND es.TenantId = @TenantId AND es.IsDeleted = 0
    ORDER BY es.EndDate DESC
) empSponsor

LEFT JOIN Sponsors sponsor
       ON sponsor.Id = empSponsor.SponsorId AND sponsor.TenantId = @TenantId AND sponsor.IsDeleted = 0

OUTER APPLY (
    SELECT TOP 1 sb.AccountNumber, sb.PaymentBankId
    FROM SponsorBanks sb
    WHERE sb.SponsorId = sponsor.Id AND sb.TenantId = @TenantId AND sb.IsDeleted = 0
    ORDER BY sb.EndDate DESC
) sponsorBankPick

LEFT JOIN Banks bankOfSponsor
       ON bankOfSponsor.Id = sponsorBankPick.PaymentBankId AND bankOfSponsor.TenantId = @TenantId AND bankOfSponsor.IsDeleted = 0

WHERE emp.TenantId = @TenantId
  AND emp.IsDeleted = 0
  AND sr.InPeriod = sr.ForPeriod
  AND DATEPART(YEAR,  sr.ForPeriod) = @PrmYear
  AND DATEPART(MONTH, sr.ForPeriod) = @PrmMonth
  AND sr.PayrollRunType = @PrmPayrollRunType
  AND sponsor.Id = @PrmSponsor
  AND ((ISNULL(@PrmEmpBank, '') <> '' AND bank.Code = @PrmEmpBank) OR ISNULL(@PrmEmpBank, '') = '')
  AND emp.Id NOT IN (SELECT DISTINCT oh.EmployeeId FROM EmployeeOnHolds oh
                     WHERE oh.TenantId = @TenantId AND oh.IsDeleted = 0
                       AND oh.StartDate <= sr.ForPeriod AND oh.EndDate >= sr.ForPeriod
                       AND oh.BankTransfer = 1);";

        // -------------------------------------------------------------------------------------------
        // rptCashFile -- the cash-payment counterpart of the bank file.
        // -------------------------------------------------------------------------------------------
        public const string CashFileQuery = @"-- rptCashFile :: master query. FINANCIAL OUTPUT.
-- Same skeleton as BankFileArQuery, with two differences the legacy source is explicit about:
--   * AccountNum is ALWAYS the IBAN here (the 'same bank -> internal account number' CASE is
--     commented out in the legacy SQL). Preserved.
--   * zero-net-pay rows are excluded (ISNULL(NetPay,0) != 0).
--
-- DATA GAP: EmployeeBanks / Sponsors / SponsorBanks are empty in HCMv3 -> bank and sponsor columns
-- render blank. With no sponsor selected the payroll rows themselves DO come back.
SELECT A.SrNo, A.SponsorEn, A.SponsorAr, A.MOSALID, A.SponsorAccountNumber, A.SponsorIBAN
     , A.BankofSponsorCode, A.BankofSponsorEn, A.BankofSponsorAr
     , A.Code, A.FullNameEn, A.FullNameAr, A.CivilID, A.ForPeriod, A.PaymentType
     , A.EmpBankCode, A.EmpBankEn, A.EmpBankAr, A.EmpBankBranch, A.PayeeBankCode
     , A.AccountNum, A.MonthAr, A.MonthEn, A.Year, A.NetPay
FROM (
    SELECT ROW_NUMBER() OVER (ORDER BY emp.Code)      AS SrNo
         , sponsor.NameEn                             AS SponsorEn
         , sponsor.NameAr                             AS SponsorAr
         , sponsor.MOSALID
         , sponsorBankPick.AccountNumber              AS SponsorAccountNumber
         , sponsorBankPick.IBAN                       AS SponsorIBAN
         , bankOfSponsor.Code                         AS BankofSponsorCode
         , bankOfSponsor.NameEn                       AS BankofSponsorEn
         , bankOfSponsor.NameAr                       AS BankofSponsorAr
         , emp.Code
         , emp.FullNameEn
         , emp.FullNameAr
         , emp.CivilID
         , sr.ForPeriod
         , CASE WHEN empBank.PaymentType = 0 OR empBank.PaymentType IS NULL THEN 'BANK' ELSE 'CASH' END AS PaymentType
         , bankOfEmployee.Code                        AS EmpBankCode
         , bankOfEmployee.NameEn                      AS EmpBankEn
         , bankOfEmployee.NameAr                      AS EmpBankAr
         , empBank.BankBranch                         AS EmpBankBranch
         , payeeBank.BankCode                         AS PayeeBankCode
         , empBank.IBAN                               AS AccountNum
         , CASE DATEPART(MONTH, sr.ForPeriod)
                WHEN 1  THEN N'يناير'   WHEN 2  THEN N'فبراير' WHEN 3  THEN N'مارس'
                WHEN 4  THEN N'ابريل'   WHEN 5  THEN N'مايو'   WHEN 6  THEN N'يونيو'
                WHEN 7  THEN N'يوليو'   WHEN 8  THEN N'أغسطس'  WHEN 9  THEN N'سبتمبر'
                WHEN 10 THEN N'أكتوبر'  WHEN 11 THEN N'نوفمبر' WHEN 12 THEN N'ديسمبر'
           END                                        AS MonthAr
         , UPPER(DATENAME(MONTH, sr.ForPeriod))       AS MonthEn
         , DATEPART(YEAR, sr.ForPeriod)               AS Year
         , ISNULL(srdNet.Amount, 0)                   AS NetPay
    FROM Employees emp

    LEFT JOIN SalaryRegisters sr
           ON sr.EmployeeId = emp.Id AND sr.TenantId = @TenantId AND sr.IsDeleted = 0

    LEFT JOIN SalaryRegisterDetails srdNet
           ON srdNet.SalaryRegisterId = sr.Id AND srdNet.TenantId = @TenantId AND srdNet.IsDeleted = 0
          AND srdNet.WageTypeId IN (SELECT TOP 1 wt.Id FROM WageTypes wt
                                    WHERE wt.Code = '9000' AND wt.TenantId = @TenantId AND wt.IsDeleted = 0)

    LEFT JOIN EmployeeBanks empBank
           ON empBank.EmployeeId = emp.Id AND empBank.TenantId = @TenantId AND empBank.IsDeleted = 0
          AND empBank.StartDate <= EOMONTH(DATEFROMPARTS(@PrmYear, @PrmMonth, 1))
          AND empBank.EndDate   >= EOMONTH(DATEFROMPARTS(@PrmYear, @PrmMonth, 1))

    LEFT JOIN Banks bankOfEmployee
           ON bankOfEmployee.Id = empBank.BankId AND bankOfEmployee.TenantId = @TenantId AND bankOfEmployee.IsDeleted = 0

    OUTER APPLY (
        SELECT TOP 1 es.SponsorId
        FROM EmployeeSponsors es
        WHERE es.EmployeeId = emp.Id AND es.TenantId = @TenantId AND es.IsDeleted = 0
        ORDER BY es.EndDate DESC
    ) empSponsor

    LEFT JOIN Sponsors sponsor
           ON sponsor.Id = empSponsor.SponsorId AND sponsor.TenantId = @TenantId AND sponsor.IsDeleted = 0

    OUTER APPLY (
        SELECT TOP 1 sb.AccountNumber, sb.IBAN, sb.PaymentBankId
        FROM SponsorBanks sb
        WHERE sb.SponsorId = sponsor.Id AND sb.TenantId = @TenantId AND sb.IsDeleted = 0
        ORDER BY sb.EndDate DESC
    ) sponsorBankPick

    LEFT JOIN Banks bankOfSponsor
           ON bankOfSponsor.Id = sponsorBankPick.PaymentBankId AND bankOfSponsor.TenantId = @TenantId AND bankOfSponsor.IsDeleted = 0

    LEFT JOIN PayeeBankDetails payeeBank
           ON payeeBank.BankId      = bankOfSponsor.Id
          AND payeeBank.PayeeBankId = empBank.BankId
          AND payeeBank.TenantId    = @TenantId
          AND payeeBank.IsDeleted   = 0

    WHERE emp.TenantId = @TenantId
      AND emp.IsDeleted = 0
      AND ISNULL(srdNet.Amount, 0) <> 0
      AND sr.InPeriod = sr.ForPeriod
      AND DATEPART(YEAR,  sr.ForPeriod) = @PrmYear
      AND DATEPART(MONTH, sr.ForPeriod) = @PrmMonth
      AND sr.PayrollRunType = @PrmPayrollRunType
      AND ((ISNULL(@PrmSponsor, '') <> '' AND sponsor.Id = @PrmSponsor) OR ISNULL(@PrmSponsor, '') = '')
      AND ((ISNULL(@PrmEmpBank, '') <> '' AND bankOfEmployee.Code = @PrmEmpBank) OR ISNULL(@PrmEmpBank, '') = '')
) A
WHERE ((ISNULL(@PrmPaymentType, 'ALL') = 'ALL' OR ISNULL(@PrmPaymentType, '') = '')
       OR (ISNULL(@PrmPaymentType, '') <> '' AND A.PaymentType = @PrmPaymentType));";
    }
}
