-- rptPayslipEn :: PayslipDeductionQuery (srptPaySlipDeductionEn subreport -- the deductions block)
--
-- NON-MECHANICAL: legacy carried "SRdetail.WageType <> 65". 65 is a RAW LEGACY PRIMARY KEY, and in
-- HRMS_RCF WageType OID 65 = Code '9000' (Net Pay) -- i.e. the clause means "do not print the net-pay
-- carrier row as a deduction", exactly mirroring the earnings query's "code <> 9000". It is a real
-- rule, not dead legacy noise, and is ported as a Code comparison.
-- (The FastReport pilot in HCMv3.Application/Reports/ReportsAppService.cs dropped this filter on the
-- grounds that 65 had "no stable meaning in HCMv3" -- worth revisiting there.)
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
  AND sr.PayrollRunType = @RunType;
