-- rptPayslipEn :: LeaveEncashQuery (srptPayslipLeaveEncashment subreport)
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
  AND wt.Code IN ('1052');
