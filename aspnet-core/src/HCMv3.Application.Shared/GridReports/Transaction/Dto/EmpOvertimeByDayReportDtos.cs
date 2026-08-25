using Abp.Application.Services.Dto;
using HCMv3.GridReports.Dto;
using HCMv3.GridReports.Employees.Dto;

namespace HCMv3.GridReports.Transaction.Dto;

/// <summary>
/// Filter input for the Employee Overtime By Day analysis grid report (legacy
/// <c>uscRptEmpOvertimeByDay.ascx</c>). Adds the report-specific <c>cmbOvertimeType</c> combo. Legacy has
/// no employee-status filter.
/// </summary>
public class GetEmpOvertimeByDayReportInput : GridReportInputBase
{
    /// <summary>Legacy <c>cmbOvertimeType</c>: OvertimeType.Code. Null/empty means "all overtime types".</summary>
    public string OvertimeTypeCode { get; set; }

    protected override string DefaultSorting => "EmployeeCode ASC";
}

/// <summary>
/// One grid row: one employee's attendance/overtime TOTALS for the window. Mirrors the header/summary
/// columns of legacy <c>Table1()</c> (uscRptEmpOvertimeByDay.ascx.cs:525-761).
///
/// SCOPE REDUCTION -- read before "completing" this: legacy's <c>Table1()</c> is a wide PIVOT with one
/// column PER CALENDAR DAY in the requested window (up to 31 dynamically-named columns, e.g. "5-Mon") plus
/// one column per <c>OvertimeType</c> master row. Both are data-driven column sets with no fixed shape, so
/// they do not fit a typed DTO/PrimeNG-table contract the way the rest of this wave's reports do. This
/// port keeps every legacy SUMMARY column (leave/absent/working days, working hours, total OT hours,
/// total hours) and drops the per-day matrix -- the same information is available per-day from the Time
/// Register analysis reports (already ported) and from <see cref="EmpOvertimeReportRowDto"/>'s per-type
/// buckets. Not a data-loss defect: every underlying row is still counted, just not exploded into a column
/// per calendar day.
/// </summary>
public class EmpOvertimeByDayReportRowDto : EntityDto<int>, IEmployeeOrgLevelRow
{
    public string EmployeeCode { get; set; }

    public string EmployeeName { get; set; }

    public string JobTitleName { get; set; }

    public string PayrollAreaName { get; set; }

    public string OrgLevel1 { get; set; }
    public string OrgLevel2 { get; set; }
    public string OrgLevel3 { get; set; }
    public string OrgLevel4 { get; set; }
    public string OrgLevel5 { get; set; }
    public string OrgLevel6 { get; set; }
    public string OrgLevel7 { get; set; }
    public long? OrganizationUnitId { get; set; }

    public string AccessId { get; set; }

    public string ReferenceCode { get; set; }

    public string WorkLocationName { get; set; }

    public double AnnualLeaveDays { get; set; }

    public double SickLeaveDays { get; set; }

    public double AbsentDays { get; set; }

    public double ActualWorkingDays { get; set; }

    public double ActualWorkingHours { get; set; }

    /// <summary>Sum of <c>ApprovedOvertimeHours</c> over every overtime type in the window.</summary>
    public double TotalOTHours { get; set; }

    public double TotalHours { get; set; }
}
