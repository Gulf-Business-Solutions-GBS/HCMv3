-- rptPayslipEn :: PayslipAttendanceQuery (srptPayslipAttendance subreport)
-- MECHANICAL rewrite. Legacy listed code 4022 twice in the IN list -- harmless, de-duplicated here.
SELECT
      emp.Code    AS [Employee Code]
    , sr.ForPeriod
    , sr.InPeriod
    , sr.PayrollRunType
    , srd.Number
    , wt.NameEn   AS [Wage Type]
    , wt.Id       AS WageTypeId
    , wt.Code
FROM SalaryRegisters sr
INNER JOIN Employees emp             ON emp.Id = sr.EmployeeId       AND emp.TenantId = @TenantId AND emp.IsDeleted = 0
INNER JOIN SalaryRegisterDetails srd ON srd.SalaryRegisterId = sr.Id AND srd.TenantId = @TenantId AND srd.IsDeleted = 0
INNER JOIN WageTypes wt              ON wt.Id = srd.WageTypeId       AND wt.TenantId  = @TenantId AND wt.IsDeleted  = 0
WHERE sr.TenantId = @TenantId AND sr.IsDeleted = 0
  AND wt.Code IN ('4021', '4022', '4000', '4002')
  AND emp.Code          = @empcode
  AND sr.ForPeriod      = @ForPeriod
  AND sr.InPeriod       = @InPeriod
  AND sr.PayrollRunType = @RunType;
