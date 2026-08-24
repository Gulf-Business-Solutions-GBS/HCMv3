-- rptPayslipEn :: PayslipEarningQuery (srptPaySlipEarningEn subreport -- the earnings block)
--
-- MECHANICAL rewrite, except:
--   * "wagetype.code <> 9000" compares an nvarchar Code to a numeric literal in the legacy schema.
--     HCMv3 WageTypes.Code is nvarchar, so it is quoted: <> '9000'. Left unquoted SQL Server would
--     implicitly convert the WHOLE column to int and throw on any non-numeric code.
--   * EmployeeBasicPayDetail.WageType = 1 was a RAW LEGACY PRIMARY KEY meaning Code '1000'
--     (Basic Salary). HCMv3 has an unrelated wage type whose Code is literally '1'
--     (Employee PIFSS Ded), so the literal must NOT be carried over. See PORTING-NOTES.md.
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
  AND emp.Code = @empcode
  AND sr.ForPeriod      = @ForPeriod
  AND sr.InPeriod       = @InPeriod
  AND sr.PayrollRunType = @RunType;
