-- rptPayslipEn :: PayslipIndemnityQuery (srptPayslipIndemnity subreport)
-- MECHANICAL rewrite. Wage-type Codes stay as the legacy business codes (4004..4018) -- these ARE
-- codes, not row ids -- but must be quoted now that Code is nvarchar (see PayslipEarningQuery note).
SELECT
      emp.Code    AS [Employee Code]
    , sr.ForPeriod
    , sr.InPeriod
    , sr.PayrollRunType
    , srd.Number
    , wt.NameEn   AS [Wage Type]
    , wt.Id       AS WageTypeId
FROM Employees emp
INNER JOIN SalaryRegisters sr        ON sr.EmployeeId = emp.Id           AND sr.TenantId  = @TenantId AND sr.IsDeleted  = 0
INNER JOIN SalaryRegisterDetails srd ON srd.SalaryRegisterId = sr.Id     AND srd.TenantId = @TenantId AND srd.IsDeleted = 0
INNER JOIN WageTypes wt              ON wt.Id = srd.WageTypeId           AND wt.TenantId  = @TenantId AND wt.IsDeleted  = 0
WHERE emp.TenantId = @TenantId AND emp.IsDeleted = 0
  AND wt.Code IN ('4004', '4005', '4006', '4017', '4018')
  AND emp.Code          = @empcode
  AND sr.ForPeriod      = @ForPeriod
  AND sr.InPeriod       = @InPeriod
  AND sr.PayrollRunType = @RunType;
