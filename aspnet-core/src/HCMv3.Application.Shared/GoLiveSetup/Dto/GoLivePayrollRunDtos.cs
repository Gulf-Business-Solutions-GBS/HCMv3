using System;

namespace HCMv3.GoLiveSetup.Dto;

/// <summary>Where a background payroll run stands.</summary>
public enum GoLiveRunStatus
{
    /// <summary>No run has been started for this company.</summary>
    None = 0,

    /// <summary>Started, waiting for the job to pick it up.</summary>
    Queued = 1,

    /// <summary>The engines are working through the employees.</summary>
    Running = 2,

    /// <summary>Finished; <see cref="GoLivePayrollRunProgressDto.Result"/> says how.</summary>
    Completed = 3,

    /// <summary>Stopped on request between two batches. What ran before is saved.</summary>
    Stopped = 4,

    /// <summary>Ended without finishing: an error, or the server stopped under it.</summary>
    Failed = 5,
}

/// <summary>
/// A background payroll run as the screen shows it: where it is, how far each engine has got, and once it is
/// over, the result a direct run would have returned. Read from the server, so a reload or a new login finds
/// the same run.
/// </summary>
public class GoLivePayrollRunProgressDto
{
    public GoLiveRunStatus Status { get; set; }

    public string PayrollAreaName { get; set; }

    public DateTime? PayInDate { get; set; }

    public DateTime? StartedAt { get; set; }

    public DateTime? FinishedAt { get; set; }

    /// <summary>Employees the run covers.</summary>
    public int TotalEmployees { get; set; }

    /// <summary>Employees the time engine has worked out, failed ones included.</summary>
    public int TimeDone { get; set; }

    public int TimeFailed { get; set; }

    /// <summary>Employees the payroll engine has paid, failed ones included.</summary>
    public int PayrollDone { get; set; }

    public int PayrollFailed { get; set; }

    /// <summary>Average time per employee across both engines so far, for the screen's estimate.</summary>
    public double AverageMilliseconds { get; set; }

    public bool StopRequested { get; set; }

    /// <summary>Set once the run is over: what a direct Run payroll would have returned.</summary>
    public GoLiveRunPayrollOutput Result { get; set; }
}
