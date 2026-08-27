// SQL for rptLeaveSettlement / rptLeaveSettlementAr / rptLeaveEncashment and their subreports,
// rewritten for the HCMv3 schema.
//
// MULTI-TENANCY: these run through DevExpress SqlDataSource as RAW SQL, so EF Core's per-tenant
// global query filter does NOT apply. Every query filters TenantId explicitly via @TenantId, which
// is supplied as a hidden report parameter (PrmTenantId) populated from AbpSession.TenantId by the
// caller. Removing an "AND TenantId = @TenantId" silently leaks rows across tenants.
//
// WAGE-TYPE TRAP (cost this project real debugging time -- do not undo):
//   Legacy `EmployeeBasicPayDetail.WageType = 1` is a raw legacy PRIMARY KEY. In HRMS_RCF that row
//   is the wage type whose *Code* is '1000' (Basic Salary). HCMv3 has an UNRELATED wage type whose
//   Code is literally '1' (Employee PIFSS Deduction). The integer must never be carried across --
//   every such comparison below is translated to `WageTypes.Code = '1000'`.
//   Likewise legacy `WageType <> 65` means Code '9000' (Net Pay).
//   Wage-type literals that were already CODES in the legacy SQL (1000, 1013, 4000, 4001, 4007,
//   4008, 4009, 4011, 4014, 1052, 9000) carry across unchanged, but must now be QUOTED because
//   HCMv3 WageTypes.Code is nvarchar -- unquoted, SQL Server would implicitly convert the whole
//   column to int and throw on the first non-numeric code.
//
// v23.2 NOTE: DevExpress CustomSqlQuery rejects any statement that is not purely SELECT, so the
// legacy `DECLARE @PaySlipDate ...` header is inlined as DATEFROMPARTS(@PrmYear, @PrmMonth, 1) at
// each use site. Semantics are identical (legacy built the string 'yyyy-M-01').

namespace HCMv3.Reports.Payroll
{
    /// <summary>Raw SQL used by the leave settlement / leave encashment report family.</summary>
    public static class LeaveSettlementSql
    {
        public const string PayrollArea = @"-- rptLeaveSettlement :: PayrollArea (look-up list feeding the @PrmPayrollArea combo)
-- Legacy was a designer SelectQuery over the table ""PayrollArea"". A SelectQuery cannot express the
-- mandatory tenant predicate, so it becomes a CustomSqlQuery with the same data-member name.
SELECT *
FROM PayrollAreas
WHERE TenantId = @TenantId
  AND IsDeleted = 0
ORDER BY Name;";

        // ===========================================================================================
        // rptLeaveSettlement :: PayslipOrgV2Query (MASTER -- one row per employee/period)
        //
        // NON-MECHANICAL REWRITES
        //   1. Department. Legacy EmployeeOrgAssignment carried seven flat FK columns OrgLevel1..7 and
        //      picked whichever one pointed at an OrganizationUnit of type 'Department' (14 aliased
        //      joins). HCMv3 has a SINGLE OrganizationUnitId into AbpOrganizationUnits, an ABP
        //      materialised-path tree with no unit-type concept, so "which ancestor is the Department"
        //      is not expressible; the employee's own unit is used.  *** SEMANTIC DEVIATION ***
        //   2. CompanyLogo. Legacy sourced OrganizationUnit.logo. AbpOrganizationUnits has no Logo
        //      column and HCMv3 has no Company entity carrying one, so it is returned as NULL.
        //      *** FEATURE LOST *** (the xrPictureBox binding stays so the layout is unchanged.)
        //   3. PayrollArea join. Legacy joined on a STRING (PayrollArea.Code = EOA.PayrollArea);
        //      HCMv3 models it as EmployeeOrgAssignments.PayrollAreaId -> PayrollAreas.Id. The
        //      @PrmPayrollArea parameter still filters on PayrollAreas.Name for operator parity.
        //   4. EmployeeBasicPayDetail.WageType = 1 -> WageTypes.Code = '1000'. See the file header.
        //   5. The legacy EmployeeSponsor and EmployeeSeparation joins are dropped: no column from
        //      either is projected, and as LEFT JOINs to 1:N tables they could only fan the master row
        //      out. Legacy bug, not ported.
        //   6. lblLeaveStartDate / lblLeaveEndDate / lblExpReturnDate / lblPaidLeave / lblUnpaidLeave
        //      were filled by an XPO BeforePrint handler in the legacy code-behind. The values it
        //      recomputed are ALREADY produced by this query (LeaveStartDate, LeaveEndDate,
        //      ExpReturnDate, PaidLeaveDays, UnpaidLeaveDays), so the handler is replaced by
        //      expression bindings onto these columns. See rptLeaveSettlement.cs.
        // ===========================================================================================
        public const string LeaveSettlementMasterEn = @"-- rptLeaveSettlement :: PayslipOrgV2Query (English master)
SELECT
      A.Code
    , A.FullNameEn
    , A.BankEn
    , A.AccountNum
    , A.IBAN
    , A.BankBranch
    , A.Department
    , A.EmployeeGroup
    , A.EmployeeSubGroup
    , A.Jobtitle
    , A.JoiningDate
    , A.CivilID
    , A.ForPeriod
    , A.InPeriod
    , A.PayrollRunType
    , A.Currency
    , A.CompanyLogo

    -- Paid leave days, plus any fractional adjustment landing in the same month as the leave end.
    , A.PaidLeaveDays + ISNULL((
          SELECT SUM(eaa.QuotaAdjusted)
          FROM EmployeeAbsAdjustments eaa
          WHERE eaa.TenantId = @TenantId AND eaa.IsDeleted = 0
            AND eaa.EmployeeId = A.EmployeeId
            AND eaa.AdjustmentType = 2   -- AdjustmentType.Fractional
            AND eaa.EffectiveDate >= A.LeaveEndDate
            AND DATEPART(Month, eaa.EffectiveDate) = DATEPART(Month, A.LeaveEndDate)
            AND DATEPART(Year,  eaa.EffectiveDate) = DATEPART(Year,  A.LeaveEndDate)
            AND eaa.AbsenceTypeId IN (SELECT at2.Id FROM AbsenceTypes at2 WHERE at2.IsPaid = 1 AND at2.TenantId = @TenantId AND at2.IsDeleted = 0)
      ), 0) AS PaidLeaveDays

    -- Unpaid leave days, plus fractional adjustments and any unpaid absence starting after the
    -- paid leave ended but inside the same month.
    , A.UnpaidLeaveDays
      + ISNULL((
          SELECT SUM(eaa.QuotaAdjusted)
          FROM EmployeeAbsAdjustments eaa
          WHERE eaa.TenantId = @TenantId AND eaa.IsDeleted = 0
            AND eaa.EmployeeId = A.EmployeeId
            AND eaa.AdjustmentType = 2
            AND eaa.EffectiveDate > A.LeaveEndDate
            AND DATEPART(Month, eaa.EffectiveDate) = DATEPART(Month, A.LeaveEndDate)
            AND DATEPART(Year,  eaa.EffectiveDate) = DATEPART(Year,  A.LeaveEndDate)
            AND eaa.AbsenceTypeId IN (SELECT at2.Id FROM AbsenceTypes at2 WHERE at2.IsPaid = 0 AND at2.TenantId = @TenantId AND at2.IsDeleted = 0)
      ), 0)
      + ISNULL((
          SELECT SUM(ea.TotalLeaveDays)
          FROM EmployeeAbsences ea
          WHERE ea.TenantId = @TenantId AND ea.IsDeleted = 0
            AND ea.EmployeeId = A.EmployeeId
            AND ea.StartDate > A.LeaveEndDate
            AND DATEPART(Month, ea.StartDate) = DATEPART(Month, A.LeaveEndDate)
            AND DATEPART(Year,  ea.StartDate) = DATEPART(Year,  A.LeaveEndDate)
            AND ea.AbsenceTypeId IN (SELECT at2.Id FROM AbsenceTypes at2 WHERE at2.IsPaid = 0 AND at2.TenantId = @TenantId AND at2.IsDeleted = 0)
      ), 0) AS UnpaidLeaveDays

    , A.PHday
    , A.WorkingDays
    , A.BasicPay
    , A.NetPay
    , A.PayrollArea
    , A.LeaveStartDate

    , ISNULL((
          SELECT MAX(ea.EndDate)
          FROM EmployeeAbsences ea
          WHERE ea.TenantId = @TenantId AND ea.IsDeleted = 0
            AND ea.EmployeeId = A.EmployeeId
            AND ea.StartDate > A.LeaveEndDate
            AND DATEPART(Month, ea.StartDate) = DATEPART(Month, A.LeaveEndDate)
            AND DATEPART(Year,  ea.StartDate) = DATEPART(Year,  A.LeaveEndDate)
            AND ea.AbsenceTypeId IN (SELECT at2.Id FROM AbsenceTypes at2 WHERE at2.IsPaid = 0 AND at2.TenantId = @TenantId AND at2.IsDeleted = 0)
      ), A.LeaveEndDate) AS LeaveEndDate

    , ISNULL(DATEADD(DAY, 1, (
          SELECT MAX(ea.EndDate)
          FROM EmployeeAbsences ea
          WHERE ea.TenantId = @TenantId AND ea.IsDeleted = 0
            AND ea.EmployeeId = A.EmployeeId
            AND ea.StartDate > A.LeaveEndDate
            AND DATEPART(Month, ea.StartDate) = DATEPART(Month, A.LeaveEndDate)
            AND DATEPART(Year,  ea.StartDate) = DATEPART(Year,  A.LeaveEndDate)
            AND ea.AbsenceTypeId IN (SELECT at2.Id FROM AbsenceTypes at2 WHERE at2.IsPaid = 0 AND at2.TenantId = @TenantId AND at2.IsDeleted = 0)
      )), A.ExpReturnDate) AS ExpReturnDate

    , (SELECT SUM(eaa.QuotaAdjusted)
       FROM EmployeeAbsAdjustments eaa
       WHERE eaa.TenantId = @TenantId AND eaa.IsDeleted = 0
         AND eaa.EmployeeId = A.EmployeeId
         AND eaa.AdjustmentType = 2
         AND eaa.EffectiveDate > A.LeaveEndDate
         AND DATEPART(Month, eaa.EffectiveDate) = DATEPART(Month, A.LeaveEndDate)
         AND DATEPART(Year,  eaa.EffectiveDate) = DATEPART(Year,  A.LeaveEndDate)
         AND eaa.AbsenceTypeId IN (SELECT at2.Id FROM AbsenceTypes at2 WHERE at2.IsPaid = 1 AND at2.TenantId = @TenantId AND at2.IsDeleted = 0)
      ) AS AdditionalFractionPaidLeave

    , (SELECT SUM(eaa.QuotaAdjusted)
       FROM EmployeeAbsAdjustments eaa
       WHERE eaa.TenantId = @TenantId AND eaa.IsDeleted = 0
         AND eaa.EmployeeId = A.EmployeeId
         AND eaa.AdjustmentType = 2
         AND eaa.EffectiveDate > A.LeaveEndDate
         AND DATEPART(Month, eaa.EffectiveDate) = DATEPART(Month, A.LeaveEndDate)
         AND DATEPART(Year,  eaa.EffectiveDate) = DATEPART(Year,  A.LeaveEndDate)
         AND eaa.AbsenceTypeId IN (SELECT at2.Id FROM AbsenceTypes at2 WHERE at2.IsPaid = 0 AND at2.TenantId = @TenantId AND at2.IsDeleted = 0)
      ) AS AdditionalFractionUnpaidLeave

    , (SELECT SUM(ea.TotalLeaveDays)
       FROM EmployeeAbsences ea
       WHERE ea.TenantId = @TenantId AND ea.IsDeleted = 0
         AND ea.EmployeeId = A.EmployeeId
         AND ea.StartDate > A.LeaveEndDate
         AND DATEPART(Month, ea.StartDate) = DATEPART(Month, A.LeaveEndDate)
         AND DATEPART(Year,  ea.StartDate) = DATEPART(Year,  A.LeaveEndDate)
         AND ea.AbsenceTypeId IN (SELECT at2.Id FROM AbsenceTypes at2 WHERE at2.IsPaid = 0 AND at2.TenantId = @TenantId AND at2.IsDeleted = 0)
      ) AS AdditionalUnpaidLeaveDays

    , (SELECT MAX(ea.EndDate)
       FROM EmployeeAbsences ea
       WHERE ea.TenantId = @TenantId AND ea.IsDeleted = 0
         AND ea.EmployeeId = A.EmployeeId
         AND ea.StartDate > A.LeaveEndDate
         AND DATEPART(Month, ea.StartDate) = DATEPART(Month, A.LeaveEndDate)
         AND DATEPART(Year,  ea.StartDate) = DATEPART(Year,  A.LeaveEndDate)
         AND ea.AbsenceTypeId IN (SELECT at2.Id FROM AbsenceTypes at2 WHERE at2.IsPaid = 0 AND at2.TenantId = @TenantId AND at2.IsDeleted = 0)
      ) AS AdditionalUnpaidLeaveEndDate

FROM (
    SELECT
          emp.Id                                AS EmployeeId
        , emp.Code
        , emp.FullNameEn
        , bank.NameEn                           AS BankEn
        , empBank.AccountNum
        , empBank.IBAN
        , empBank.BankBranch
        , orgUnit.DisplayName                   AS Department        -- see rewrite note 1
        , grp.NameEn                            AS EmployeeGroup
        , subGroup.NameEn                       AS EmployeeSubGroup
        , jobTitle.NameEn                       AS Jobtitle
        , emp.JoiningDate
        , emp.CivilID
        , sr.ForPeriod
        , sr.InPeriod
        , sr.PayrollRunType
        , basicPayCurrency.CurrencyShortFormEn  AS Currency
        , CAST(NULL AS VARBINARY(MAX))          AS CompanyLogo       -- see rewrite note 2
        , ISNULL(PaidLeave.TotalLeaveDays,   0) + ISNULL(AdjustedPaidLeave.QuotaAdjusted,   0) AS PaidLeaveDays
        , ISNULL(UnpaidLeave.TotalLeaveDays, 0) + ISNULL(AdjustedUnpaidLeave.QuotaAdjusted, 0) AS UnpaidLeaveDays
        , ISNULL(srdPHday.Number,    0)         AS PHday
        , ISNULL(srdBasicPay.Number, 0)         AS WorkingDays
        , ISNULL(srdBasicPay.Rate,   0)         AS BasicPay
        , srdNetPay.Amount                      AS NetPay
        , payrollArea.Name                      AS PayrollArea
        , PaidLeave.StartDate                   AS LeaveStartDate
        , CASE WHEN UnpaidLeave.LeaveEndDate IS NOT NULL AND PaidLeave.EndDate < UnpaidLeave.LeaveEndDate
                    THEN UnpaidLeave.LeaveEndDate
               ELSE PaidLeave.EndDate
          END                                   AS LeaveEndDate
        , DATEADD(DAY, 1,
            CASE WHEN UnpaidLeave.LeaveEndDate IS NOT NULL AND PaidLeave.EndDate < UnpaidLeave.LeaveEndDate
                      THEN UnpaidLeave.LeaveEndDate
                 ELSE PaidLeave.EndDate
            END)                                AS ExpReturnDate

    FROM Employees emp

    LEFT JOIN EmployeeOrgAssignments eoa
           ON eoa.EmployeeId = emp.Id
          AND eoa.TenantId   = @TenantId
          AND eoa.IsDeleted  = 0
          AND DATEFROMPARTS(@PrmYear, @PrmMonth, 1) BETWEEN eoa.StartDate AND eoa.EndDate
    LEFT JOIN JobTitles         jobTitle    ON jobTitle.Id    = eoa.JobTitleId          AND jobTitle.TenantId    = @TenantId AND jobTitle.IsDeleted    = 0
    LEFT JOIN PayrollAreas      payrollArea ON payrollArea.Id = eoa.PayrollAreaId       AND payrollArea.TenantId = @TenantId AND payrollArea.IsDeleted = 0
    LEFT JOIN EmployeeGroups    grp         ON grp.Id         = eoa.EmployeeGroupId     AND grp.TenantId         = @TenantId AND grp.IsDeleted         = 0
    LEFT JOIN EmployeeSubGroups subGroup    ON subGroup.Id    = eoa.EmployeeSubGroupId  AND subGroup.TenantId    = @TenantId AND subGroup.IsDeleted    = 0
    LEFT JOIN AbpOrganizationUnits orgUnit  ON orgUnit.Id     = eoa.OrganizationUnitId  AND orgUnit.TenantId     = @TenantId AND orgUnit.IsDeleted     = 0

    LEFT JOIN EmployeeBanks empBank
           ON empBank.EmployeeId = emp.Id
          AND empBank.TenantId   = @TenantId
          AND empBank.IsDeleted  = 0
          AND DATEFROMPARTS(@PrmYear, @PrmMonth, 1) BETWEEN empBank.StartDate AND empBank.EndDate
    LEFT JOIN Banks bank ON bank.Id = empBank.BankId AND bank.TenantId = @TenantId AND bank.IsDeleted = 0

    LEFT JOIN SalaryRegisters sr
           ON sr.EmployeeId = emp.Id AND sr.TenantId = @TenantId AND sr.IsDeleted = 0

    -- Wage-type carrier rows. The literals below ARE business codes in the legacy SQL, so they
    -- carry across unchanged -- only the quoting is new (Code is nvarchar in HCMv3).
    LEFT JOIN SalaryRegisterDetails srdPHday
           ON srdPHday.SalaryRegisterId = sr.Id AND srdPHday.TenantId = @TenantId AND srdPHday.IsDeleted = 0
          AND srdPHday.WageTypeId IN (SELECT wt.Id FROM WageTypes wt WHERE wt.Code = '4007' AND wt.TenantId = @TenantId AND wt.IsDeleted = 0)
    LEFT JOIN SalaryRegisterDetails srdBasicPay
           ON srdBasicPay.SalaryRegisterId = sr.Id AND srdBasicPay.TenantId = @TenantId AND srdBasicPay.IsDeleted = 0
          AND srdBasicPay.WageTypeId IN (SELECT wt.Id FROM WageTypes wt WHERE wt.Code = '1000' AND wt.TenantId = @TenantId AND wt.IsDeleted = 0)
    LEFT JOIN SalaryRegisterDetails srdNetPay
           ON srdNetPay.SalaryRegisterId = sr.Id AND srdNetPay.TenantId = @TenantId AND srdNetPay.IsDeleted = 0
          AND srdNetPay.WageTypeId IN (SELECT wt.Id FROM WageTypes wt WHERE wt.Code = '9000' AND wt.TenantId = @TenantId AND wt.IsDeleted = 0)

    -- Currency of the employee's most recent BASIC PAY line. Legacy filtered
    -- EmployeeBasicPayDetail.WageType = 1 (a raw PK) -- translated through the CODE. See header.
    OUTER APPLY (
        SELECT TOP 1 cur.CurrencyShortFormEn
        FROM EmployeeBasicPays ebp
        JOIN EmployeeBasicPayDetails ebpd ON ebpd.EmployeeBasicPayId = ebp.Id AND ebpd.TenantId = @TenantId AND ebpd.IsDeleted = 0
        JOIN WageTypes wtBasic ON wtBasic.Id = ebpd.WageTypeId AND wtBasic.Code = '1000' AND wtBasic.TenantId = @TenantId AND wtBasic.IsDeleted = 0
        LEFT JOIN Currencies cur ON cur.Id = ebpd.CurrencyId AND cur.TenantId = @TenantId AND cur.IsDeleted = 0
        WHERE ebp.EmployeeId = emp.Id AND ebp.TenantId = @TenantId AND ebp.IsDeleted = 0
        ORDER BY ebp.StartDate DESC
    ) basicPayCurrency

    LEFT JOIN (
        SELECT ea.EmployeeId
             , MAX(ea.StartDate)       AS StartDate
             , MAX(ea.EndDate)         AS EndDate
             , SUM(ea.TotalLeaveDays)  AS TotalLeaveDays
        FROM EmployeeAbsences ea
        WHERE ea.TenantId = @TenantId AND ea.IsDeleted = 0
          AND ea.AbsenceTypeId IN (SELECT at2.Id FROM AbsenceTypes at2 WHERE at2.IsPaid = 1 AND at2.TenantId = @TenantId AND at2.IsDeleted = 0)
          AND ((DATEPART(Month, ea.StartDate) = @PrmMonth AND DATEPART(Year, ea.StartDate) = @PrmYear)
            OR (DATEPART(Month, ea.EndDate)   = @PrmMonth AND DATEPART(Year, ea.EndDate)   = @PrmYear))
        GROUP BY ea.EmployeeId
    ) PaidLeave ON PaidLeave.EmployeeId = emp.Id

    LEFT JOIN (
        SELECT ea.EmployeeId
             , SUM(ea.TotalLeaveDays)  AS TotalLeaveDays
             , MAX(ea.EndDate)         AS LeaveEndDate
        FROM EmployeeAbsences ea
        WHERE ea.TenantId = @TenantId AND ea.IsDeleted = 0
          AND ea.AbsenceTypeId IN (SELECT at2.Id FROM AbsenceTypes at2 WHERE at2.IsPaid = 0 AND at2.TenantId = @TenantId AND at2.IsDeleted = 0)
          AND ((DATEPART(Month, ea.StartDate) = @PrmMonth AND DATEPART(Year, ea.StartDate) = @PrmYear)
            OR (DATEPART(Month, ea.EndDate)   = @PrmMonth AND DATEPART(Year, ea.EndDate)   = @PrmYear))
        GROUP BY ea.EmployeeId
    ) UnpaidLeave ON UnpaidLeave.EmployeeId = emp.Id

    LEFT JOIN (
        SELECT eaa.EmployeeId, SUM(eaa.QuotaAdjusted) AS QuotaAdjusted
        FROM EmployeeAbsAdjustments eaa
        WHERE eaa.TenantId = @TenantId AND eaa.IsDeleted = 0
          AND eaa.AdjustmentType = 2
          AND eaa.AbsenceTypeId IN (SELECT at2.Id FROM AbsenceTypes at2 WHERE at2.IsPaid = 1 AND at2.TenantId = @TenantId AND at2.IsDeleted = 0)
          AND DATEPART(Month, eaa.EffectiveDate) = @PrmMonth
          AND DATEPART(Year,  eaa.EffectiveDate) = @PrmYear
        GROUP BY eaa.EmployeeId
    ) AdjustedPaidLeave ON AdjustedPaidLeave.EmployeeId = emp.Id

    LEFT JOIN (
        SELECT eaa.EmployeeId, SUM(eaa.QuotaAdjusted) AS QuotaAdjusted
        FROM EmployeeAbsAdjustments eaa
        WHERE eaa.TenantId = @TenantId AND eaa.IsDeleted = 0
          AND eaa.AdjustmentType = 2
          AND eaa.AbsenceTypeId IN (SELECT at2.Id FROM AbsenceTypes at2 WHERE at2.IsPaid = 0 AND at2.TenantId = @TenantId AND at2.IsDeleted = 0)
          AND DATEPART(Month, eaa.EffectiveDate) = @PrmMonth
          AND DATEPART(Year,  eaa.EffectiveDate) = @PrmYear
        GROUP BY eaa.EmployeeId
    ) AdjustedUnpaidLeave ON AdjustedUnpaidLeave.EmployeeId = emp.Id

    WHERE emp.TenantId = @TenantId
      AND emp.IsDeleted = 0
      AND sr.InPeriod = sr.ForPeriod
      AND DATEPART(Month, sr.InPeriod) = @PrmMonth
      AND DATEPART(Year,  sr.InPeriod) = @PrmYear
      AND sr.PayrollRunType = @PrmPayrollRunType
      AND (
            (ISNULL(@PrmEmpCode, '') <> '' AND emp.Code = @PrmEmpCode)
         OR (ISNULL(@PrmEmpCode, '') =  '' AND ISNULL(@PrmPayrollArea, '') <> '' AND payrollArea.Name = @PrmPayrollArea)
          )
) A
ORDER BY
    -- Legacy sort key preserved verbatim: numeric codes right-aligned zero-padded to 20,
    -- non-numeric left-aligned. (The legacy REPLICATE('', 21) is a no-op; kept for byte parity.)
    CASE WHEN ISNUMERIC(A.Code) = 1 THEN RIGHT(REPLICATE('0', 21) + A.Code, 20)
         WHEN ISNUMERIC(A.Code) = 0 THEN LEFT(A.Code + REPLICATE('', 21), 20)
         ELSE A.Code
    END;";

        /// <summary>
        /// Arabic master. Identical to <see cref="LeaveSettlementMasterEn"/> except that FullNameEn,
        /// Bank.NameEn, OrganizationUnit name, EmployeeGroup/SubGroup/JobTitle NameEn and
        /// CurrencyShortFormEn become their *Ar counterparts -- which is exactly the legacy diff.
        /// The projected COLUMN ALIASES stay English (FullNameAr / BankAr excepted, matching legacy)
        /// so the two layouts can share binding names.
        /// </summary>
        public const string LeaveSettlementMasterAr = @"-- rptLeaveSettlementAr :: PayslipOrgV2Query (Arabic master)
SELECT
      A.Code
    , A.FullNameAr
    , A.BankAr
    , A.AccountNum
    , A.IBAN
    , A.BankBranch
    , A.Department
    , A.EmployeeGroup
    , A.EmployeeSubGroup
    , A.Jobtitle
    , A.JoiningDate
    , A.CivilID
    , A.ForPeriod
    , A.InPeriod
    , A.PayrollRunType
    , A.Currency
    , A.CompanyLogo

    , A.PaidLeaveDays + ISNULL((
          SELECT SUM(eaa.QuotaAdjusted)
          FROM EmployeeAbsAdjustments eaa
          WHERE eaa.TenantId = @TenantId AND eaa.IsDeleted = 0
            AND eaa.EmployeeId = A.EmployeeId
            AND eaa.AdjustmentType = 2
            AND eaa.EffectiveDate >= A.LeaveEndDate
            AND DATEPART(Month, eaa.EffectiveDate) = DATEPART(Month, A.LeaveEndDate)
            AND DATEPART(Year,  eaa.EffectiveDate) = DATEPART(Year,  A.LeaveEndDate)
            AND eaa.AbsenceTypeId IN (SELECT at2.Id FROM AbsenceTypes at2 WHERE at2.IsPaid = 1 AND at2.TenantId = @TenantId AND at2.IsDeleted = 0)
      ), 0) AS PaidLeaveDays

    , A.UnpaidLeaveDays
      + ISNULL((
          SELECT SUM(eaa.QuotaAdjusted)
          FROM EmployeeAbsAdjustments eaa
          WHERE eaa.TenantId = @TenantId AND eaa.IsDeleted = 0
            AND eaa.EmployeeId = A.EmployeeId
            AND eaa.AdjustmentType = 2
            AND eaa.EffectiveDate > A.LeaveEndDate
            AND DATEPART(Month, eaa.EffectiveDate) = DATEPART(Month, A.LeaveEndDate)
            AND DATEPART(Year,  eaa.EffectiveDate) = DATEPART(Year,  A.LeaveEndDate)
            AND eaa.AbsenceTypeId IN (SELECT at2.Id FROM AbsenceTypes at2 WHERE at2.IsPaid = 0 AND at2.TenantId = @TenantId AND at2.IsDeleted = 0)
      ), 0)
      + ISNULL((
          SELECT SUM(ea.TotalLeaveDays)
          FROM EmployeeAbsences ea
          WHERE ea.TenantId = @TenantId AND ea.IsDeleted = 0
            AND ea.EmployeeId = A.EmployeeId
            AND ea.StartDate > A.LeaveEndDate
            AND DATEPART(Month, ea.StartDate) = DATEPART(Month, A.LeaveEndDate)
            AND DATEPART(Year,  ea.StartDate) = DATEPART(Year,  A.LeaveEndDate)
            AND ea.AbsenceTypeId IN (SELECT at2.Id FROM AbsenceTypes at2 WHERE at2.IsPaid = 0 AND at2.TenantId = @TenantId AND at2.IsDeleted = 0)
      ), 0) AS UnpaidLeaveDays

    , A.PHday
    , A.WorkingDays
    , A.BasicPay
    , A.NetPay
    , A.PayrollArea
    , A.LeaveStartDate

    , ISNULL((
          SELECT MAX(ea.EndDate)
          FROM EmployeeAbsences ea
          WHERE ea.TenantId = @TenantId AND ea.IsDeleted = 0
            AND ea.EmployeeId = A.EmployeeId
            AND ea.StartDate > A.LeaveEndDate
            AND DATEPART(Month, ea.StartDate) = DATEPART(Month, A.LeaveEndDate)
            AND DATEPART(Year,  ea.StartDate) = DATEPART(Year,  A.LeaveEndDate)
            AND ea.AbsenceTypeId IN (SELECT at2.Id FROM AbsenceTypes at2 WHERE at2.IsPaid = 0 AND at2.TenantId = @TenantId AND at2.IsDeleted = 0)
      ), A.LeaveEndDate) AS LeaveEndDate

    , ISNULL(DATEADD(DAY, 1, (
          SELECT MAX(ea.EndDate)
          FROM EmployeeAbsences ea
          WHERE ea.TenantId = @TenantId AND ea.IsDeleted = 0
            AND ea.EmployeeId = A.EmployeeId
            AND ea.StartDate > A.LeaveEndDate
            AND DATEPART(Month, ea.StartDate) = DATEPART(Month, A.LeaveEndDate)
            AND DATEPART(Year,  ea.StartDate) = DATEPART(Year,  A.LeaveEndDate)
            AND ea.AbsenceTypeId IN (SELECT at2.Id FROM AbsenceTypes at2 WHERE at2.IsPaid = 0 AND at2.TenantId = @TenantId AND at2.IsDeleted = 0)
      )), A.ExpReturnDate) AS ExpReturnDate

    , (SELECT SUM(eaa.QuotaAdjusted)
       FROM EmployeeAbsAdjustments eaa
       WHERE eaa.TenantId = @TenantId AND eaa.IsDeleted = 0
         AND eaa.EmployeeId = A.EmployeeId
         AND eaa.AdjustmentType = 2
         AND eaa.EffectiveDate > A.LeaveEndDate
         AND DATEPART(Month, eaa.EffectiveDate) = DATEPART(Month, A.LeaveEndDate)
         AND DATEPART(Year,  eaa.EffectiveDate) = DATEPART(Year,  A.LeaveEndDate)
         AND eaa.AbsenceTypeId IN (SELECT at2.Id FROM AbsenceTypes at2 WHERE at2.IsPaid = 1 AND at2.TenantId = @TenantId AND at2.IsDeleted = 0)
      ) AS AdditionalFractionPaidLeave

    , (SELECT SUM(eaa.QuotaAdjusted)
       FROM EmployeeAbsAdjustments eaa
       WHERE eaa.TenantId = @TenantId AND eaa.IsDeleted = 0
         AND eaa.EmployeeId = A.EmployeeId
         AND eaa.AdjustmentType = 2
         AND eaa.EffectiveDate > A.LeaveEndDate
         AND DATEPART(Month, eaa.EffectiveDate) = DATEPART(Month, A.LeaveEndDate)
         AND DATEPART(Year,  eaa.EffectiveDate) = DATEPART(Year,  A.LeaveEndDate)
         AND eaa.AbsenceTypeId IN (SELECT at2.Id FROM AbsenceTypes at2 WHERE at2.IsPaid = 0 AND at2.TenantId = @TenantId AND at2.IsDeleted = 0)
      ) AS AdditionalFractionUnpaidLeave

    , (SELECT SUM(ea.TotalLeaveDays)
       FROM EmployeeAbsences ea
       WHERE ea.TenantId = @TenantId AND ea.IsDeleted = 0
         AND ea.EmployeeId = A.EmployeeId
         AND ea.StartDate > A.LeaveEndDate
         AND DATEPART(Month, ea.StartDate) = DATEPART(Month, A.LeaveEndDate)
         AND DATEPART(Year,  ea.StartDate) = DATEPART(Year,  A.LeaveEndDate)
         AND ea.AbsenceTypeId IN (SELECT at2.Id FROM AbsenceTypes at2 WHERE at2.IsPaid = 0 AND at2.TenantId = @TenantId AND at2.IsDeleted = 0)
      ) AS AdditionalUnpaidLeaveDays

    , (SELECT MAX(ea.EndDate)
       FROM EmployeeAbsences ea
       WHERE ea.TenantId = @TenantId AND ea.IsDeleted = 0
         AND ea.EmployeeId = A.EmployeeId
         AND ea.StartDate > A.LeaveEndDate
         AND DATEPART(Month, ea.StartDate) = DATEPART(Month, A.LeaveEndDate)
         AND DATEPART(Year,  ea.StartDate) = DATEPART(Year,  A.LeaveEndDate)
         AND ea.AbsenceTypeId IN (SELECT at2.Id FROM AbsenceTypes at2 WHERE at2.IsPaid = 0 AND at2.TenantId = @TenantId AND at2.IsDeleted = 0)
      ) AS AdditionalUnpaidLeaveEndDate

FROM (
    SELECT
          emp.Id                                AS EmployeeId
        , emp.Code
        , emp.FullNameAr
        , bank.NameAr                           AS BankAr
        , empBank.AccountNum
        , empBank.IBAN
        , empBank.BankBranch
        , orgUnit.DisplayName                   AS Department        -- see LeaveSettlementMasterEn note 1
        , grp.NameAr                            AS EmployeeGroup
        , subGroup.NameAr                       AS EmployeeSubGroup
        , jobTitle.NameAr                       AS Jobtitle
        , emp.JoiningDate
        , emp.CivilID
        , sr.ForPeriod
        , sr.InPeriod
        , sr.PayrollRunType
        , basicPayCurrency.CurrencyShortFormAr  AS Currency
        , CAST(NULL AS VARBINARY(MAX))          AS CompanyLogo       -- see LeaveSettlementMasterEn note 2
        , ISNULL(PaidLeave.TotalLeaveDays,   0) + ISNULL(AdjustedPaidLeave.QuotaAdjusted,   0) AS PaidLeaveDays
        , ISNULL(UnpaidLeave.TotalLeaveDays, 0) + ISNULL(AdjustedUnpaidLeave.QuotaAdjusted, 0) AS UnpaidLeaveDays
        , ISNULL(srdPHday.Number,    0)         AS PHday
        , ISNULL(srdBasicPay.Number, 0)         AS WorkingDays
        , ISNULL(srdBasicPay.Rate,   0)         AS BasicPay
        , srdNetPay.Amount                      AS NetPay
        , payrollArea.Name                      AS PayrollArea
        , PaidLeave.StartDate                   AS LeaveStartDate
        , CASE WHEN UnpaidLeave.LeaveEndDate IS NOT NULL AND PaidLeave.EndDate < UnpaidLeave.LeaveEndDate
                    THEN UnpaidLeave.LeaveEndDate
               ELSE PaidLeave.EndDate
          END                                   AS LeaveEndDate
        , DATEADD(DAY, 1,
            CASE WHEN UnpaidLeave.LeaveEndDate IS NOT NULL AND PaidLeave.EndDate < UnpaidLeave.LeaveEndDate
                      THEN UnpaidLeave.LeaveEndDate
                 ELSE PaidLeave.EndDate
            END)                                AS ExpReturnDate

    FROM Employees emp

    LEFT JOIN EmployeeOrgAssignments eoa
           ON eoa.EmployeeId = emp.Id
          AND eoa.TenantId   = @TenantId
          AND eoa.IsDeleted  = 0
          AND DATEFROMPARTS(@PrmYear, @PrmMonth, 1) BETWEEN eoa.StartDate AND eoa.EndDate
    LEFT JOIN JobTitles         jobTitle    ON jobTitle.Id    = eoa.JobTitleId          AND jobTitle.TenantId    = @TenantId AND jobTitle.IsDeleted    = 0
    LEFT JOIN PayrollAreas      payrollArea ON payrollArea.Id = eoa.PayrollAreaId       AND payrollArea.TenantId = @TenantId AND payrollArea.IsDeleted = 0
    LEFT JOIN EmployeeGroups    grp         ON grp.Id         = eoa.EmployeeGroupId     AND grp.TenantId         = @TenantId AND grp.IsDeleted         = 0
    LEFT JOIN EmployeeSubGroups subGroup    ON subGroup.Id    = eoa.EmployeeSubGroupId  AND subGroup.TenantId    = @TenantId AND subGroup.IsDeleted    = 0
    LEFT JOIN AbpOrganizationUnits orgUnit  ON orgUnit.Id     = eoa.OrganizationUnitId  AND orgUnit.TenantId     = @TenantId AND orgUnit.IsDeleted     = 0

    LEFT JOIN EmployeeBanks empBank
           ON empBank.EmployeeId = emp.Id
          AND empBank.TenantId   = @TenantId
          AND empBank.IsDeleted  = 0
          AND DATEFROMPARTS(@PrmYear, @PrmMonth, 1) BETWEEN empBank.StartDate AND empBank.EndDate
    LEFT JOIN Banks bank ON bank.Id = empBank.BankId AND bank.TenantId = @TenantId AND bank.IsDeleted = 0

    LEFT JOIN SalaryRegisters sr
           ON sr.EmployeeId = emp.Id AND sr.TenantId = @TenantId AND sr.IsDeleted = 0

    LEFT JOIN SalaryRegisterDetails srdPHday
           ON srdPHday.SalaryRegisterId = sr.Id AND srdPHday.TenantId = @TenantId AND srdPHday.IsDeleted = 0
          AND srdPHday.WageTypeId IN (SELECT wt.Id FROM WageTypes wt WHERE wt.Code = '4007' AND wt.TenantId = @TenantId AND wt.IsDeleted = 0)
    LEFT JOIN SalaryRegisterDetails srdBasicPay
           ON srdBasicPay.SalaryRegisterId = sr.Id AND srdBasicPay.TenantId = @TenantId AND srdBasicPay.IsDeleted = 0
          AND srdBasicPay.WageTypeId IN (SELECT wt.Id FROM WageTypes wt WHERE wt.Code = '1000' AND wt.TenantId = @TenantId AND wt.IsDeleted = 0)
    LEFT JOIN SalaryRegisterDetails srdNetPay
           ON srdNetPay.SalaryRegisterId = sr.Id AND srdNetPay.TenantId = @TenantId AND srdNetPay.IsDeleted = 0
          AND srdNetPay.WageTypeId IN (SELECT wt.Id FROM WageTypes wt WHERE wt.Code = '9000' AND wt.TenantId = @TenantId AND wt.IsDeleted = 0)

    OUTER APPLY (
        SELECT TOP 1 cur.CurrencyShortFormAr
        FROM EmployeeBasicPays ebp
        JOIN EmployeeBasicPayDetails ebpd ON ebpd.EmployeeBasicPayId = ebp.Id AND ebpd.TenantId = @TenantId AND ebpd.IsDeleted = 0
        JOIN WageTypes wtBasic ON wtBasic.Id = ebpd.WageTypeId AND wtBasic.Code = '1000' AND wtBasic.TenantId = @TenantId AND wtBasic.IsDeleted = 0
        LEFT JOIN Currencies cur ON cur.Id = ebpd.CurrencyId AND cur.TenantId = @TenantId AND cur.IsDeleted = 0
        WHERE ebp.EmployeeId = emp.Id AND ebp.TenantId = @TenantId AND ebp.IsDeleted = 0
        ORDER BY ebp.StartDate DESC
    ) basicPayCurrency

    LEFT JOIN (
        SELECT ea.EmployeeId
             , MAX(ea.StartDate)       AS StartDate
             , MAX(ea.EndDate)         AS EndDate
             , SUM(ea.TotalLeaveDays)  AS TotalLeaveDays
        FROM EmployeeAbsences ea
        WHERE ea.TenantId = @TenantId AND ea.IsDeleted = 0
          AND ea.AbsenceTypeId IN (SELECT at2.Id FROM AbsenceTypes at2 WHERE at2.IsPaid = 1 AND at2.TenantId = @TenantId AND at2.IsDeleted = 0)
          AND ((DATEPART(Month, ea.StartDate) = @PrmMonth AND DATEPART(Year, ea.StartDate) = @PrmYear)
            OR (DATEPART(Month, ea.EndDate)   = @PrmMonth AND DATEPART(Year, ea.EndDate)   = @PrmYear))
        GROUP BY ea.EmployeeId
    ) PaidLeave ON PaidLeave.EmployeeId = emp.Id

    LEFT JOIN (
        SELECT ea.EmployeeId
             , SUM(ea.TotalLeaveDays)  AS TotalLeaveDays
             , MAX(ea.EndDate)         AS LeaveEndDate
        FROM EmployeeAbsences ea
        WHERE ea.TenantId = @TenantId AND ea.IsDeleted = 0
          AND ea.AbsenceTypeId IN (SELECT at2.Id FROM AbsenceTypes at2 WHERE at2.IsPaid = 0 AND at2.TenantId = @TenantId AND at2.IsDeleted = 0)
          AND ((DATEPART(Month, ea.StartDate) = @PrmMonth AND DATEPART(Year, ea.StartDate) = @PrmYear)
            OR (DATEPART(Month, ea.EndDate)   = @PrmMonth AND DATEPART(Year, ea.EndDate)   = @PrmYear))
        GROUP BY ea.EmployeeId
    ) UnpaidLeave ON UnpaidLeave.EmployeeId = emp.Id

    LEFT JOIN (
        SELECT eaa.EmployeeId, SUM(eaa.QuotaAdjusted) AS QuotaAdjusted
        FROM EmployeeAbsAdjustments eaa
        WHERE eaa.TenantId = @TenantId AND eaa.IsDeleted = 0
          AND eaa.AdjustmentType = 2
          AND eaa.AbsenceTypeId IN (SELECT at2.Id FROM AbsenceTypes at2 WHERE at2.IsPaid = 1 AND at2.TenantId = @TenantId AND at2.IsDeleted = 0)
          AND DATEPART(Month, eaa.EffectiveDate) = @PrmMonth
          AND DATEPART(Year,  eaa.EffectiveDate) = @PrmYear
        GROUP BY eaa.EmployeeId
    ) AdjustedPaidLeave ON AdjustedPaidLeave.EmployeeId = emp.Id

    LEFT JOIN (
        SELECT eaa.EmployeeId, SUM(eaa.QuotaAdjusted) AS QuotaAdjusted
        FROM EmployeeAbsAdjustments eaa
        WHERE eaa.TenantId = @TenantId AND eaa.IsDeleted = 0
          AND eaa.AdjustmentType = 2
          AND eaa.AbsenceTypeId IN (SELECT at2.Id FROM AbsenceTypes at2 WHERE at2.IsPaid = 0 AND at2.TenantId = @TenantId AND at2.IsDeleted = 0)
          AND DATEPART(Month, eaa.EffectiveDate) = @PrmMonth
          AND DATEPART(Year,  eaa.EffectiveDate) = @PrmYear
        GROUP BY eaa.EmployeeId
    ) AdjustedUnpaidLeave ON AdjustedUnpaidLeave.EmployeeId = emp.Id

    WHERE emp.TenantId = @TenantId
      AND emp.IsDeleted = 0
      AND sr.InPeriod = sr.ForPeriod
      AND DATEPART(Month, sr.InPeriod) = @PrmMonth
      AND DATEPART(Year,  sr.InPeriod) = @PrmYear
      AND sr.PayrollRunType = @PrmPayrollRunType
      AND (
            (ISNULL(@PrmEmpCode, '') <> '' AND emp.Code = @PrmEmpCode)
         OR (ISNULL(@PrmEmpCode, '') =  '' AND ISNULL(@PrmPayrollArea, '') <> '' AND payrollArea.Name = @PrmPayrollArea)
          )
) A
ORDER BY
    CASE WHEN ISNUMERIC(A.Code) = 1 THEN RIGHT(REPLICATE('0', 21) + A.Code, 20)
         WHEN ISNUMERIC(A.Code) = 0 THEN LEFT(A.Code + REPLICATE('', 21), 20)
         ELSE A.Code
    END;";

        public const string LeaveSettlementEarningEn = @"-- srptLeaveSettlementEarningEn :: Query (earnings block)
-- MECHANICAL rewrite, except:
--   * legacy ""wagetype.code <> 9000"" compares nvarchar Code to a numeric literal -- now quoted.
--   * legacy ""B.WageType = 1"" in the basic-pay currency sub-select was a RAW LEGACY PRIMARY KEY
--     meaning Code '1000'. See the file header -- it must NOT be carried over as the integer 1.
SELECT
      emp.Code       AS [Employee Code]
    , emp.FullNameEn AS [Employee Name]
    , sr.ForPeriod
    , sr.InPeriod
    , sr.PayrollRunType
    , srd.Amount
    , srd.Number
    , srd.Rate
    , wt.NameEn      AS [Wage Type]
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
  AND emp.Code = @PrmEmpCode
  AND sr.InPeriod = sr.ForPeriod
  AND DATEPART(Month, sr.InPeriod) = @PrmMonth
  AND DATEPART(Year,  sr.InPeriod) = @PrmYear
  AND sr.PayrollRunType = @PrmPayrollRunType;";

        public const string LeaveSettlementEarningAr = @"-- srptLeaveSettlementEarningAr :: Query (earnings block, Arabic)
-- Identical to LeaveSettlementEarningEn with the En -> Ar name columns swapped (the legacy diff).
SELECT
      emp.Code       AS [Employee Code]
    , emp.FullNameAr AS [Employee Name]
    , sr.ForPeriod
    , sr.InPeriod
    , sr.PayrollRunType
    , srd.Amount
    , srd.Number
    , srd.Rate
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
  AND emp.Code = @PrmEmpCode
  AND sr.InPeriod = sr.ForPeriod
  AND DATEPART(Month, sr.InPeriod) = @PrmMonth
  AND DATEPART(Year,  sr.InPeriod) = @PrmYear
  AND sr.PayrollRunType = @PrmPayrollRunType;";

        public const string LeaveSettlementDeductionEn = @"-- srptLeaveSettlementDeductionEn :: Query (deductions block)
-- Same rewrite notes as LeaveSettlementEarningEn; only BillingType flips to 1 (Deduction).
SELECT
      emp.Code       AS [Employee Code]
    , emp.FullNameEn AS [Employee Name]
    , sr.ForPeriod
    , sr.InPeriod
    , sr.PayrollRunType
    , srd.Amount
    , srd.Number
    , srd.Rate
    , wt.NameEn      AS [Wage Type]
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
       AND wt.Code <> '9000'
       AND ISNULL(wt.DontImpactNetPay, 0) = 0
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
  AND sr.PayrollRunType = @PrmPayrollRunType;";

        public const string LeaveSettlementDeductionAr = @"-- srptLeaveSettlementDeductionAr :: Query (deductions block, Arabic)
SELECT
      emp.Code       AS [Employee Code]
    , emp.FullNameAr AS [Employee Name]
    , sr.ForPeriod
    , sr.InPeriod
    , sr.PayrollRunType
    , srd.Amount
    , srd.Number
    , srd.Rate
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
       AND wt.Code <> '9000'
       AND ISNULL(wt.DontImpactNetPay, 0) = 0
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
  AND emp.Code = @PrmEmpCode
  AND sr.InPeriod = sr.ForPeriod
  AND DATEPART(Month, sr.InPeriod) = @PrmMonth
  AND DATEPART(Year,  sr.InPeriod) = @PrmYear
  AND sr.PayrollRunType = @PrmPayrollRunType;";

        public const string LeaveSettlementWorkingDays = @"-- srptLeaveSettlementWorkingDaysSummaryEn :: Query
-- MECHANICAL rewrite. Wage-type codes 4007..4014 ARE business codes and carry over unchanged, but
-- must be quoted now that WageTypes.Code is nvarchar.
SELECT
      emp.Code    AS [Employee Code]
    , sr.ForPeriod
    , sr.InPeriod
    , sr.PayrollRunType
    , srd.Number
    , wt.NameEn   AS [Wage Type]
    , wt.Id       AS oid
    , wt.Code
FROM SalaryRegisters sr
INNER JOIN Employees emp             ON emp.Id = sr.EmployeeId       AND emp.TenantId = @TenantId AND emp.IsDeleted = 0
INNER JOIN SalaryRegisterDetails srd ON srd.SalaryRegisterId = sr.Id AND srd.TenantId = @TenantId AND srd.IsDeleted = 0
INNER JOIN WageTypes wt              ON wt.Id = srd.WageTypeId       AND wt.TenantId  = @TenantId AND wt.IsDeleted  = 0
WHERE sr.TenantId = @TenantId AND sr.IsDeleted = 0
  AND wt.Code IN ('4007', '4008', '4009', '4011', '4014')
  AND emp.Code = @PrmEmpCode
  AND sr.InPeriod = sr.ForPeriod
  AND DATEPART(Month, sr.InPeriod) = @PrmMonth
  AND DATEPART(Year,  sr.InPeriod) = @PrmYear
  AND sr.PayrollRunType = @PrmPayrollRunType;";

        // ===========================================================================================
        // rptLeaveEncashment :: LeaveAccrualQuery
        //
        // NON-MECHANICAL REWRITES (in addition to the DECLARE inlining noted in the file header)
        //   1. Department / Division / Section. Legacy resolved each from OrgLevel1..7 by
        //      OrganizationUnitType (21 aliased joins). HCMv3 has one OrganizationUnitId into the ABP
        //      unit tree with no type concept, so Department gets the employee's own unit and
        //      Division / Section are returned as NULL.  *** SEMANTIC DEVIATION ***
        //   2. CompanyLogo -> NULL (AbpOrganizationUnits has no Logo column).  *** FEATURE LOST ***
        //   3. EmployeeBasicPayDetail.WageType IN (SELECT OID ... Code = '1000') was already a code
        //      look-up here and carries over as Code = '1000'.
        //   4. EmployeeAbsAdjustment.AbsenceType = 1 in the encashment-date sub-select is a RAW
        //      LEGACY PRIMARY KEY meaning the 'Annual Paid Leave' absence type. It is translated to
        //      the NameEn look-up the rest of the query already uses, never to the integer 1.
        //   5. The legacy PayrollArea join was on a string code; HCMv3 uses PayrollAreaId.
        // ===========================================================================================
        public const string LeaveAccrualQuery = @"-- rptLeaveEncashment :: LeaveAccrualQuery
SELECT
      B.*
    , ISNULL((
        SELECT TOP 1
            CONVERT(DECIMAL(10,3),
                ISNULL(acc.TotalAccrual,    0)
              + ISNULL(att.QuotaAdjusted,   0)
              - ISNULL(absTaken.TotalLeaveDays,  0)
              - ISNULL(adj.QuotaAdjusted,   0)) AS Act_Balance
        FROM EmployeeAbsenceQuotas eaq
        INNER JOIN Employees empQ    ON empQ.Id = eaq.EmployeeId   AND empQ.TenantId = @TenantId AND empQ.IsDeleted = 0
        INNER JOIN AbsenceTypes atQ  ON atQ.Id  = eaq.AbsenceTypeId AND atQ.TenantId = @TenantId AND atQ.IsDeleted = 0
        LEFT JOIN (
            SELECT tr.EmployeeId, SUM(tr.AnnualLeaveAccrualPerDay) AS TotalAccrual
            FROM TimeRegisters tr
            INNER JOIN Employees e2 ON e2.Id = tr.EmployeeId AND e2.TenantId = @TenantId AND e2.IsDeleted = 0
            WHERE tr.TenantId = @TenantId AND tr.IsDeleted = 0
              AND tr.DayDate >= (SELECT MIN(tt.TransferDate) FROM TimeTransfers tt WHERE tt.TenantId = @TenantId AND tt.IsDeleted = 0)
              AND tr.DayDate <= B.EncashmentDate
              AND e2.Code = @PrmEmpCode
            GROUP BY tr.EmployeeId
        ) acc ON acc.EmployeeId = eaq.EmployeeId
        LEFT JOIN (
            SELECT ea.EmployeeId, SUM(ea.TotalLeaveDays) AS TotalLeaveDays
            FROM EmployeeAbsences ea
            INNER JOIN Employees e3   ON e3.Id  = ea.EmployeeId    AND e3.TenantId  = @TenantId AND e3.IsDeleted  = 0
            LEFT JOIN AbsenceTypes at3 ON at3.Id = ea.AbsenceTypeId AND at3.TenantId = @TenantId AND at3.IsDeleted = 0
            WHERE ea.TenantId = @TenantId AND ea.IsDeleted = 0
              AND (at3.NameEn IN ('Annual Paid Leave') OR (at3.HaveQuotaAccrual = 1 AND at3.QuotaType = 1))
              AND ea.StartDate >= (SELECT MIN(tt.TransferDate) FROM TimeTransfers tt WHERE tt.TenantId = @TenantId AND tt.IsDeleted = 0)
              AND ea.EndDate <= B.EncashmentDate
              AND e3.Code = @PrmEmpCode
            GROUP BY ea.EmployeeId
        ) absTaken ON absTaken.EmployeeId = eaq.EmployeeId
        LEFT JOIN (
            SELECT eaa.EmployeeId, SUM(eaa.QuotaAdjusted) AS QuotaAdjusted
            FROM EmployeeAbsAdjustments eaa
            INNER JOIN Employees e4    ON e4.Id  = eaa.EmployeeId    AND e4.TenantId  = @TenantId AND e4.IsDeleted  = 0
            LEFT JOIN AbsenceTypes at4 ON at4.Id = eaa.AbsenceTypeId AND at4.TenantId = @TenantId AND at4.IsDeleted = 0
            WHERE eaa.TenantId = @TenantId AND eaa.IsDeleted = 0
              AND at4.NameEn IN ('Annual Paid Leave')
              AND eaa.EffectiveDate < B.EncashmentDate
              AND e4.Code = @PrmEmpCode
              AND eaa.AdjustmentType <> 3   -- AdjustmentType.NoImpactOnQuota
            GROUP BY eaa.EmployeeId
        ) adj ON adj.EmployeeId = eaq.EmployeeId
        LEFT JOIN (
            SELECT eat.EmployeeId, SUM(eat.TotalAttendDays) AS QuotaAdjusted
            FROM EmployeeAttendances eat
            INNER JOIN Employees e5       ON e5.Id  = eat.EmployeeId       AND e5.TenantId   = @TenantId AND e5.IsDeleted   = 0
            LEFT JOIN AttendanceTypes att5 ON att5.Id = eat.AttendanceTypeId AND att5.TenantId = @TenantId AND att5.IsDeleted = 0
            LEFT JOIN AbsenceTypes at5     ON at5.Id  = att5.LinkLeaveQuotaId AND at5.TenantId  = @TenantId AND at5.IsDeleted  = 0
            WHERE eat.TenantId = @TenantId AND eat.IsDeleted = 0
              AND at5.NameEn IN ('Annual Paid Leave')
              AND eat.StartDate <= B.EncashmentDate
              AND e5.Code = @PrmEmpCode
            GROUP BY eat.EmployeeId
        ) att ON att.EmployeeId = eaq.EmployeeId
        WHERE eaq.TenantId = @TenantId AND eaq.IsDeleted = 0
          AND atQ.NameEn IN ('Annual Paid Leave')
          AND empQ.Code = @PrmEmpCode
      ), 0) AS LeaveBalance
FROM (
    SELECT
          A.Code
        , A.FullNameEn
        , A.FullNameAr
        , A.WorkLocationEn
        , A.WorkLocationAr
        , A.CostCenterEn
        , A.CostCenterAr
        , A.JoiningDate
        , A.Department
        , A.Division
        , A.Section
        , A.LastBasicPay
        , A.ForPeriod
        , A.InPeriod
        , A.PayrollRunType
        , A.WageType
        , SUM(A.Rate)   AS Rate
        , SUM(A.Number) AS Number
        , SUM(A.Amount) AS Amount
        , A.PayrollArea
        , A.Currency
        , A.CompanyLogo
        , A.BankEn
        , A.AccountNum
        , A.IBAN
        , A.BankBranch
        , A.EncashmentSalary
        , A.EncashmentDays
        , A.EncashmentPay
        , A.EncashmentDate
        , A.CivilID
        , A.MOSALID
    FROM (
        SELECT
              emp.Code
            , emp.FullNameEn
            , emp.FullNameAr
            , workLoc.NameEn                    AS WorkLocationEn
            , workLoc.NameAr                    AS WorkLocationAr
            , costCentre.NameEn                 AS CostCenterEn
            , costCentre.NameAr                 AS CostCenterAr
            , orgUnit.DisplayName               AS Department       -- see rewrite note 1
            , CAST(NULL AS NVARCHAR(128))       AS Division         -- see rewrite note 1
            , CAST(NULL AS NVARCHAR(128))       AS Section          -- see rewrite note 1
            , emp.JoiningDate
            , ebpd.Amount                       AS LastBasicPay
            , sr.ForPeriod
            , sr.InPeriod
            , sr.PayrollRunType
            , wt.NameEn                         AS WageType
            , srd.Rate
            , srd.Number
            , srd.Amount
            , payrollArea.Name                  AS PayrollArea
            , cur.Code                          AS Currency
            , CAST(NULL AS VARBINARY(MAX))      AS CompanyLogo      -- see rewrite note 2
            , bank.NameEn                       AS BankEn
            , empBank.AccountNum
            , empBank.IBAN
            , empBank.BankBranch
            , (SELECT SUM(srd2.Rate)   FROM SalaryRegisterDetails srd2
               WHERE srd2.SalaryRegisterId = sr.Id AND srd2.TenantId = @TenantId AND srd2.IsDeleted = 0
                 AND srd2.WageTypeId IN (SELECT wt2.Id FROM WageTypes wt2 WHERE wt2.Code IN ('1052') AND wt2.TenantId = @TenantId AND wt2.IsDeleted = 0)) AS EncashmentSalary
            , (SELECT SUM(srd2.Number) FROM SalaryRegisterDetails srd2
               WHERE srd2.SalaryRegisterId = sr.Id AND srd2.TenantId = @TenantId AND srd2.IsDeleted = 0
                 AND srd2.WageTypeId IN (SELECT wt2.Id FROM WageTypes wt2 WHERE wt2.Code IN ('1052') AND wt2.TenantId = @TenantId AND wt2.IsDeleted = 0)) AS EncashmentDays
            , (SELECT SUM(srd2.Amount) FROM SalaryRegisterDetails srd2
               WHERE srd2.SalaryRegisterId = sr.Id AND srd2.TenantId = @TenantId AND srd2.IsDeleted = 0
                 AND srd2.WageTypeId IN (SELECT wt2.Id FROM WageTypes wt2 WHERE wt2.Code IN ('1052') AND wt2.TenantId = @TenantId AND wt2.IsDeleted = 0)) AS EncashmentPay
            , encashDate.EffectiveDate          AS EncashmentDate
            , emp.CivilID
            , sponsor.MOSALID
        FROM Employees emp

        LEFT JOIN EmployeeOrgAssignments eoa
               ON eoa.EmployeeId = emp.Id
              AND eoa.TenantId   = @TenantId
              AND eoa.IsDeleted  = 0
              AND DATEFROMPARTS(@PrmYear, @PrmMonth, 1) BETWEEN eoa.StartDate AND eoa.EndDate
        LEFT JOIN JobTitles     jobTitle    ON jobTitle.Id    = eoa.JobTitleId       AND jobTitle.TenantId    = @TenantId AND jobTitle.IsDeleted    = 0
        LEFT JOIN PayrollAreas  payrollArea ON payrollArea.Id = eoa.PayrollAreaId    AND payrollArea.TenantId = @TenantId AND payrollArea.IsDeleted = 0
        LEFT JOIN WorkLocations workLoc     ON workLoc.Id     = eoa.WorkLocationId   AND workLoc.TenantId     = @TenantId AND workLoc.IsDeleted     = 0
        LEFT JOIN CostCenters   costCentre  ON costCentre.Id  = eoa.CostCenterId     AND costCentre.TenantId  = @TenantId AND costCentre.IsDeleted  = 0
        LEFT JOIN AbpOrganizationUnits orgUnit ON orgUnit.Id  = eoa.OrganizationUnitId AND orgUnit.TenantId   = @TenantId AND orgUnit.IsDeleted     = 0

        LEFT JOIN EmployeeBasicPays ebp
               ON ebp.EmployeeId = emp.Id
              AND ebp.TenantId   = @TenantId
              AND ebp.IsDeleted  = 0
              AND DATEFROMPARTS(@PrmYear, @PrmMonth, 1) BETWEEN ebp.StartDate AND ebp.EndDate
        LEFT JOIN EmployeeBasicPayDetails ebpd
               ON ebpd.EmployeeBasicPayId = ebp.Id
              AND ebpd.TenantId  = @TenantId
              AND ebpd.IsDeleted = 0
              AND ebpd.WageTypeId IN (SELECT wt3.Id FROM WageTypes wt3 WHERE wt3.Code = '1000' AND wt3.TenantId = @TenantId AND wt3.IsDeleted = 0)
        LEFT JOIN Currencies cur ON cur.Id = ebpd.CurrencyId AND cur.TenantId = @TenantId AND cur.IsDeleted = 0

        OUTER APPLY (
            SELECT TOP 1 es.SponsorId
            FROM EmployeeSponsors es
            WHERE es.EmployeeId = emp.Id AND es.TenantId = @TenantId AND es.IsDeleted = 0
              AND DATEFROMPARTS(@PrmYear, @PrmMonth, 1) BETWEEN es.StartDate AND es.EndDate
            ORDER BY es.StartDate DESC
        ) empSponsor
        LEFT JOIN Sponsors sponsor ON sponsor.Id = empSponsor.SponsorId AND sponsor.TenantId = @TenantId AND sponsor.IsDeleted = 0

        OUTER APPLY (
            SELECT TOP 1 eb.BankId, eb.AccountNum, eb.IBAN, eb.BankBranch
            FROM EmployeeBanks eb
            WHERE eb.EmployeeId = emp.Id AND eb.TenantId = @TenantId AND eb.IsDeleted = 0
              AND DATEFROMPARTS(@PrmYear, @PrmMonth, 1) BETWEEN eb.StartDate AND eb.EndDate
            ORDER BY eb.StartDate DESC
        ) empBank
        LEFT JOIN Banks bank ON bank.Id = empBank.BankId AND bank.TenantId = @TenantId AND bank.IsDeleted = 0

        LEFT JOIN SalaryRegisters sr        ON sr.EmployeeId = emp.Id       AND sr.TenantId  = @TenantId AND sr.IsDeleted  = 0
        LEFT JOIN SalaryRegisterDetails srd ON srd.SalaryRegisterId = sr.Id AND srd.TenantId = @TenantId AND srd.IsDeleted = 0
        LEFT JOIN WageTypes wt              ON wt.Id = srd.WageTypeId       AND wt.TenantId  = @TenantId AND wt.IsDeleted  = 0
        LEFT JOIN PayrollReasonTypes reason ON reason.Id = sr.PayrollReasonTypeId AND reason.TenantId = @TenantId AND reason.IsDeleted = 0

        LEFT JOIN (
            SELECT eaa.EmployeeId, MAX(eaa.EffectiveDate) AS EffectiveDate
            FROM EmployeeAbsAdjustments eaa
            LEFT JOIN AbsenceTypes at6 ON at6.Id = eaa.AbsenceTypeId AND at6.TenantId = @TenantId AND at6.IsDeleted = 0
            WHERE eaa.TenantId = @TenantId AND eaa.IsDeleted = 0
              AND (at6.NameEn IN ('Annual Paid Leave') OR at6.QuotaType = 1)   -- see rewrite note 4
              AND eaa.AdjustmentType = 0   -- AdjustmentType.Encashment
              AND DATEPART(Month, eaa.EffectiveDate) = @PrmMonth
              AND DATEPART(Year,  eaa.EffectiveDate) = @PrmYear
            GROUP BY eaa.EmployeeId
        ) encashDate ON encashDate.EmployeeId = emp.Id

        WHERE emp.TenantId = @TenantId
          AND emp.IsDeleted = 0
          AND sr.InPeriod = sr.ForPeriod
          AND DATEPART(Month, sr.InPeriod) = @PrmMonth
          AND DATEPART(Year,  sr.InPeriod) = @PrmYear
          AND reason.NameEn = 'Leave Encashment'
          AND wt.Code IN ('1052')
          AND emp.Code = @PrmEmpCode
    ) A
    GROUP BY
          A.Code
        , A.FullNameEn
        , A.FullNameAr
        , A.WorkLocationEn
        , A.WorkLocationAr
        , A.CostCenterEn
        , A.CostCenterAr
        , A.JoiningDate
        , A.Department
        , A.Division
        , A.Section
        , A.LastBasicPay
        , A.ForPeriod
        , A.InPeriod
        , A.PayrollRunType
        , A.WageType
        , A.PayrollArea
        , A.Currency
        , A.CompanyLogo
        , A.BankEn
        , A.AccountNum
        , A.IBAN
        , A.BankBranch
        , A.EncashmentSalary
        , A.EncashmentDays
        , A.EncashmentPay
        , A.EncashmentDate
        , A.CivilID
        , A.MOSALID
) B
ORDER BY
    CASE WHEN ISNUMERIC(B.Code) = 1 THEN RIGHT(REPLICATE('0', 21) + B.Code, 20)
         WHEN ISNUMERIC(B.Code) = 0 THEN LEFT(B.Code + REPLICATE('', 21), 20)
         ELSE B.Code
    END;";
    }
}
