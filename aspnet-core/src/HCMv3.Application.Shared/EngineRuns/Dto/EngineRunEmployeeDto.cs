using System;
using Abp.Application.Services.Dto;

namespace HCMv3.EngineRuns.Dto;

/// <summary>
/// One row of the operator's per-employee grid: EVERY employee the run processed, succeeded and failed
/// alike, with the failure message when it failed.
/// </summary>
public class EngineRunEmployeeDto : EntityDto<long>
{
    /// <summary>1-based processing order. The grid's natural default sort.</summary>
    public int SequenceNo { get; set; }

    public int? EmployeeId { get; set; }

    public string EmployeeCode { get; set; }

    public string EmployeeName { get; set; }

    public bool Succeeded { get; set; }

    /// <summary>"Successfull" for successes; the (1000-char truncated) error text for failures.</summary>
    public string Message { get; set; }

    public DateTime StartedAt { get; set; }

    public DateTime CompletedAt { get; set; }

    /// <summary>Measured with a stopwatch around this employee's processing.</summary>
    public int ElapsedMilliseconds { get; set; }

    /// <summary><see cref="ElapsedMilliseconds"/> as seconds, rounded to 3 decimals, for direct display.</summary>
    public double ElapsedSeconds { get; set; }
}
