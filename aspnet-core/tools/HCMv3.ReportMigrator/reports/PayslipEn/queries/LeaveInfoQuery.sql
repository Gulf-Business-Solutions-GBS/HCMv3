-- rptPayslipEn :: LeaveInfoQuery (srptPayslipLeaveInfo subreport; off-cycle runs only)
-- MECHANICAL rewrite. The "AND @PrmPayrollRunType = 1" guard at the end is legacy's way of making the
-- subreport return nothing on a regular run; kept verbatim rather than moved into report logic.
SELECT
      emp.Code
    , emp.FullNameEn
    , emp.FullNameAr
    , at.NameEn AS AbsenceTypeEn
    , at.NameAr AS AbsenceTypeAr
    , ea.StartDate
    , ea.EndDate
    , ea.StartTime
    , ea.EndTime
    , ea.AbsenceHours
    , ea.TotalLeaveDays
    , ea.CalendarDays
    , ea.PaymentDate
    , ea.ReturnDate
    , CASE ea.LeavePaymentType
        WHEN 0 THEN 'Pay With Payroll'
        WHEN 1 THEN 'Pay In Advance'
        ELSE ''
      END AS LeavePaymentType
FROM EmployeeAbsences ea
LEFT JOIN Employees    emp ON emp.Id = ea.EmployeeId    AND emp.TenantId = @TenantId AND emp.IsDeleted = 0
LEFT JOIN AbsenceTypes at  ON at.Id  = ea.AbsenceTypeId AND at.TenantId  = @TenantId AND at.IsDeleted  = 0
WHERE ea.TenantId = @TenantId AND ea.IsDeleted = 0
  AND ea.LeavePaymentType = 2
  AND emp.Code = @PrmEmpCode
  AND ea.PaymentDate >= DATEFROMPARTS(@PrmYear, @PrmMonth, 1)
  AND ea.PaymentDate <= EOMONTH(DATEFROMPARTS(@PrmYear, @PrmMonth, 1))
  AND @PrmPayrollRunType = 1;
