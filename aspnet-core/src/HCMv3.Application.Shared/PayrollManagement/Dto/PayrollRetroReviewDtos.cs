using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Abp.Application.Services.Dto;

namespace HCMv3.PayrollManagement.Dto;

/// <summary>
/// The three server-side tabs of legacy's Retro popup (<c>rdRetroAll</c> / <c>rdRetroOnly</c> / <c>rdRetroNone</c>
/// in <c>uscPayrollEngine.ascx</c>). Legacy's fourth tab, "Changed", shows edits not yet saved; those exist only in
/// the dialog, so the dialog filters them itself.
/// </summary>
public enum PayrollRetroReviewFilter
{
    All = 0,

    /// <summary>Legacy's default tab when the popup opens.</summary>
    Retro = 1,

    NoRetro = 2
}

/// <summary>One page of the Retro review grid, for one payroll area.</summary>
public class GetPayrollRetroReviewEmployeesInput : PagedAndSortedResultRequestDto
{
    [Required]
    [StringLength(256)]
    public string PayrollAreaName { get; set; }

    public PayrollRetroReviewFilter RetroFilter { get; set; } = PayrollRetroReviewFilter.Retro;

    /// <summary>Searches employee code and English name (legacy grid's filter row).</summary>
    public string Filter { get; set; }
}

/// <summary>A row of legacy's <c>RetroPopupModel</c> as saved in the database.</summary>
public class PayrollRetroReviewEmployeeDto
{
    public int EmployeeId { get; set; }

    public string EmployeeCode { get; set; }

    public string EmployeeName { get; set; }

    /// <summary><c>EmployeePayrollStatus.EarliestMasterDataChange</c>; null when it was never set.</summary>
    public DateTime? RetroFromDate { get; set; }

    /// <summary>Whole months from the retro month to the payroll month (legacy <c>TotalRetroMonth</c>).</summary>
    public int RetroMonths { get; set; }

    /// <summary>The reason on the employee's latest Retro Reason Log line; blank when there is no retro date.</summary>
    public string Reason { get; set; }

    /// <summary>The retro date falls before the payroll period start (legacy <c>HasRetro</c>).</summary>
    public bool HasRetro { get; set; }
}

/// <summary>
/// The page plus what legacy shows around the grid: the tab counts and the period the retro status is measured
/// against (the payroll area's control record).
/// </summary>
public class GetPayrollRetroReviewEmployeesOutput : PagedResultDto<PayrollRetroReviewEmployeeDto>
{
    public int AllCount { get; set; }

    public int RetroCount { get; set; }

    public int NoRetroCount { get; set; }

    public DateTime PeriodStartDate { get; set; }

    public DateTime PeriodEndDate { get; set; }
}

/// <summary>One employee's new retro date. A null date removes the retro, as legacy's note says.</summary>
public class PayrollRetroDateChangeDto
{
    [Range(1, int.MaxValue)]
    public int EmployeeId { get; set; }

    public DateTime? RetroFromDate { get; set; }

    /// <summary>Required (legacy "Reason is required for update."). 500 = legacy grid's Reason column limit.</summary>
    [StringLength(500)]
    public string Reason { get; set; }
}

/// <summary>
/// Everything legacy's Save button sends: every employee changed in the dialog, each with its own date and reason,
/// saved together or not at all.
/// </summary>
public class UpdatePayrollRetroDatesInput
{
    [Required]
    [StringLength(256)]
    public string PayrollAreaName { get; set; }

    public List<PayrollRetroDateChangeDto> Changes { get; set; } = new();
}
