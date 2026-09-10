using System;
using System.Collections.Generic;

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

    /// <summary>The month end the engine actually paid up to, once it got as far as starting.</summary>
    public DateTime? PayInDate { get; set; }

    /// <summary>One row per employee the run reached, paid or not.</summary>
    public List<GoLiveV2EmployeeResultDto> Employees { get; set; } = new();
}

public class GoLiveV2EmployeeResultDto
{
    public string EmployeeCode { get; set; }

    public bool Succeeded { get; set; }

    /// <summary>The engine's own message for this employee.</summary>
    public string Message { get; set; }
}
