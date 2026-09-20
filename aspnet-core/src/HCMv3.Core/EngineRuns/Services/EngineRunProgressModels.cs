using System;

namespace HCMv3.EngineRuns.Services;

/// <summary>Everything known about a run at the moment it starts.</summary>
public class EngineRunStartInfo
{
    public EngineType EngineType { get; set; }

    /// <summary>Optional caller-supplied correlation id. A GUID string is generated when omitted.</summary>
    public string RunKey { get; set; }

    public string StartedByUserName { get; set; }

    /// <summary>The same pipe-separated string the engine puts on <c>ExecutionLog.Parameters</c>.</summary>
    public string Parameters { get; set; }
}

/// <summary>One finished employee, success or failure.</summary>
public class EngineRunEmployeeProgress
{
    public long EngineRunId { get; set; }

    public int SequenceNo { get; set; }

    public int? EmployeeId { get; set; }

    public string EmployeeCode { get; set; }

    public string EmployeeName { get; set; }

    public bool Succeeded { get; set; }

    public string Message { get; set; }

    public DateTime StartedAt { get; set; }

    public DateTime CompletedAt { get; set; }

    public int ElapsedMilliseconds { get; set; }
}

/// <summary>
/// The little that a caller needs to know about a run already in progress: enough to tell the operator who is
/// running what, and since when, rather than a bare "try again later".
/// </summary>
public class ActiveEngineRun
{
    public long EngineRunId { get; set; }

    /// <summary>The lock key -- for the payroll engine, the payroll area name.</summary>
    public string RunKey { get; set; }

    public string StartedByUserName { get; set; }

    public System.DateTime StartedAt { get; set; }
}
