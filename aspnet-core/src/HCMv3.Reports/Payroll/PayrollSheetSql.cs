// SQL for the seven "payroll sheet" reports, rewritten for the HCMv3 schema.
//
//   rptPayrollSheet, rptPayrollSheetByCostCenter, rptPayrollSheetByCostCenterPostingGroup,
//   rptPayrollSheetByCostCenterSummary, rptPayrollSheetBySponsor, rptPayrollSheetSummary,
//   rptSalaryByBudgetingGroup
//
// These are ONE query family. Every one of them asks the same question -- "for this month, bucket
// each employee's salary-register lines by wage type" -- and then differs only in which grouping
// dimension it carries and which buckets it prints. In the legacy corpus that shared body was
// copy-pasted seven times (21 KB - 61 KB per .resx) and had drifted: the same bucket name meant
// different wage types in different reports. Here the body is written ONCE (<see cref="Buckets"/>,
// <see cref="EmployeeDimensions"/>) and each report composes it, so a bucket definition has exactly
// one home. Where the legacy copies genuinely disagreed the divergent buckets are kept under
// DISTINCT names (MiscPay1017 vs CalcPay, OtherLeaveCostCentre vs OtherLeaveSummary,
// LoanRecoveryDed vs LoanTypeDed, TotalDeductionCostCentre vs TotalDeductionSummary) rather than
// silently unified -- see PORTING-NOTES-PayrollSheets.md.
//
// MULTI-TENANCY: these run through DevExpress SqlDataSource as RAW SQL, so EF Core's per-tenant
// global query filter does NOT apply. Every table reference filters TenantId explicitly via
// @TenantId, which HcmReportBase.EnsureHcmParameters auto-wires from the PrmTenantId report
// parameter (keyed on the literal text "@TenantId" appearing in the SQL). Removing an
// "AND TenantId = @TenantId" silently leaks rows across tenants.
//
// WAGE TYPES: the legacy corpus mixes wage-type CODES ('1000') with raw legacy PRIMARY KEYS
// (WageType = 1, WageType <> 65). Only the codes are portable -- HCMv3 has an unrelated wage type
// whose Code is literally '1'. Every wage-type test below goes through WageTypes.Code or through a
// real HCMv3 FK (OvertimeTypes.WageTypeId, LoanTypes.WageTypeId, AbsenceTypes.WageTypeId).
// No legacy integer is carried across.

namespace HCMv3.Reports.Payroll
{
    /// <summary>Raw SQL shared by the seven payroll-sheet reports.</summary>
    public static class PayrollSheetSql
    {
        // =============================================================================================
        // SHARED FRAGMENT 1 -- the wage-type bucket aggregate.
        //
        // Legacy shape: 16-31 separate "LEFT JOIN (SELECT ... GROUP BY ...) AS SrDetail_Xxx" derived
        // tables per report, each re-scanning SalaryRegisterDetail and each re-running the correlated
        // "OR WageType.ArrearSourceWageType IN (SELECT OID FROM WageType WHERE ...)" sub-select.
        //
        // HCMv3 shape: ONE pass over SalaryRegisterDetails with conditional aggregation, keyed on
        // SalaryRegisterId. The "arrear source" alternative becomes a single LEFT JOIN back to
        // WageTypes (alias wtSrc) instead of one correlated sub-select per bucket.
        //
        // Semantics preserved: the legacy bucket sub-queries also re-filtered
        // "InPeriod = ForPeriod AND month/year AND PayrollRunType = @PrmPayrollRunType". The first
        // three are kept here (they prune the scan); the PayrollRunType filter is DROPPED as
        // redundant -- every bucket is joined back to the outer SalaryRegisters row on
        // SalaryRegisterId, and that row is already run-type filtered by the calling report. (The
        // legacy PresentDays bucket had already commented its own run-type filter out for the same
        // reason.)
        //
        // A NULL bucket therefore means "this register had no line of that kind"; every consumer
        // wraps it in ISNULL(..., 0), exactly as the legacy LEFT JOINs did.
        // =============================================================================================
        private const string Buckets = @"
    SELECT
          srd.SalaryRegisterId

        -- Basic pay (Code 1000). Legacy 'WageType = 1' in some copies -- a legacy PK, NOT a code.
        , SUM(CASE WHEN wt.Code IN ('1000') OR wtSrc.Code IN ('1000') THEN srd.Rate   END) AS BasicPay_Rate
        , SUM(CASE WHEN wt.Code IN ('1000') OR wtSrc.Code IN ('1000') THEN srd.Number END) AS BasicPay_Number
        , SUM(CASE WHEN wt.Code IN ('1000') OR wtSrc.Code IN ('1000') THEN srd.Amount END) AS BasicPay_Amount

        -- All allowance = everything else that counts towards basic pay.
        , SUM(CASE WHEN (wt.IncludeInBasicPay    = 1 AND wt.Code    NOT IN ('1000'))
                     OR (wtSrc.IncludeInBasicPay = 1 AND wtSrc.Code NOT IN ('1000')) THEN srd.Rate   END) AS Allowance_Rate
        , SUM(CASE WHEN (wt.IncludeInBasicPay    = 1 AND wt.Code    NOT IN ('1000'))
                     OR (wtSrc.IncludeInBasicPay = 1 AND wtSrc.Code NOT IN ('1000')) THEN srd.Amount END) AS Allowance_Amount

        -- Attendance / day counters.
        , SUM(CASE WHEN wt.Code IN ('4014') OR wtSrc.Code IN ('4014') THEN srd.Number END) AS PresentDays_Number
        , SUM(CASE WHEN wt.Code IN ('4001') OR wtSrc.Code IN ('4001') THEN srd.Amount END) AS UnpaidDays_Amount

        -- Named allowances.
        , SUM(CASE WHEN wt.Code IN ('1062')        OR wtSrc.Code IN ('1062')        THEN srd.Amount END) AS BasicAdjust_Amount
        , SUM(CASE WHEN wt.Code IN ('1001')        OR wtSrc.Code IN ('1001')        THEN srd.Amount END) AS HousingAllowance_Amount
        , SUM(CASE WHEN wt.Code IN ('1002','1066') OR wtSrc.Code IN ('1002','1066') THEN srd.Amount END) AS TransportAllowance_Amount
        , SUM(CASE WHEN wt.Code IN ('1026','1068') OR wtSrc.Code IN ('1026','1068') THEN srd.Amount END) AS InchargeAllowance_Amount
        , SUM(CASE WHEN wt.Code IN ('1018','1067') OR wtSrc.Code IN ('1018','1067') THEN srd.Amount END) AS OtherAllowance_Amount

        -- Overtime. Legacy tested 'WageType.OID IN (SELECT DISTINCT WageType FROM OvertimeType)';
        -- HCMv3 models the same link as OvertimeTypes.WageTypeId, so the FK is used directly.
        -- NOTE the deliberate asymmetry, faithful to legacy: the cost-centre family did NOT allow the
        -- arrear-source alternative for OT, the summary report DID. Both are represented -- OT_* has
        -- no wtSrc term, AllOT_/NormalOT_/WeekendOT_/PHOT_ do.
        , SUM(CASE WHEN otAny.WageTypeId IS NOT NULL
                   THEN srd.Number END) AS OT_Number
        , SUM(CASE WHEN otAny.WageTypeId IS NOT NULL
                   THEN srd.Amount END) AS OT_Amount
        , SUM(CASE WHEN otNormal.WageTypeId IS NOT NULL
                     OR otNormalSrc.WageTypeId IS NOT NULL
                   THEN srd.Number END) AS NormalOT_Number
        , SUM(CASE WHEN otWeekend.WageTypeId IS NOT NULL
                     OR otWeekendSrc.WageTypeId IS NOT NULL
                   THEN srd.Number END) AS WeekendOT_Number
        , SUM(CASE WHEN otPH.WageTypeId IS NOT NULL
                     OR otPHSrc.WageTypeId IS NOT NULL
                   THEN srd.Number END) AS PHOT_Number
        , SUM(CASE WHEN otAny.WageTypeId IS NOT NULL
                     OR otAnySrc.WageTypeId IS NOT NULL
                   THEN srd.Amount END) AS AllOT_Amount
        , SUM(CASE WHEN wt.Code IN ('1043') OR wtSrc.Code IN ('1043') THEN srd.Amount END) AS DirectOT_Amount

        -- Leave-related pay.
        , SUM(CASE WHEN wt.Code IN ('1052')                          THEN srd.Amount END) AS ALPayFractional_Amount   -- legacy: no arrear alternative
        , SUM(CASE WHEN wt.Code IN ('1020','1005','1069','1070')
                     OR wtSrc.Code IN ('1020','1005','1069','1070')  THEN srd.Amount END) AS MaternityHajj_Amount
        , SUM(CASE WHEN wt.Code IN ('1013') OR wtSrc.Code IN ('1013') THEN srd.Number END) AS SickLeave_Number
        , SUM(CASE WHEN wt.Code IN ('1013') OR wtSrc.Code IN ('1013') THEN srd.Amount END) AS SickLeave_Amount
        , SUM(CASE WHEN wt.Code IN ('1021') OR wtSrc.Code IN ('1021') THEN srd.Amount END) AS DeathLeave_Amount
        , SUM(CASE WHEN wt.Code IN ('1003') OR wtSrc.Code IN ('1003') THEN srd.Number END) AS AnnualPaidLeave_Number
        , SUM(CASE WHEN wt.Code IN ('1003') OR wtSrc.Code IN ('1003') THEN srd.Amount END) AS AnnualPaidLeave_Amount

        -- ""Other leave"" genuinely means two different things in the legacy corpus. Both are kept.
        --   cost-centre family : Code IN ('1003','1061')  (arrear alternative only for '1061')
        --   summary report     : any wage type an AbsenceType points at, except '1003'/'1013'
        , SUM(CASE WHEN wt.Code IN ('1003','1061') OR wtSrc.Code IN ('1061') THEN srd.Amount END) AS OtherLeaveCostCentre_Amount
        , SUM(CASE WHEN (absAny.WageTypeId IS NOT NULL AND wt.Code    NOT IN ('1003','1013'))
                     OR (absAnySrc.WageTypeId IS NOT NULL AND wtSrc.Code NOT IN ('1003','1013'))
                   THEN srd.Amount END) AS OtherLeaveSummary_Amount

        -- Other pay.
        , SUM(CASE WHEN wt.Code IN ('1017') OR wtSrc.Code IN ('1017') THEN srd.Amount END) AS MiscPay1017_Amount
        , SUM(CASE WHEN wt.Code IN ('1053') OR wtSrc.Code IN ('1053') THEN srd.Amount END) AS BonusPay_Amount
        , SUM(CASE WHEN wt.Code IN ('1004') OR wtSrc.Code IN ('1004') THEN srd.Amount END) AS AirTicketPay_Amount
        , SUM(CASE WHEN (wt.IncludeInCalculatedPay    = 1 AND wt.BillingType    = 0 AND wt.Code    NOT IN ('1043','1052','1053','1069','1070'))
                     OR (wtSrc.IncludeInCalculatedPay = 1 AND wtSrc.BillingType = 0 AND wtSrc.Code NOT IN ('1043','1052','1053','1069','1070'))
                   THEN srd.Amount END) AS AllOtherPay_Amount

        -- The deduction mirror of AllOtherPay: BillingType = 1 rather than 0. Legacy built it as the
        -- SrDetail_AllOtherDed derived table (rptPayrollSheetByCostCenter.resx), whose predicate is
        -- reproduced verbatim here, including the six excluded codes -- those are the deductions that
        -- already have their own named bucket, so counting them here would double-count them.
        -- This bucket was REFERENCED by the cost-centre family but never produced, which is an
        -- Invalid column name 'AllOtherDed_Amount' at run time; it was masked until the
        -- subquery-inside-aggregate error above was fixed, because that one failed first.
        , SUM(CASE WHEN (wt.IncludeInCalculatedPay    = 1 AND wt.BillingType    = 1 AND wt.Code    NOT IN ('2001','2002','2014','2027','2018','2028'))
                     OR (wtSrc.IncludeInCalculatedPay = 1 AND wtSrc.BillingType = 1 AND wtSrc.Code NOT IN ('2001','2002','2014','2027','2018','2028'))
                   THEN srd.Amount END) AS AllOtherDed_Amount

        -- rptPayrollSheet's ""MiscPay"" / ""MiscDed"" -- the *calculated pay* totals. Same NAME as the
        -- cost-centre family's MiscPay above, completely different definition; kept apart on purpose.
        , SUM(CASE WHEN (wt.IncludeInCalculatedPay    = 1 AND wt.BillingType    = 0 AND wt.DontImpactNetPay    = 0)
                     OR (wtSrc.IncludeInCalculatedPay = 1 AND wtSrc.BillingType = 0 AND wtSrc.DontImpactNetPay = 0)
                   THEN srd.Amount END) AS CalcPay_Amount
        , SUM(CASE WHEN ((wt.IncludeInCalculatedPay    = 1 AND wt.BillingType    = 1 AND wt.DontImpactNetPay    = 0
                          AND loanAny.WageTypeId IS NULL)
                     OR  (wtSrc.IncludeInCalculatedPay = 1 AND wtSrc.BillingType = 1 AND wtSrc.DontImpactNetPay = 0
                          AND loanAnySrc.WageTypeId IS NULL))
                   THEN srd.Amount END) AS CalcDed_Amount

        -- Gross / total earnings. Signed: a deduction line inside the gross bucket subtracts.
        , SUM(CASE WHEN (wt.BillingType = 0 AND wt.DontImpactNetPay = 0 AND wt.Code <> '9000') OR wtSrc.Code IN ('2005')
                   THEN CASE WHEN wt.BillingType = 1 THEN -srd.Amount ELSE srd.Amount END END) AS GrossSalary_Amount
        , SUM(CASE WHEN (wt.BillingType    = 0 AND wt.DontImpactNetPay    = 0 AND wt.Code <> '9000')
                     OR (wtSrc.BillingType = 0 AND wtSrc.DontImpactNetPay = 0)
                   THEN srd.Amount END) AS TotalEarnings_Amount

        -- Named deductions.
        , SUM(CASE WHEN wt.Code IN ('2001','2028') OR wtSrc.Code IN ('2001','2028') THEN srd.Amount END) AS LoanRecoveryDed_Amount
        , SUM(CASE WHEN wt.Code IN ('2002')        OR wtSrc.Code IN ('2002')        THEN srd.Amount END) AS SocialSecurityDed_Amount
        , SUM(CASE WHEN wt.Code IN ('2014')        OR wtSrc.Code IN ('2014')        THEN srd.Amount END) AS LastSalaryDed_Amount
        , SUM(CASE WHEN wt.Code IN ('2027')        OR wtSrc.Code IN ('2027')        THEN srd.Amount END) AS TardinessDed_Amount
        , SUM(CASE WHEN wt.Code IN ('2018')        OR wtSrc.Code IN ('2018')        THEN srd.Amount END) AS MobileChargesDed_Amount
        , SUM(CASE WHEN wt.Code IN ('2019')        OR wtSrc.Code IN ('2019')        THEN srd.Amount END) AS ResiDed_Amount
        , SUM(CASE WHEN wt.Code IN ('2022')        OR wtSrc.Code IN ('2022')        THEN srd.Amount END) AS WarningLetterDed_Amount
        , SUM(CASE WHEN wt.Code IN ('2002','2024') OR wtSrc.Code IN ('2002','2024') THEN srd.Amount END) AS PIFSSDed_Amount
        , SUM(CASE WHEN wt.Code IN ('2028')        OR wtSrc.Code IN ('2028')        THEN srd.Amount END) AS RentDed_Amount
        , SUM(CASE WHEN wt.Code IN ('2005')        OR wtSrc.Code IN ('2005')        THEN srd.Amount END) AS AdvancePaymentDed_Amount

        -- Loan recovery, rptPayrollSheet flavour: ""any wage type a LoanType points at"" rather than
        -- the hard-coded codes 2001/2028 used by the cost-centre family.
        , SUM(CASE WHEN (loanAny.WageTypeId IS NOT NULL AND wt.DontImpactNetPay = 0)
                     OR  loanAnySrc.WageTypeId IS NOT NULL
                   THEN srd.Amount END) AS LoanTypeDed_Amount

        , SUM(CASE WHEN (wt.BillingType = 1 OR wtSrc.BillingType = 1) AND wt.Code NOT IN ('8005')
                   THEN srd.Amount END) AS TotalDeductionCostCentre_Amount
        , SUM(CASE WHEN (wt.BillingType    = 1 AND wt.DontImpactNetPay    = 0)
                     OR (wtSrc.BillingType = 1 AND wtSrc.DontImpactNetPay = 0)
                   THEN srd.Amount END) AS TotalDeductionSummary_Amount

        -- Net pay. Two flavours, both legacy: the carrier line (Code 9000, what payroll actually
        -- wrote) and the recomputed signed total. Legacy 'WageType <> 65' meant Code '9000'.
        , SUM(CASE WHEN wt.Code = '9000' THEN srd.Amount END) AS NetPayCarrier_Amount
        , SUM(CASE WHEN wt.BillingType IN (0, 1) AND wt.DontImpactNetPay = 0 AND wt.Code NOT IN ('9000')
                   THEN CASE WHEN wt.BillingType = 1 THEN -srd.Amount ELSE srd.Amount END END) AS NetPaySigned_Amount

    FROM SalaryRegisterDetails srd
    INNER JOIN SalaryRegisters srb
            ON srb.Id = srd.SalaryRegisterId AND srb.TenantId = @TenantId AND srb.IsDeleted = 0
    INNER JOIN WageTypes wt
            ON wt.Id = srd.WageTypeId AND wt.TenantId = @TenantId AND wt.IsDeleted = 0
    LEFT JOIN WageTypes wtSrc
           ON wtSrc.Id = wt.ArrearSourceWageTypeId AND wtSrc.TenantId = @TenantId AND wtSrc.IsDeleted = 0

    -- MEMBERSHIP SETS. SQL Server rejects a subquery inside an aggregate's argument --
    -- ""Cannot perform an aggregate function on an expression containing an aggregate or a
    -- subquery"" -- which is exactly what SUM(CASE WHEN wt.Id IN (SELECT ...)) is. The legacy
    -- copies got away with it because they tested the same sets OUTSIDE any aggregate. The lookups
    -- are therefore pre-joined once here and the CASE tests become plain IS NULL / IS NOT NULL
    -- checks. Each set is joined twice: once for the wage type itself (wt) and once for the arrear
    -- source wage type (wtSrc), because several buckets accept either.
    LEFT JOIN (SELECT DISTINCT ot.WageTypeId FROM OvertimeTypes ot
                WHERE ot.TenantId = @TenantId AND ot.IsDeleted = 0 AND ot.WageTypeId IS NOT NULL) otAny
           ON otAny.WageTypeId = wt.Id
    LEFT JOIN (SELECT DISTINCT ot.WageTypeId FROM OvertimeTypes ot
                WHERE ot.TenantId = @TenantId AND ot.IsDeleted = 0 AND ot.WageTypeId IS NOT NULL) otAnySrc
           ON otAnySrc.WageTypeId = wtSrc.Id
    LEFT JOIN (SELECT DISTINCT ot.WageTypeId FROM OvertimeTypes ot
                WHERE ot.TenantId = @TenantId AND ot.IsDeleted = 0 AND ot.NameEn IN ('Normal Day OT') AND ot.WageTypeId IS NOT NULL) otNormal
           ON otNormal.WageTypeId = wt.Id
    LEFT JOIN (SELECT DISTINCT ot.WageTypeId FROM OvertimeTypes ot
                WHERE ot.TenantId = @TenantId AND ot.IsDeleted = 0 AND ot.NameEn IN ('Normal Day OT') AND ot.WageTypeId IS NOT NULL) otNormalSrc
           ON otNormalSrc.WageTypeId = wtSrc.Id
    LEFT JOIN (SELECT DISTINCT ot.WageTypeId FROM OvertimeTypes ot
                WHERE ot.TenantId = @TenantId AND ot.IsDeleted = 0 AND ot.NameEn IN ('Weekend OT') AND ot.WageTypeId IS NOT NULL) otWeekend
           ON otWeekend.WageTypeId = wt.Id
    LEFT JOIN (SELECT DISTINCT ot.WageTypeId FROM OvertimeTypes ot
                WHERE ot.TenantId = @TenantId AND ot.IsDeleted = 0 AND ot.NameEn IN ('Weekend OT') AND ot.WageTypeId IS NOT NULL) otWeekendSrc
           ON otWeekendSrc.WageTypeId = wtSrc.Id
    LEFT JOIN (SELECT DISTINCT ot.WageTypeId FROM OvertimeTypes ot
                WHERE ot.TenantId = @TenantId AND ot.IsDeleted = 0 AND ot.NameEn IN ('Public Holiday OT') AND ot.WageTypeId IS NOT NULL) otPH
           ON otPH.WageTypeId = wt.Id
    LEFT JOIN (SELECT DISTINCT ot.WageTypeId FROM OvertimeTypes ot
                WHERE ot.TenantId = @TenantId AND ot.IsDeleted = 0 AND ot.NameEn IN ('Public Holiday OT') AND ot.WageTypeId IS NOT NULL) otPHSrc
           ON otPHSrc.WageTypeId = wtSrc.Id
    LEFT JOIN (SELECT DISTINCT at.WageTypeId FROM AbsenceTypes at
                WHERE at.TenantId = @TenantId AND at.IsDeleted = 0 AND at.WageTypeId IS NOT NULL) absAny
           ON absAny.WageTypeId = wt.Id
    LEFT JOIN (SELECT DISTINCT at.WageTypeId FROM AbsenceTypes at
                WHERE at.TenantId = @TenantId AND at.IsDeleted = 0 AND at.WageTypeId IS NOT NULL) absAnySrc
           ON absAnySrc.WageTypeId = wtSrc.Id
    LEFT JOIN (SELECT DISTINCT lt.WageTypeId FROM LoanTypes lt
                WHERE lt.TenantId = @TenantId AND lt.IsDeleted = 0 AND lt.WageTypeId IS NOT NULL) loanAny
           ON loanAny.WageTypeId = wt.Id
    LEFT JOIN (SELECT DISTINCT lt.WageTypeId FROM LoanTypes lt
                WHERE lt.TenantId = @TenantId AND lt.IsDeleted = 0 AND lt.WageTypeId IS NOT NULL) loanAnySrc
           ON loanAnySrc.WageTypeId = wtSrc.Id
    WHERE srd.TenantId = @TenantId
      AND srd.IsDeleted = 0
      AND srb.InPeriod = srb.ForPeriod
      AND DATEPART(Month, srb.ForPeriod) = @PrmMonth
      AND DATEPART(Year,  srb.ForPeriod) = @PrmYear
    GROUP BY srd.SalaryRegisterId";

        // =============================================================================================
        // SHARED FRAGMENT 2 -- tardiness (late) hours.
        //
        // The odd one out: sourced from EmployeePayments, not SalaryRegisterDetails, and keyed on the
        // employee rather than the register, so it cannot fold into Buckets. Legacy summed
        // EmployeePayment.Amount for wage-type Code 2027 in the reporting month and labelled the
        // result ""LateHours"" -- an amount displayed as hours. DEFECT CARRIED OVER (see notes).
        // =============================================================================================
        private const string TardinessHours = @"
    SELECT ep.EmployeeId, SUM(ep.Amount) AS LateHours
    FROM EmployeePayments ep
    INNER JOIN WageTypes wtLate
            ON wtLate.Id = ep.WageTypeId AND wtLate.TenantId = @TenantId AND wtLate.IsDeleted = 0
           AND wtLate.Code IN ('2027')
    WHERE ep.TenantId = @TenantId
      AND ep.IsDeleted = 0
      AND DATEPART(Month, ep.PaymentDate) = @PrmMonth
      AND DATEPART(Year,  ep.PaymentDate) = @PrmYear
    GROUP BY ep.EmployeeId";

        // =============================================================================================
        // SHARED FRAGMENT 3 -- the employee/organisation dimension joins.
        //
        // ORG LEVELS -- the legacy OrgLevel1..7 walk, translated properly.
        //
        // Legacy EmployeeOrgAssignment carried seven flat FK columns (OrgLevel1..OrgLevel7) into
        // OrganizationUnit, and each report picked whichever of the seven pointed at a unit whose
        // OrganizationUnitType was 'Company' / 'Division' / 'Department' / 'Section'. That is up to
        // 28 aliased joins per report.
        //
        // HCMv3 EmployeeOrgAssignments has a SINGLE OrganizationUnitId into AbpOrganizationUnits,
        // ABP's materialised-path tree (Code / ParentId). The unit TYPE still exists -- it moved off
        // the ABP table onto the HCMv3-side OrgUnitProfiles.OrganizationUnitTypeId ->
        // OrganizationUnitTypes. So the legacy intent IS expressible: walk from the employee's own
        // unit up its materialised path and take the nearest-to-root ancestor whose profile type is
        // named 'Company' (resp. 'Division' / 'Department' / 'Section'). Four OUTER APPLYs replace
        // the 28 joins. This follows Bank/BankSql.cs, which does the same walk.
        //
        // (Do NOT copy the fallback in PayslipSql.cs / SalaryCertificateSql.cs, which concluded the
        // walk was inexpressible and substituted the employee's own unit. It is expressible; those
        // two are being corrected centrally.)
        //
        // AbpOrganizationUnits has only DisplayName (no NameEn/NameAr), so the Ar columns repeat the
        // English value -- there is no Arabic org-unit name anywhere in HCMv3.
        //
        // CompanyLogo: legacy read OrganizationUnit.Logo, an image column; HCMv3 keeps logos in the
        // file store. Companylogo stays a NULL placeholder and DimensionColumns also returns the
        // employee's unit (eoa.OrganizationUnitId) as CompanylogoOrgUnitId; HcmReportBase resolves the
        // logo from it through ICompanyLogoResolver (legacy Company_1..7 rule). rptPayrollSheet's own
        // header logo is NOT this column -- it binds the separate CompanyLogoQuery, see there.
        // rptPayrollSheetBySponsor: legacy also grouped by the logo IMAGE, so employees of companies
        // with different logos fell into separate rows. That is not reproduced (the grouping and the
        // totals stay as already ported); a row spanning companies shows MIN(unit)'s company logo.
        //
        // The period anchor is the reporting month end, matching the legacy
        // ""EOMONTH(STR(@PrmYear)+'-'+STR(@PrmMonth)+'-01')"" expression, written as the equivalent
        // EOMONTH(DATEFROMPARTS(...)) because the legacy string form relies on implicit conversion.
        // =============================================================================================
        private const string EmployeeDimensions = @"
LEFT JOIN EmployeeOrgAssignments eoa
       ON eoa.EmployeeId = emp.Id AND eoa.TenantId = @TenantId AND eoa.IsDeleted = 0
      AND EOMONTH(DATEFROMPARTS(@PrmYear, @PrmMonth, 1)) BETWEEN eoa.StartDate AND eoa.EndDate
LEFT JOIN JobTitles       jobTitle ON jobTitle.Id = eoa.JobTitleId       AND jobTitle.TenantId = @TenantId AND jobTitle.IsDeleted = 0
LEFT JOIN PayrollAreas    payrollArea ON payrollArea.Id = eoa.PayrollAreaId AND payrollArea.TenantId = @TenantId AND payrollArea.IsDeleted = 0
LEFT JOIN CostCenters     costCenter ON costCenter.Id = eoa.CostCenterId AND costCenter.TenantId = @TenantId AND costCenter.IsDeleted = 0
LEFT JOIN CostCenterGroups costCenterGroup ON costCenterGroup.Id = costCenter.CostCenterGroupId AND costCenterGroup.TenantId = @TenantId AND costCenterGroup.IsDeleted = 0
LEFT JOIN PostingGroups   postingGroup ON postingGroup.Id = eoa.PostingGroupId AND postingGroup.TenantId = @TenantId AND postingGroup.IsDeleted = 0
LEFT JOIN BudgetingGroups budgetingGroup ON budgetingGroup.Id = eoa.BudgetingGroupId AND budgetingGroup.TenantId = @TenantId AND budgetingGroup.IsDeleted = 0
LEFT JOIN AbpOrganizationUnits orgUnit
       ON orgUnit.Id = eoa.OrganizationUnitId AND orgUnit.TenantId = @TenantId AND orgUnit.IsDeleted = 0

-- the seven OrgLevel1..7 'Company' joins, collapsed to one ancestor walk
OUTER APPLY (
    SELECT TOP 1 anc.DisplayName
    FROM AbpOrganizationUnits anc
    INNER JOIN OrgUnitProfiles ancp       ON ancp.OrganizationUnitId = anc.Id      AND ancp.TenantId = @TenantId AND ancp.IsDeleted = 0
    INNER JOIN OrganizationUnitTypes anct ON anct.Id = ancp.OrganizationUnitTypeId AND anct.TenantId = @TenantId AND anct.IsDeleted = 0
    WHERE anc.TenantId = @TenantId AND anc.IsDeleted = 0
      AND orgUnit.Code LIKE anc.Code + '%'
      AND anct.NameEn = 'Company'
    ORDER BY LEN(anc.Code)
) company
OUTER APPLY (
    SELECT TOP 1 anc.DisplayName
    FROM AbpOrganizationUnits anc
    INNER JOIN OrgUnitProfiles ancp       ON ancp.OrganizationUnitId = anc.Id      AND ancp.TenantId = @TenantId AND ancp.IsDeleted = 0
    INNER JOIN OrganizationUnitTypes anct ON anct.Id = ancp.OrganizationUnitTypeId AND anct.TenantId = @TenantId AND anct.IsDeleted = 0
    WHERE anc.TenantId = @TenantId AND anc.IsDeleted = 0
      AND orgUnit.Code LIKE anc.Code + '%'
      AND anct.NameEn = 'Division'
    ORDER BY LEN(anc.Code)
) division
OUTER APPLY (
    SELECT TOP 1 anc.DisplayName
    FROM AbpOrganizationUnits anc
    INNER JOIN OrgUnitProfiles ancp       ON ancp.OrganizationUnitId = anc.Id      AND ancp.TenantId = @TenantId AND ancp.IsDeleted = 0
    INNER JOIN OrganizationUnitTypes anct ON anct.Id = ancp.OrganizationUnitTypeId AND anct.TenantId = @TenantId AND anct.IsDeleted = 0
    WHERE anc.TenantId = @TenantId AND anc.IsDeleted = 0
      AND orgUnit.Code LIKE anc.Code + '%'
      AND anct.NameEn = 'Department'
    ORDER BY LEN(anc.Code)
) department
OUTER APPLY (
    SELECT TOP 1 anc.DisplayName
    FROM AbpOrganizationUnits anc
    INNER JOIN OrgUnitProfiles ancp       ON ancp.OrganizationUnitId = anc.Id      AND ancp.TenantId = @TenantId AND ancp.IsDeleted = 0
    INNER JOIN OrganizationUnitTypes anct ON anct.Id = ancp.OrganizationUnitTypeId AND anct.TenantId = @TenantId AND anct.IsDeleted = 0
    WHERE anc.TenantId = @TenantId AND anc.IsDeleted = 0
      AND orgUnit.Code LIKE anc.Code + '%'
      AND anct.NameEn = 'Section'
    ORDER BY LEN(anc.Code)
) section

LEFT JOIN EmployeeBanks empBank
       ON empBank.EmployeeId = emp.Id AND empBank.TenantId = @TenantId AND empBank.IsDeleted = 0
      AND EOMONTH(DATEFROMPARTS(@PrmYear, @PrmMonth, 1)) BETWEEN empBank.StartDate AND empBank.EndDate
LEFT JOIN EmployeeBasicPays empBasicPay
       ON empBasicPay.EmployeeId = emp.Id AND empBasicPay.TenantId = @TenantId AND empBasicPay.IsDeleted = 0
      AND EOMONTH(DATEFROMPARTS(@PrmYear, @PrmMonth, 1)) BETWEEN empBasicPay.StartDate AND empBasicPay.EndDate";

        /// <summary>
        /// The organisation / employee descriptive columns every report in the family projects.
        /// Kept as one fragment so a rename lands everywhere at once.
        /// </summary>
        private const string DimensionColumns = @"
        , emp.Code
        , emp.FullNameEn
        , emp.FullNameAr
        , emp.JoiningDate
        , jobTitle.NameEn                                   AS JobTitleEn
        , jobTitle.NameAr                                   AS JobTitleAr
        , payrollArea.Name                                  AS PayrollArea
        , costCenter.Code                                   AS CostCenterCode
        , costCenter.Code + '-' + costCenter.NameEn         AS CostCenterEn
        , costCenter.NameAr                                 AS CostCenterAr
        , costCenterGroup.NameEn                            AS CostCenterGroup
        , postingGroup.Code                                 AS PostingGroupCode
        , postingGroup.NameEn                               AS PostingGroupEn
        , postingGroup.NameAr                               AS PostingGroupAr
        , budgetingGroup.Code                               AS BudgetingGroupCode
        , budgetingGroup.NameEn                             AS BudgetingGroupEn
        , budgetingGroup.NameAr                             AS BudgetingGroupAr
        , company.DisplayName                               AS CompanyEn      -- see EmployeeDimensions
        , company.DisplayName                               AS CompanyAr      -- no Arabic org-unit name in HCMv3
        , department.DisplayName                            AS DepartmentEn
        , department.DisplayName                            AS DepartmentAr
        , division.DisplayName                              AS DivisionEn
        , section.DisplayName                               AS SectionsEn
        , CAST(NULL AS VARBINARY(MAX))                      AS Companylogo    -- placeholder, see EmployeeDimensions
        , eoa.OrganizationUnitId                            AS CompanylogoOrgUnitId   -- see EmployeeDimensions
        , CASE WHEN empBank.PaymentType = 0 OR empBank.PaymentType IS NULL THEN 'BANK' ELSE 'CASH' END AS PaymentType
        , CASE WHEN ISNULL(empBasicPay.PayInterval, 0) = 0 THEN 'MONTHLY' ELSE 'DAILY' END            AS PayInterval";

        /// <summary>
        /// The legacy employee sort key, preserved verbatim: numeric codes right-aligned zero-padded
        /// to 20, non-numeric left-aligned. (REPLICATE('', 21) is a no-op; kept for byte parity, as in
        /// PayslipSql.)
        /// </summary>
        private const string CodeSortOrder = @"
ORDER BY
    CASE WHEN ISNUMERIC(B.Code) = 1 THEN RIGHT(REPLICATE('0', 21) + B.Code, 20)
         WHEN ISNUMERIC(B.Code) = 0 THEN LEFT(B.Code + REPLICATE('', 21), 20)
         ELSE B.Code
    END;";

        /// <summary>
        /// ""Is this employee in scope this month?"" -- shared by the four cost-centre-family reports.
        /// If the employee has a separation record, keep them while their last day in service has not
        /// yet passed; otherwise keep them only while Status = 1 (active).
        /// </summary>
        private const string InServiceFilter = @"
  AND (
        (    EXISTS (SELECT 1 FROM EmployeeSeparations es WHERE es.EmployeeId = emp.Id AND es.TenantId = @TenantId AND es.IsDeleted = 0)
         AND NOT EXISTS (SELECT 1 FROM EmployeeSeparations es
                         WHERE es.EmployeeId = emp.Id AND es.TenantId = @TenantId AND es.IsDeleted = 0
                           AND EOMONTH(es.LastDayInService) < EOMONTH(DATEFROMPARTS(@PrmYear, @PrmMonth, 1)))
        )
     OR (NOT EXISTS (SELECT 1 FROM EmployeeSeparations es WHERE es.EmployeeId = emp.Id AND es.TenantId = @TenantId AND es.IsDeleted = 0)
         AND emp.Status = 1)
      )";

        /// <summary>
        /// Run-type selection shared by the four cost-centre-family reports:
        ///   -1 (ALL)      take the off-cycle register first, then the regular one (RankOrder = 1)
        ///    0 (Regular)  drop employees who also have exactly one off-cycle register this month
        ///    1 (OffCycle) keep off-cycle registers only
        /// </summary>
        private const string RunTypeSelection = @"
  AND ((@PrmPayrollRunType = -1 AND B.RankOrder = 1) OR (@PrmPayrollRunType <> -1 AND 1 = 1))
  AND ((@PrmPayrollRunType = 0 AND B.EmployeeId NOT IN (
            SELECT sro.EmployeeId
            FROM SalaryRegisters sro
            WHERE sro.TenantId = @TenantId AND sro.IsDeleted = 0
              AND sro.InPeriod = sro.ForPeriod
              AND DATEPART(Month, sro.ForPeriod) = @PrmMonth
              AND DATEPART(Year,  sro.ForPeriod) = @PrmYear
              AND sro.PayrollRunType = 1
            GROUP BY sro.EmployeeId
            HAVING COUNT(sro.EmployeeId) = 1))
       OR (@PrmPayrollRunType <> 0 AND 1 = 1))
  AND ((@PrmPayrollRunType = 1 AND B.PayrollRunType = 'OffCycle') OR (@PrmPayrollRunType <> 1 AND 1 = 1))";

        /// <summary>
        /// ""Was this employee on a pay-in-advance leave that swallowed the whole month, and did that
        /// leave end during it?"" -- drives the HasFullMonthLeave / HasLeaveEnd zeroing in the three
        /// cost-centre-family detail reports and in rptSalaryByBudgetingGroup.
        /// </summary>
        private const string LeaveStatus = @"
    SELECT
          emp2.Id AS EmployeeId
        , CASE WHEN fullMonth.EmployeeId IS NOT NULL THEN 1 ELSE 0 END AS HasFullMonthLeave
        , CASE WHEN leaveEnd.EmployeeId  IS NOT NULL THEN 1 ELSE 0 END AS HasLeaveEnd
    FROM Employees emp2
    OUTER APPLY (
        SELECT TOP 1 ea.EmployeeId
        FROM EmployeeAbsences ea
        WHERE ea.EmployeeId = emp2.Id AND ea.TenantId = @TenantId AND ea.IsDeleted = 0
          AND ea.LeavePaymentType = 2
          AND (ea.StartDate <= DATEFROMPARTS(@PrmYear, @PrmMonth, 1)
               OR ea.PaymentDate <= DATEFROMPARTS(@PrmYear, @PrmMonth, 1))
          AND ea.EndDate >= EOMONTH(DATEFROMPARTS(@PrmYear, @PrmMonth, 1))
    ) fullMonth
    OUTER APPLY (
        SELECT TOP 1 ea.EmployeeId
        FROM EmployeeAbsences ea
        INNER JOIN AbsenceTypes at2
                ON at2.Id = ea.AbsenceTypeId AND at2.TenantId = @TenantId AND at2.IsDeleted = 0
               AND at2.IsPaid = 1
        WHERE ea.EmployeeId = emp2.Id AND ea.TenantId = @TenantId AND ea.IsDeleted = 0
          AND ea.LeavePaymentType = 2
          AND ea.EndDate >= DATEFROMPARTS(@PrmYear, @PrmMonth, 1)
          AND ea.EndDate <= EOMONTH(DATEFROMPARTS(@PrmYear, @PrmMonth, 1))
    ) leaveEnd
    WHERE emp2.TenantId = @TenantId AND emp2.IsDeleted = 0";

        // =============================================================================================
        // REPORT QUERIES
        // =============================================================================================

        /// <summary>
        /// rptPayrollSheet :: PayrollSheetQuery.
        /// The flat detail sheet: one row per employee/register, bucketed into basic / allowance /
        /// annual leave / sick leave / misc pay / misc ded / loan recovery / net pay.
        /// </summary>
        public const string PayrollSheetQuery = @"-- rptPayrollSheet :: PayrollSheetQuery (master)
-- HCMv3 rewrite. Shared body: PayrollSheetSql.Buckets + PayrollSheetSql.EmployeeDimensions.
-- Legacy filtered on @PrmPayrollRunType with an ""IS NULL means all"" escape; kept verbatim.
SELECT B.*
FROM (
    SELECT
          sr.ForPeriod
        , sr.PayrollRunType" + DimensionColumns + @"
        , ISNULL(b.BasicPay_Rate, 0)                                        AS BasicSalary_Rate
        , ISNULL(b.Allowance_Rate, 0)                                       AS Allowance_Rate
        , ISNULL(b.BasicPay_Rate, 0) + ISNULL(b.Allowance_Rate, 0)          AS TotalSalary_Rate
        , ISNULL(b.BasicPay_Number, 0)                                      AS WorkingDays
        , ISNULL(b.BasicPay_Amount, 0)                                      AS BasicSalary
        , ISNULL(b.Allowance_Amount, 0)                                     AS AllAllowance
        , ISNULL(b.BasicPay_Amount, 0) + ISNULL(b.Allowance_Amount, 0)      AS TotalSalaryPaid
        , ISNULL(b.AnnualPaidLeave_Number, 0)                               AS AnnualLeaveDay
        , ISNULL(b.AnnualPaidLeave_Amount, 0)                               AS AnnualLeavePay
        , ISNULL(b.SickLeave_Number, 0)                                     AS SickLeaveDays
        , ISNULL(b.SickLeave_Amount, 0)                                     AS SickLeavePay
        , ISNULL(b.CalcPay_Amount, 0)                                       AS MiscPay
        , ISNULL(b.CalcDed_Amount, 0)                                       AS MiscDed
        , ISNULL(b.LoanTypeDed_Amount, 0)                                   AS LoanRecoveryDed
        , ISNULL(b.NetPayCarrier_Amount, 0)                                 AS NetPay
    FROM SalaryRegisters sr
    INNER JOIN Employees emp
            ON emp.Id = sr.EmployeeId AND emp.TenantId = @TenantId AND emp.IsDeleted = 0" + EmployeeDimensions + @"
    LEFT JOIN (" + Buckets + @"
    ) b ON b.SalaryRegisterId = sr.Id
    WHERE sr.TenantId = @TenantId
      AND sr.IsDeleted = 0
      AND sr.InPeriod = sr.ForPeriod
      AND DATEPART(Month, sr.ForPeriod) = @PrmMonth
      AND DATEPART(Year,  sr.ForPeriod) = @PrmYear
      AND ((@PrmPayrollRunType IS NOT NULL AND sr.PayrollRunType = @PrmPayrollRunType)
           OR (@PrmPayrollRunType IS NULL AND 1 = 1))
) B
WHERE 1 = 1
  AND ((ISNULL(@PrmPayrollArea, '') <> '' AND B.PayrollArea  = @PrmPayrollArea) OR (ISNULL(@PrmPayrollArea, '') = '' AND 1 = 1))
  AND ((ISNULL(@PrmCostCenter,  '') <> '' AND B.CostCenterEn = @PrmCostCenter)  OR (ISNULL(@PrmCostCenter,  '') = '' AND 1 = 1))
  AND ((ISNULL(@PrmDepartment,  '') <> '' AND B.DepartmentEn = @PrmDepartment)  OR (ISNULL(@PrmDepartment,  '') = '' AND 1 = 1))
  AND ((ISNULL(@PrmPaymentType, '') <> '' AND B.PaymentType  = @PrmPaymentType) OR (ISNULL(@PrmPaymentType, '') = '' AND 1 = 1))
  AND ((ISNULL(@PrmPayInterval, '') <> '' AND B.PayInterval  = @PrmPayInterval) OR (ISNULL(@PrmPayInterval, '') = '' AND 1 = 1))" + CodeSortOrder;

        /// <summary>
        /// rptPayrollSheetSummary :: PayrollSheetQuery.
        /// One row per employee with the wide earnings / overtime / deduction breakdown, grouped in
        /// the layout by Division / Department / Section.
        /// </summary>
        public const string PayrollSheetSummaryQuery = @"-- rptPayrollSheetSummary :: PayrollSheetQuery (master)
-- HCMv3 rewrite. Shared body: PayrollSheetSql.Buckets + PayrollSheetSql.EmployeeDimensions.
-- OtherEarnings / OtherDeduction are the legacy ""total minus the named buckets"" residuals.
SELECT
      ROW_NUMBER() OVER (ORDER BY B.Code) AS SrNo
    , B.*
    , CONVERT(DECIMAL(18, 3), B.TotalEarnings - (B.BasicSalary + B.AnnualLeavePay + B.SickLeavePay + B.OTAmount))       AS OtherEarnings
    , CONVERT(DECIMAL(18, 3), B.TotalDeduction - (B.ResiDed + B.WaningLetterDed + B.PIFSSDed + B.RentDed))              AS OtherDeduction
FROM (
    SELECT
          sr.ForPeriod
        , sr.PayrollRunType" + DimensionColumns + @"
        , ISNULL(b.BasicPay_Rate, 0)                                        AS BasicSalary_Rate
        , ISNULL(b.Allowance_Rate, 0)                                       AS Allowance_Rate
        , ISNULL(b.BasicPay_Rate, 0) + ISNULL(b.Allowance_Rate, 0)          AS TotalSalary_Rate
        , ISNULL(b.BasicPay_Number, 0)                                      AS WorkingDays
        , ISNULL(b.BasicPay_Amount, 0) + ISNULL(b.Allowance_Amount, 0)      AS BasicSalary
        , ISNULL(b.AnnualPaidLeave_Number, 0)                               AS AnnualLeaveDays
        , ISNULL(b.AnnualPaidLeave_Amount, 0)                               AS AnnualLeavePay
        , ISNULL(b.SickLeave_Number, 0)                                     AS SickLeaveDays
        , ISNULL(b.SickLeave_Amount, 0)                                     AS SickLeavePay
        , ISNULL(b.UnpaidDays_Amount, 0)                                    AS UnpaidDays
        , ISNULL(b.NormalOT_Number, 0)                                      AS NormalOTHours
        , ISNULL(b.WeekendOT_Number, 0)                                     AS WeekendOTHours
        , ISNULL(b.PHOT_Number, 0)                                          AS PHOTHours
        , ISNULL(b.AllOT_Amount, 0)                                         AS OTAmount
        , ISNULL(b.TotalEarnings_Amount, 0)                                 AS TotalEarnings
        , ISNULL(b.ResiDed_Amount, 0)                                       AS ResiDed
        , ISNULL(b.WarningLetterDed_Amount, 0)                              AS WaningLetterDed
        , ISNULL(b.PIFSSDed_Amount, 0)                                      AS PIFSSDed
        , ISNULL(b.RentDed_Amount, 0)                                       AS RentDed
        , ISNULL(b.TotalDeductionSummary_Amount, 0)                         AS TotalDeduction
        , ISNULL(b.NetPayCarrier_Amount, 0)                                 AS NetPay
    FROM SalaryRegisters sr
    INNER JOIN Employees emp
            ON emp.Id = sr.EmployeeId AND emp.TenantId = @TenantId AND emp.IsDeleted = 0" + EmployeeDimensions + @"
    LEFT JOIN (" + Buckets + @"
    ) b ON b.SalaryRegisterId = sr.Id
    WHERE sr.TenantId = @TenantId
      AND sr.IsDeleted = 0
      AND sr.InPeriod = sr.ForPeriod
      AND DATEPART(Month, sr.ForPeriod) = @PrmMonth
      AND DATEPART(Year,  sr.ForPeriod) = @PrmYear
      AND sr.PayrollRunType = @PrmPayrollRunType
) B
WHERE ((ISNULL(@PrmPayrollArea, '') <> '' AND B.PayrollArea  = @PrmPayrollArea) OR (ISNULL(@PrmPayrollArea, '') = '' AND 1 = 1))
  AND ((ISNULL(@PrmDepartment,  '') <> '' AND B.DepartmentEn = @PrmDepartment)  OR (ISNULL(@PrmDepartment,  '') = '' AND 1 = 1))
  AND ((ISNULL(@PrmDivision,    '') <> '' AND B.DivisionEn   = @PrmDivision)    OR (ISNULL(@PrmDivision,    '') = '' AND 1 = 1))
  AND ((ISNULL(@PrmPaymentType, '') <> '' AND B.PaymentType  = @PrmPaymentType) OR (ISNULL(@PrmPaymentType, '') = '' AND 1 = 1))
  AND ((ISNULL(@PrmPayInterval, '') <> '' AND B.PayInterval  = @PrmPayInterval) OR (ISNULL(@PrmPayInterval, '') = '' AND 1 = 1))" + CodeSortOrder;

        // ---------------------------------------------------------------------------------------------
        // The cost-centre family. These four share:
        //   * the same inner projection (bucket columns + RankOrder + leave-status zeroing),
        //   * InServiceFilter, RunTypeSelection, CodeSortOrder.
        // They differ only in the grouping dimension they filter on and the columns the layout prints.
        // ---------------------------------------------------------------------------------------------

        /// <summary>The inner per-employee projection shared by the four cost-centre-family reports.</summary>
        private const string CostCentreFamilyBody = @"
    SELECT
          RANK() OVER (PARTITION BY emp.Code ORDER BY sr.PayrollRunType DESC)  AS RankOrder
        , emp.Id                                                               AS EmployeeId
        , ISNULL(sr.ForPeriod, EOMONTH(DATEFROMPARTS(@PrmYear, @PrmMonth, 1))) AS ForPeriod
        , CASE WHEN sr.PayrollRunType = 1 THEN 'OffCycle' ELSE 'Regular' END   AS PayrollRunType
        , ls.HasFullMonthLeave
        , ls.HasLeaveEnd" + DimensionColumns + @"
        , ISNULL(b.BasicPay_Rate, 0)                                           AS BasicSalary_Rate
        , CASE WHEN sr.PayrollRunType = 0 THEN ISNULL(b.PresentDays_Number, 0)
               ELSE ISNULL(b.BasicPay_Number, 0) END                           AS WorkingDays
        , ISNULL(th.LateHours, 0)                                              AS LateHours
        , ISNULL(b.OT_Number, 0)                                               AS OTHours
        , ISNULL(b.BasicPay_Amount, 0)
            + ISNULL(b.BasicAdjust_Amount, 0)
            + ISNULL(b.SickLeave_Amount, 0)
            + ISNULL(b.MiscPay1017_Amount, 0)                                  AS BasicSalary
        , ISNULL(b.HousingAllowance_Amount, 0)                                 AS HousingAllowance
        , ISNULL(b.TransportAllowance_Amount, 0)                               AS TransportAllowance
        , ISNULL(b.InchargeAllowance_Amount, 0)                                AS InchargeAllowance
        , ISNULL(b.OtherAllowance_Amount, 0)                                   AS OtherAllowance
        , ISNULL(b.OT_Amount, 0) + ISNULL(b.DirectOT_Amount, 0)                AS OTPay
        , ISNULL(b.AnnualPaidLeave_Amount, 0)                                  AS AnnualPaidLeavePay
        -- Legacy: when the advance leave ENDED this month its annual-leave pay was already paid, so
        -- subtract it back out of ""other leave"", the gross and the total-deduction residual.
        , ISNULL(b.OtherLeaveCostCentre_Amount, 0) + ISNULL(b.ALPayFractional_Amount, 0)
            - (CASE WHEN ls.HasLeaveEnd = 1 THEN ISNULL(b.AnnualPaidLeave_Amount, 0) ELSE 0 END) AS OtherLeavePay
        , ISNULL(b.BonusPay_Amount, 0)                                         AS BonusPay
        , ISNULL(b.AirTicketPay_Amount, 0)                                     AS AirTicketPay
        , ISNULL(b.MaternityHajj_Amount, 0)                                    AS Maternity_HajjLeavePay
        , ISNULL(b.AllOtherPay_Amount, 0)                                      AS AllOtherPay
        , ISNULL(b.GrossSalary_Amount, 0)
            - (CASE WHEN ls.HasLeaveEnd = 1 THEN ISNULL(b.AnnualPaidLeave_Amount, 0) ELSE 0 END)  AS GrossSalary
        , ISNULL(b.TardinessDed_Amount, 0)                                     AS TardinessDed
        , ISNULL(b.LoanRecoveryDed_Amount, 0)                                  AS LoanRecoveryDed
        , ISNULL(b.SocialSecurityDed_Amount, 0)                                AS SocialSecurityDed
        , ISNULL(b.LastSalaryDed_Amount, 0)                                    AS LastSalaryDed
        , ISNULL(b.MobileChargesDed_Amount, 0)                                 AS MobileChargesDed
        , ISNULL(b.AdvancePaymentDed_Amount, 0)                                AS AdvancePaymentDed
        , ISNULL(b.AllOtherDed_Amount, 0)
            - (CASE WHEN ls.HasLeaveEnd = 1 THEN ISNULL(b.AdvancePaymentDed_Amount, 0) ELSE 0 END) AS AllOtherDed
        , ISNULL(b.TotalDeductionCostCentre_Amount, 0)
            - (CASE WHEN ls.HasLeaveEnd = 1 THEN ISNULL(b.AdvancePaymentDed_Amount, 0) ELSE 0 END) AS TotalDeduction
        , ISNULL(b.NetPaySigned_Amount, 0)                                     AS NetPay
        , 1                                                                    AS Number
    FROM Employees emp
    LEFT JOIN SalaryRegisters sr
           ON sr.EmployeeId = emp.Id AND sr.TenantId = @TenantId AND sr.IsDeleted = 0
          AND sr.InPeriod = sr.ForPeriod
          AND DATEPART(Month, sr.ForPeriod) = @PrmMonth
          AND DATEPART(Year,  sr.ForPeriod) = @PrmYear
          AND (sr.PayrollReasonTypeId IS NULL
               OR sr.PayrollReasonTypeId NOT IN (SELECT prt.Id FROM PayrollReasonTypes prt
                                                 WHERE prt.TenantId = @TenantId AND prt.IsDeleted = 0
                                                   AND prt.NameEn = 'End Of Service'))
    LEFT JOIN (" + Buckets + @"
    ) b ON b.SalaryRegisterId = sr.Id
    LEFT JOIN (" + TardinessHours + @"
    ) th ON th.EmployeeId = emp.Id
    LEFT JOIN (" + LeaveStatus + @"
    ) ls ON ls.EmployeeId = emp.Id" + EmployeeDimensions + @"
    WHERE emp.TenantId = @TenantId
      AND emp.IsDeleted = 0" + InServiceFilter;

        /// <summary>
        /// The ""a full month of advance leave means nothing is payable this month"" zeroing that the
        /// three detail cost-centre reports apply to every money column of
        /// <see cref="CostCentreFamilyBody"/>.
        /// </summary>
        private const string FullMonthLeaveZeroing = @"
    , CASE WHEN B.HasFullMonthLeave = 1 THEN 0 ELSE B.BasicSalary_Rate END       AS BasicSalary_Rate
    , CASE WHEN B.HasFullMonthLeave = 1 THEN 0 ELSE B.WorkingDays END            AS WorkingDays
    , CASE WHEN B.HasFullMonthLeave = 1 THEN 0 ELSE B.OTHours END                AS OTHours
    , CASE WHEN B.HasFullMonthLeave = 1 THEN 0 ELSE B.LateHours END              AS LateHours
    , CASE WHEN B.HasFullMonthLeave = 1 THEN 0 ELSE B.BasicSalary END            AS BasicSalary
    , CASE WHEN B.HasFullMonthLeave = 1 THEN 0 ELSE B.HousingAllowance END       AS HousingAllowance
    , CASE WHEN B.HasFullMonthLeave = 1 THEN 0 ELSE B.TransportAllowance END     AS TransportAllowance
    , CASE WHEN B.HasFullMonthLeave = 1 THEN 0 ELSE B.InchargeAllowance END      AS InchargeAllowance
    , CASE WHEN B.HasFullMonthLeave = 1 THEN 0 ELSE B.OtherAllowance END         AS OtherAllowance
    , CASE WHEN B.HasFullMonthLeave = 1 THEN 0 ELSE B.OTPay END                  AS OTPay
    , CASE WHEN B.HasFullMonthLeave = 1 THEN 0 ELSE B.AnnualPaidLeavePay END     AS AnnualPaidLeavePay
    , CASE WHEN B.HasFullMonthLeave = 1 THEN 0 ELSE B.OtherLeavePay END          AS OtherLeavePay
    , CASE WHEN B.HasFullMonthLeave = 1 THEN 0 ELSE B.BonusPay END               AS BonusPay
    , CASE WHEN B.HasFullMonthLeave = 1 THEN 0 ELSE B.AirTicketPay END           AS AirTicketPay
    , CASE WHEN B.HasFullMonthLeave = 1 THEN 0 ELSE B.Maternity_HajjLeavePay END AS Maternity_HajjLeavePay
    , CASE WHEN B.HasFullMonthLeave = 1 THEN 0 ELSE B.AllOtherPay END            AS AllOtherPay
    , CASE WHEN B.HasFullMonthLeave = 1 THEN 0 ELSE B.TardinessDed END           AS TardinessDed
    , CASE WHEN B.HasFullMonthLeave = 1 THEN 0 ELSE B.LoanRecoveryDed END        AS LoanRecoveryDed
    , CASE WHEN B.HasFullMonthLeave = 1 THEN 0 ELSE B.SocialSecurityDed END      AS SocialSecurityDed
    , CASE WHEN B.HasFullMonthLeave = 1 THEN 0 ELSE B.LastSalaryDed END          AS LastSalaryDed
    , CASE WHEN B.HasFullMonthLeave = 1 THEN 0 ELSE B.MobileChargesDed END       AS MobileChargesDed
    , CASE WHEN B.HasFullMonthLeave = 1 THEN 0 ELSE B.AdvancePaymentDed END      AS AdvancePaymentDed
    , CASE WHEN B.HasFullMonthLeave = 1 THEN 0 ELSE B.AllOtherDed END            AS AllOtherDed
    , CASE WHEN B.HasFullMonthLeave = 1 THEN 0 ELSE B.TotalDeduction END         AS TotalDeduction
    , CASE WHEN B.HasFullMonthLeave = 1 THEN 0 ELSE B.NetPay END                 AS NetPay";

        /// <summary>
        /// GrossSalary under the same full-month-leave zeroing. Split out because
        /// rptSalaryByBudgetingGroup prints a DIFFERENT quantity under the same column name -- see
        /// <see cref="GrossSalaryLessBasicColumn"/>.
        /// </summary>
        private const string GrossSalaryColumn = @"
    , CASE WHEN B.HasFullMonthLeave = 1 THEN 0 ELSE B.GrossSalary END            AS GrossSalary";

        /// <summary>
        /// rptSalaryByBudgetingGroup's GrossSalary. DEFECT CARRIED OVER: the legacy report labelled
        /// this column "GrossSalary" but computed gross MINUS basic, and its printed totals were
        /// reconciled against that. Reproduced rather than fixed.
        /// </summary>
        private const string GrossSalaryLessBasicColumn = @"
    , (CASE WHEN B.HasFullMonthLeave = 1 THEN 0 ELSE B.GrossSalary END)
      - (CASE WHEN B.HasFullMonthLeave = 1 THEN 0 ELSE B.BasicSalary END)        AS GrossSalary";

        /// <summary>The descriptive (non-money) columns the cost-centre-family outer SELECTs carry through.</summary>
        private const string CostCentreFamilyHeaderColumns = @"
      B.HasFullMonthLeave
    , B.HasLeaveEnd
    , B.Code
    , B.RankOrder
    , B.EmployeeId
    , B.ForPeriod
    , B.PayrollRunType
    , B.PaymentType
    , B.JoiningDate
    , B.FullNameEn
    , B.FullNameAr
    , B.JobTitleEn
    , B.JobTitleAr
    , B.PostingGroupCode
    , B.PostingGroupEn
    , B.PostingGroupAr
    , B.BudgetingGroupCode
    , B.BudgetingGroupEn
    , B.BudgetingGroupAr
    , B.CostCenterEn
    , B.CostCenterCode
    , B.CostCenterAr
    , B.CostCenterGroup
    , B.CompanyEn
    , B.CompanyAr
    , B.Companylogo
    , B.CompanylogoOrgUnitId
    , B.DepartmentEn
    , B.DepartmentAr
    , B.Number";

        /// <summary>rptPayrollSheetByCostCenter :: PayrollSheetQuery. Detail sheet grouped by cost centre.</summary>
        public const string PayrollSheetByCostCenterQuery = @"-- rptPayrollSheetByCostCenter :: PayrollSheetQuery (master)
-- HCMv3 rewrite; shared body: PayrollSheetSql.CostCentreFamilyBody.
--
-- LEGACY DIVERGENCE MADE CONSISTENT: this report alone did NOT apply the HasFullMonthLeave zeroing
-- or the RunTypeSelection block (its copy had them commented out), while its three siblings did.
-- Both are applied here so the four agree -- see PORTING-NOTES-PayrollSheets.md, ""Layout deviations"".
SELECT" + CostCentreFamilyHeaderColumns + FullMonthLeaveZeroing + GrossSalaryColumn + @"
FROM (" + CostCentreFamilyBody + @"
) B
WHERE ((ISNULL(@PrmCompany, '')        <> '' AND B.CompanyEn       = @PrmCompany)        OR (ISNULL(@PrmCompany, '')        = '' AND 1 = 1))
  AND ((ISNULL(@PrmCostCenterGroup,'') <> '' AND B.CostCenterGroup = @PrmCostCenterGroup) OR (ISNULL(@PrmCostCenterGroup,'') = '' AND 1 = 1))
  AND ((ISNULL(@PrmCostCenter, '')     <> '' AND B.CostCenterEn    = @PrmCostCenter)     OR (ISNULL(@PrmCostCenter, '')     = '' AND 1 = 1))" + RunTypeSelection + CodeSortOrder;

        /// <summary>rptPayrollSheetByCostCenterPostingGroup :: PayrollSheetQuery. Cost centre within posting group.</summary>
        public const string PayrollSheetByCostCenterPostingGroupQuery = @"-- rptPayrollSheetByCostCenterPostingGroup :: PayrollSheetQuery (master)
-- HCMv3 rewrite; shared body: PayrollSheetSql.CostCentreFamilyBody.
--
-- PORT DEVIATION: the legacy SQL was wrapped in ""IF (ISNULL(@PrmCompany,'') != '') BEGIN ... END"".
-- DevExpress CustomSqlQuery rejects anything that is not purely SELECT, so the guard is expressed as
-- an ordinary predicate below. Semantics identical: no company selected returns no rows.
SELECT" + CostCentreFamilyHeaderColumns + FullMonthLeaveZeroing + GrossSalaryColumn + @"
FROM (" + CostCentreFamilyBody + @"
) B
WHERE ISNULL(@PrmCompany, '') <> ''
  AND B.CompanyEn = @PrmCompany
  AND ((ISNULL(@PrmCostCenterGroup,'') <> '' AND B.CostCenterGroup = @PrmCostCenterGroup) OR (ISNULL(@PrmCostCenterGroup,'') = '' AND 1 = 1))
  AND ((ISNULL(@PrmCostCenter, '')     <> '' AND B.CostCenterEn    = @PrmCostCenter)     OR (ISNULL(@PrmCostCenter, '')     = '' AND 1 = 1))" + RunTypeSelection + CodeSortOrder;

        /// <summary>rptPayrollSheetByCostCenterSummary :: PayrollSheetQuery. Cost-centre roll-up sheet.</summary>
        public const string PayrollSheetByCostCenterSummaryQuery = @"-- rptPayrollSheetByCostCenterSummary :: PayrollSheetQuery (master)
-- HCMv3 rewrite; shared body: PayrollSheetSql.CostCentreFamilyBody.
SELECT" + CostCentreFamilyHeaderColumns + FullMonthLeaveZeroing + GrossSalaryColumn + @"
FROM (" + CostCentreFamilyBody + @"
) B
WHERE ((ISNULL(@PrmCostCenter, '') <> '' AND B.CostCenterEn = @PrmCostCenter) OR (ISNULL(@PrmCostCenter, '') = '' AND 1 = 1))
  AND ((ISNULL(@PrmCompany, '')    <> '' AND B.CompanyEn    = @PrmCompany)    OR (ISNULL(@PrmCompany, '')    = '' AND 1 = 1))" + RunTypeSelection + CodeSortOrder;

        /// <summary>
        /// rptSalaryByBudgetingGroup :: LeaveAccrualQuery. Workmen-compensation category sheet,
        /// grouped by budgeting group.
        /// </summary>
        public const string SalaryByBudgetingGroupQuery = @"-- rptSalaryByBudgetingGroup :: LeaveAccrualQuery (master)
-- HCMv3 rewrite; shared body: PayrollSheetSql.CostCentreFamilyBody.
--
-- PORT DEVIATION: as with the posting-group report, the legacy ""IF ... BEGIN ... END"" wrapper is
-- expressed as a predicate -- CustomSqlQuery accepts SELECT only.
--
-- DEFECT CARRIED OVER: GrossSalary is printed NET OF BasicSalary (""GrossSalary - BasicSalary"").
-- That is what the legacy report did and what its totals were reconciled against; it is preserved.
SELECT" + CostCentreFamilyHeaderColumns + FullMonthLeaveZeroing + GrossSalaryLessBasicColumn + @"
FROM (" + CostCentreFamilyBody + @"
) B
WHERE ISNULL(@PrmCompany, '') <> ''
  AND B.CompanyEn = @PrmCompany
  AND ((ISNULL(@PrmBudgetingGroup, '') <> '' AND B.BudgetingGroupEn = @PrmBudgetingGroup) OR (ISNULL(@PrmBudgetingGroup, '') = '' AND 1 = 1))" + RunTypeSelection + CodeSortOrder;

        /// <summary>
        /// rptPayrollSheetBySponsor :: PayrollSheetQuery.
        /// The one aggregate member of the family: headcount and total net pay per
        /// sponsor / department / payment type / pay interval.
        /// </summary>
        public const string PayrollSheetBySponsorQuery = @"-- rptPayrollSheetBySponsor :: PayrollSheetQuery (master)
-- HCMv3 rewrite. Uses PayrollSheetSql.EmployeeDimensions but not Buckets: the only money column is
-- the net-pay carrier line, so a single conditional sum is cheaper than the full bucket pass.
--
-- MECHANICAL NOTES
--   * Legacy took the sponsor via ""EmployeeSponsor.OID IN (SELECT TOP 1 ... ORDER BY EndDate DESC)"";
--     rewritten as an OUTER APPLY expressing the same ""latest sponsorship"" intent.
--   * FileNumber is the Arabic/English MOSAL file label; Sponsors.MOSALID is bigint in HCMv3, so it
--     is CONVERTed exactly as the legacy did.
--   * The legacy query's leading half was a commented-out CROSS JOIN scaffold producing empty
--     sponsor/pay-interval combinations. Not ported -- it was dead code.
SELECT
      B.Sponsor
    , B.FileNumber
    , B.PaymentType
    , B.PayInterval
    , B.ForPeriod
    , B.DepartmentEn
    , B.DepartmentAr
    , B.Companylogo
    -- one logo per row without changing the grouping (and so the totals): any employee of the row
    , MIN(B.CompanylogoOrgUnitId)  AS CompanylogoOrgUnitId
    , COUNT(*)                     AS Number
    , SUM(ISNULL(B.NetPay, 0))     AS Amount
FROM (
    SELECT
          sr.ForPeriod
        , sponsor.NameEn                                                    AS Sponsor
        , N'ملف رقم / File Number :' + CONVERT(VARCHAR(100), sponsor.MOSALID) AS FileNumber" + DimensionColumns + @"
        , ISNULL(srdNet.Amount, 0)                                          AS NetPay
    FROM SalaryRegisters sr
    INNER JOIN Employees emp
            ON emp.Id = sr.EmployeeId AND emp.TenantId = @TenantId AND emp.IsDeleted = 0" + EmployeeDimensions + @"
    OUTER APPLY (
        SELECT TOP 1 es.SponsorId
        FROM EmployeeSponsors es
        WHERE es.EmployeeId = emp.Id AND es.TenantId = @TenantId AND es.IsDeleted = 0
        ORDER BY es.EndDate DESC
    ) empSponsor
    LEFT JOIN Sponsors sponsor
           ON sponsor.Id = empSponsor.SponsorId AND sponsor.TenantId = @TenantId AND sponsor.IsDeleted = 0
    OUTER APPLY (
        SELECT SUM(srd.Amount) AS Amount
        FROM SalaryRegisterDetails srd
        INNER JOIN WageTypes wtNet
                ON wtNet.Id = srd.WageTypeId AND wtNet.TenantId = @TenantId AND wtNet.IsDeleted = 0
               AND wtNet.Code = '9000'
        WHERE srd.SalaryRegisterId = sr.Id AND srd.TenantId = @TenantId AND srd.IsDeleted = 0
    ) srdNet
    WHERE sr.TenantId = @TenantId
      AND sr.IsDeleted = 0
      AND sr.InPeriod = sr.ForPeriod
      AND DATEPART(Month, sr.ForPeriod) = @PrmMonth
      AND DATEPART(Year,  sr.ForPeriod) = @PrmYear
      AND sr.PayrollRunType = @PrmPayrollRunType
      AND ((ISNULL(@PrmPayrollArea, '') <> '' AND payrollArea.Name = @PrmPayrollArea)
           OR (ISNULL(@PrmPayrollArea, '') = '' AND 1 = 1))
) B
GROUP BY
      B.PayInterval
    , B.Sponsor
    , B.DepartmentEn
    , B.FileNumber
    , B.PaymentType
    , B.ForPeriod
    , B.DepartmentAr
    , B.Companylogo;";

        // =============================================================================================
        // PARAMETER LOOK-UP LISTS
        //
        // Each of these feeds a report parameter combo. The three org-unit ones (Company / Department
        // / Division) legacy-filtered on OrganizationUnitType.NameEn, which HCMv3 does not have -- see
        // EmployeeDimensions. Company lists the ROOTS of the ABP tree; Department lists the non-roots;
        // Division has no HCMv3 equivalent and is BLOCKED (see notes) -- the query is retained so the
        // combo binds, but it returns nothing.
        // =============================================================================================

        // NOTE ON THE DUPLICATED ALIASES BELOW. The seven legacy layouts bind their parameter combos
        // to DIFFERENT column names for the SAME look-up list -- dsPayrollArea is read as [Name] by
        // rptPayrollSheet and as [PayrollArea] by rptPayrollSheetBySponsor; dsCostCenter is [NameEn]
        // in the cost-centre family and [CostCenterEn] in rptPayrollSheet. Rather than edit seven
        // layouts (a designer round-trip would undo it) each look-up projects every alias its callers
        // ask for. A DisplayMember that does not resolve silently yields an EMPTY combo, which is one
        // of the invisible failures this port is guarding against.

        /// <summary>
        /// Organisation units whose profile type is 'Company'. Legacy:
        /// <c>OrganizationUnit LEFT JOIN OrganizationUnitType WHERE OrganizationUnitType.NameEn = 'Company'</c>;
        /// in HCMv3 the unit type moved off the ABP table onto OrgUnitProfiles.
        /// </summary>
        public const string CompanyLookup = @"-- Payroll sheets :: dsCompany (parameter look-up list)
SELECT
      ou.DisplayName AS Company
    , ou.DisplayName AS CompanyEn
    , ou.DisplayName AS NameEn
FROM AbpOrganizationUnits ou
INNER JOIN OrgUnitProfiles p       ON p.OrganizationUnitId = ou.Id      AND p.TenantId = @TenantId AND p.IsDeleted = 0
INNER JOIN OrganizationUnitTypes t ON t.Id = p.OrganizationUnitTypeId   AND t.TenantId = @TenantId AND t.IsDeleted = 0
WHERE ou.TenantId = @TenantId
  AND ou.IsDeleted = 0
  AND t.NameEn = 'Company'
ORDER BY ou.DisplayName;";

        /// <summary>Organisation units whose profile type is 'Department'.</summary>
        public const string DepartmentLookup = @"-- Payroll sheets :: dsDepartment (parameter look-up list)
SELECT
      ou.DisplayName AS DepartmentEn
    , ou.DisplayName AS NameEn
FROM AbpOrganizationUnits ou
INNER JOIN OrgUnitProfiles p       ON p.OrganizationUnitId = ou.Id      AND p.TenantId = @TenantId AND p.IsDeleted = 0
INNER JOIN OrganizationUnitTypes t ON t.Id = p.OrganizationUnitTypeId   AND t.TenantId = @TenantId AND t.IsDeleted = 0
WHERE ou.TenantId = @TenantId
  AND ou.IsDeleted = 0
  AND t.NameEn = 'Department'
ORDER BY ou.DisplayName;";

        /// <summary>Organisation units whose profile type is 'Division'.</summary>
        public const string DivisionLookup = @"-- Payroll sheets :: dsDivision (parameter look-up list)
SELECT
      ou.DisplayName AS DivisionEn
    , ou.DisplayName AS NameEn
FROM AbpOrganizationUnits ou
INNER JOIN OrgUnitProfiles p       ON p.OrganizationUnitId = ou.Id      AND p.TenantId = @TenantId AND p.IsDeleted = 0
INNER JOIN OrganizationUnitTypes t ON t.Id = p.OrganizationUnitTypeId   AND t.TenantId = @TenantId AND t.IsDeleted = 0
WHERE ou.TenantId = @TenantId
  AND ou.IsDeleted = 0
  AND t.NameEn = 'Division'
ORDER BY ou.DisplayName;";

        /// <summary>dsPayrollArea.</summary>
        public const string PayrollAreaLookup = @"-- Payroll sheets :: dsPayrollArea (parameter look-up list)
SELECT
      pa.Name AS Name
    , pa.Name AS PayrollArea
FROM PayrollAreas pa
WHERE pa.TenantId = @TenantId
  AND pa.IsDeleted = 0
ORDER BY pa.Name;";

        /// <summary>dsCostCenter. Matches the CostCenterEn projection ('code-name') so the filter compares like with like.</summary>
        public const string CostCenterLookup = @"-- Payroll sheets :: dsCostCenter (parameter look-up list)
SELECT
      cc.Id                     AS Id
    , cc.Code + '-' + cc.NameEn AS NameEn
    , cc.Code + '-' + cc.NameEn AS CostCenterEn
    , cc.NameAr                 AS NameAr
    , cc.NameAr                 AS CostCenterAr
FROM CostCenters cc
WHERE cc.TenantId = @TenantId
  AND cc.IsDeleted = 0
ORDER BY cc.Code + '-' + cc.NameEn;";

        /// <summary>dsCostCenterGroup.</summary>
        public const string CostCenterGroupLookup = @"-- Payroll sheets :: dsCostCenterGroup (parameter look-up list)
SELECT
      ccg.Id    AS Id
    , ccg.NameEn AS NameEn
    , ccg.NameEn AS CostCenterGroup
    , ccg.NameAr AS NameAr
FROM CostCenterGroups ccg
WHERE ccg.TenantId = @TenantId
  AND ccg.IsDeleted = 0
ORDER BY ccg.NameEn;";

        /// <summary>dsBudgetingGroup. The combo shows 'code - name' but stores the plain name.</summary>
        public const string BudgetingGroupLookup = @"-- rptSalaryByBudgetingGroup :: dsBudgetingGroup (parameter look-up list)
SELECT
      bg.Code + ' - ' + bg.NameEn AS CodeNameEn
    , bg.NameEn                   AS NameEn
    , bg.NameEn                   AS BudgetingGroupEn
FROM BudgetingGroups bg
WHERE bg.TenantId = @TenantId
  AND bg.IsDeleted = 0
ORDER BY bg.Code;";

        /// <summary>
        /// rptPayrollSheet's standalone ""Query"" data source, bound in the layout to the header picture
        /// box as [Query].[logo]. Kept for the layout's shape only: HcmReportBase rewires that box to
        /// ICompanyLogoResolver with CompanyLogoSource.FirstLevel1Unit (legacy: ""SELECT logo FROM
        /// OrganizationUnit ... WHERE OrganizationUnitType.Code LIKE '%OrgLevel1%'"" -- note the legacy
        /// designer filtered on the level-1 TYPE CODE, not on the 'Company' name used below), loading
        /// the logo from the file store. This query's NULL column is no longer read.
        /// </summary>
        public const string CompanyLogoQuery = @"-- rptPayrollSheet :: Query (header logo)
-- FEATURE LOST: legacy read OrganizationUnit.Logo for the unit of type 'Company'. The unit TYPE is
-- expressible in HCMv3 (OrgUnitProfiles.OrganizationUnitTypeId, used below), but neither
-- AbpOrganizationUnits nor OrgUnitProfiles carries a Logo column, so the image itself is gone.
SELECT TOP 1 CAST(NULL AS VARBINARY(MAX)) AS logo
FROM AbpOrganizationUnits ou
INNER JOIN OrgUnitProfiles p       ON p.OrganizationUnitId = ou.Id      AND p.TenantId = @TenantId AND p.IsDeleted = 0
INNER JOIN OrganizationUnitTypes t ON t.Id = p.OrganizationUnitTypeId   AND t.TenantId = @TenantId AND t.IsDeleted = 0
WHERE ou.TenantId = @TenantId
  AND ou.IsDeleted = 0
  AND t.NameEn = 'Company';";

        /// <summary>
        /// The CalcHeaders calculated-field expression, byte-identical in all six legacy .resx files
        /// that carried it. Hoisted here so it has one home.
        /// (It is a DevExpress expression, not SQL -- no tenant concern.)
        /// </summary>
        public const string CalcHeadersExpression =
            "Iif([Parameters].[PrmPayrollRunType] == 0,'Regular Payslip for the Month: ' + \n" +
            "Iif([Parameters].[PrmPeriodMonth] = 1,'January',\n" +
            "Iif([Parameters].[PrmPeriodMonth] = 2,'February',\n" +
            "Iif([Parameters].[PrmPeriodMonth] = 3,'March',\n" +
            "Iif([Parameters].[PrmPeriodMonth] = 4,'April',\n" +
            "Iif([Parameters].[PrmPeriodMonth] = 5,'May',\n" +
            "Iif([Parameters].[PrmPeriodMonth] = 6,'June',\n" +
            "Iif([Parameters].[PrmPeriodMonth] = 7,'July',\n" +
            "Iif([Parameters].[PrmPeriodMonth] = 8,'August',\n" +
            "Iif([Parameters].[PrmPeriodMonth] = 9,'September',\n" +
            "Iif([Parameters].[PrmPeriodMonth] = 10,'October',\n" +
            "Iif([Parameters].[PrmPeriodMonth] = 11,'November',\n" +
            "Iif([Parameters].[PrmPeriodMonth] = 12,'December',\n" +
            "''))))))))))))\n" +
            "\n" +
            ", Iif([Parameters].[PrmPayrollRunType] == 1, 'Leave Salary Payslip' , ''))";
    }
}
