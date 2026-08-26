-- rptPayslipEn :: PayrollArea (parameter look-up list feeding the @PrmPayrollArea combo)
-- Legacy: "select * from PayrollArea"  -- MECHANICAL rewrite, plus the two ABP filters the legacy
-- corpus has no concept of.
SELECT *
FROM PayrollAreas
WHERE TenantId = @TenantId
  AND IsDeleted = 0
ORDER BY Name;
