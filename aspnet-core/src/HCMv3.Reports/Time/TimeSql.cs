// SQL for the HCMv3.Reports/Time family, rewritten for the HCMv3 schema.
//
// MULTI-TENANCY: raw SQL through DevExpress SqlDataSource bypasses EF Core's global tenant filter.
// Every query filters TenantId explicitly via @TenantId (the hidden PrmTenantId report parameter).
//
// ===================================================================================================
// REWRITES SHARED BY EVERY QUERY IN THIS FILE
// ===================================================================================================
//   * Table pluralisation, OID -> Id, <Entity> -> <Entity>Id, and "AND TenantId = @TenantId
//     AND IsDeleted = 0" added to EVERY join.
//   * *** SEMANTIC DEVIATION: OrgLevel1..7 'Company' / 'Department'. *** Seven flat FK columns +
//     OrganizationUnitType become ONE ancestor walk over the AbpOrganizationUnits materialised path,
//     with the unit type read from OrgUnitProfiles.OrganizationUnitTypeId. Full note in BankSql.cs.
//   * *** FEATURE LOST: Companylogo. *** No logo column exists in HCMv3. Returned as NULL.
//     The Arabic company / department name comes from OrgUnitProfiles.NameAr, since
//     AbpOrganizationUnits carries only DisplayName.
//   * WageTypes.Code is nvarchar -- code literals stay quoted.
//
// ===================================================================================================
// *** THE ONE NON-OBVIOUS REBINDING IN THIS FOLDER: the "no punch" sentinel date ***
// ===================================================================================================
// Legacy TimeRegister stored "this timestamp is not set" as the literal '1800-01-01 00:00:00.000',
// and rptEmployeeAttendance tests for exactly that value in ELEVEN places to decide whether to print
// a blank cell, and whether a late-in / early-out is real.
//
// HCMv3's TimeRegisters stores the same absence as **0001-01-01** -- EF Core's default(DateTime),
// verified against the live table. An equality test against '1800-01-01' therefore matches NOTHING
// in HCMv3: every unpunched day would print "01/01/0001" instead of a blank, and every unpunched day
// would be scored as an enormous late-in.
//
// Rewritten as "<= '1800-01-01'", which covers BOTH sentinels -- migrated legacy rows that still
// carry 1800-01-01 and native HCMv3 rows carrying 0001-01-01 -- and cannot collide with a real punch.
// This is the kind of break that produces a report that renders, returns rows, and is simply wrong.
// ===================================================================================================

namespace HCMv3.Reports.Time
{
    /// <summary>Raw SQL used by the HCMv3.Reports.Time reports.</summary>
    public static class TimeSql
    {
        // -------------------------------------------------------------------------------------------
        // rptEmployeeAttendance -- daily punch sheet for a payroll area over a date range.
        // -------------------------------------------------------------------------------------------
        public const string EmployeeAttendanceQuery = @"-- rptEmployeeAttendance :: master query. One row per employee per day.
--
-- SENTINEL REWRITE: every '= ''1800-01-01 00:00:00.000''' in the legacy source becomes
-- '<= ''1800-01-01''' -- see the file header. Getting this wrong renders 01/01/0001 in every blank
-- punch cell and turns unpunched days into multi-century late-ins.
SELECT A.Code
     , A.FullNameEn
     , A.Companylogo
     , A.CompanyEn
     , A.CompanyAr
     , A.DepartmentEn
     , A.DepartmentAr
     , 'From ' + CONVERT(VARCHAR(10), @PrmFromDate, 105) + '  -  ' + CONVERT(VARCHAR(10), @PrmToDate, 105) AS Period
     , A.DayDate
     , A.DayofWeek
     , A.WeeklyOff
     , A.RestDay
     , A.ShiftStart
     , A.ShiftEnd
     , A.BreakStart
     , A.BreakEnd
     , A.PunchIn
     , A.PunchOut

     , A.LateIn
     , A.EarlyOut
     , (A.LateIn + A.EarlyOut) AS Short
     , CASE WHEN (A.LateIn + A.EarlyOut) = 0 THEN NULL
            ELSE CONVERT(VARCHAR(5), DATEADD(MINUTE, (A.LateIn + A.EarlyOut), 0), 114) END AS ShortTime

     , A.PlannedWorkingDay
     , A.PlannedWorkingMins
     , CASE WHEN A.PlannedWorkingMins = 0 THEN NULL
            ELSE CONVERT(VARCHAR(5), DATEADD(MINUTE, A.PlannedWorkingMins, 0), 114) END AS PlannedWorkingHours

     -- a day with a missing punch counts as zero worked, whatever the time engine posted
     , CASE WHEN A.PunchIn IS NULL OR A.PunchOut IS NULL THEN 0    ELSE A.ActualWorkingDay  END AS ActualWorkingDay
     , CASE WHEN A.PunchIn IS NULL OR A.PunchOut IS NULL THEN 0    ELSE A.ActualWorkingMins END AS ActualWorkingMins
     , CASE WHEN A.PunchIn IS NULL OR A.PunchOut IS NULL THEN NULL
            ELSE CONVERT(VARCHAR(5), DATEADD(MINUTE, A.ActualWorkingMins, 0), 114) END AS ActualWorkingHours

     , A.OvertimeMinutes
     , CASE WHEN A.OvertimeMinutes = 0 THEN NULL
            ELSE CONVERT(VARCHAR(5), DATEADD(MINUTE, A.OvertimeMinutes, 0), 114) END AS OvertimeHours
FROM (
    SELECT emp.Code
         , emp.FullNameEn
         , CAST(NULL AS VARBINARY(MAX))     AS Companylogo    -- FEATURE LOST, see header
         , company.DisplayName              AS CompanyEn
         , companyProfile.NameAr            AS CompanyAr
         , department.DisplayName           AS DepartmentEn
         , departmentProfile.NameAr         AS DepartmentAr
         , tr.DayDate
         , DATENAME(dw, tr.DayDate)         AS DayofWeek
         , tr.WeeklyOff
         , tr.RestDay
         -- a weekly-off day has no shift to show even if the engine stamped one
         , CASE WHEN tr.ShiftStartTime1 <= '1800-01-01' OR tr.WeeklyOff = 1 THEN NULL ELSE tr.ShiftStartTime1 END AS ShiftStart
         , CASE WHEN tr.ShiftEndTime1   <= '1800-01-01' OR tr.WeeklyOff = 1 THEN NULL ELSE tr.ShiftEndTime1   END AS ShiftEnd
         , CASE WHEN tr.BreakStartTime  <= '1800-01-01' THEN NULL ELSE tr.BreakStartTime END AS BreakStart
         , CASE WHEN tr.BreakEndTime    <= '1800-01-01' THEN NULL ELSE tr.BreakEndTime   END AS BreakEnd
         , CASE WHEN tr.ActualIn        <= '1800-01-01' THEN NULL ELSE tr.ActualIn       END AS PunchIn
         , CASE WHEN tr.ActualOut       <= '1800-01-01' THEN NULL ELSE tr.ActualOut      END AS PunchOut

         , CASE WHEN tr.ActualIn  > '1800-01-01' AND tr.ShiftStartTime1 > '1800-01-01' AND tr.ActualIn  > tr.ShiftStartTime1
                THEN DATEDIFF(MINUTE, tr.ShiftStartTime1, tr.ActualIn) ELSE 0 END AS LateIn
         , CASE WHEN tr.ActualOut > '1800-01-01' AND tr.ShiftEndTime1   > '1800-01-01' AND tr.ActualOut < tr.ShiftEndTime1
                THEN DATEDIFF(MINUTE, tr.ActualOut, tr.ShiftEndTime1)   ELSE 0 END AS EarlyOut

         , tr.PlannedWorkingDay
         , CONVERT(INT, tr.PlannedWorkingMins) AS PlannedWorkingMins
         , tr.ActualWorkingDay
         , CONVERT(INT, tr.ActualWorkingMins)  AS ActualWorkingMins
         , CONVERT(INT, tr.OvertimeMinutes)    AS OvertimeMinutes
    FROM Employees emp

    LEFT JOIN TimeRegisters tr
           ON tr.EmployeeId = emp.Id AND tr.TenantId = @TenantId AND tr.IsDeleted = 0

    LEFT JOIN EmployeeOrgAssignments orgAsg
           ON orgAsg.EmployeeId = emp.Id AND orgAsg.TenantId = @TenantId AND orgAsg.IsDeleted = 0
          AND orgAsg.StartDate <= @PrmToDate
          AND orgAsg.EndDate   >= @PrmToDate
    LEFT JOIN PayrollAreas payrollArea
           ON payrollArea.Id = orgAsg.PayrollAreaId AND payrollArea.TenantId = @TenantId AND payrollArea.IsDeleted = 0

    LEFT JOIN AbpOrganizationUnits ou
           ON ou.Id = orgAsg.OrganizationUnitId AND ou.TenantId = @TenantId AND ou.IsDeleted = 0
    OUTER APPLY (
        SELECT TOP 1 anc.Id, anc.DisplayName
        FROM AbpOrganizationUnits anc
        INNER JOIN OrgUnitProfiles ancp       ON ancp.OrganizationUnitId = anc.Id      AND ancp.TenantId = @TenantId AND ancp.IsDeleted = 0
        INNER JOIN OrganizationUnitTypes anct ON anct.Id = ancp.OrganizationUnitTypeId AND anct.TenantId = @TenantId AND anct.IsDeleted = 0
        WHERE anc.TenantId = @TenantId AND anc.IsDeleted = 0
          AND ou.Code LIKE anc.Code + '%'
          AND anct.NameEn = 'Company'
        ORDER BY LEN(anc.Code)
    ) company
    LEFT JOIN OrgUnitProfiles companyProfile
           ON companyProfile.OrganizationUnitId = company.Id AND companyProfile.TenantId = @TenantId AND companyProfile.IsDeleted = 0
    OUTER APPLY (
        SELECT TOP 1 anc.Id, anc.DisplayName
        FROM AbpOrganizationUnits anc
        INNER JOIN OrgUnitProfiles ancp       ON ancp.OrganizationUnitId = anc.Id      AND ancp.TenantId = @TenantId AND ancp.IsDeleted = 0
        INNER JOIN OrganizationUnitTypes anct ON anct.Id = ancp.OrganizationUnitTypeId AND anct.TenantId = @TenantId AND anct.IsDeleted = 0
        WHERE anc.TenantId = @TenantId AND anc.IsDeleted = 0
          AND ou.Code LIKE anc.Code + '%'
          AND anct.NameEn = 'Department'
        ORDER BY LEN(anc.Code)
    ) department
    LEFT JOIN OrgUnitProfiles departmentProfile
           ON departmentProfile.OrganizationUnitId = department.Id AND departmentProfile.TenantId = @TenantId AND departmentProfile.IsDeleted = 0

    -- legacy also LEFT JOINed DutyPeriod, Duty, TimePolicy and JobTitle and projected nothing from
    -- any of them; dropped (see BankSql.cs 'DROPPED UNUSED JOINS').

    WHERE emp.TenantId = @TenantId
      AND emp.IsDeleted = 0
      -- @PrmEmpCode is a COMMA-SEPARATED LIST here, not a single code. The legacy trick is to wrap
      -- both sides in commas and use LIKE so that '1000' does not match '11000'. Ported verbatim.
      AND ((ISNULL(@PrmEmpCode, '') <> '' AND ',' + @PrmEmpCode + ',' LIKE '%,' + CONVERT(VARCHAR(50), emp.Code) + ',%')
           OR ISNULL(@PrmEmpCode, '') = '')
      -- NOTE: payroll area is MANDATORY here (the legacy optional form is commented out just below
      -- it in the source). A caller that leaves PrmPayrollArea blank gets an empty report by design.
      AND payrollArea.Name = @PrmPayrollArea
      AND tr.DayDate >= @PrmFromDate
      AND tr.DayDate <= @PrmToDate
) A
WHERE ((ISNULL(@PrmDepartment, '') <> '' AND A.DepartmentEn = @PrmDepartment) OR ISNULL(@PrmDepartment, '') = '');";

        /// <summary>Payroll-area combo for rptEmployeeAttendance.</summary>
        public const string PayrollAreaList = @"-- Legacy inline designer SQL: select ""PayrollArea"".""Code"", ""PayrollArea"".""Name"" from ""dbo"".""PayrollArea""
SELECT p.Code
     , p.Name
FROM PayrollAreas p
WHERE p.TenantId = @TenantId
  AND p.IsDeleted = 0
ORDER BY p.Name;";

        /// <summary>Department combo for rptEmployeeAttendance.</summary>
        public const string DepartmentList = @"-- Legacy: OrganizationUnit LEFT JOIN OrganizationUnitType WHERE NameEn = 'Department'.
-- HCMv3: the unit type moved off the ABP table onto OrgUnitProfiles.
SELECT ou.DisplayName AS DepartmentEn
FROM AbpOrganizationUnits ou
INNER JOIN OrgUnitProfiles p       ON p.OrganizationUnitId    = ou.Id AND p.TenantId = @TenantId AND p.IsDeleted = 0
INNER JOIN OrganizationUnitTypes t ON t.Id = p.OrganizationUnitTypeId AND t.TenantId = @TenantId AND t.IsDeleted = 0
WHERE ou.TenantId = @TenantId
  AND ou.IsDeleted = 0
  AND t.NameEn = 'Department';";

        // -------------------------------------------------------------------------------------------
        // rptEmployeeOvertime -- approved overtime and its cash value, monthly or for one day.
        // -------------------------------------------------------------------------------------------
        public const string EmployeeOvertimeQuery = @"-- rptEmployeeOvertime :: master query. FINANCIAL OUTPUT -- TotalOTPay is what the employee is paid.
--
-- THE OVERTIME FORMULA, reproduced exactly (this is Kuwait labour-law payroll arithmetic, not a
-- rounding preference -- do not simplify):
--     OTPayParDayRate  = OTPay / 26                      -- 26 = the statutory working days per month
--     hourlyRate       = OTPayParDayRate / DefaultWorkingHours   (ManagePayrollSettingses, = 8)
--     TotalOTHOurs     = FinalNormalDayOT + FinalWeekendOT + FinalPHOT   (RATE-WEIGHTED hours)
--     TotalOTPay       = TotalOTHOurs * hourlyRate, CONVERTed to DECIMAL(10,3)
--   * ""Final*"" columns are SUM(ApprovedOvertimeHours * OvertimeTypes.OvertimeRate) -- hours already
--     multiplied by the statutory premium (1.25 normal / 1.5 weekend / 2.0 public holiday). The
--     unweighted ""*OTHours"" columns are printed alongside for the reader. Mixing the two up silently
--     under- or over-pays by the premium.
--   * OTPay is the SUM of every basic-pay component whose wage type has IncludeInOvertimePay = 1 --
--     NOT the basic salary. BasicSalary (wage type Code '1000') is printed but not used in the maths.
--   * The DECIMAL(10,3) conversion is the last step, matching KWD's three decimal places.
--
-- *** LEGACY DEFECT REPRODUCED ON PURPOSE: NULL propagation kills the money column. ***
-- Each Final* column is a SUM over EmployeeOvertimes, which is NULL -- not 0 -- when the employee
-- booked no overtime of that category in the period. TotalOTHOurs and therefore TotalOTPay are
-- NULL + x = NULL unless the employee happens to have overtime in ALL THREE categories.
-- Verified against live HCMv3 data: employee 3400 / June 2024 has 27.5 normal-day hours, 34.375
-- rate-weighted, and both totals come back NULL.
-- NOT ""fixed"" with ISNULL(...,0): this column is what an employee is PAID, and quietly changing a
-- blank into a number changes payroll output. *** RAISE AS A PRODUCT BUG and change it deliberately,
-- with sign-off, rather than as a side effect of a port. ***
--
-- OVERTIME TYPES are matched BY NAME ('Normal Day OT' / 'Weekend OT' / 'Public Holiday OT'), exactly
-- as the legacy source does. That is fragile -- renaming an overtime type silently drops its hours
-- from the pay -- but it is what the legacy report computed, and switching to Code would change
-- results for any tenant whose names differ. *** WORTH RAISING AS A PRODUCT BUG. ***
SELECT A.Code
     , A.FullNameEn
     , A.Companylogo
     , A.CompanyEn
     , A.CompanyAr
     , A.DepartmentEn
     , A.DepartmentAr
     , CASE WHEN @PrmReportType = 'MONTHLY' THEN UPPER(FORMAT(@PrmAsOnDate, 'MMMM-yyyy'))
            ELSE CONVERT(VARCHAR(10), @PrmAsOnDate, 103) END AS Period

     , A.BasicSalary
     , A.OTPay
     , A.OTPayParDayRate
     , A.DefaultWorkingHours

     , A.NormalDayOTHours
     , A.FinalNormalDayOTHours
     , A.WeekendOTHours
     , A.FinalWeekendOTHours
     , A.PHOTHours
     , A.FinalPHOTHours

     , (A.FinalNormalDayOTHours + A.FinalWeekendOTHours + A.FinalPHOTHours) AS TotalOTHOurs

     , CONVERT(DECIMAL(10, 3),
         CASE WHEN A.DefaultWorkingHours <> 0
              THEN (A.FinalNormalDayOTHours + A.FinalWeekendOTHours + A.FinalPHOTHours)
                   * (A.OTPayParDayRate / A.DefaultWorkingHours)
              ELSE 0 END) AS TotalOTPay
FROM (
    SELECT emp.Code
         , emp.FullNameEn
         , CAST(NULL AS VARBINARY(MAX))     AS Companylogo    -- FEATURE LOST, see header
         , company.DisplayName              AS CompanyEn
         , companyProfile.NameAr            AS CompanyAr
         , department.DisplayName           AS DepartmentEn
         , departmentProfile.NameAr         AS DepartmentAr

         , basicPayRate.Amount              AS BasicSalary
         , otPayTbl.OTPay
         , CONVERT(DECIMAL(10, 6), (otPayTbl.OTPay / 26)) AS OTPayParDayRate
         , (SELECT TOP 1 ps.DefaultWorkingHours FROM ManagePayrollSettingses ps
            WHERE ps.TenantId = @TenantId AND ps.IsDeleted = 0) AS DefaultWorkingHours

         , ot.NormalDayOTHours
         , ot.FinalNormalDayOTHours
         , ot.WeekendOTHours
         , ot.FinalWeekendOTHours
         , ot.PHOTHours
         , ot.FinalPHOTHours
    FROM Employees emp

    LEFT JOIN EmployeeOrgAssignments orgAsg
           ON orgAsg.EmployeeId = emp.Id AND orgAsg.TenantId = @TenantId AND orgAsg.IsDeleted = 0
          AND orgAsg.StartDate <= EOMONTH(@PrmAsOnDate)
          AND orgAsg.EndDate   >= EOMONTH(@PrmAsOnDate)

    LEFT JOIN AbpOrganizationUnits ou
           ON ou.Id = orgAsg.OrganizationUnitId AND ou.TenantId = @TenantId AND ou.IsDeleted = 0
    OUTER APPLY (
        SELECT TOP 1 anc.Id, anc.DisplayName
        FROM AbpOrganizationUnits anc
        INNER JOIN OrgUnitProfiles ancp       ON ancp.OrganizationUnitId = anc.Id      AND ancp.TenantId = @TenantId AND ancp.IsDeleted = 0
        INNER JOIN OrganizationUnitTypes anct ON anct.Id = ancp.OrganizationUnitTypeId AND anct.TenantId = @TenantId AND anct.IsDeleted = 0
        WHERE anc.TenantId = @TenantId AND anc.IsDeleted = 0
          AND ou.Code LIKE anc.Code + '%'
          AND anct.NameEn = 'Company'
        ORDER BY LEN(anc.Code)
    ) company
    LEFT JOIN OrgUnitProfiles companyProfile
           ON companyProfile.OrganizationUnitId = company.Id AND companyProfile.TenantId = @TenantId AND companyProfile.IsDeleted = 0
    OUTER APPLY (
        SELECT TOP 1 anc.Id, anc.DisplayName
        FROM AbpOrganizationUnits anc
        INNER JOIN OrgUnitProfiles ancp       ON ancp.OrganizationUnitId = anc.Id      AND ancp.TenantId = @TenantId AND ancp.IsDeleted = 0
        INNER JOIN OrganizationUnitTypes anct ON anct.Id = ancp.OrganizationUnitTypeId AND anct.TenantId = @TenantId AND anct.IsDeleted = 0
        WHERE anc.TenantId = @TenantId AND anc.IsDeleted = 0
          AND ou.Code LIKE anc.Code + '%'
          AND anct.NameEn = 'Department'
        ORDER BY LEN(anc.Code)
    ) department
    LEFT JOIN OrgUnitProfiles departmentProfile
           ON departmentProfile.OrganizationUnitId = department.Id AND departmentProfile.TenantId = @TenantId AND departmentProfile.IsDeleted = 0

    LEFT JOIN EmployeeBasicPays basicPay
           ON basicPay.EmployeeId = emp.Id AND basicPay.TenantId = @TenantId AND basicPay.IsDeleted = 0
          AND basicPay.StartDate <= EOMONTH(@PrmAsOnDate)
          AND basicPay.EndDate   >= EOMONTH(@PrmAsOnDate)
    LEFT JOIN EmployeeBasicPayDetails basicPayRate
           ON basicPayRate.EmployeeBasicPayId = basicPay.Id AND basicPayRate.TenantId = @TenantId AND basicPayRate.IsDeleted = 0
          AND basicPayRate.WageTypeId IN (SELECT wt.Id FROM WageTypes wt
                                          WHERE wt.Code = '1000' AND wt.TenantId = @TenantId AND wt.IsDeleted = 0)

    -- the overtime-bearing part of the salary: every component whose wage type opts into overtime pay
    LEFT JOIN (
        SELECT bp.Id AS EmployeeBasicPayId
             , SUM(bpd.Amount) AS OTPay
        FROM EmployeeBasicPayDetails bpd
        LEFT JOIN EmployeeBasicPays bp ON bp.Id = bpd.EmployeeBasicPayId AND bp.TenantId = @TenantId AND bp.IsDeleted = 0
        LEFT JOIN WageTypes wt         ON wt.Id = bpd.WageTypeId         AND wt.TenantId = @TenantId AND wt.IsDeleted = 0
        WHERE bpd.TenantId = @TenantId AND bpd.IsDeleted = 0
          AND wt.IncludeInOvertimePay = 1
        GROUP BY bp.Id
    ) otPayTbl ON otPayTbl.EmployeeBasicPayId = basicPay.Id

    -- The legacy version repeated the same correlated subquery SIX times, once per column. Collapsed
    -- into ONE conditional aggregate: identical arithmetic, one pass over EmployeeOvertimes instead
    -- of six. Overtime types are still matched by NAME -- see the header note.
    OUTER APPLY (
        SELECT SUM(CASE WHEN oty.NameEn = 'Normal Day OT'      THEN eot.ApprovedOvertimeHours END)                     AS NormalDayOTHours
             , SUM(CASE WHEN oty.NameEn = 'Normal Day OT'      THEN eot.ApprovedOvertimeHours * oty.OvertimeRate END)  AS FinalNormalDayOTHours
             , SUM(CASE WHEN oty.NameEn = 'Weekend OT'         THEN eot.ApprovedOvertimeHours END)                     AS WeekendOTHours
             , SUM(CASE WHEN oty.NameEn = 'Weekend OT'         THEN eot.ApprovedOvertimeHours * oty.OvertimeRate END)  AS FinalWeekendOTHours
             , SUM(CASE WHEN oty.NameEn = 'Public Holiday OT'  THEN eot.ApprovedOvertimeHours END)                     AS PHOTHours
             , SUM(CASE WHEN oty.NameEn = 'Public Holiday OT'  THEN eot.ApprovedOvertimeHours * oty.OvertimeRate END)  AS FinalPHOTHours
        FROM EmployeeOvertimes eot
        LEFT JOIN OvertimeTypes oty
               ON oty.Id = eot.OvertimeTypeId AND oty.TenantId = @TenantId AND oty.IsDeleted = 0
        WHERE eot.EmployeeId = emp.Id AND eot.TenantId = @TenantId AND eot.IsDeleted = 0
          AND ((@PrmReportType = 'MONTHLY'
                AND MONTH(eot.EffectiveDate) = MONTH(@PrmAsOnDate)
                AND YEAR(eot.EffectiveDate)  = YEAR(@PrmAsOnDate))
            OR (@PrmReportType = 'DAILY' AND eot.EffectiveDate = @PrmAsOnDate))
    ) ot

    WHERE emp.TenantId = @TenantId
      AND emp.IsDeleted = 0
      AND ((ISNULL(@PrmEmpCode, '') <> '' AND emp.Code = @PrmEmpCode) OR ISNULL(@PrmEmpCode, '') = '')
) A;";
    }
}
