-- rptPayslipEn :: OvertimeQuery (srptPayslipOvertime subreport)
-- MECHANICAL rewrite: table pluralisation + OID/FK renames + TenantId/IsDeleted.
-- EOMonth(EffectiveDate) = @ForPeriod is kept verbatim: @ForPeriod is always a month-end date.
SELECT
      emp.Code
    , emp.FullNameEn
    , ot.NameEn                              AS Type
    , SUM(eot.ApprovedOvertimeHours)         AS ActualHrs
    , EOMONTH(eot.EffectiveDate)             AS EndOfMonth
FROM EmployeeOvertimes eot
LEFT JOIN OvertimeTypes ot ON ot.Id  = eot.OvertimeTypeId AND ot.TenantId  = @TenantId AND ot.IsDeleted  = 0
LEFT JOIN Employees     emp ON emp.Id = eot.EmployeeId     AND emp.TenantId = @TenantId AND emp.IsDeleted = 0
WHERE eot.TenantId  = @TenantId
  AND eot.IsDeleted = 0
  AND emp.Code = @empcode
  AND EOMONTH(eot.EffectiveDate) = @ForPeriod
GROUP BY emp.Code, emp.FullNameEn, ot.NameEn, EOMONTH(eot.EffectiveDate);
