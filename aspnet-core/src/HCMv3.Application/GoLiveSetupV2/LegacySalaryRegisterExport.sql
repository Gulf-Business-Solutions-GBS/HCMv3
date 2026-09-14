/*
    Legacy salary register export for Go-Live Setup V2 "Compare with legacy salary".

    Read-only: SELECT statements only. Run it against the legacy HRMS database (e.g. HRMS_RCF) in SQL Server
    Management Studio, then save the result as CSV or Excel: in the results grid, right-click, then
    "Save Results As..." (CSV), or "Copy with Headers" and paste into Excel. Upload that file in V2.

    One row per employee and wage type for the month: what the legacy payroll calculated -- Rate, Number
    and Amount. A wage type's lines in one register are added up.

    Only the month's own regular, current results are exported: ForPeriod = InPeriod = the month end,
    PayrollRunType Regular (0), PayrollResultType Current (0). Retro lines for earlier months and off-cycle
    runs are left out, because a first HCMv3 payroll has neither.

    Set the three values below, then run.
*/
SET NOCOUNT ON;

DECLARE @PayMonth        date          = '2025-03-31'; -- any day in the month to compare
DECLARE @EmployeeCodes   nvarchar(max) = N'';          -- comma separated, e.g. N'1001,1002'; blank = everyone
DECLARE @PayrollAreaCode nvarchar(100) = N'';          -- blank = every payroll area

DECLARE @MonthEnd date = EOMONTH(@PayMonth);

WITH Employees AS (
    SELECT e.OID, e.Code, e.FullNameEn
    FROM dbo.Employee e
    WHERE e.GCRecord IS NULL
      -- Not STRING_SPLIT: legacy databases often run at a compatibility level that does not have it.
      AND (@EmployeeCodes = N''
           OR N',' + REPLACE(@EmployeeCodes, N' ', N'') + N',' LIKE N'%,' + e.Code + N',%')
      AND (@PayrollAreaCode = N''
           OR EXISTS (SELECT 1
                      FROM dbo.EmployeeOrgAssignment oa
                      JOIN dbo.PayrollArea pa ON pa.OID = oa.PayrollArea
                      WHERE oa.Employee = e.OID
                        AND oa.GCRecord IS NULL
                        AND oa.StartDate <= @MonthEnd
                        AND oa.EndDate >= @MonthEnd
                        AND pa.Code = @PayrollAreaCode))
),
Calculated AS (
    SELECT s.Employee,
           d.WageType,
           MAX(d.Rate)   AS Rate,
           SUM(d.Number) AS Number,
           SUM(d.Amount) AS Amount
    FROM dbo.SalaryRegister s
    JOIN dbo.SalaryRegisterDetail d ON d.SalaryRegister = s.OID AND d.GCRecord IS NULL
    JOIN Employees e ON e.OID = s.Employee
    WHERE s.GCRecord IS NULL
      AND CONVERT(date, s.ForPeriod) = @MonthEnd
      AND CONVERT(date, s.InPeriod) = @MonthEnd
      AND s.PayrollRunType = 0
      AND s.PayrollResultType = 0
    GROUP BY s.Employee, d.WageType
)
SELECT e.Code                                         AS EmployeeCode,
       e.FullNameEn                                   AS EmployeeName,
       CONVERT(char(10), @MonthEnd, 23)               AS PayMonth,
       w.Code                                         AS WageTypeCode,
       w.NameEn                                       AS WageTypeName,
       CASE w.BillingType WHEN 0 THEN 'Earning' WHEN 1 THEN 'Deduction'
                          WHEN 2 THEN 'TimeType' WHEN 3 THEN 'Provision' END AS BillingType,
       CONVERT(decimal(18, 6), c.Rate)                AS Rate,
       CONVERT(decimal(18, 6), c.Number)              AS Number,
       CONVERT(decimal(18, 3), c.Amount)              AS Amount
FROM Calculated c
JOIN Employees e ON e.OID = c.Employee
JOIN dbo.WageType w ON w.OID = c.WageType
ORDER BY e.Code, w.Code;
