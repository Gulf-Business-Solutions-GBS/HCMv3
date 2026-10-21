using System;
using System.ComponentModel.DataAnnotations;
using Abp.Application.Services.Dto;
using HCMv3.PayrollManagement.Dto;

namespace HCMv3.ProjectManagement.Dto;

/// <summary>
/// The period the Project Cost Engine screen shows for a payroll area. Legacy <c>SetCurrentDates</c> (Current,
/// <c>uscProjectCosting.ascx.cs</c> lines 1317-1330) and <c>SetDates</c> (Other, lines 1307-1316): month, year
/// and the read-only start / end dates. Dates are <c>null</c> when legacy left the boxes as they were (no control
/// record for Current; an area without a valid payroll period for Other).
/// </summary>
public class ProjectCostEnginePeriodDto
{
    public string PayrollAreaName { get; set; }

    /// <summary>The area has a payroll control record (always reported, whichever period was asked for).</summary>
    public bool HasControlRecord { get; set; }

    public int? PayrollMonth { get; set; }

    public int? PayrollYear { get; set; }

    public DateTime? CalendarMonthStart { get; set; }

    /// <summary>Legacy <c>txtEndDate</c>: the date the run is sent with.</summary>
    public DateTime? CalendarMonthEnd { get; set; }

    /// <summary>The control record's period is locked -- what a regular run needs (with the end date being its own).</summary>
    public bool PeriodLocked { get; set; }

    public bool PeriodClosed { get; set; }

    /// <summary>
    /// The engine would accept a run for <see cref="CalendarMonthEnd"/>: the control record is locked AND this is its
    /// own period end (legacy <c>Main</c> line 235). Informational -- legacy did not stop the click; the engine refuses.
    /// </summary>
    public bool RunAllowed { get; set; }

    /// <summary>Why there are no dates, when there are none.</summary>
    public string Message { get; set; }
}

/// <summary>Input for the employee picker (legacy <c>BuildEmployeePopupList</c> + the popup's filters).</summary>
public class GetProjectCostEngineCandidatesInput : PagedAndSortedResultRequestDto
{
    [Required]
    [StringLength(256)]
    public string PayrollAreaName { get; set; }

    /// <summary>The period end the screen shows (legacy <c>txtEndDate</c>).</summary>
    [Required]
    public DateTime PayrollDate { get; set; }

    /// <summary>All / Processed / Pending / Selected (legacy rdAll / rdProcessed / rdPending / rdSelected).</summary>
    public PayrollRunCandidateFilter StatusFilter { get; set; }

    /// <summary>Text search on employee code and name (the grid's auto-filter row).</summary>
    [StringLength(256)]
    public string Filter { get; set; }

    /// <summary>The comma-separated codes currently in the Employee Code box; they come back marked as selected.</summary>
    public string SelectedEmployeeCodes { get; set; }
}

/// <summary>
/// The run log grid (legacy <c>grid_Payroll</c>, rows of <c>ProjectLog</c>, lines 2317-2339) with its
/// All / Successful / Failed filter (legacy rdLogAll / rdLogSuccess / rdLogFailed).
/// </summary>
public class GetProjectCostEngineLogsInput : PagedAndSortedResultRequestDto
{
    /// <summary><c>true</c> = failed rows only, <c>false</c> = successful only, <c>null</c> = all.</summary>
    public bool? OnlyFailed { get; set; }
}

/// <summary>Which of the log grid's rows to export (legacy <c>btnExportTimeLog_Click</c>, lines 2340-2351).</summary>
public class GetProjectCostEngineLogsToExcelInput
{
    /// <summary><c>true</c> = failed rows only, <c>false</c> = successful only, <c>null</c> = all.</summary>
    public bool? OnlyFailed { get; set; }
}

/// <summary>One row of the run log grid -- legacy <c>LogPopupModel</c> (lines 2396-2405).</summary>
public class ProjectCostEngineLogDto : EntityDto<int>
{
    /// <summary>Empty for the run-level failure row, which names no employee.</summary>
    public string EmployeeCode { get; set; }

    /// <summary>Arabic name under an Arabic UI when the employee has one (legacy <c>isArabic</c>).</summary>
    public string EmployeeName { get; set; }

    public DateTime RunTimeStamp { get; set; }

    /// <summary>0 = successful, 1 = failed (legacy <c>StatusText</c>).</summary>
    public int Status { get; set; }

    public string Message { get; set; }

    public DateTime InPeriod { get; set; }
}

/// <summary>The counts on the log grid's filter buttons (legacy <c>RefreshPayrollLogCounts</c>, lines 1679-1691).</summary>
public class ProjectCostEngineLogsOutput : PagedResultDto<ProjectCostEngineLogDto>
{
    public int AllCount { get; set; }

    public int SuccessCount { get; set; }

    public int FailedCount { get; set; }
}
