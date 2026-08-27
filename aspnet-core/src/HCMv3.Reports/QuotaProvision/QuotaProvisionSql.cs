// SQL for the HCMv3.Reports/QuotaProvision family, rewritten for the HCMv3 schema.
//
// MULTI-TENANCY: raw SQL through DevExpress SqlDataSource bypasses EF Core's global tenant filter.
// Every query filters TenantId explicitly via @TenantId (the hidden PrmTenantId report parameter).
//
// ===================================================================================================
// READ THIS BEFORE CHANGING ANY ARITHMETIC IN THIS FILE
// ===================================================================================================
// These four queries implement KUWAIT LABOUR-LAW leave and end-of-service (indemnity) provisioning.
// The slab arithmetic, the /365 and /26 divisors, the DECIMAL scales and the exact set of days that
// are excluded from service are legal calculations, not formatting choices. They are ported
// STATEMENT FOR STATEMENT, including the parts that look redundant. Do not simplify, do not
// "obviously correct" a divisor, and do not add ISNULL() where the legacy has none -- a NULL that
// propagates is often what stops a wrong number being printed.
//
// The recurring shape, in all four:
//     TotalDays        = DATEDIFF(day, StartDate, EndDate) + 1        -- inclusive service span
//     TotalDaysCount   = TotalDays  -  excluded days                  -- qualifying service
//     LastChangeYear   = the previous slab's ChangeYear (0 for the first slab)
//     MaxSlabDays      = ChangeYear * 365
//     RemainingDays    = TotalDaysCount - LastChangeYear * 365
//     SlabWiseDays     = MIN(RemainingDays, MaxSlabDays)
//     SlabWise<X>      = RemainingDays >= MaxSlabDays
//                          ? <X>PerYear * ChangeYear                  -- the slab is full
//                          : RemainingDays * (<X>PerYear / 365)       -- pro-rata within the slab
// One row per (employee, slab); the report sums the slabs.
//
// ===================================================================================================
// SHARED REWRITES
// ===================================================================================================
//   * Table pluralisation, OID -> Id, <Entity> -> <Entity>Id, "AND TenantId = @TenantId
//     AND IsDeleted = 0" on EVERY join and EVERY correlated subquery. The subqueries matter as much
//     as the joins here: an un-scoped SUM(UnpaidLeaveDay) would pull another tenant's absences into
//     a statutory service calculation.
//   * *** PORT DEVIATION (forced): the BEGIN ... END wrapper. *** Two of these queries are wrapped in
//     a bare BEGIN/END block in the legacy .resx. DevExpress's CustomSqlQuery validator rejects
//     anything that is not purely SELECT ("A custom SQL query should contain only SELECT statements"),
//     so the wrapper is removed. It was a no-op -- no local variables were declared inside it (the
//     DECLARE/SET of @AsOnDate is commented out in the source).
//   * *** PORT DEVIATION (forced): EOMONTH(STR(@PrmYear)+'-'+STR(@PrmMonth)+'-'+'01'). ***
//     Written here as EOMONTH(DATEFROMPARTS(@PrmYear, @PrmMonth, 1)). The legacy form round-trips
//     through a string that STR() LEFT-PADS WITH SPACES ('  2024- 6-01') and relies on SQL Server's
//     lenient date parsing; DATEFROMPARTS is the same date with no parsing at all.
//   * *** LEGACY BUG FIXED: the PayrollArea join. *** rptLeaveIndemnityStatement joined
//     PayrollArea.OID = EmployeeOrgAssignment.PayrollArea, which is correct, but the sibling
//     Transaction reports joined on Code. Here it is PayrollAreas.Id = orgAsg.PayrollAreaId.
//   * *** SEMANTIC DEVIATION: OrgLevel1..7 'Company' / 'Department'. *** Collapsed into an ancestor
//     walk over AbpOrganizationUnits using OrgUnitProfiles.OrganizationUnitTypeId. Full note in
//     BankSql.cs. Arabic org names come from OrgUnitProfiles.NameAr.
//   * *** FEATURE LOST: Companylogo. *** No logo column in HCMv3. Returned as NULL.
//   * WageTypes.Code / AbsenceTypes.Code are nvarchar -- every code literal stays QUOTED.
//     Every code used here ('4004','4001','4029','4025','4010','3004','4021','4022','4030','4000'
//     and their 5xxx/6xxx arrear twins, AbsenceTypes '1','2','15') is a BUSINESS CODE in the legacy
//     source too, not a raw legacy primary key -- verified against HCMv3's WageTypes/AbsenceTypes.
//
// ===================================================================================================
// KNOWN HCMv3 DATA GAPS (not porting defects)
// ===================================================================================================
//   * IndemnityPayRules / IndemnityPayRuleDetails are EMPTY -> PayPercentage falls back to the
//     legacy ISNULL default of 100, i.e. no statutory reduction is applied.
//   * EmployeeGratuities is EMPTY -> no gratuity reset is ever detected, IsReset is always 0 and
//     service always runs from the first EmployeeContracts.StartDate.
//   * EmployeeSeparations is EMPTY -> LastDayInService and PeriodOfService render blank in
//     rptIndemnityBreakdown.

namespace HCMv3.Reports.QuotaProvision
{
    /// <summary>Raw SQL used by the HCMv3.Reports.QuotaProvision reports.</summary>
    public static class QuotaProvisionSql
    {
        /// <summary>Payroll-area combo for rptLeaveIndemnityStatement.</summary>
        public const string PayrollAreaList = @"SELECT p.Code
     , p.Name
FROM PayrollAreas p
WHERE p.TenantId = @TenantId
  AND p.IsDeleted = 0
ORDER BY p.Name;";

        // -------------------------------------------------------------------------------------------
        // rptLeaveIndemnityStatement (master) -- the annual-leave provision movement for a month.
        // -------------------------------------------------------------------------------------------
        public const string LeaveIndemnityStatementQuery = @"-- rptLeaveIndemnityStatement :: master query. FINANCIAL OUTPUT -- an annual-leave provision
-- statement. One row per employee for the selected month.
--
-- The twelve SalaryRegisterDetails self-joins each pull ONE wage type out of the month's payroll
-- result. They come in CURRENT / ARREAR pairs (4xxx = current period, 5xxx = arrear, 3004/6004 =
-- the provision totals):
--     4021/5021 Annual Leave Opening Balance      4022/5022 Annual Leave Closing Balance
--     4010/5010 Current Month Accrual             4030/5030 Current Month Adjustment
--     4000/5000 Annual Leave Paid Days            3004/6004 Total Annual Leave Provision
--
-- *** DO NOT ""TIDY"" AL_Closing_2. *** It recomputes the closing balance from the movements:
--     Opening + Accrual - Adjustment - AdjustmentArrear - PaidDays - PaidDaysArrear
-- and is printed BESIDE the payroll engine's own AL_Closing (wage type 4022) so an operator can see
-- the two disagree. Note the legacy deliberately COMMENTS OUT the OpeningArrear and AccrualArrear
-- terms -- those two lines are inert in the source and are kept inert here. Adding them back changes
-- a reconciliation figure.
SELECT emp.Code
     , emp.FullNameEn
     , emp.JoiningDate
     , EOMONTH(DATEFROMPARTS(@PrmYear, @PrmMonth, 1))    AS AsOnDate
     , jobTitle.NameEn                                   AS JobTitleEn
     , jobTitle.NameAr                                   AS JobTitleAr
     , empGroup.NameEn                                   AS EmployeeGroup
     , empSubGroup.NameEn                                AS EmployeeSubGroup
     , basicPay.Amount                                   AS BasicSalary
     , CAST(NULL AS VARBINARY(MAX))                      AS Companylogo    -- FEATURE LOST
     , company.DisplayName                               AS CompanyEn
     , companyProfile.NameAr                             AS CompanyAr
     , department.DisplayName                            AS DepartmentEn
     , departmentProfile.NameAr                          AS DepartmentAr

     , ISNULL(AL_Opening.Number, 0)                      AS AL_Opening
     , ISNULL(AL_OpeningArrear.Number, 0)                AS AL_OpeningArrear
     , ISNULL(AL_CurrMonthAccr.Number, 0)                AS AL_CurrMonthAccr
     , ISNULL(AL_CurrMonthAccrArrear.Number, 0)          AS AL_CurrMonthAccrArrear
     , ISNULL(AL_CurrMonthAdjust.Number, 0)              AS AL_CurrMonthAdjust
     , ISNULL(AL_CurrMonthAdjustArrear.Number, 0)        AS AL_CurrMonthAdjustArrear
     , ISNULL(AL_CurrMonthPaidDays.Number, 0)            AS AL_CurrMonthPaidDays
     , ISNULL(AL_CurrMonthPaidDaysArrear.Number, 0)      AS AL_CurrMonthPaidDaysArrear

     , ISNULL(AL_Closing.Number, 0)                      AS AL_Closing
     , ISNULL(AL_ClosingArrear.Number, 0)                AS AL_ClosingArrear

     -- recomputed closing balance; see the note above. The two commented terms are inert in the
     -- legacy source and stay inert.
     , ISNULL(AL_Opening.Number, 0)
    -- + ISNULL(AL_OpeningArrear.Number, 0)
       + ISNULL(AL_CurrMonthAccr.Number, 0)
    -- + ISNULL(AL_CurrMonthAccrArrear.Number, 0)
       - ISNULL(AL_CurrMonthAdjust.Number, 0)
       - ISNULL(AL_CurrMonthAdjustArrear.Number, 0)
       - ISNULL(AL_CurrMonthPaidDays.Number, 0)
       - ISNULL(AL_CurrMonthPaidDaysArrear.Number, 0)    AS AL_Closing_2

     , ISNULL(AL_TotalProvision.Rate, 0)                 AS AL_TotalProvision_Rate
     , ISNULL(AL_TotalProvision.Number, 0)               AS AL_TotalProvision_Number
     , ISNULL(AL_TotalProvision.Amount, 0)               AS AL_TotalProvision_Amount
     , ISNULL(AL_TotalProvisionArrear.Number, 0)         AS AL_TotalProvisionArrear
     , currency.CurrencyShortFormEn                      AS CurrencyEn
     , ISNULL(subGroupAbsence.TotalDays, 0)              AS TotalDaysAllowed
FROM Employees emp

LEFT JOIN SalaryRegisters sr
       ON sr.EmployeeId = emp.Id AND sr.TenantId = @TenantId AND sr.IsDeleted = 0
      AND sr.ForPeriod = EOMONTH(DATEFROMPARTS(@PrmYear, @PrmMonth, 1))
      AND sr.ForPeriod = sr.InPeriod
      AND sr.PayrollRunType = 0

LEFT JOIN SalaryRegisterDetails AL_Opening                  ON AL_Opening.SalaryRegisterId                  = sr.Id AND AL_Opening.TenantId                  = @TenantId AND AL_Opening.IsDeleted                  = 0 AND AL_Opening.WageTypeId                  IN (SELECT wt.Id FROM WageTypes wt WHERE wt.Code = '4021' AND wt.TenantId = @TenantId AND wt.IsDeleted = 0)
LEFT JOIN SalaryRegisterDetails AL_OpeningArrear            ON AL_OpeningArrear.SalaryRegisterId            = sr.Id AND AL_OpeningArrear.TenantId            = @TenantId AND AL_OpeningArrear.IsDeleted            = 0 AND AL_OpeningArrear.WageTypeId            IN (SELECT wt.Id FROM WageTypes wt WHERE wt.Code = '5021' AND wt.TenantId = @TenantId AND wt.IsDeleted = 0)
LEFT JOIN SalaryRegisterDetails AL_CurrMonthAccr            ON AL_CurrMonthAccr.SalaryRegisterId            = sr.Id AND AL_CurrMonthAccr.TenantId            = @TenantId AND AL_CurrMonthAccr.IsDeleted            = 0 AND AL_CurrMonthAccr.WageTypeId            IN (SELECT wt.Id FROM WageTypes wt WHERE wt.Code = '4010' AND wt.TenantId = @TenantId AND wt.IsDeleted = 0)
LEFT JOIN SalaryRegisterDetails AL_CurrMonthAccrArrear      ON AL_CurrMonthAccrArrear.SalaryRegisterId      = sr.Id AND AL_CurrMonthAccrArrear.TenantId      = @TenantId AND AL_CurrMonthAccrArrear.IsDeleted      = 0 AND AL_CurrMonthAccrArrear.WageTypeId      IN (SELECT wt.Id FROM WageTypes wt WHERE wt.Code = '5010' AND wt.TenantId = @TenantId AND wt.IsDeleted = 0)
LEFT JOIN SalaryRegisterDetails AL_CurrMonthPaidDays        ON AL_CurrMonthPaidDays.SalaryRegisterId        = sr.Id AND AL_CurrMonthPaidDays.TenantId        = @TenantId AND AL_CurrMonthPaidDays.IsDeleted        = 0 AND AL_CurrMonthPaidDays.WageTypeId        IN (SELECT wt.Id FROM WageTypes wt WHERE wt.Code = '4000' AND wt.TenantId = @TenantId AND wt.IsDeleted = 0)
LEFT JOIN SalaryRegisterDetails AL_CurrMonthPaidDaysArrear  ON AL_CurrMonthPaidDaysArrear.SalaryRegisterId  = sr.Id AND AL_CurrMonthPaidDaysArrear.TenantId  = @TenantId AND AL_CurrMonthPaidDaysArrear.IsDeleted  = 0 AND AL_CurrMonthPaidDaysArrear.WageTypeId  IN (SELECT wt.Id FROM WageTypes wt WHERE wt.Code = '5000' AND wt.TenantId = @TenantId AND wt.IsDeleted = 0)
LEFT JOIN SalaryRegisterDetails AL_CurrMonthAdjust          ON AL_CurrMonthAdjust.SalaryRegisterId          = sr.Id AND AL_CurrMonthAdjust.TenantId          = @TenantId AND AL_CurrMonthAdjust.IsDeleted          = 0 AND AL_CurrMonthAdjust.WageTypeId          IN (SELECT wt.Id FROM WageTypes wt WHERE wt.Code = '4030' AND wt.TenantId = @TenantId AND wt.IsDeleted = 0)
LEFT JOIN SalaryRegisterDetails AL_CurrMonthAdjustArrear    ON AL_CurrMonthAdjustArrear.SalaryRegisterId    = sr.Id AND AL_CurrMonthAdjustArrear.TenantId    = @TenantId AND AL_CurrMonthAdjustArrear.IsDeleted    = 0 AND AL_CurrMonthAdjustArrear.WageTypeId    IN (SELECT wt.Id FROM WageTypes wt WHERE wt.Code = '5030' AND wt.TenantId = @TenantId AND wt.IsDeleted = 0)
LEFT JOIN SalaryRegisterDetails AL_Closing                  ON AL_Closing.SalaryRegisterId                  = sr.Id AND AL_Closing.TenantId                  = @TenantId AND AL_Closing.IsDeleted                  = 0 AND AL_Closing.WageTypeId                  IN (SELECT wt.Id FROM WageTypes wt WHERE wt.Code = '4022' AND wt.TenantId = @TenantId AND wt.IsDeleted = 0)
LEFT JOIN SalaryRegisterDetails AL_ClosingArrear            ON AL_ClosingArrear.SalaryRegisterId            = sr.Id AND AL_ClosingArrear.TenantId            = @TenantId AND AL_ClosingArrear.IsDeleted            = 0 AND AL_ClosingArrear.WageTypeId            IN (SELECT wt.Id FROM WageTypes wt WHERE wt.Code = '5022' AND wt.TenantId = @TenantId AND wt.IsDeleted = 0)
LEFT JOIN SalaryRegisterDetails AL_TotalProvision           ON AL_TotalProvision.SalaryRegisterId           = sr.Id AND AL_TotalProvision.TenantId           = @TenantId AND AL_TotalProvision.IsDeleted           = 0 AND AL_TotalProvision.WageTypeId           IN (SELECT wt.Id FROM WageTypes wt WHERE wt.Code = '3004' AND wt.TenantId = @TenantId AND wt.IsDeleted = 0)
LEFT JOIN SalaryRegisterDetails AL_TotalProvisionArrear     ON AL_TotalProvisionArrear.SalaryRegisterId     = sr.Id AND AL_TotalProvisionArrear.TenantId     = @TenantId AND AL_TotalProvisionArrear.IsDeleted     = 0 AND AL_TotalProvisionArrear.WageTypeId     IN (SELECT wt.Id FROM WageTypes wt WHERE wt.Code = '6004' AND wt.TenantId = @TenantId AND wt.IsDeleted = 0)

LEFT JOIN EmployeeOrgAssignments orgAsg
       ON orgAsg.EmployeeId = emp.Id AND orgAsg.TenantId = @TenantId AND orgAsg.IsDeleted = 0
      AND orgAsg.StartDate <= EOMONTH(DATEFROMPARTS(@PrmYear, @PrmMonth, 1))
      AND orgAsg.EndDate   >= EOMONTH(DATEFROMPARTS(@PrmYear, @PrmMonth, 1))
LEFT JOIN JobTitles jobTitle
       ON jobTitle.Id = orgAsg.JobTitleId AND jobTitle.TenantId = @TenantId AND jobTitle.IsDeleted = 0
LEFT JOIN PayrollAreas payrollArea
       ON payrollArea.Id = orgAsg.PayrollAreaId AND payrollArea.TenantId = @TenantId AND payrollArea.IsDeleted = 0
LEFT JOIN EmployeeGroups empGroup
       ON empGroup.Id = orgAsg.EmployeeGroupId AND empGroup.TenantId = @TenantId AND empGroup.IsDeleted = 0
LEFT JOIN EmployeeSubGroups empSubGroup
       ON empSubGroup.Id = orgAsg.EmployeeSubGroupId AND empSubGroup.TenantId = @TenantId AND empSubGroup.IsDeleted = 0

-- the sub-group's annual-leave entitlement (AbsenceTypes Code '1' = Annual Paid Leave)
LEFT JOIN EmployeeSubGroupAbsences subGroupAbsence
       ON subGroupAbsence.EmployeeSubGroupId = empSubGroup.Id
      AND subGroupAbsence.TenantId = @TenantId AND subGroupAbsence.IsDeleted = 0
      AND subGroupAbsence.AbsenceTypeId IN (SELECT at.Id FROM AbsenceTypes at
                                            WHERE at.Code = '1' AND at.TenantId = @TenantId AND at.IsDeleted = 0)

LEFT JOIN AbpOrganizationUnits ou
       ON ou.Id = orgAsg.OrganizationUnitId AND ou.TenantId = @TenantId AND ou.IsDeleted = 0
OUTER APPLY (
    SELECT TOP 1 anc.Id, anc.DisplayName
    FROM AbpOrganizationUnits anc
    INNER JOIN OrgUnitProfiles ancp       ON ancp.OrganizationUnitId = anc.Id      AND ancp.TenantId = @TenantId AND ancp.IsDeleted = 0
    INNER JOIN OrganizationUnitTypes anct ON anct.Id = ancp.OrganizationUnitTypeId AND anct.TenantId = @TenantId AND anct.IsDeleted = 0
    WHERE anc.TenantId = @TenantId AND anc.IsDeleted = 0 AND ou.Code LIKE anc.Code + '%' AND anct.NameEn = 'Company'
    ORDER BY LEN(anc.Code)
) company
LEFT JOIN OrgUnitProfiles companyProfile
       ON companyProfile.OrganizationUnitId = company.Id AND companyProfile.TenantId = @TenantId AND companyProfile.IsDeleted = 0
OUTER APPLY (
    SELECT TOP 1 anc.Id, anc.DisplayName
    FROM AbpOrganizationUnits anc
    INNER JOIN OrgUnitProfiles ancp       ON ancp.OrganizationUnitId = anc.Id      AND ancp.TenantId = @TenantId AND ancp.IsDeleted = 0
    INNER JOIN OrganizationUnitTypes anct ON anct.Id = ancp.OrganizationUnitTypeId AND anct.TenantId = @TenantId AND anct.IsDeleted = 0
    WHERE anc.TenantId = @TenantId AND anc.IsDeleted = 0 AND ou.Code LIKE anc.Code + '%' AND anct.NameEn = 'Department'
    ORDER BY LEN(anc.Code)
) department
LEFT JOIN OrgUnitProfiles departmentProfile
       ON departmentProfile.OrganizationUnitId = department.Id AND departmentProfile.TenantId = @TenantId AND departmentProfile.IsDeleted = 0

LEFT JOIN EmployeeBasicPays basicPay
       ON basicPay.EmployeeId = emp.Id AND basicPay.TenantId = @TenantId AND basicPay.IsDeleted = 0
      AND basicPay.StartDate <= EOMONTH(DATEFROMPARTS(@PrmYear, @PrmMonth, 1))
      AND basicPay.EndDate   >= EOMONTH(DATEFROMPARTS(@PrmYear, @PrmMonth, 1))

-- the indemnity-bearing part of the salary: every component whose wage type opts into indemnity
LEFT JOIN (
    SELECT bpd.EmployeeBasicPayId
         , bpd.CurrencyId
         , SUM(bpd.Amount) AS IndemnityRate
    FROM EmployeeBasicPayDetails bpd
    LEFT JOIN WageTypes wt ON wt.Id = bpd.WageTypeId AND wt.TenantId = @TenantId AND wt.IsDeleted = 0
    WHERE bpd.TenantId = @TenantId AND bpd.IsDeleted = 0
      AND wt.IncludeInIndemnity = 1
    GROUP BY bpd.EmployeeBasicPayId, bpd.CurrencyId
) basicPayDetail ON basicPayDetail.EmployeeBasicPayId = basicPay.Id
LEFT JOIN Currencies currency
       ON currency.Id = basicPayDetail.CurrencyId AND currency.TenantId = @TenantId AND currency.IsDeleted = 0

WHERE emp.TenantId = @TenantId
  AND emp.IsDeleted = 0
  AND ((ISNULL(@PrmEmpCode, '')     <> '' AND emp.Code         = @PrmEmpCode)     OR ISNULL(@PrmEmpCode, '')     = '')
  AND ((ISNULL(@PrmPayrollArea, '') <> '' AND payrollArea.Name = @PrmPayrollArea) OR ISNULL(@PrmPayrollArea, '') = '')

ORDER BY CASE WHEN ISNUMERIC(emp.Code) = 1 THEN RIGHT(REPLICATE('0', 21) + emp.Code, 20)
              WHEN ISNUMERIC(emp.Code) = 0 THEN LEFT(emp.Code + REPLICATE('', 21), 20)
              ELSE emp.Code
         END;";

        // -------------------------------------------------------------------------------------------
        // srptLeaveStatement (subreport) -- annual-leave entitlement by service slab.
        // -------------------------------------------------------------------------------------------
        public const string LeaveStatementQuery = @"-- srptLeaveStatement :: subreport query. One row per (employee, annual-leave entitlement slab).
--
-- SERVICE SPAN: from the employee's FIRST contract start to the earlier of the last contract end and
-- the end of the selected month. Unlike the indemnity statement there is no gratuity-reset concept
-- here, so IsReset is the constant 0.
--
-- EXCLUDED DAYS: only days on which no annual leave accrued -- TimeRegisters rows with
-- AnnualLeaveAccrualPerDay = 0. NOTE the legacy counts them with COUNT(AnnualLeaveAccrualPerDay),
-- not SUM: it is a count of non-accruing DAYS, and COUNT skips NULLs, so a day with a NULL accrual
-- is NOT excluded. Preserved -- switching to SUM or COUNT(*) changes qualifying service.
--
-- BasisDays is the constant 0 in this query, so DaysDivFactor always falls through to FixedDays
-- (wage type '4010' Annual Leave Current Month Accrual, FixedDays = 26). The CASE is kept because
-- the layout binds to DaysDivFactor.
--
-- DEAD COLUMNS PRESERVED: totalQuotaAdjustedDays is computed in the innermost SELECT and never
-- referenced by any outer level. Kept so the two subreports stay diffable against each other.
SELECT C.OID
     , C.Code
     , C.FullNameEn
     , C.EmployeeGroup
     , C.EmployeeSubGroup
     , C.CurrencyEn
     , C.IsReset
     , C.StartDate
     , C.EndDate
     , C.ALPaidDays
     , C.ALPayRate
     , C.FixedDays
     , C.BasisDays
     , CASE WHEN C.BasisDays <> 0 THEN C.BasisDays ELSE C.FixedDays END AS DaysDivFactor
     , C.TotalDays
     , C.TotalAbsentDay
     , C.TotalDaysCount
     , C.LastChangeYear
     , (C.LastChangeYear * 365)                                        AS LastUsedDays
     , C.TotalDaysCount - ISNULL((C.LastChangeYear * 365), 0)          AS RemainingDays
     , C.MaxSlabDays
     , C.ChangeYear
     , CASE WHEN (C.TotalDaysCount - ISNULL((C.LastChangeYear * 365), 0)) >= C.MaxSlabDays
            THEN C.MaxSlabDays
            ELSE (C.TotalDaysCount - ISNULL((C.LastChangeYear * 365), 0))
       END                                                             AS SlabWiseDays
     , C.DivisionFactor
     , C.ALDays
     , CONVERT(DECIMAL(10, 6),
         CASE WHEN (C.TotalDaysCount - ISNULL((C.LastChangeYear * 365), 0)) >= C.MaxSlabDays
              THEN C.ALDays * C.ChangeYear
              ELSE ((C.TotalDaysCount - ISNULL((C.LastChangeYear * 365), 0)) * (C.ALDays / 365))
         END)                                                          AS SlabWiseALDays
FROM (
    SELECT emp.Id                                   AS OID
         , emp.Code
         , emp.FullNameEn
         , empGroup.NameEn                          AS EmployeeGroup
         , empSubGroup.NameEn                       AS EmployeeSubGroup
         , currency.CurrencyShortFormEn             AS CurrencyEn
         , B.IsReset
         , B.StartDate
         , B.EndDate
         , B.ALPaidDays
         , basicPayDetail.ALPayRate
         , B.FixedDays
         , 0                                        AS BasisDays
         , B.TotalDays
         , B.TotalAbsentDay
         , B.TotalDaysCount
         -- the ChangeYear of the slab immediately below this one (0 for the first slab)
         , ISNULL((SELECT TOP 1 prevSlab.ChangeYear
                   FROM EmployeeSubGroupAbsences prevSlab
                   WHERE prevSlab.EmployeeSubGroupId = empSubGroup.Id
                     AND prevSlab.TenantId = @TenantId AND prevSlab.IsDeleted = 0
                     AND prevSlab.ChangeYear < subGroupAbsence.ChangeYear
                   ORDER BY prevSlab.ChangeYear DESC), 0) AS LastChangeYear
         -- ChangeYear 0 means 'the final, open-ended slab'; the legacy encodes that as 99 years
         , CASE WHEN ISNULL(subGroupAbsence.ChangeYear, 0) = 0 THEN 99
                ELSE ISNULL(subGroupAbsence.ChangeYear, 0) END AS ChangeYear
         , CONVERT(DECIMAL(10, 6), subGroupAbsence.TotalDays) AS ALDays
         , 365                                      AS DivisionFactor
         , (CASE WHEN ISNULL(subGroupAbsence.ChangeYear, 0) = 0 THEN 99
                 ELSE ISNULL(subGroupAbsence.ChangeYear, 0) END * 365) AS MaxSlabDays
    FROM (
        SELECT A.EmployeeId
             , A.IsReset
             , A.StartDate
             , A.EndDate
             , A.TotalDays
             , A.FixedDays
             , A.totalUnpaidDays                    AS TotalAbsentDay
             , A.TotalDays - A.totalUnpaidDays      AS TotalDaysCount
             , A.ALPaidDays
        FROM (
            SELECT emp.Id                           AS EmployeeId
                 , ALStartTbl.IsReset
                 , ALStartTbl.StartDate
                 , ALStartTbl.EndDate
                 , DATEDIFF(day, ALStartTbl.StartDate, ALStartTbl.EndDate) + 1 AS TotalDays
                 , ISNULL((SELECT TOP 1 ISNULL(wt.FixedDays, 0) FROM WageTypes wt
                           WHERE wt.Code = '4010' AND wt.TenantId = @TenantId AND wt.IsDeleted = 0), 0) AS FixedDays

                 -- days on which NO annual leave accrued. COUNT, not SUM -- see the header note.
                 , ISNULL((SELECT ISNULL(COUNT(tr.AnnualLeaveAccrualPerDay), 0)
                           FROM TimeRegisters tr
                           WHERE tr.EmployeeId = emp.Id AND tr.TenantId = @TenantId AND tr.IsDeleted = 0
                             AND tr.DayDate >= ALStartTbl.StartDate
                             AND tr.DayDate <= ALStartTbl.EndDate
                             AND tr.AnnualLeaveAccrualPerDay = 0), 0) AS totalUnpaidDays

                 -- computed and never used by any outer level; kept, see the header note
                 , ISNULL((SELECT ISNULL(SUM(adj.QuotaAdjusted), 0)
                           FROM EmployeeAbsAdjustments adj
                           WHERE adj.EmployeeId = emp.Id AND adj.TenantId = @TenantId AND adj.IsDeleted = 0
                             AND adj.EffectiveDate >= ALStartTbl.StartDate
                             AND adj.EffectiveDate <= ALStartTbl.EndDate
                             AND adj.AbsenceTypeId IS NOT NULL
                             AND adj.AbsenceTypeId IN (SELECT at.Id FROM AbsenceTypes at
                                                       WHERE at.Code = '2' AND at.TenantId = @TenantId AND at.IsDeleted = 0)), 0) AS totalQuotaAdjustedDays

                 , ISNULL((SELECT ISNULL(SUM(adj.QuotaAdjusted), 0)
                           FROM EmployeeAbsAdjustments adj
                           WHERE adj.EmployeeId = emp.Id AND adj.TenantId = @TenantId AND adj.IsDeleted = 0
                             AND adj.EffectiveDate >= ALStartTbl.StartDate
                             AND adj.EffectiveDate <= ALStartTbl.EndDate
                             AND adj.AbsenceTypeId IN (SELECT at.Id FROM AbsenceTypes at
                                                       WHERE at.Code = '1' AND at.TenantId = @TenantId AND at.IsDeleted = 0)), 0) AS ALPaidDays
            FROM Employees emp
            LEFT JOIN (
                SELECT e2.Id AS EmployeeId
                     , 0     AS IsReset
                     , contract.StartDate
                     , CASE WHEN contract.EndDate >= EOMONTH(DATEFROMPARTS(@PrmYear, @PrmMonth, 1))
                            THEN EOMONTH(DATEFROMPARTS(@PrmYear, @PrmMonth, 1))
                            ELSE contract.EndDate END AS EndDate
                FROM Employees e2
                LEFT JOIN (
                    SELECT ec.EmployeeId
                         , MIN(ec.StartDate) AS StartDate
                         , MAX(ec.EndDate)   AS EndDate
                    FROM EmployeeContracts ec
                    WHERE ec.TenantId = @TenantId AND ec.IsDeleted = 0
                    GROUP BY ec.EmployeeId
                ) contract ON contract.EmployeeId = e2.Id
                WHERE e2.TenantId = @TenantId AND e2.IsDeleted = 0
            ) ALStartTbl ON ALStartTbl.EmployeeId = emp.Id
            WHERE emp.TenantId = @TenantId AND emp.IsDeleted = 0
        ) A
    ) B

    LEFT JOIN Employees emp ON emp.Id = B.EmployeeId AND emp.TenantId = @TenantId AND emp.IsDeleted = 0

    -- joined for its side effect on row multiplicity only; nothing from it is projected. Preserved
    -- because dropping it would change the row count when a period has more than one 4025 row.
    LEFT JOIN SalaryRegisters sr
           ON sr.EmployeeId = emp.Id AND sr.TenantId = @TenantId AND sr.IsDeleted = 0
          AND sr.ForPeriod = EOMONTH(DATEFROMPARTS(@PrmYear, @PrmMonth, 1))
          AND sr.ForPeriod = sr.InPeriod
          AND sr.PayrollRunType = 0
    LEFT JOIN SalaryRegisterDetails srd
           ON srd.SalaryRegisterId = sr.Id AND srd.TenantId = @TenantId AND srd.IsDeleted = 0
          AND srd.WageTypeId IN (SELECT wt.Id FROM WageTypes wt
                                 WHERE wt.Code = '4025' AND wt.TenantId = @TenantId AND wt.IsDeleted = 0)

    LEFT JOIN EmployeeOrgAssignments orgAsg
           ON orgAsg.EmployeeId = emp.Id AND orgAsg.TenantId = @TenantId AND orgAsg.IsDeleted = 0
          AND orgAsg.StartDate <= EOMONTH(DATEFROMPARTS(@PrmYear, @PrmMonth, 1))
          AND orgAsg.EndDate   >= EOMONTH(DATEFROMPARTS(@PrmYear, @PrmMonth, 1))
    LEFT JOIN EmployeeGroups empGroup
           ON empGroup.Id = orgAsg.EmployeeGroupId AND empGroup.TenantId = @TenantId AND empGroup.IsDeleted = 0
    LEFT JOIN EmployeeSubGroups empSubGroup
           ON empSubGroup.Id = orgAsg.EmployeeSubGroupId AND empSubGroup.TenantId = @TenantId AND empSubGroup.IsDeleted = 0
    -- one row per entitlement slab -- this is what fans the query out to one row per slab
    LEFT JOIN EmployeeSubGroupAbsences subGroupAbsence
           ON subGroupAbsence.EmployeeSubGroupId = empSubGroup.Id
          AND subGroupAbsence.TenantId = @TenantId AND subGroupAbsence.IsDeleted = 0

    LEFT JOIN EmployeeBasicPays basicPay
           ON basicPay.EmployeeId = emp.Id AND basicPay.TenantId = @TenantId AND basicPay.IsDeleted = 0
          AND basicPay.StartDate <= EOMONTH(DATEFROMPARTS(@PrmYear, @PrmMonth, 1))
          AND basicPay.EndDate   >= EOMONTH(DATEFROMPARTS(@PrmYear, @PrmMonth, 1))

    -- the annual-leave-bearing part of the salary: wage types linked to AbsenceTypes Code '1'
    LEFT JOIN (
        SELECT bpd.EmployeeBasicPayId
             , bpd.CurrencyId
             , SUM(bpd.Amount) AS ALPayRate
        FROM EmployeeBasicPayDetails bpd
        LEFT JOIN WageTypes wt ON wt.Id = bpd.WageTypeId AND wt.TenantId = @TenantId AND wt.IsDeleted = 0
        WHERE bpd.TenantId = @TenantId AND bpd.IsDeleted = 0
          AND wt.Id IN (SELECT wtl.WageTypeId FROM WageTypeLeaves wtl
                        WHERE wtl.TenantId = @TenantId AND wtl.IsDeleted = 0
                          AND wtl.AbsenceTypeId IN (SELECT at.Id FROM AbsenceTypes at
                                                    WHERE at.Code = '1' AND at.TenantId = @TenantId AND at.IsDeleted = 0))
        GROUP BY bpd.EmployeeBasicPayId, bpd.CurrencyId
    ) basicPayDetail ON basicPayDetail.EmployeeBasicPayId = basicPay.Id
    LEFT JOIN Currencies currency
           ON currency.Id = basicPayDetail.CurrencyId AND currency.TenantId = @TenantId AND currency.IsDeleted = 0

    WHERE subGroupAbsence.AbsenceTypeId IN (SELECT at.Id FROM AbsenceTypes at
                                            WHERE at.Code = '1' AND at.TenantId = @TenantId AND at.IsDeleted = 0)
      AND ((ISNULL(@PrmEmpCode, '') <> '' AND emp.Code = @PrmEmpCode) OR ISNULL(@PrmEmpCode, '') = '')
) C
-- a slab the employee has not reached yet produces a negative remainder and is dropped
WHERE C.TotalDaysCount - ISNULL((C.LastChangeYear * 365), 0) >= 0
ORDER BY C.OID, C.ChangeYear;";

        // -------------------------------------------------------------------------------------------
        // srptIndemnityStatement (subreport) -- end-of-service gratuity by service slab.
        // -------------------------------------------------------------------------------------------
        public const string IndemnityStatementQuery = @"-- srptIndemnityStatement :: subreport query. FINANCIAL OUTPUT -- Kuwait end-of-service indemnity.
-- One row per (employee, gratuity slab).
--
-- SERVICE SPAN, and why IsReset exists: service normally runs from the FIRST contract start. But if
-- the employee has an EmployeeGratuities row with ResetGratuityCalc = 1 dated on or before the
-- as-on date, and that reset is LATER than the contract start, service restarts at the reset date --
-- the gratuity has already been paid out up to there. IsReset flags that so the report can say so.
--
-- EXCLUDED DAYS -- the statutory part. Two independent switches, each read from a WAGE TYPE's
-- IncludeInIndemnity flag rather than from a setting:
--     UnpaidInclude  <- wage type '4001' (Absent Unpaid Days) has IncludeInIndemnity = 1
--                       -> subtract unpaid leave + unapproved leave + quota adjustments
--     AbsentInclude  <- wage type '4029' (Absent Days) has IncludeInIndemnity = 1
--                       -> subtract absent days
--     totalOpening   <- always subtracted: the migrated opening balance of absent/unpaid days,
--                       carried as TimeTransfers of type 'Absent Unpaid Days Opening Balance'
-- The names read backwards -- ""Include"" here means ""include in the DEDUCTION"" -- but that is the
-- legacy semantics and the flag names are the wage types', not this query's.
--
-- TWO SPANS ARE COMPUTED, and they are not interchangeable:
--     TotalDaysCount    -- service to the END OF THE SELECTED MONTH. Drives the printed slabs.
--     EOSTotalDaysCount -- service to @PrmLastDayInService, i.e. a hypothetical leaving date.
--                          Drives SuggestedTotalIndemnityDays only.
--
-- SuggestedTotalIndemnityDays is a WINDOW SUM over all of the employee's slabs, then scaled by
-- MAX(PayPercentage) OVER the same partition -- the statutory reduction for resignation vs
-- termination, from IndemnityPayRuleDetails, selected by years of service and @PrmSeparationType.
-- The percentage is per EMPLOYEE, not per slab, hence MAX(...) OVER rather than a per-row multiply.
-- DATA GAP: IndemnityPayRules is EMPTY in HCMv3 -> ISNULL(...,100) -> no reduction applied.
SELECT C.OID
     , C.Code
     , C.FullNameEn
     , C.EmployeeGroup
     , C.EmployeeSubGroup
     , C.EmployeeSubGroupOid
     , C.CurrencyEn
     , C.IsReset
     , C.StartDate
     , C.EndDate
     , C.IndemnityPaidDays
     , C.IndemnityRate
     , C.FixedDays
     , C.BasisDays
     , CASE WHEN C.BasisDays <> 0 THEN C.BasisDays ELSE C.FixedDays END AS DaysDivFactor
     , CASE WHEN (CASE WHEN C.BasisDays <> 0 THEN C.BasisDays ELSE C.FixedDays END) <> 0
            THEN C.IndemnityRate / (CASE WHEN C.BasisDays <> 0 THEN C.BasisDays ELSE C.FixedDays END)
            ELSE 0 END                                                 AS PerDayRate
     , C.TotalDays
     , C.TotalAbsentDay
     , C.TotalDaysCount
     , C.EOSTotalDaysCount
     , (C.TotalDaysCount / 365.0)                                      AS TotalYears
     , C.LastChangeYear
     , (C.LastChangeYear * 365)                                        AS LastUsedDays
     , C.TotalDaysCount - ISNULL((C.LastChangeYear * 365), 0)          AS RemainingDays
     , C.MaxSlabDays
     , C.ChangeYear
     , CASE WHEN C.LastChangeYear = 0 THEN 'First ' + CONVERT(VARCHAR(10), C.ChangeYear)     + ' Years'
            ELSE                           'Above ' + CONVERT(VARCHAR(10), C.LastChangeYear) + ' Years'
       END                                                             AS ChangeYearText
     , CASE WHEN (C.TotalDaysCount - ISNULL((C.LastChangeYear * 365), 0)) >= C.MaxSlabDays
            THEN C.MaxSlabDays
            ELSE (C.TotalDaysCount - ISNULL((C.LastChangeYear * 365), 0))
       END                                                             AS SlabWiseDays
     , C.DivisionFactor
     , C.GratuityDays
     , CONVERT(DECIMAL(10, 6),
         CASE WHEN (C.TotalDaysCount - ISNULL((C.LastChangeYear * 365), 0)) >= C.MaxSlabDays
              THEN C.GratuityDays * C.ChangeYear
              ELSE (C.TotalDaysCount - ISNULL((C.LastChangeYear * 365), 0)) * (C.GratuityDays / 365)
         END)                                                          AS SlabWiseIndemnity

     -- per-employee total over every slab, at the hypothetical leaving date, then the statutory
     -- percentage. DECIMAL(10,3) per slab BEFORE the sum -- rounding each slab then adding is not
     -- the same as adding then rounding, and this is the order the legacy uses.
     , SUM(CONVERT(DECIMAL(10, 3),
             CASE WHEN (C.EOSTotalDaysCount - ISNULL((C.LastChangeYear * 365), 0)) >= C.MaxSlabDays
                  THEN C.GratuityDays * C.ChangeYear
                  ELSE (C.EOSTotalDaysCount - ISNULL((C.LastChangeYear * 365), 0)) * (C.GratuityDays / 365)
             END)) OVER (PARTITION BY C.OID)
       * (MAX(C.PayPercentage) OVER (PARTITION BY C.OID) / 100.0)      AS SuggestedTotalIndemnityDays
     , C.IndemnityPaidAmount
FROM (
    SELECT emp.Id                                   AS OID
         , emp.Code
         , emp.FullNameEn
         , empGroup.NameEn                          AS EmployeeGroup
         , empSubGroup.NameEn                       AS EmployeeSubGroup
         , empSubGroup.Id                           AS EmployeeSubGroupOid
         , currency.CurrencyShortFormEn             AS CurrencyEn
         , B.IsReset
         , B.StartDate
         , B.EndDate
         , B.IndemnityPaidDays
         , basicPayDetail.IndemnityRate
         , B.FixedDays
         , subGroupGratuity.BasisDays
         , B.TotalDays
         , B.TotalAbsentDay
         , B.TotalDaysCount
         , B.EOSTotalDaysCount
         , ISNULL((SELECT TOP 1 prevSlab.ChangeYear
                   FROM EmployeeSubGroupGratuities prevSlab
                   WHERE prevSlab.EmployeeSubGroupId = empSubGroup.Id
                     AND prevSlab.TenantId = @TenantId AND prevSlab.IsDeleted = 0
                     AND prevSlab.ChangeYear < subGroupGratuity.ChangeYear
                   ORDER BY prevSlab.ChangeYear DESC), 0) AS LastChangeYear
         , subGroupGratuity.ChangeYear
         , subGroupGratuity.GratuityDays
         , 365                                      AS DivisionFactor
         , (subGroupGratuity.ChangeYear * 365)      AS MaxSlabDays
         , ISNULL(indemnityPaid.IndemnityPaidAmount, 0) AS IndemnityPaidAmount
         , ISNULL(payRule.PayPercentage, 100)       AS PayPercentage
         , indemnityPayRule.MaxNumber
    FROM (
        SELECT A.EmployeeId
             , A.IsReset
             , A.StartDate
             , A.EndDate
             , DATEDIFF(day, A.StartDate, @PrmLastDayInService) + 1 AS EOSTotalDays
             , A.TotalDays
             , A.FixedDays
             , A.totalOpening
               + CASE WHEN A.UnpaidInclude = 1 THEN (A.totalUnpaidDays + A.totalUnapprovedLeaveDays + A.totalQuotaAdjustedDays) ELSE 0 END
               + CASE WHEN A.AbsentInclude = 1 THEN (A.totalAbsentDays) ELSE 0 END AS TotalAbsentDay

             , A.TotalDays
               - (A.totalOpening
                  + CASE WHEN A.UnpaidInclude = 1 THEN (A.totalUnpaidDays + A.totalUnapprovedLeaveDays + A.totalQuotaAdjustedDays) ELSE 0 END
                  + CASE WHEN A.AbsentInclude = 1 THEN (A.totalAbsentDays) ELSE 0 END) AS TotalDaysCount

             , DATEDIFF(day, A.StartDate, @PrmLastDayInService) + 1
               - (A.totalOpening
                  + CASE WHEN A.UnpaidInclude = 1 THEN (A.totalUnpaidDays + A.totalUnapprovedLeaveDays + A.totalQuotaAdjustedDays) ELSE 0 END
                  + CASE WHEN A.AbsentInclude = 1 THEN (A.totalAbsentDays) ELSE 0 END) AS EOSTotalDaysCount
             , A.IndemnityPaidDays
        FROM (
            SELECT emp.Id                           AS EmployeeId
                 , startTbl.IsReset
                 , startTbl.StartDate
                 , startTbl.EndDate
                 , DATEDIFF(day, startTbl.StartDate, startTbl.EndDate) + 1 AS TotalDays
                 , ISNULL((SELECT TOP 1 ISNULL(wt.FixedDays, 0) FROM WageTypes wt
                           WHERE wt.Code = '4004' AND wt.TenantId = @TenantId AND wt.IsDeleted = 0), 0) AS FixedDays

                 -- migrated opening balance of absent/unpaid days
                 , ISNULL((SELECT ISNULL(SUM(tt.TransferCount), 0)
                           FROM TimeTransfers tt
                           WHERE tt.EmployeeId = emp.Id AND tt.TenantId = @TenantId AND tt.IsDeleted = 0
                             AND tt.TransferTypeId IN (SELECT TOP 1 ttt.Id FROM TimeTransferTypes ttt
                                                       WHERE ttt.NameEn = 'Absent Unpaid Days Opening Balance'
                                                         AND ttt.TenantId = @TenantId AND ttt.IsDeleted = 0)
                             AND tt.TransferDate >= startTbl.StartDate
                             AND tt.TransferDate <= startTbl.EndDate), 0) AS totalOpening

                 -- switch: does unpaid time reduce indemnity service for this tenant?
                 , CASE WHEN EXISTS (SELECT TOP 1 wt.Id FROM WageTypes wt
                                     WHERE wt.Code = '4001' AND wt.IncludeInIndemnity = 1
                                       AND wt.TenantId = @TenantId AND wt.IsDeleted = 0)
                        THEN 1 ELSE 0 END AS UnpaidInclude

                 , ISNULL((SELECT ISNULL(SUM(tr.UnpaidLeaveDay), 0)
                           FROM TimeRegisters tr
                           WHERE tr.EmployeeId = emp.Id AND tr.TenantId = @TenantId AND tr.IsDeleted = 0
                             AND tr.DayDate >= startTbl.StartDate
                             AND tr.DayDate <= startTbl.EndDate), 0) AS totalUnpaidDays

                 , ISNULL((SELECT ISNULL(SUM(tr.LeaveDay), 0)
                           FROM TimeRegisters tr
                           WHERE tr.EmployeeId = emp.Id AND tr.TenantId = @TenantId AND tr.IsDeleted = 0
                             AND tr.DayDate >= startTbl.StartDate
                             AND tr.DayDate <= startTbl.EndDate
                             AND tr.LeaveTypeId IS NOT NULL
                             AND tr.LeaveTypeId IN (SELECT at.Id FROM AbsenceTypes at
                                                    WHERE at.Code = '15' AND at.TenantId = @TenantId AND at.IsDeleted = 0)), 0) AS totalUnapprovedLeaveDays

                 , ISNULL((SELECT ISNULL(SUM(adj.QuotaAdjusted), 0)
                           FROM EmployeeAbsAdjustments adj
                           WHERE adj.EmployeeId = emp.Id AND adj.TenantId = @TenantId AND adj.IsDeleted = 0
                             AND adj.EffectiveDate >= startTbl.StartDate
                             AND adj.EffectiveDate <= startTbl.EndDate
                             AND adj.AbsenceTypeId IS NOT NULL
                             AND adj.AbsenceTypeId IN (SELECT at.Id FROM AbsenceTypes at
                                                       WHERE at.Code = '2' AND at.TenantId = @TenantId AND at.IsDeleted = 0)), 0) AS totalQuotaAdjustedDays

                 -- switch: do absent days reduce indemnity service for this tenant?
                 , CASE WHEN EXISTS (SELECT TOP 1 wt.Id FROM WageTypes wt
                                     WHERE wt.Code = '4029' AND wt.IncludeInIndemnity = 1
                                       AND wt.TenantId = @TenantId AND wt.IsDeleted = 0)
                        THEN 1 ELSE 0 END AS AbsentInclude

                 -- ""AND UnpaidLeaveDay = 0"" so a day is not counted twice, once here and once above
                 , ISNULL((SELECT ISNULL(SUM(tr.AbsentDay), 0)
                           FROM TimeRegisters tr
                           WHERE tr.EmployeeId = emp.Id AND tr.TenantId = @TenantId AND tr.IsDeleted = 0
                             AND tr.DayDate >= startTbl.StartDate
                             AND tr.DayDate <= startTbl.EndDate
                             AND tr.UnpaidLeaveDay = 0), 0) AS totalAbsentDays

                 , ISNULL((SELECT ISNULL(SUM(eg.GratuityPaidDays), 0)
                           FROM EmployeeGratuities eg
                           WHERE eg.EmployeeId = emp.Id AND eg.TenantId = @TenantId AND eg.IsDeleted = 0
                             AND eg.GratuityPaidDate >= startTbl.StartDate
                             AND eg.GratuityPaidDate <= startTbl.EndDate), 0) AS IndemnityPaidDays
            FROM Employees emp
            LEFT JOIN (
                SELECT e2.Id AS EmployeeId
                     , CASE WHEN gratuityReset.EmployeeId IS NOT NULL AND gratuityReset.StartDate > contract.StartDate
                            THEN 1 ELSE 0 END AS IsReset
                     , CASE WHEN gratuityReset.EmployeeId IS NOT NULL AND gratuityReset.StartDate > contract.StartDate
                            THEN gratuityReset.StartDate
                            ELSE contract.StartDate END AS StartDate
                     , CASE WHEN contract.EndDate >= EOMONTH(DATEFROMPARTS(@PrmYear, @PrmMonth, 1))
                            THEN EOMONTH(DATEFROMPARTS(@PrmYear, @PrmMonth, 1))
                            ELSE contract.EndDate END AS EndDate
                FROM Employees e2
                LEFT JOIN (
                    SELECT ec.EmployeeId, MIN(ec.StartDate) AS StartDate, MAX(ec.EndDate) AS EndDate
                    FROM EmployeeContracts ec
                    WHERE ec.TenantId = @TenantId AND ec.IsDeleted = 0
                    GROUP BY ec.EmployeeId
                ) contract ON contract.EmployeeId = e2.Id
                LEFT JOIN (
                    SELECT eg.EmployeeId, MAX(eg.GratuityPaidDate) AS StartDate
                    FROM EmployeeGratuities eg
                    WHERE eg.TenantId = @TenantId AND eg.IsDeleted = 0
                      AND eg.ResetGratuityCalc = 1
                      AND eg.GratuityPaidDate <= EOMONTH(DATEFROMPARTS(@PrmYear, @PrmMonth, 1))
                    GROUP BY eg.EmployeeId
                ) gratuityReset ON gratuityReset.EmployeeId = e2.Id
                WHERE e2.TenantId = @TenantId AND e2.IsDeleted = 0
            ) startTbl ON startTbl.EmployeeId = emp.Id
            WHERE emp.TenantId = @TenantId AND emp.IsDeleted = 0
        ) A
    ) B

    LEFT JOIN Employees emp ON emp.Id = B.EmployeeId AND emp.TenantId = @TenantId AND emp.IsDeleted = 0

    -- projected nowhere; kept for row-multiplicity parity (see the sibling leave statement)
    LEFT JOIN SalaryRegisters sr
           ON sr.EmployeeId = emp.Id AND sr.TenantId = @TenantId AND sr.IsDeleted = 0
          AND sr.ForPeriod = EOMONTH(DATEFROMPARTS(@PrmYear, @PrmMonth, 1))
          AND sr.ForPeriod = sr.InPeriod
          AND sr.PayrollRunType = 0
    LEFT JOIN SalaryRegisterDetails srd
           ON srd.SalaryRegisterId = sr.Id AND srd.TenantId = @TenantId AND srd.IsDeleted = 0
          AND srd.WageTypeId IN (SELECT wt.Id FROM WageTypes wt
                                 WHERE wt.Code = '4025' AND wt.TenantId = @TenantId AND wt.IsDeleted = 0)

    LEFT JOIN EmployeeOrgAssignments orgAsg
           ON orgAsg.EmployeeId = emp.Id AND orgAsg.TenantId = @TenantId AND orgAsg.IsDeleted = 0
          AND orgAsg.StartDate <= EOMONTH(DATEFROMPARTS(@PrmYear, @PrmMonth, 1))
          AND orgAsg.EndDate   >= EOMONTH(DATEFROMPARTS(@PrmYear, @PrmMonth, 1))
    LEFT JOIN PayrollAreas payrollArea
           ON payrollArea.Id = orgAsg.PayrollAreaId AND payrollArea.TenantId = @TenantId AND payrollArea.IsDeleted = 0

    -- the statutory rule set: per country, per separation type (resignation / termination / ...)
    LEFT JOIN IndemnityPayRules indemnityPayRule
           ON indemnityPayRule.CountryId = payrollArea.CountryId
          AND indemnityPayRule.SeparationType = @PrmSeparationType
          AND indemnityPayRule.TenantId = @TenantId AND indemnityPayRule.IsDeleted = 0
    OUTER APPLY (
        SELECT TOP 1 d.PayPercentage
        FROM IndemnityPayRuleDetails d
        WHERE d.IndemnityPayRuleId = indemnityPayRule.Id
          AND d.TenantId = @TenantId AND d.IsDeleted = 0
          AND (B.TotalDaysCount / 365.0) BETWEEN d.MinYearOfService AND d.MaxYearOfService
    ) payRule

    LEFT JOIN EmployeeGroups empGroup
           ON empGroup.Id = orgAsg.EmployeeGroupId AND empGroup.TenantId = @TenantId AND empGroup.IsDeleted = 0
    LEFT JOIN EmployeeSubGroups empSubGroup
           ON empSubGroup.Id = orgAsg.EmployeeSubGroupId AND empSubGroup.TenantId = @TenantId AND empSubGroup.IsDeleted = 0
    -- one row per gratuity slab -- this is what fans the query out to one row per slab
    LEFT JOIN EmployeeSubGroupGratuities subGroupGratuity
           ON subGroupGratuity.EmployeeSubGroupId = empSubGroup.Id
          AND subGroupGratuity.TenantId = @TenantId AND subGroupGratuity.IsDeleted = 0

    LEFT JOIN EmployeeBasicPays basicPay
           ON basicPay.EmployeeId = emp.Id AND basicPay.TenantId = @TenantId AND basicPay.IsDeleted = 0
          AND basicPay.StartDate <= EOMONTH(DATEFROMPARTS(@PrmYear, @PrmMonth, 1))
          AND basicPay.EndDate   >= EOMONTH(DATEFROMPARTS(@PrmYear, @PrmMonth, 1))
    LEFT JOIN (
        SELECT bpd.EmployeeBasicPayId, bpd.CurrencyId, SUM(bpd.Amount) AS IndemnityRate
        FROM EmployeeBasicPayDetails bpd
        LEFT JOIN WageTypes wt ON wt.Id = bpd.WageTypeId AND wt.TenantId = @TenantId AND wt.IsDeleted = 0
        WHERE bpd.TenantId = @TenantId AND bpd.IsDeleted = 0
          AND wt.IncludeInIndemnity = 1
        GROUP BY bpd.EmployeeBasicPayId, bpd.CurrencyId
    ) basicPayDetail ON basicPayDetail.EmployeeBasicPayId = basicPay.Id
    LEFT JOIN Currencies currency
           ON currency.Id = basicPayDetail.CurrencyId AND currency.TenantId = @TenantId AND currency.IsDeleted = 0

    -- gratuity already advanced to the employee, carried as a loan of a specifically NAMED type.
    -- Matching a loan type by NameEn is fragile, but it is what the legacy does and changing it to a
    -- Code would change a printed deduction. *** WORTH RAISING AS A PRODUCT BUG. ***
    LEFT JOIN (
        SELECT loan.EmployeeId, SUM(loan.DueAmount) AS IndemnityPaidAmount
        FROM EmployeeLoans loan
        LEFT JOIN LoanTypes lt ON lt.Id = loan.LoanTypeId AND lt.TenantId = @TenantId AND lt.IsDeleted = 0
        WHERE loan.TenantId = @TenantId AND loan.IsDeleted = 0
          AND lt.NameEn IN ('End of service Gratuity Loan')
        GROUP BY loan.EmployeeId
    ) indemnityPaid ON indemnityPaid.EmployeeId = emp.Id

    WHERE ((ISNULL(@PrmEmpCode, '') <> '' AND emp.Code = @PrmEmpCode) OR ISNULL(@PrmEmpCode, '') = '')
) C
WHERE C.TotalDaysCount - ISNULL((C.LastChangeYear * 365), 0) >= 0
ORDER BY C.OID, C.ChangeYear;";

        // -------------------------------------------------------------------------------------------
        // rptIndemnityBreakdown (master) -- the same slab arithmetic, priced, as at TODAY.
        // -------------------------------------------------------------------------------------------
        public const string IndemnityBreakdownQuery = @"-- rptIndemnityBreakdown :: master query. FINANCIAL OUTPUT -- the end-of-service settlement
-- breakdown handed to a leaving employee. One row per (employee, gratuity slab).
--
-- Structurally the sibling of IndemnityStatementQuery, with FOUR deliberate differences. Do not
-- ""unify"" the two: they answer different questions and the differences are the point.
--
--  1. AS AT TODAY, NOT AS AT A CHOSEN MONTH. Every effective-date join here uses GETDATE() and the
--     service span runs to the last contract EndDate, UNCLAMPED. There is no @PrmYear/@PrmMonth.
--
--     *** THIS PRODUCES NONSENSE AGAINST HCMv3 DATA AND NEEDS A PRODUCT DECISION BEFORE RELEASE. ***
--     HCMv3 stores an open-ended EmployeeContracts.EndDate as 9999-12-31. The sibling
--     IndemnityStatementQuery CLAMPS its EndDate to the end of the selected month; this one does not
--     clamp at all, because the legacy source does not. Verified against live data, employee 3400:
--         EndDate = 9999-12-31  ->  TotalDays = 2,919,199 (~7,997 years)
--         -> slab ""Above 5 Years"" fills completely -> Amount = 18,810.000 KWD
--     against a true service span of ~17 years, which should yield roughly 1,000 KWD.
--     The obvious fix is to clamp EndDate to GETDATE() the way the sibling clamps to the as-on date.
--     NOT APPLIED HERE: this is an end-of-service settlement figure handed to a leaving employee, and
--     silently changing a boundary condition during a port is exactly how a payroll defect ships.
--     Raise it, decide it, and change it deliberately.
--
--  2. TWO SERVICE SPANS, and the printed one is the OTHER one:
--       TotalDaysCount          = TotalDays - totalOpening                 (gross of unpaid days)
--       TotalDaysCountAfterUnpaid = TotalDays - totalOpening - the same UnpaidInclude / AbsentInclude
--                                   deductions the indemnity statement applies
--     Every printed slab figure (SlabWiseDaysAfter, Years, SlabWiseIndemnity, Amount) uses
--     TotalDaysCountAfterUnpaid. TotalDaysCount survives only in the final
--     ""WHERE TotalDaysCount - LastChangeYear*365 >= 0"" slab filter -- so which slabs APPEAR is
--     decided gross, while what each slab is WORTH is computed net. That asymmetry is in the legacy
--     source and is preserved.
--
--  3. THE MONEY. The indemnity statement stops at days; this report prices them:
--         Amount           = IndemnityRate * SlabWiseIndemnityDays / 26
--         LessAdvanceTotal = Amount - IndemnityPaidAmount
--     26 is the statutory working days per month, matching wage type '4004' FixedDays. DECIMAL(10,3)
--     for KWD, applied to the day count BEFORE the multiply -- the legacy's order, kept.
--
--  4. A 5-YEAR SPLIT of unpaid days, keyed on the JOINING DATE and NOT on the service start:
--         AbsentDaysFirst5Years / AbsentDaysAfter5Years, selected by ""ChangeYear <= 5"".
--     Note these two subqueries have NO date lower bound and NO reset handling -- they span the
--     employee's whole TimeRegisters history regardless of a gratuity reset. Legacy behaviour;
--     preserved, and worth a second look with the business.
--
-- ""Years"" is SlabWiseIndemnityDays / GratuityDays -- i.e. the slab expressed back in years --
-- guarded against GratuityDays = 0.
SELECT C.OID
     , C.Code
     , C.FullNameEn
     , C.EmployeeGroup
     , C.EmployeeSubGroup
     , C.CurrencyEn
     , C.JoiningDate
     , C.CompanyEn
     , C.CompanyAr
     , C.Companylogo
     -- Period of service as ""y years m months d days"", borrow-corrected on the day-of-month
     , CAST((DATEDIFF(MONTH, C.JoiningDate, C.LastDayInService)
             - CASE WHEN DAY(C.LastDayInService) < DAY(C.JoiningDate) THEN 1 ELSE 0 END) / 12 AS VARCHAR)
       + ' years ' +
       CAST((DATEDIFF(MONTH, C.JoiningDate, C.LastDayInService)
             - CASE WHEN DAY(C.LastDayInService) < DAY(C.JoiningDate) THEN 1 ELSE 0 END) % 12 AS VARCHAR)
       + ' months ' +
       CAST(DATEDIFF(DAY,
                     DATEADD(MONTH,
                             (DATEDIFF(MONTH, C.JoiningDate, C.LastDayInService)
                              - CASE WHEN DAY(C.LastDayInService) < DAY(C.JoiningDate) THEN 1 ELSE 0 END),
                             C.JoiningDate),
                     C.LastDayInService) AS VARCHAR)
       + ' days'                                                       AS PeriodOfService
     , C.LastDayInService
     -- ADDED BY THE PORT (no legacy counterpart in the SQL). The legacy code-behind opened its own
     -- XPObjectSpaceProvider inside BeforePrint solely to read this one enum off EmployeeSeparation
     -- and print 'Resignation' / 'Termination'. Projected here instead so the report needs no DI.
     , C.SeparationType
     , C.IsReset
     , C.StartDate
     , C.EndDate
     , C.IndemnityPaidDays
     , C.IndemnityRate
     , C.FixedDays
     , C.BasisDays
     , CASE WHEN C.BasisDays <> 0 THEN C.BasisDays ELSE C.FixedDays END AS DaysDivFactor
     , CASE WHEN (CASE WHEN C.BasisDays <> 0 THEN C.BasisDays ELSE C.FixedDays END) <> 0
            THEN C.IndemnityRate / (CASE WHEN C.BasisDays <> 0 THEN C.BasisDays ELSE C.FixedDays END)
            ELSE 0 END                                                 AS PerDayRate
     , C.TotalDays
     , C.TotalAbsentDay
     , C.TotalDaysCount
     , C.LastChangeYear
     , (C.LastChangeYear * 365)                                        AS LastUsedDays
     , C.TotalDaysCount - ISNULL((C.LastChangeYear * 365), 0)          AS RemainingDays
     , C.MaxSlabDays
     , C.ChangeYear
     , CASE WHEN C.LastChangeYear = 0 THEN 'First ' + CONVERT(VARCHAR(10), C.ChangeYear)     + ' Years'
            ELSE                           'Above ' + CONVERT(VARCHAR(10), C.LastChangeYear) + ' Years'
       END                                                             AS ChangeYearText
     , CASE WHEN (C.TotalDaysCount - ISNULL((C.LastChangeYear * 365), 0)) >= C.MaxSlabDays
            THEN C.MaxSlabDays
            ELSE (C.TotalDaysCount - ISNULL((C.LastChangeYear * 365), 0))
       END                                                             AS SlabWiseDays

     -- the NET slab, and the printed one -- see difference 2 in the header
     , CASE WHEN (C.TotalDaysCountAfterUnpaid - ISNULL((C.LastChangeYear * 365), 0)) >= C.MaxSlabDays
            THEN C.MaxSlabDays
            ELSE (C.TotalDaysCountAfterUnpaid - ISNULL((C.LastChangeYear * 365), 0))
       END                                                             AS SlabWiseDaysAfter
     , CONVERT(DECIMAL(10, 2),
         CASE WHEN C.GratuityDays <> 0
              THEN (CASE WHEN (C.TotalDaysCountAfterUnpaid - ISNULL((C.LastChangeYear * 365), 0)) >= C.MaxSlabDays
                         THEN C.GratuityDays * C.ChangeYear
                         ELSE (C.TotalDaysCountAfterUnpaid - ISNULL((C.LastChangeYear * 365), 0)) * (C.GratuityDays / 365)
                    END) / C.GratuityDays
              ELSE 0 END)                                              AS Years
     , C.DivisionFactor
     , C.GratuityDays
     , CONVERT(DECIMAL(10, 3),
         CASE WHEN (C.TotalDaysCountAfterUnpaid - ISNULL((C.LastChangeYear * 365), 0)) >= C.MaxSlabDays
              THEN C.GratuityDays * C.ChangeYear
              ELSE (C.TotalDaysCountAfterUnpaid - ISNULL((C.LastChangeYear * 365), 0)) * (C.GratuityDays / 365)
         END)                                                          AS SlabWiseIndemnity

     , CASE WHEN C.ChangeYear <= 5 THEN C.AbsentDaysFirst5Years
            ELSE C.AbsentDaysAfter5Years END                           AS AbsentDays
     , C.TotalDaysCountAfterUnpaid
     , C.IndemnityPaidAmount

     -- the money. 26 = statutory working days per month; see difference 3 in the header.
     , CONVERT(DECIMAL(10, 3), C.IndemnityRate *
         (CONVERT(DECIMAL(10, 3),
            CASE WHEN (C.TotalDaysCountAfterUnpaid - ISNULL((C.LastChangeYear * 365), 0)) >= C.MaxSlabDays
                 THEN C.GratuityDays * C.ChangeYear
                 ELSE (C.TotalDaysCountAfterUnpaid - ISNULL((C.LastChangeYear * 365), 0)) * (C.GratuityDays / 365)
            END))) / 26                                                AS Amount
     , CONVERT(DECIMAL(10, 3), C.IndemnityRate *
         (CONVERT(DECIMAL(10, 3),
            CASE WHEN (C.TotalDaysCountAfterUnpaid - ISNULL((C.LastChangeYear * 365), 0)) >= C.MaxSlabDays
                 THEN C.GratuityDays * C.ChangeYear
                 ELSE (C.TotalDaysCountAfterUnpaid - ISNULL((C.LastChangeYear * 365), 0)) * (C.GratuityDays / 365)
            END))) / 26 - C.IndemnityPaidAmount                        AS LessAdvanceTotal
FROM (
    SELECT emp.Id                                   AS OID
         , emp.Code
         , emp.FullNameEn
         , emp.JoiningDate
         , separation.LastDayInService
         , separation.SeparationType
         , empGroup.NameEn                          AS EmployeeGroup
         , empSubGroup.NameEn                       AS EmployeeSubGroup
         , currency.CurrencyShortFormEn             AS CurrencyEn
         , B.CompanyEn
         , B.CompanyAr
         , B.Companylogo
         , B.IsReset
         , B.StartDate
         , B.EndDate
         , B.IndemnityPaidDays
         , basicPayDetail.IndemnityRate
         , B.FixedDays
         , subGroupGratuity.BasisDays
         , B.TotalDays
         , B.TotalAbsentDay
         , B.TotalDaysCount
         , B.TotalDaysCountAfterUnpaid
         , ISNULL((SELECT TOP 1 prevSlab.ChangeYear
                   FROM EmployeeSubGroupGratuities prevSlab
                   WHERE prevSlab.EmployeeSubGroupId = empSubGroup.Id
                     AND prevSlab.TenantId = @TenantId AND prevSlab.IsDeleted = 0
                     AND prevSlab.ChangeYear < subGroupGratuity.ChangeYear
                   ORDER BY prevSlab.ChangeYear DESC), 0) AS LastChangeYear
         , subGroupGratuity.ChangeYear
         , subGroupGratuity.GratuityDays
         , 365                                      AS DivisionFactor
         , (subGroupGratuity.ChangeYear * 365)      AS MaxSlabDays
         , B.AbsentDaysFirst5Years
         , B.AbsentDaysAfter5Years
         , ISNULL(indemnityPaid.IndemnityPaidAmount, 0) AS IndemnityPaidAmount
    FROM (
        SELECT A.EmployeeId
             , A.JoiningDate
             , A.CompanyEn
             , A.CompanyAr
             , A.Companylogo
             , A.Code
             , A.IsReset
             , A.StartDate
             , A.EndDate
             , A.TotalDays
             , A.FixedDays
             , A.totalOpening
               + CASE WHEN A.UnpaidInclude = 1 THEN (A.totalUnpaidDays + A.totalUnapprovedLeaveDays + A.totalQuotaAdjustedDays) ELSE 0 END
               + CASE WHEN A.AbsentInclude = 1 THEN (A.totalAbsentDays) ELSE 0 END AS TotalAbsentDay

             -- GROSS: opening balance only. Decides which slabs appear.
             , A.TotalDays - (A.totalOpening) AS TotalDaysCount

             -- NET: opening balance plus the two statutory switches. Decides what each slab is worth.
             , A.TotalDays - (A.totalOpening
                  + CASE WHEN A.UnpaidInclude = 1 THEN (A.totalUnpaidDays + A.totalUnapprovedLeaveDays + A.totalQuotaAdjustedDays) ELSE 0 END
                  + CASE WHEN A.AbsentInclude = 1 THEN (A.totalAbsentDays) ELSE 0 END) AS TotalDaysCountAfterUnpaid

             , A.AbsentDaysFirst5Years
             , A.AbsentDaysAfter5Years
             , A.IndemnityPaidDays
        FROM (
            SELECT emp.Id                           AS EmployeeId
                 , emp.FullNameEn
                 , emp.FullNameAr
                 , emp.Code
                 , emp.JoiningDate
                 , company.DisplayName              AS CompanyEn
                 , companyProfile.NameAr            AS CompanyAr
                 , CAST(NULL AS VARBINARY(MAX))     AS Companylogo    -- FEATURE LOST
                 , startTbl.IsReset
                 , startTbl.StartDate
                 , startTbl.EndDate
                 , DATEDIFF(day, startTbl.StartDate, startTbl.EndDate) + 1 AS TotalDays
                 , ISNULL((SELECT TOP 1 ISNULL(wt.FixedDays, 0) FROM WageTypes wt
                           WHERE wt.Code = '4004' AND wt.TenantId = @TenantId AND wt.IsDeleted = 0), 0) AS FixedDays

                 , ISNULL((SELECT ISNULL(SUM(tt.TransferCount), 0)
                           FROM TimeTransfers tt
                           WHERE tt.EmployeeId = emp.Id AND tt.TenantId = @TenantId AND tt.IsDeleted = 0
                             AND tt.TransferTypeId IN (SELECT TOP 1 ttt.Id FROM TimeTransferTypes ttt
                                                       WHERE ttt.NameEn = 'Absent Unpaid Days Opening Balance'
                                                         AND ttt.TenantId = @TenantId AND ttt.IsDeleted = 0)
                             AND tt.TransferDate >= startTbl.StartDate
                             AND tt.TransferDate <= startTbl.EndDate), 0) AS totalOpening

                 , CASE WHEN EXISTS (SELECT TOP 1 wt.Id FROM WageTypes wt
                                     WHERE wt.Code = '4001' AND wt.IncludeInIndemnity = 1
                                       AND wt.TenantId = @TenantId AND wt.IsDeleted = 0)
                        THEN 1 ELSE 0 END AS UnpaidInclude

                 , ISNULL((SELECT ISNULL(SUM(tr.UnpaidLeaveDay), 0)
                           FROM TimeRegisters tr
                           WHERE tr.EmployeeId = emp.Id AND tr.TenantId = @TenantId AND tr.IsDeleted = 0
                             AND tr.DayDate >= startTbl.StartDate
                             AND tr.DayDate <= startTbl.EndDate), 0) AS totalUnpaidDays

                 -- 5-year split. NOTE: no lower date bound and no reset handling -- see header note 4.
                 , ISNULL((SELECT SUM(tr.UnpaidLeaveDay)
                           FROM TimeRegisters tr
                           WHERE tr.EmployeeId = emp.Id AND tr.TenantId = @TenantId AND tr.IsDeleted = 0
                             AND tr.DayDate <= DATEADD(YEAR, 5, emp.JoiningDate)), 0) AS AbsentDaysFirst5Years
                 , ISNULL((SELECT SUM(tr.UnpaidLeaveDay)
                           FROM TimeRegisters tr
                           WHERE tr.EmployeeId = emp.Id AND tr.TenantId = @TenantId AND tr.IsDeleted = 0
                             AND tr.DayDate > DATEADD(YEAR, 5, emp.JoiningDate)), 0) AS AbsentDaysAfter5Years

                 , ISNULL((SELECT ISNULL(SUM(tr.LeaveDay), 0)
                           FROM TimeRegisters tr
                           WHERE tr.EmployeeId = emp.Id AND tr.TenantId = @TenantId AND tr.IsDeleted = 0
                             AND tr.DayDate >= startTbl.StartDate
                             AND tr.DayDate <= startTbl.EndDate
                             AND tr.LeaveTypeId IS NOT NULL
                             AND tr.LeaveTypeId IN (SELECT at.Id FROM AbsenceTypes at
                                                    WHERE at.Code = '15' AND at.TenantId = @TenantId AND at.IsDeleted = 0)), 0) AS totalUnapprovedLeaveDays

                 , ISNULL((SELECT ISNULL(SUM(adj.QuotaAdjusted), 0)
                           FROM EmployeeAbsAdjustments adj
                           WHERE adj.EmployeeId = emp.Id AND adj.TenantId = @TenantId AND adj.IsDeleted = 0
                             AND adj.EffectiveDate >= startTbl.StartDate
                             AND adj.EffectiveDate <= startTbl.EndDate
                             AND adj.AbsenceTypeId IS NOT NULL
                             AND adj.AbsenceTypeId IN (SELECT at.Id FROM AbsenceTypes at
                                                       WHERE at.Code = '2' AND at.TenantId = @TenantId AND at.IsDeleted = 0)), 0) AS totalQuotaAdjustedDays

                 , CASE WHEN EXISTS (SELECT TOP 1 wt.Id FROM WageTypes wt
                                     WHERE wt.Code = '4029' AND wt.IncludeInIndemnity = 1
                                       AND wt.TenantId = @TenantId AND wt.IsDeleted = 0)
                        THEN 1 ELSE 0 END AS AbsentInclude

                 , ISNULL((SELECT ISNULL(SUM(tr.AbsentDay), 0)
                           FROM TimeRegisters tr
                           WHERE tr.EmployeeId = emp.Id AND tr.TenantId = @TenantId AND tr.IsDeleted = 0
                             AND tr.DayDate >= startTbl.StartDate
                             AND tr.DayDate <= startTbl.EndDate
                             AND tr.UnpaidLeaveDay = 0), 0) AS totalAbsentDays

                 , ISNULL((SELECT ISNULL(SUM(eg.GratuityPaidDays), 0)
                           FROM EmployeeGratuities eg
                           WHERE eg.EmployeeId = emp.Id AND eg.TenantId = @TenantId AND eg.IsDeleted = 0
                             AND eg.GratuityPaidDate >= startTbl.StartDate
                             AND eg.GratuityPaidDate <= startTbl.EndDate), 0) AS IndemnityPaidDays
                 , separation.LastDayInService
                 , separation.SeparationType
            FROM Employees emp
            LEFT JOIN EmployeeSeparations separation
                   ON separation.EmployeeId = emp.Id AND separation.TenantId = @TenantId AND separation.IsDeleted = 0
            LEFT JOIN EmployeeOrgAssignments orgAsg
                   ON orgAsg.EmployeeId = emp.Id AND orgAsg.TenantId = @TenantId AND orgAsg.IsDeleted = 0
                  AND orgAsg.StartDate <= GETDATE() AND orgAsg.EndDate >= GETDATE()
            LEFT JOIN AbpOrganizationUnits ou
                   ON ou.Id = orgAsg.OrganizationUnitId AND ou.TenantId = @TenantId AND ou.IsDeleted = 0
            OUTER APPLY (
                SELECT TOP 1 anc.Id, anc.DisplayName
                FROM AbpOrganizationUnits anc
                INNER JOIN OrgUnitProfiles ancp       ON ancp.OrganizationUnitId = anc.Id      AND ancp.TenantId = @TenantId AND ancp.IsDeleted = 0
                INNER JOIN OrganizationUnitTypes anct ON anct.Id = ancp.OrganizationUnitTypeId AND anct.TenantId = @TenantId AND anct.IsDeleted = 0
                WHERE anc.TenantId = @TenantId AND anc.IsDeleted = 0 AND ou.Code LIKE anc.Code + '%' AND anct.NameEn = 'Company'
                ORDER BY LEN(anc.Code)
            ) company
            LEFT JOIN OrgUnitProfiles companyProfile
                   ON companyProfile.OrganizationUnitId = company.Id AND companyProfile.TenantId = @TenantId AND companyProfile.IsDeleted = 0
            LEFT JOIN (
                SELECT e2.Id AS EmployeeId
                     , CASE WHEN gratuityReset.EmployeeId IS NOT NULL AND gratuityReset.StartDate > contract.StartDate
                            THEN 1 ELSE 0 END AS IsReset
                     , CASE WHEN gratuityReset.EmployeeId IS NOT NULL AND gratuityReset.StartDate > contract.StartDate
                            THEN gratuityReset.StartDate
                            ELSE contract.StartDate END AS StartDate
                     -- NOTE: unclamped, unlike the indemnity statement. See header difference 1.
                     , contract.EndDate AS EndDate
                FROM Employees e2
                LEFT JOIN (
                    SELECT ec.EmployeeId, MIN(ec.StartDate) AS StartDate, MAX(ec.EndDate) AS EndDate
                    FROM EmployeeContracts ec
                    WHERE ec.TenantId = @TenantId AND ec.IsDeleted = 0
                    GROUP BY ec.EmployeeId
                ) contract ON contract.EmployeeId = e2.Id
                LEFT JOIN (
                    SELECT eg.EmployeeId, MAX(eg.GratuityPaidDate) AS StartDate
                    FROM EmployeeGratuities eg
                    WHERE eg.TenantId = @TenantId AND eg.IsDeleted = 0
                      AND eg.ResetGratuityCalc = 1
                      AND eg.GratuityPaidDate <= GETDATE()
                    GROUP BY eg.EmployeeId
                ) gratuityReset ON gratuityReset.EmployeeId = e2.Id
                WHERE e2.TenantId = @TenantId AND e2.IsDeleted = 0
            ) startTbl ON startTbl.EmployeeId = emp.Id
            WHERE emp.TenantId = @TenantId AND emp.IsDeleted = 0
        ) A
    ) B

    LEFT JOIN Employees emp ON emp.Id = B.EmployeeId AND emp.TenantId = @TenantId AND emp.IsDeleted = 0
    LEFT JOIN EmployeeOrgAssignments orgAsg
           ON orgAsg.EmployeeId = emp.Id AND orgAsg.TenantId = @TenantId AND orgAsg.IsDeleted = 0
          AND orgAsg.StartDate <= GETDATE() AND orgAsg.EndDate >= GETDATE()
    LEFT JOIN EmployeeGroups empGroup
           ON empGroup.Id = orgAsg.EmployeeGroupId AND empGroup.TenantId = @TenantId AND empGroup.IsDeleted = 0
    LEFT JOIN EmployeeSubGroups empSubGroup
           ON empSubGroup.Id = orgAsg.EmployeeSubGroupId AND empSubGroup.TenantId = @TenantId AND empSubGroup.IsDeleted = 0
    LEFT JOIN EmployeeSubGroupGratuities subGroupGratuity
           ON subGroupGratuity.EmployeeSubGroupId = empSubGroup.Id
          AND subGroupGratuity.TenantId = @TenantId AND subGroupGratuity.IsDeleted = 0

    LEFT JOIN EmployeeSeparations separation
           ON separation.EmployeeId = emp.Id AND separation.TenantId = @TenantId AND separation.IsDeleted = 0
    LEFT JOIN EmployeeBasicPays basicPay
           ON basicPay.EmployeeId = emp.Id AND basicPay.TenantId = @TenantId AND basicPay.IsDeleted = 0
          AND basicPay.StartDate <= GETDATE() AND basicPay.EndDate >= GETDATE()
    LEFT JOIN (
        SELECT bpd.EmployeeBasicPayId, bpd.CurrencyId, SUM(bpd.Amount) AS IndemnityRate
        FROM EmployeeBasicPayDetails bpd
        LEFT JOIN WageTypes wt ON wt.Id = bpd.WageTypeId AND wt.TenantId = @TenantId AND wt.IsDeleted = 0
        WHERE bpd.TenantId = @TenantId AND bpd.IsDeleted = 0
          AND wt.IncludeInIndemnity = 1
        GROUP BY bpd.EmployeeBasicPayId, bpd.CurrencyId
    ) basicPayDetail ON basicPayDetail.EmployeeBasicPayId = basicPay.Id
    LEFT JOIN Currencies currency
           ON currency.Id = basicPayDetail.CurrencyId AND currency.TenantId = @TenantId AND currency.IsDeleted = 0

    LEFT JOIN (
        SELECT loan.EmployeeId, SUM(loan.DueAmount) AS IndemnityPaidAmount
        FROM EmployeeLoans loan
        LEFT JOIN LoanTypes lt ON lt.Id = loan.LoanTypeId AND lt.TenantId = @TenantId AND lt.IsDeleted = 0
        WHERE loan.TenantId = @TenantId AND loan.IsDeleted = 0
          AND lt.NameEn IN ('End of service Gratuity Loan')
        GROUP BY loan.EmployeeId
    ) indemnityPaid ON indemnityPaid.EmployeeId = emp.Id

    WHERE ((ISNULL(@PrmEmpCode, '') <> '' AND emp.Code = @PrmEmpCode) OR ISNULL(@PrmEmpCode, '') = '')
) C
-- gross span decides which slabs appear -- see header difference 2
WHERE C.TotalDaysCount - ISNULL((C.LastChangeYear * 365), 0) >= 0
ORDER BY C.OID, C.ChangeYear;";
    }
}



