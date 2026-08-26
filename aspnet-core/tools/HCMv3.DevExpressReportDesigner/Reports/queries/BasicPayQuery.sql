-- rptPayslipEn :: BasicPayQuery (srptPayslipBasicPay subreport)
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
  AND ebp.EndDate   >= @ForPeriod;
