using System;
using System.ComponentModel.DataAnnotations;
using Abp.Application.Services.Dto;
using HCMv3.EngineRuns.Dto;
using HCMv3.PayrollManagement.Dto;

namespace HCMv3.TimeManagement.Dto;

/// <summary>
/// The Time Engine screen's per-employee grid: the shared grid input plus the payroll area the screen has
/// selected, so the grid follows that area's latest run.
/// </summary>
public class GetTimeEngineRunEmployeesInput : GetEngineRunEmployeesInput
{
    /// <summary>Null or blank = the latest Time Engine run of any area.</summary>
    [StringLength(256)]
    public string PayrollAreaName { get; set; }
}

/// <summary>One employee's days in one run -- the day-level view legacy's log grid showed.</summary>
public class GetTimeEngineRunDaysInput
{
    public long RunId { get; set; }

    [Range(1, int.MaxValue)]
    public int EmployeeId { get; set; }

    /// <summary>true = failed days only, false = successful days only, null = every day.</summary>
    public bool? OnlyFailed { get; set; }
}

/// <summary>
/// One row of legacy's Time Engine log (<c>uscTimeEngine.SetLogGridDataSource</c>): one employee, one day, its
/// status and message, read from <c>TimeLog</c>.
/// </summary>
public class TimeEngineRunDayDto
{
    public int? EmployeeId { get; set; }

    public string EmployeeCode { get; set; }

    public string EmployeeName { get; set; }

    public DateTime DayDate { get; set; }

    public bool Succeeded { get; set; }

    public string Message { get; set; }

    public DateTime RunTimeStamp { get; set; }
}

/// <summary>Which run's log to export, and with which of the grid's filters.</summary>
public class GetTimeEngineRunLogToExcelInput
{
    /// <summary>Null = the latest run the screen follows (see <see cref="PayrollAreaName"/>).</summary>
    public long? RunId { get; set; }

    /// <summary>The payroll area the screen has selected; used only when <see cref="RunId"/> is null.</summary>
    [StringLength(256)]
    public string PayrollAreaName { get; set; }

    /// <summary>Text search on employee code, name and message.</summary>
    [StringLength(256)]
    public string Filter { get; set; }

    /// <summary>true = failed days only, false = successful days only, null = every day (legacy All/Successful/Failed).</summary>
    public bool? OnlyFailed { get; set; }
}

/// <summary>The Time Engine screen's employee picker (legacy <c>uscTimeEngine</c> popupTimeEmployees).</summary>
public class GetTimeEngineRunCandidatesInput : PagedAndSortedResultRequestDto
{
    [Required]
    [StringLength(256)]
    public string PayrollAreaName { get; set; }

    /// <summary>The Start Date on the screen; null = today, as legacy.</summary>
    public DateTime? StartDate { get; set; }

    /// <summary>The End Date on the screen; null = the start date, as legacy.</summary>
    public DateTime? EndDate { get; set; }

    public PayrollRunCandidateFilter StatusFilter { get; set; }

    /// <summary>Text search on employee code and name.</summary>
    [StringLength(256)]
    public string Filter { get; set; }

    /// <summary>The comma-separated codes currently in the Employee Code box; they come back marked as selected.</summary>
    public string SelectedEmployeeCodes { get; set; }
}

/// <summary>A page of candidates plus the counts legacy shows on its filter buttons (over the whole list, not the page).</summary>
public class TimeEngineRunCandidatesOutput : PagedResultDto<PayrollRunCandidateDto>
{
    public int AllCount { get; set; }

    public int ProcessedCount { get; set; }

    public int PendingCount { get; set; }

    public int SelectedCount { get; set; }

    /// <summary>Legacy lblPopupTimePeriod: "dd/MM/yyyy - dd/MM/yyyy".</summary>
    public string PeriodText { get; set; }
}

/// <summary>The screen's run history: the Time Engine's execution log, newest first by default.</summary>
public class GetTimeEngineRunHistoryInput : PagedAndSortedResultRequestDto
{
}
