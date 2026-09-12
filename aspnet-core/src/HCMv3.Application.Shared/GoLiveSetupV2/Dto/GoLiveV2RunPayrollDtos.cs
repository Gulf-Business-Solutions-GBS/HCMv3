using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.GoLiveSetupV2.Dto;

/// <summary>
/// Everything optional on purpose -- see <see cref="IGoLiveV2AppService.RunPayroll"/>. Validation lives in
/// the engine, and its refusals are the point.
/// </summary>
public class GoLiveV2RunPayrollInput
{
    /// <summary>
    /// Any day in the month to pay. V2 sends that month's last day, which is the only date the engine accepts.
    /// </summary>
    public DateTime? PayDate { get; set; }

    /// <summary>Name of the payroll area to run, as the engine matches it.</summary>
    public string PayrollAreaName { get; set; }
}

/// <summary>Where a run stopped. <see cref="None"/> means it went all the way through.</summary>
public enum GoLiveV2StoppedAt
{
    /// <summary>Nothing stopped it -- every employee was paid.</summary>
    None = 0,

    /// <summary>Something the engine needs before it will even try was not given.</summary>
    Input = 1,

    /// <summary>The engine refused to start the run.</summary>
    Engine = 2,

    /// <summary>The run went ahead, but one or more employees could not be paid.</summary>
    Employees = 3,

    /// <summary>The run went ahead and finished, but found nobody in the payroll area to pay.</summary>
    NoEmployees = 4,

    /// <summary>
    /// The time engine, run first for the month, could not start or failed for some employee's days -- so the
    /// payroll engine was not run: it pays days from the time registers the time engine writes.
    /// </summary>
    TimeEngine = 5,

    /// <summary>The month's control record or pay calendar was missing, and V2 could not set them up.</summary>
    MonthSetUp = 6,
}

public class GoLiveV2RunPayrollOutput
{
    public bool Succeeded { get; set; }

    public GoLiveV2StoppedAt StoppedAt { get; set; }

    /// <summary>
    /// What stopped it, in the engine's own words. Deliberately not rephrased: V2 is reverse-engineering the
    /// engine, and the exact wording is what the next step is built from.
    /// </summary>
    public string Message { get; set; }

    /// <summary>
    /// The V2 step that answers this refusal, when V2 has one. Worked out from what is actually missing, not
    /// by matching the message, so a reworded engine message cannot silently hide the step.
    /// </summary>
    public GoLiveV2NextStep NextStep { get; set; }

    /// <summary>The month end the engine actually paid up to, once it got as far as starting.</summary>
    public DateTime? PayInDate { get; set; }

    /// <summary>One row per employee the run reached, paid or not.</summary>
    public List<GoLiveV2EmployeeResultDto> Employees { get; set; } = new();

    /// <summary>
    /// What Run payroll set up for the month before running -- payroll area, control record, pay calendar --
    /// when any of it was missing. Null when the month was already set up.
    /// </summary>
    public GoLiveV2SetUpPayrollMonthOutput MonthSetUp { get; set; }

    /// <summary>
    /// "Paid during leave" links added from the legacy catalogue before running, because the company had none;
    /// 0 when it already had them. Without them annual leave pay and the annual leave provision are zero.
    /// </summary>
    public int LeavePayLinksAdded { get; set; }
}

public class GoLiveV2EmployeeResultDto
{
    public string EmployeeCode { get; set; }

    public bool Succeeded { get; set; }

    /// <summary>The engine's own message for this employee.</summary>
    public string Message { get; set; }
}

/// <summary>A step V2 can take to answer the engine's latest refusal. Grows one member per link answered.</summary>
public enum GoLiveV2NextStep
{
    None = 0,

    /// <summary>
    /// "Payroll Control Record not found" or "Payroll Calender does not have record for Payroll month": create
    /// whichever of the payroll area, its control record and the year's pay calendar is missing.
    /// </summary>
    SetUpPayrollMonth = 1,

    /// <summary>The run went through but found nobody in the payroll area: load the employees.</summary>
    LoadEmployees = 2,
}

/// <summary>The month and payroll area to set up -- the same two things the run was given.</summary>
public class GoLiveV2SetUpPayrollMonthInput
{
    [Required]
    public DateTime? PayDate { get; set; }

    [Required]
    [StringLength(SimpleLookupConsts.MaxNameLength)]
    public string PayrollAreaName { get; set; }
}

/// <summary>What the set-up did, so the screen can say so -- including the defaults it chose.</summary>
public class GoLiveV2SetUpPayrollMonthOutput
{
    public string PayrollAreaName { get; set; }

    /// <summary>False when a payroll area of that name already existed and was used as it was.</summary>
    public bool PayrollAreaCreated { get; set; }

    public string CurrencyCode { get; set; }

    public string PayYear { get; set; }

    public int CutOffDay { get; set; }

    /// <summary>False when the area already had a control record, which was left as it was.</summary>
    public bool ControlRecordCreated { get; set; }

    public DateTime MonthStart { get; set; }

    public DateTime MonthEnd { get; set; }

    /// <summary>The year whose pay calendar was laid down, January to December.</summary>
    public int CalendarYear { get; set; }

    /// <summary>How many of its twelve months were added; 0 when the calendar was already complete.</summary>
    public int CalendarMonthsCreated { get; set; }
}
