// SQL for the HCMv3.Reports/Transaction family, rewritten for the HCMv3 schema.
//
// MULTI-TENANCY: raw SQL through DevExpress SqlDataSource bypasses EF Core's global tenant filter.
// Every query filters TenantId explicitly via @TenantId (the hidden PrmTenantId report parameter).
//
// ===================================================================================================
// REWRITES SHARED BY EVERY QUERY IN THIS FILE
// ===================================================================================================
//   * Table pluralisation, OID -> Id, <Entity> -> <Entity>Id, and "AND TenantId = @TenantId
//     AND IsDeleted = 0" added to EVERY join (the legacy corpus has zero occurrences of either).
//   * WageTypes.Code is nvarchar -- code literals stay QUOTED or SQL Server implicitly converts the
//     whole column to int and throws on the first non-numeric code.
//   * *** LEGACY BUG FIXED: PayrollArea join. *** Legacy wrote
//         LEFT JOIN PayrollArea ON PayrollArea.Code = EmployeeOrgAssignment.PayrollArea
//     which compares a Code against a raw FK value -- it matched only by coincidence. HCMv3 models
//     the relation properly, so this is now PayrollAreas.Id = EmployeeOrgAssignments.PayrollAreaId.
//   * *** SEMANTIC DEVIATION: the OrgLevel1..7 'Company' / 'Department' joins. ***
//     Legacy EmployeeOrgAssignment carried seven flat FK columns and picked whichever pointed at an
//     OrganizationUnit of the wanted type. HCMv3 has one OrganizationUnitId into AbpOrganizationUnits
//     (a materialised-path tree) and holds the unit TYPE on OrgUnitProfiles.OrganizationUnitTypeId.
//     The 14 aliased joins collapse to two ancestor walks: among the employee's own unit and its
//     ancestors (ancestor <=> the employee unit's Code starts with the ancestor's Code), take the
//     shallowest whose profile type is 'Company' (resp. 'Department'). See BankSql.cs for the full note.
//   * *** FEATURE LOST: CompanyLogo. *** Legacy read OrganizationUnit.logo; neither
//     AbpOrganizationUnits nor OrgUnitProfiles has a logo column. Returned as NULL.
//   * DevExpress CustomSqlQuery rejects anything that is not purely SELECT ("A custom SQL query
//     should contain only SELECT statements"), so legacy DECLARE locals are inlined at their use sites.
//
// KNOWN HCMv3 DATA GAPS (not porting defects): EmployeeBanks, EmployeeFunds and EmployeeAssetInvoices
// are all EMPTY, and only one AbpOrganizationUnits row exists (with no 'Company'/'Department' type).

namespace HCMv3.Reports.Transaction
{
    /// <summary>Raw SQL used by the HCMv3.Reports.Transaction reports.</summary>
    public static class TransactionSql
    {
        // -------------------------------------------------------------------------------------------
        // rptEmployeeLoan -- one employee's outstanding loans.
        // -------------------------------------------------------------------------------------------
        public const string EmployeeLoanQuery = @"-- rptEmployeeLoan :: master query. One row per outstanding loan for @PrmEmpCode.
SELECT emp.Code
     , emp.FullNameEn
     , emp.FullNameAr
     , workLocation.NameEn                  AS WorkLocationEn
     , workLocation.NameAr                  AS WorkLocationAr
     , costCenter.NameEn                    AS CostCenterEn
     , costCenter.NameAr                    AS CostCenterAr
     , emp.JoiningDate
     , jobTitle.NameEn                      AS JobTitleEn
     , jobTitle.NameAr                      AS JobTitleAr
     , basicPay.Amount                      AS TotalSalary
     , basicPayDetail.Amount                AS BasicPay
     , payrollArea.Name                     AS PayrollArea
     , currency.Code                        AS Currency
     , CAST(NULL AS VARBINARY(MAX))         AS CompanyLogo    -- FEATURE LOST, see header
     , department.DisplayName               AS DepartmentEn   -- SEMANTIC DEVIATION, see header
     , bank.NameEn                          AS BankEn
     , empBank.AccountNum
     , empBank.IBAN
     , empBank.BankBranch
     , loanType.NameEn                      AS LoanType
     , wageType.NameEn                      AS WageType
     , loan.StartDate
     , loan.EndDate
     , loan.RecoveryStartDate
     , loan.ActualAmount
     , loan.DueAmount
     , loan.FixedAmount
     , loan.BalanceDueAmount
FROM Employees emp

LEFT JOIN EmployeeLoans loan
       ON loan.EmployeeId = emp.Id AND loan.TenantId = @TenantId AND loan.IsDeleted = 0
LEFT JOIN LoanTypes loanType
       ON loanType.Id = loan.LoanTypeId AND loanType.TenantId = @TenantId AND loanType.IsDeleted = 0
LEFT JOIN WageTypes wageType
       ON wageType.Id = loanType.WageTypeId AND wageType.TenantId = @TenantId AND wageType.IsDeleted = 0

LEFT JOIN EmployeeOrgAssignments orgAsg
       ON orgAsg.EmployeeId = emp.Id AND orgAsg.TenantId = @TenantId AND orgAsg.IsDeleted = 0
      AND orgAsg.StartDate <= GETDATE() AND orgAsg.EndDate >= GETDATE()
LEFT JOIN JobTitles jobTitle
       ON jobTitle.Id = orgAsg.JobTitleId AND jobTitle.TenantId = @TenantId AND jobTitle.IsDeleted = 0
-- LEGACY BUG FIXED: was PayrollArea.Code = EmployeeOrgAssignment.PayrollArea. See header.
LEFT JOIN PayrollAreas payrollArea
       ON payrollArea.Id = orgAsg.PayrollAreaId AND payrollArea.TenantId = @TenantId AND payrollArea.IsDeleted = 0
LEFT JOIN WorkLocations workLocation
       ON workLocation.Id = orgAsg.WorkLocationId AND workLocation.TenantId = @TenantId AND workLocation.IsDeleted = 0
LEFT JOIN CostCenters costCenter
       ON costCenter.Id = orgAsg.CostCenterId AND costCenter.TenantId = @TenantId AND costCenter.IsDeleted = 0

LEFT JOIN AbpOrganizationUnits ou
       ON ou.Id = orgAsg.OrganizationUnitId AND ou.TenantId = @TenantId AND ou.IsDeleted = 0
OUTER APPLY (
    SELECT TOP 1 anc.DisplayName
    FROM AbpOrganizationUnits anc
    INNER JOIN OrgUnitProfiles ancp       ON ancp.OrganizationUnitId = anc.Id      AND ancp.TenantId = @TenantId AND ancp.IsDeleted = 0
    INNER JOIN OrganizationUnitTypes anct ON anct.Id = ancp.OrganizationUnitTypeId AND anct.TenantId = @TenantId AND anct.IsDeleted = 0
    WHERE anc.TenantId = @TenantId AND anc.IsDeleted = 0
      AND ou.Code LIKE anc.Code + '%'
      AND anct.NameEn = 'Department'
    ORDER BY LEN(anc.Code)
) department

LEFT JOIN EmployeeBasicPays basicPay
       ON basicPay.EmployeeId = emp.Id AND basicPay.TenantId = @TenantId AND basicPay.IsDeleted = 0
      AND basicPay.StartDate <= GETDATE() AND basicPay.EndDate >= GETDATE()
LEFT JOIN EmployeeBasicPayDetails basicPayDetail
       ON basicPayDetail.EmployeeBasicPayId = basicPay.Id AND basicPayDetail.TenantId = @TenantId AND basicPayDetail.IsDeleted = 0
      AND basicPayDetail.WageTypeId IN (SELECT wt.Id FROM WageTypes wt
                                        WHERE wt.Code = '1000' AND wt.TenantId = @TenantId AND wt.IsDeleted = 0)
LEFT JOIN Currencies currency
       ON currency.Id = basicPayDetail.CurrencyId AND currency.TenantId = @TenantId AND currency.IsDeleted = 0

LEFT JOIN EmployeeBanks empBank
       ON empBank.EmployeeId = emp.Id AND empBank.TenantId = @TenantId AND empBank.IsDeleted = 0
      AND empBank.StartDate <= GETDATE() AND empBank.EndDate >= GETDATE()
LEFT JOIN Banks bank
       ON bank.Id = empBank.BankId AND bank.TenantId = @TenantId AND bank.IsDeleted = 0

WHERE emp.TenantId = @TenantId
  AND emp.IsDeleted = 0
  AND emp.Code = @PrmEmpCode
  AND loan.BalanceDueAmount <> 0

-- Legacy natural-ish sort: numeric codes right-aligned into a 20-char field so '9' sorts before '10'.
-- Ported verbatim, quirks included (the non-numeric branch's Replicate('',21) is a no-op).
ORDER BY CASE WHEN ISNUMERIC(emp.Code) = 1 THEN RIGHT(REPLICATE('0', 21) + emp.Code, 20)
              WHEN ISNUMERIC(emp.Code) = 0 THEN LEFT(emp.Code + REPLICATE('', 21), 20)
              ELSE emp.Code
         END;";

        // -------------------------------------------------------------------------------------------
        // rptEmployeeDeduction -- every recurring deduction in force for one employee in one month.
        // -------------------------------------------------------------------------------------------
        public const string EmployeeDeductionQuery = @"-- rptEmployeeDeduction :: master query. A UNION of four deduction sources, filtered to the wage
-- types whose BillingType is 1 (deductions).
--
-- PORT DEVIATION: the legacy source opened with
--     DECLARE @PrmAsOnDate DateTime = EOMONTH(STR(@PrmYear)+'-'+STR(@PrmMonth)+'-'+'01');
-- DevExpress rejects non-SELECT statements in a CustomSqlQuery, so it is inlined as
-- EOMONTH(DATEFROMPARTS(@PrmYear, @PrmMonth, 1)) at each use site. DATEFROMPARTS also removes the
-- legacy string round-trip, whose STR() left-pads with spaces and only parsed by luck.
--
-- *** FEATURE LOST: the ASSET INVOICE branch's wage type. *** Legacy read AssetType.WageType.
-- HCMv3's AssetTypes table has NO WageTypeId column at all, so the branch returns NULL and its rows
-- carry no wage-type name. EmployeeAssetInvoices is also empty in HCMv3, so this is currently moot --
-- but it needs a schema decision before asset deductions can print. *** NEEDS A PRODUCT DECISION. ***
SELECT emp.Code
     , emp.FullNameEn
     , emp.FullNameAr
     , workLocation.NameEn                  AS WorkLocationEn
     , workLocation.NameAr                  AS WorkLocationAr
     , costCenter.NameEn                    AS CostCenterEn
     , costCenter.NameAr                    AS CostCenterAr
     , jobTitle.NameEn                      AS JobTitleEn
     , jobTitle.NameAr                      AS JobTitleAr
     , emp.JoiningDate
     , wageType.NameEn                      AS WageType
     , basicPay.Amount                      AS TotalSalary
     , basicPayDetail.Amount                AS BasicPay
     , payrollArea.Name                     AS PayrollArea
     , currency.Code                        AS Currency
     , A.TransactionType
     , A.FromDate
     , A.ToDate
     , A.Amount
     , A.Comments
     , department.DisplayName               AS DepartmentEn
     , company.DisplayName                  AS CompanyEn
     , companyAr.NameAr                     AS CompanyAr
     , CAST(NULL AS VARBINARY(MAX))         AS Companylogo
FROM (
    SELECT 'RECURRING PAYMENT'  AS TransactionType
         , rp.EmployeeId
         , rp.WageTypeId
         , rp.StartDate         AS FromDate
         , rp.EndDate           AS ToDate
         , rp.Amount            AS Amount
         , rp.Comments
    FROM EmployeeRecurringPayments rp
    WHERE rp.TenantId = @TenantId AND rp.IsDeleted = 0
      AND rp.StartDate <= EOMONTH(DATEFROMPARTS(@PrmYear, @PrmMonth, 1))
      AND rp.EndDate   >= EOMONTH(DATEFROMPARTS(@PrmYear, @PrmMonth, 1))

    UNION ALL

    SELECT 'LOAN'               AS TransactionType
         , loan.EmployeeId
         , loanType.WageTypeId
         , loan.StartDate       AS FromDate
         , loan.EndDate         AS ToDate
         , loan.FixedAmount     AS Amount
         , loan.Comments
    FROM EmployeeLoans loan
    LEFT JOIN LoanTypes loanType
           ON loanType.Id = loan.LoanTypeId AND loanType.TenantId = @TenantId AND loanType.IsDeleted = 0
    WHERE loan.TenantId = @TenantId AND loan.IsDeleted = 0
      AND loan.StartDate <= EOMONTH(DATEFROMPARTS(@PrmYear, @PrmMonth, 1))
      AND loan.EndDate   >= EOMONTH(DATEFROMPARTS(@PrmYear, @PrmMonth, 1))
      AND loan.BalanceDueAmount > 0

    UNION ALL

    -- Legacy pinned the fund deduction to WageType Code '23'. That is a business CODE, not a raw
    -- legacy primary key, so it survives verbatim -- quoted, because HCMv3 Code is nvarchar.
    SELECT 'FUND'               AS TransactionType
         , fund.EmployeeId
         , (SELECT TOP 1 wt.Id FROM WageTypes wt
            WHERE wt.Code = '23' AND wt.TenantId = @TenantId AND wt.IsDeleted = 0) AS WageTypeId
         , fund.StartDate       AS FromDate
         , fund.EndDate         AS ToDate
         , (SELECT TOP 1 fd.Amount FROM EmployeeFundDetails fd
            WHERE fd.EmployeeFundId = fund.Id AND fd.TenantId = @TenantId AND fd.IsDeleted = 0
              AND fd.FundPayType = 0
              AND MONTH(fd.TransactionDate) = @PrmMonth
              AND YEAR(fd.TransactionDate)  = @PrmYear) AS Amount
         , fund.Comments
    FROM EmployeeFunds fund
    WHERE fund.TenantId = @TenantId AND fund.IsDeleted = 0
      AND fund.StartDate <= EOMONTH(DATEFROMPARTS(@PrmYear, @PrmMonth, 1))
      AND fund.EndDate   >= EOMONTH(DATEFROMPARTS(@PrmYear, @PrmMonth, 1))
      AND (SELECT TOP 1 fd.Amount FROM EmployeeFundDetails fd
           WHERE fd.EmployeeFundId = fund.Id AND fd.TenantId = @TenantId AND fd.IsDeleted = 0
             AND fd.FundPayType = 0
             AND MONTH(fd.TransactionDate) = @PrmMonth
             AND YEAR(fd.TransactionDate)  = @PrmYear) IS NOT NULL

    UNION ALL

    SELECT 'ASSET INVOICE'      AS TransactionType
         , inv.EmployeeId
         , CAST(NULL AS INT)    AS WageTypeId   -- FEATURE LOST: AssetTypes has no WageTypeId in HCMv3
         , EOMONTH(DATEFROMPARTS(@PrmYear, @PrmMonth, 1)) AS FromDate
         , EOMONTH(DATEFROMPARTS(@PrmYear, @PrmMonth, 1)) AS ToDate
         , SUM(inv.DeductionAmount) AS Amount
         , ''                   AS Comments
    FROM EmployeeAssetInvoices inv
    WHERE inv.TenantId = @TenantId AND inv.IsDeleted = 0
      AND MONTH(inv.InvoiceDate) = @PrmMonth
      AND YEAR(inv.InvoiceDate)  = @PrmYear
      AND inv.DeductionAmount > 0
      AND inv.DeductFromEmployee = 1
    GROUP BY inv.EmployeeId
) A

LEFT JOIN Employees emp
       ON emp.Id = A.EmployeeId AND emp.TenantId = @TenantId AND emp.IsDeleted = 0
LEFT JOIN WageTypes wageType
       ON wageType.Id = A.WageTypeId AND wageType.TenantId = @TenantId AND wageType.IsDeleted = 0

LEFT JOIN EmployeeOrgAssignments orgAsg
       ON orgAsg.EmployeeId = emp.Id AND orgAsg.TenantId = @TenantId AND orgAsg.IsDeleted = 0
      AND orgAsg.StartDate <= GETDATE() AND orgAsg.EndDate >= GETDATE()
LEFT JOIN JobTitles jobTitle
       ON jobTitle.Id = orgAsg.JobTitleId AND jobTitle.TenantId = @TenantId AND jobTitle.IsDeleted = 0
LEFT JOIN PayrollAreas payrollArea
       ON payrollArea.Id = orgAsg.PayrollAreaId AND payrollArea.TenantId = @TenantId AND payrollArea.IsDeleted = 0
LEFT JOIN WorkLocations workLocation
       ON workLocation.Id = orgAsg.WorkLocationId AND workLocation.TenantId = @TenantId AND workLocation.IsDeleted = 0
LEFT JOIN CostCenters costCenter
       ON costCenter.Id = orgAsg.CostCenterId AND costCenter.TenantId = @TenantId AND costCenter.IsDeleted = 0

LEFT JOIN AbpOrganizationUnits ou
       ON ou.Id = orgAsg.OrganizationUnitId AND ou.TenantId = @TenantId AND ou.IsDeleted = 0
OUTER APPLY (
    SELECT TOP 1 anc.Id, anc.DisplayName
    FROM AbpOrganizationUnits anc
    INNER JOIN OrgUnitProfiles ancp       ON ancp.OrganizationUnitId = anc.Id      AND ancp.TenantId = @TenantId AND ancp.IsDeleted = 0
    INNER JOIN OrganizationUnitTypes anct ON anct.Id = ancp.OrganizationUnitTypeId AND anct.TenantId = @TenantId AND anct.IsDeleted = 0
    WHERE anc.TenantId = @TenantId AND anc.IsDeleted = 0
      AND ou.Code LIKE anc.Code + '%'
      AND anct.NameEn = 'Company'
    ORDER BY LEN(anc.Code)
) company
-- AbpOrganizationUnits has only DisplayName; the Arabic name lives on OrgUnitProfiles.NameAr.
LEFT JOIN OrgUnitProfiles companyAr
       ON companyAr.OrganizationUnitId = company.Id AND companyAr.TenantId = @TenantId AND companyAr.IsDeleted = 0
OUTER APPLY (
    SELECT TOP 1 anc.DisplayName
    FROM AbpOrganizationUnits anc
    INNER JOIN OrgUnitProfiles ancp       ON ancp.OrganizationUnitId = anc.Id      AND ancp.TenantId = @TenantId AND ancp.IsDeleted = 0
    INNER JOIN OrganizationUnitTypes anct ON anct.Id = ancp.OrganizationUnitTypeId AND anct.TenantId = @TenantId AND anct.IsDeleted = 0
    WHERE anc.TenantId = @TenantId AND anc.IsDeleted = 0
      AND ou.Code LIKE anc.Code + '%'
      AND anct.NameEn = 'Department'
    ORDER BY LEN(anc.Code)
) department

LEFT JOIN EmployeeBasicPays basicPay
       ON basicPay.EmployeeId = emp.Id AND basicPay.TenantId = @TenantId AND basicPay.IsDeleted = 0
      AND basicPay.StartDate <= GETDATE() AND basicPay.EndDate >= GETDATE()
LEFT JOIN EmployeeBasicPayDetails basicPayDetail
       ON basicPayDetail.EmployeeBasicPayId = basicPay.Id AND basicPayDetail.TenantId = @TenantId AND basicPayDetail.IsDeleted = 0
      AND basicPayDetail.WageTypeId IN (SELECT wt.Id FROM WageTypes wt
                                        WHERE wt.Code = '1000' AND wt.TenantId = @TenantId AND wt.IsDeleted = 0)
LEFT JOIN Currencies currency
       ON currency.Id = basicPayDetail.CurrencyId AND currency.TenantId = @TenantId AND currency.IsDeleted = 0

WHERE emp.Code = @PrmEmpCode
  -- BillingType 1 = deduction. Legacy wrote the bare column name; qualified here because the UNION
  -- subquery now also exposes columns.
  AND wageType.BillingType = 1
ORDER BY emp.Code, A.FromDate;";

        // -------------------------------------------------------------------------------------------
        // rptEmployeeLeave -- absences overlapping a date range.
        // -------------------------------------------------------------------------------------------
        public const string EmployeeLeaveQuery = @"-- rptEmployeeLeave :: master query. Absences overlapping [@PrmStartDate, @PrmEndDate].
--
-- The legacy inner query carried a fully commented-out UNION ALL branch that would have added
-- unplanned absences from TimeRegister as a synthetic AbsenceType Code '15'. NOT PORTED -- it was
-- dead in the legacy source too, and reviving it would change what the report counts.
SELECT B.Code
     , B.FullNameEn
     , B.AbsenceType
     , B.StartDate
     , B.EndDate
     , B.TotalLeaveDays
     , B.CompanyEn
     , B.CompanyAr
     , B.Companylogo
FROM (
    SELECT emp.Code
         , emp.FullNameEn
         , A.AbsenceType
         , A.StartDate
         , A.EndDate
         , A.TotalLeaveDays
         , company.DisplayName          AS CompanyEn     -- SEMANTIC DEVIATION, see header
         , companyAr.NameAr             AS CompanyAr
         , CAST(NULL AS VARBINARY(MAX)) AS Companylogo   -- FEATURE LOST, see header
         , payrollArea.Name             AS PayrollArea
         , costCenter.NameEn            AS CostCenterEn
         , costCenter.NameAr            AS CostCenterAr
    FROM (
        SELECT absence.EmployeeId
             , absenceType.NameEn       AS AbsenceType
             , absence.StartDate
             , absence.EndDate
             , absence.TotalLeaveDays
        FROM EmployeeAbsences absence
        LEFT JOIN AbsenceTypes absenceType
               ON absenceType.Id = absence.AbsenceTypeId AND absenceType.TenantId = @TenantId AND absenceType.IsDeleted = 0
        WHERE absence.TenantId = @TenantId AND absence.IsDeleted = 0
    ) A

    LEFT JOIN Employees emp
           ON emp.Id = A.EmployeeId AND emp.TenantId = @TenantId AND emp.IsDeleted = 0
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
        WHERE anc.TenantId = @TenantId AND anc.IsDeleted = 0
          AND ou.Code LIKE anc.Code + '%'
          AND anct.NameEn = 'Company'
        ORDER BY LEN(anc.Code)
    ) company
    LEFT JOIN OrgUnitProfiles companyAr
           ON companyAr.OrganizationUnitId = company.Id AND companyAr.TenantId = @TenantId AND companyAr.IsDeleted = 0

    LEFT JOIN PayrollAreas payrollArea
           ON payrollArea.Id = orgAsg.PayrollAreaId AND payrollArea.TenantId = @TenantId AND payrollArea.IsDeleted = 0
    LEFT JOIN CostCenters costCenter
           ON costCenter.Id = orgAsg.CostCenterId AND costCenter.TenantId = @TenantId AND costCenter.IsDeleted = 0
    -- legacy also LEFT JOINed WorkLocation and projected nothing from it; dropped.
) B
WHERE ((ISNULL(@PrmEmpCode, '')      <> '' AND B.Code         = @PrmEmpCode)      OR ISNULL(@PrmEmpCode, '')      = '')
  AND ((ISNULL(@PrmAbsenceType, '')  <> '' AND B.AbsenceType  = @PrmAbsenceType)  OR ISNULL(@PrmAbsenceType, '')  = '')
  AND ((ISNULL(@PrmPayrollArea, '')  <> '' AND B.PayrollArea  = @PrmPayrollArea)  OR ISNULL(@PrmPayrollArea, '')  = '')
  AND ((ISNULL(@PrmCostCenter, '')   <> '' AND B.CostCenterEn = @PrmCostCenter)   OR ISNULL(@PrmCostCenter, '')   = '')
  -- absences that ended before the tenant's earliest evaluation date were never time-evaluated
  AND B.EndDate >= ISNULL((SELECT TOP 1 ts.EarliestEvaluationDate FROM ManageTimeSettingses ts
                           WHERE ts.TenantId = @TenantId AND ts.IsDeleted = 0), CONVERT(DATETIME, '1800-01-01'))
  AND (  (B.StartDate    <= @PrmStartDate AND @PrmStartDate <= B.EndDate)
      OR (B.StartDate    <= @PrmEndDate   AND @PrmEndDate   <= B.EndDate)
      OR (@PrmStartDate  <= B.StartDate   AND B.StartDate   <= @PrmEndDate)
      OR (@PrmStartDate  <= B.EndDate     AND B.EndDate     <= @PrmEndDate))
ORDER BY B.Code;";

        // -------------------------------------------------------------------------------------------
        // Parameter look-up lists (inline designer SQL in the legacy layouts)
        // -------------------------------------------------------------------------------------------

        /// <summary>Leave-type combo for rptEmployeeLeave.</summary>
        public const string AbsenceTypeList = @"-- Legacy inline designer SQL: ""Select NameEn, NameAr From AbsenceType WHERE IsActive = 1 ORDER BY NameEn""
SELECT a.NameEn
     , a.NameAr
FROM AbsenceTypes a
WHERE a.TenantId = @TenantId
  AND a.IsDeleted = 0
  AND a.IsActive = 1
ORDER BY a.NameEn;";

        /// <summary>Payroll-area combo for rptEmployeeLeave.</summary>
        public const string PayrollAreaList = @"-- Legacy inline designer SQL: ""Select Name From PayrollArea ORDER By Name""
SELECT p.Name
FROM PayrollAreas p
WHERE p.TenantId = @TenantId
  AND p.IsDeleted = 0
ORDER BY p.Name;";

        /// <summary>
        /// Cost-centre combo for rptEmployeeLeave.
        ///
        /// *** LEGACY BUG, PORTED AS-IS -- the cost-centre filter cannot ever match. ***
        /// The look-up's ValueMember is "NameEn", but this query's NameEn is the CONCATENATION
        /// <c>Code + '-' + NameEn</c>, while the master query filters
        /// <c>B.CostCenterEn = @PrmCostCenter</c> where CostCenterEn is the BARE
        /// <c>CostCenters.NameEn</c>. Picking any cost centre therefore returns zero rows.
        /// Reproduced rather than fixed because fixing it silently changes which rows the report
        /// prints; raise it as a product bug and change both sides together.
        /// </summary>
        public const string CostCenterList = @"-- Legacy inline designer SQL: ""Select OID, Code+'-'+NameEn AS NameEn, NameAr From CostCenter ORDER BY Code+'-'+NameEn""
-- The OID alias is kept for layout compatibility even though ValueMember is NameEn.
SELECT c.Id AS OID
     , c.Code + '-' + c.NameEn AS NameEn
     , c.NameAr
FROM CostCenters c
WHERE c.TenantId = @TenantId
  AND c.IsDeleted = 0
ORDER BY c.Code + '-' + c.NameEn;";
    }
}
