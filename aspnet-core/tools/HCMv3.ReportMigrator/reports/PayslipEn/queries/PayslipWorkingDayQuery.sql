-- rptPayslipEn :: PayslipWorkingDayQuery (srptPayslipWorkingDays subreport)
-- MECHANICAL rewrite; see PayslipIndemnityQuery for the Code-quoting note.
SELECT
      emp.Code    AS [Employee Code]
    , sr.ForPeriod
    , sr.InPeriod
    , sr.PayrollRunType
    , srd.Number
    , wt.NameEn   AS [Wage Type]
FROM Employees emp
INNER JOIN SalaryRegisters sr        ON sr.EmployeeId = emp.Id       AND sr.TenantId  = @TenantId AND sr.IsDeleted  = 0
INNER JOIN SalaryRegisterDetails srd ON srd.SalaryRegisterId = sr.Id AND srd.TenantId = @TenantId AND srd.IsDeleted = 0
INNER JOIN WageTypes wt              ON wt.Id = srd.WageTypeId       AND wt.TenantId  = @TenantId AND wt.IsDeleted  = 0
WHERE emp.TenantId = @TenantId AND emp.IsDeleted = 0
  AND wt.Code IN ('4007', '4008', '4009', '4011', '4014')
  AND emp.Code          = @empcode
  AND sr.ForPeriod      = @ForPeriod
  AND sr.InPeriod       = @InPeriod
  AND sr.PayrollRunType = @RunType;
