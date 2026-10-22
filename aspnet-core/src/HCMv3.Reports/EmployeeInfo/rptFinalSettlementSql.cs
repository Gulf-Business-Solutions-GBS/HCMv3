namespace HCMv3.Reports.EmployeeInfo
{
    /// <summary>
    /// Raw SQL used by rptFinalSettlement and rptFinalSettlementAr (both use data source
    /// <c>MonthlyLeaveAccrualDS</c>, query <c>LeaveAccrualQuery</c>).
    ///
    /// The two legacy queries differ in exactly three projected expressions -- the wage-type name,
    /// the currency short form and the bank name switch from English to Arabic columns -- so the
    /// query is written once with those three parameterised into the two constants below.
    /// </summary>
    internal static class rptFinalSettlementSql
    {
        // The three language-varying expressions. Note the Arabic variant still aliases the bank
        // column "BankEn": the legacy Arabic query did too, and the layout binds that name.
        private const string EnglishWageTypeName = "wageType.NameEn";
        private const string ArabicWageTypeName = "wageType.NameAr";
        private const string EnglishCurrency = "currency.Code";
        private const string ArabicCurrency = "currency.CurrencyShortFormAr";
        private const string EnglishBankName = "bank.NameEn";
        private const string ArabicBankName = "bank.NameAr";

        /// <summary>English variant -- rptFinalSettlement.</summary>
        internal const string Query1 = Head
            + "    , CASE\n"
            + "        WHEN wageType.Code IN ('1000','1062','7000','7062') THEN 'Basic Salary'\n"
            + "        WHEN wageType.Code IN ('1002','1066','7002','7066') THEN 'Transport Allowance'\n"
            + "        WHEN wageType.Code IN ('1018','1067','7018','7067') THEN 'Other Allowance'\n"
            + "        WHEN wageType.Code IN ('1026','1068','7026','7068') THEN 'Incharge Allowance'\n"
            + "        WHEN wageType.Code IN ('1052','7052')               THEN 'Annual Leave Pay'\n"
            + "        ELSE " + EnglishWageTypeName + " END AS [Wage Type]\n"
            + "    , " + EnglishWageTypeName + " AS WageType\n"
            + "    , " + EnglishCurrency + " AS Currency\n"
            + "    , " + EnglishBankName + " AS BankEn\n"
            + Tail;

        /// <summary>Arabic variant -- rptFinalSettlementAr.</summary>
        internal const string QueryAr1 = Head
            + "    , CASE\n"
            + "        WHEN wageType.Code IN ('1000','1062','7000','7062') THEN 'Basic Salary'\n"
            + "        WHEN wageType.Code IN ('1002','1066','7002','7066') THEN 'Transport Allowance'\n"
            + "        WHEN wageType.Code IN ('1018','1067','7018','7067') THEN 'Other Allowance'\n"
            + "        WHEN wageType.Code IN ('1026','1068','7026','7068') THEN 'Incharge Allowance'\n"
            + "        WHEN wageType.Code IN ('1052','7052')               THEN 'Annual Leave Pay'\n"
            + "        ELSE " + ArabicWageTypeName + " END AS [Wage Type]\n"
            + "    , " + ArabicWageTypeName + " AS WageType\n"
            + "    , " + ArabicCurrency + " AS Currency\n"
            + "    , " + ArabicBankName + " AS BankEn\n"
            + Tail;

        /// <summary>
        /// Everything before the three language-varying projections.
        ///
        /// HCMv3 rewrite of the legacy 188-line query -- the end-of-service settlement statement:
        /// one row per wage type on the employee's End-Of-Service salary register.
        ///
        /// MECHANICAL RENAMES plus TenantId/IsDeleted on every join, as elsewhere.
        ///
        /// NON-MECHANICAL:
        ///   1. PayrollArea. Legacy joined on a STRING
        ///      (<c>PayrollArea.Code = EmployeeOrgAssignment.PayrollArea</c>); HCMv3 models it as
        ///      <c>EmployeeOrgAssignments.PayrollAreaId -> PayrollAreas.Id</c>. Same correction the
        ///      payslip port made.
        ///   2. Seven Company OrgLevel joins existed ONLY to fetch <c>Company_n.logo</c>. HCMv3 keeps
        ///      logos in the file store, not the database, so the seven joins are DROPPED, CompanyLogo
        ///      stays a NULL placeholder, and the employee's org unit is returned as
        ///      <c>CompanylogoOrgUnitId</c>: HcmReportBase hands it to ICompanyLogoResolver, which applies
        ///      the same Company_1..7 rule.
        ///   3. Legacy <c>WageType.Code NOT IN (9000)</c> -- an UNQUOTED numeric literal against
        ///      what is nvarchar in HCMv3. Left as-is it makes SQL Server convert the whole Code
        ///      column to int and throw on the first non-numeric wage-type code. Quoted here.
        ///      (The intent is the same net-pay exclusion PayslipSql documents for
        ///      <c>WageType &lt;&gt; 65</c>.)
        ///   4. The <c>EmployeeSponsor</c> ROW_NUMBER sub-select projected nine columns and NOT ONE
        ///      of them is referenced anywhere in the query or the layout. Dropped -- it could only
        ///      cost a scan. Legacy dead weight, not a lost feature.
        ///   5. The three remaining ROW_NUMBER "take the latest" sub-selects (bank, separation,
        ///      FnF payment) become OUTER APPLY / CROSS APPLY, which states the correlation
        ///      explicitly. The FnF one stays an INNER equivalent (CROSS APPLY): the legacy INNER
        ///      JOIN is the report's precondition -- no final-settlement payment, no statement.
        ///   6. <c>YearMonthdays</c> is kept as the legacy nested-DATEDIFF expression rather than
        ///      routed through HcmReportBase.GetYearMonthDay, because the legacy string is what the
        ///      layout's column width was built for and the two differ at month boundaries.
        ///
        /// *** THIS REPORT RENDERS EMPTY ON THE CURRENT HCMv3 DATABASE, LEGITIMATELY. ***
        /// It requires (a) an EmployeeFnFPayments row and (b) a SalaryRegister whose
        /// PayrollReasonType is 'End Of Service'. HCMv3 has ZERO EmployeeFnFPayments rows and its
        /// PayrollReasonTypes table holds a single placeholder row ("(none)"). Nothing about the
        /// rewrite is at fault; there is no separated employee in the database.
        /// </summary>
        private const string Head = @"-- rptFinalSettlement / rptFinalSettlementAr :: LeaveAccrualQuery
SELECT
      A.Code
    , A.FullNameEn
    , A.FullNameAr
    , A.WorkLocationEn
    , A.WorkLocationAr
    , A.CostCenterEn
    , A.CostCenterAr
    , A.JoiningDate
    , A.LastDayInService
    , A.SeparationType
    , A.YearMonthdays
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
    , MIN(A.CompanylogoOrgUnitId) AS CompanylogoOrgUnitId   -- one employee; MIN leaves the grouping alone
    , A.BankEn
    , A.AccountNum
    , A.IBAN
    , A.BankBranch
FROM (
SELECT
      emp.Code
    , emp.FullNameEn
    , emp.FullNameAr
    , workLocation.NameEn AS WorkLocationEn
    , workLocation.NameAr AS WorkLocationAr
    , costCenter.NameEn   AS CostCenterEn
    , costCenter.NameAr   AS CostCenterAr
    , emp.JoiningDate
    , separation.LastDayInService
    , separation.SeparationType
    , CAST(DATEDIFF(yy, emp.JoiningDate, separation.LastDayInService) AS VARCHAR(4)) + ' Year ' +
      CAST(DATEDIFF(mm, DATEADD(yy, DATEDIFF(yy, emp.JoiningDate, separation.LastDayInService), emp.JoiningDate), separation.LastDayInService) AS VARCHAR(2)) + ' Month ' +
      CAST(DATEDIFF(dd, DATEADD(mm, DATEDIFF(mm, DATEADD(yy, DATEDIFF(yy, emp.JoiningDate, separation.LastDayInService), emp.JoiningDate), separation.LastDayInService), DATEADD(yy, DATEDIFF(yy, emp.JoiningDate, separation.LastDayInService), emp.JoiningDate)), separation.LastDayInService) AS VARCHAR(2)) + ' Days'
                          AS YearMonthdays          -- see rewrite note 6
    , basicPayDetail.Amount AS LastBasicPay
    , sr.ForPeriod
    , sr.InPeriod
    , sr.PayrollRunType
";

        /// <summary>Everything after the three language-varying projections.</summary>
        private const string Tail = @"    , wageType.PayrollOrder
    , srd.Rate
    , srd.Number
    , CASE WHEN wageType.BillingType = 1 THEN (-1) * srd.Amount ELSE srd.Amount END AS Amount
    , payrollArea.Name          AS PayrollArea
    , CAST(NULL AS VARBINARY(MAX)) AS CompanyLogo   -- see rewrite note 2
    , orgAssign.OrganizationUnitId AS CompanylogoOrgUnitId
    , empBank.AccountNum
    , empBank.IBAN
    , empBank.BankBranch

FROM Employees emp

LEFT JOIN EmployeeOrgAssignments orgAssign
       ON orgAssign.EmployeeId = emp.Id
      AND orgAssign.TenantId   = @TenantId
      AND orgAssign.IsDeleted  = 0
      AND orgAssign.StartDate <= GETDATE()
      AND orgAssign.EndDate   >= GETDATE()

LEFT JOIN JobTitles jobTitle
       ON jobTitle.Id        = orgAssign.JobTitleId
      AND jobTitle.TenantId  = @TenantId
      AND jobTitle.IsDeleted = 0

-- see rewrite note 1: a proper FK, not the legacy string join on PayrollArea.Code
LEFT JOIN PayrollAreas payrollArea
       ON payrollArea.Id        = orgAssign.PayrollAreaId
      AND payrollArea.TenantId  = @TenantId
      AND payrollArea.IsDeleted = 0

LEFT JOIN WorkLocations workLocation
       ON workLocation.Id        = orgAssign.WorkLocationId
      AND workLocation.TenantId  = @TenantId
      AND workLocation.IsDeleted = 0

LEFT JOIN CostCenters costCenter
       ON costCenter.Id        = orgAssign.CostCenterId
      AND costCenter.TenantId  = @TenantId
      AND costCenter.IsDeleted = 0

LEFT JOIN EmployeeBasicPays basicPay
       ON basicPay.EmployeeId = emp.Id
      AND basicPay.TenantId   = @TenantId
      AND basicPay.IsDeleted  = 0
      AND basicPay.StartDate <= GETDATE()
      AND basicPay.EndDate   >= GETDATE()

LEFT JOIN EmployeeBasicPayDetails basicPayDetail
       ON basicPayDetail.EmployeeBasicPayId = basicPay.Id
      AND basicPayDetail.TenantId  = @TenantId
      AND basicPayDetail.IsDeleted = 0
      AND basicPayDetail.WageTypeId IN (
              SELECT wt.Id FROM WageTypes wt
              WHERE wt.TenantId = @TenantId AND wt.IsDeleted = 0 AND wt.Code = '1000')

LEFT JOIN Currencies currency
       ON currency.Id        = basicPayDetail.CurrencyId
      AND currency.TenantId  = @TenantId
      AND currency.IsDeleted = 0

-- see rewrite note 5. Bank data is a KNOWN HCMv3 DATA GAP: EmployeeBanks has 0 rows.
OUTER APPLY (
    SELECT TOP 1 eb.BankId, eb.AccountNum, eb.IBAN, eb.BankBranch
    FROM EmployeeBanks eb
    WHERE eb.EmployeeId = emp.Id
      AND eb.TenantId   = @TenantId
      AND eb.IsDeleted  = 0
      AND eb.StartDate <= GETDATE()
      AND eb.EndDate   >= GETDATE()
    ORDER BY eb.StartDate DESC
) empBank

LEFT JOIN Banks bank
       ON bank.Id        = empBank.BankId
      AND bank.TenantId  = @TenantId
      AND bank.IsDeleted = 0

INNER JOIN SalaryRegisters sr
        ON sr.EmployeeId = emp.Id
       AND sr.TenantId   = @TenantId
       AND sr.IsDeleted  = 0

INNER JOIN SalaryRegisterDetails srd
        ON srd.SalaryRegisterId = sr.Id
       AND srd.TenantId  = @TenantId
       AND srd.IsDeleted = 0

INNER JOIN WageTypes wageType
        ON wageType.Id        = srd.WageTypeId
       AND wageType.TenantId  = @TenantId
       AND wageType.IsDeleted = 0

INNER JOIN PayrollReasonTypes reason
        ON reason.Id        = sr.PayrollReasonTypeId
       AND reason.TenantId  = @TenantId
       AND reason.IsDeleted = 0

-- see rewrite note 5
OUTER APPLY (
    SELECT TOP 1
           sep.LastDayInService
         , CASE WHEN sep.SeparationType = 1 THEN 'Termination' ELSE 'Resignation' END AS SeparationType
    FROM EmployeeSeparations sep
    WHERE sep.EmployeeId = emp.Id
      AND sep.TenantId   = @TenantId
      AND sep.IsDeleted  = 0
    ORDER BY sep.LastDayInService DESC
) separation

-- CROSS APPLY, not OUTER: the legacy INNER JOIN is this report's precondition.
CROSS APPLY (
    SELECT TOP 1 fnf.CompensationDate
    FROM EmployeeFnFPayments fnf
    WHERE fnf.EmployeeId = emp.Id
      AND fnf.TenantId   = @TenantId
      AND fnf.IsDeleted  = 0
    ORDER BY fnf.CompensationDate DESC
) fnfPayment

WHERE emp.TenantId  = @TenantId
  AND emp.IsDeleted = 0
  AND sr.InPeriod   = sr.ForPeriod
  AND reason.NameEn = 'End Of Service'
  AND wageType.BillingType IN (0, 1)
  AND wageType.DontImpactNetPay = 0
  AND wageType.Code <> '9000'                       -- see rewrite note 3
  AND emp.Code = @PrmEmpCode
) A
GROUP BY
      A.Code, A.FullNameEn, A.FullNameAr
    , A.WorkLocationEn, A.WorkLocationAr
    , A.CostCenterEn, A.CostCenterAr
    , A.JoiningDate, A.LastDayInService, A.SeparationType, A.YearMonthdays, A.LastBasicPay
    , A.ForPeriod, A.InPeriod, A.PayrollRunType
    , A.WageType, A.PayrollOrder
    , A.PayrollArea, A.Currency, A.CompanyLogo
    , A.BankEn, A.AccountNum, A.IBAN, A.BankBranch
ORDER BY
      CASE WHEN ISNUMERIC(A.Code) = 1 THEN RIGHT(REPLICATE('0', 21) + A.Code, 20)
           ELSE LEFT(A.Code + REPLICATE(' ', 21), 20) END
    , A.PayrollOrder;";
    }
}
