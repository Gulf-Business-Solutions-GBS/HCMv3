using System;
using System.Collections.Generic;

namespace HCMv3.TimeManagement;

/// <summary>
/// Serialized payload of a queued Time Engine run. This is the ONE shape both callers produce:
/// the on-demand <see cref="TimeEngineAppService"/> and the nightly
/// <see cref="TimeEngineAutorunBackgroundWorker"/>. They differ only in who fills it in and how the dates are
/// resolved -- there is deliberately no second execution path (see
/// <see cref="TimeEngineBackgroundJob"/>).
///
/// <para>Mirrors legacy's <c>args[]</c> array (<c>HCM10.TimeEngine/Program.cs</c> lines ~150-171) with
/// <c>TenantId</c> added, because ABP background jobs start outside any tenant scope and every Time Engine
/// entity is <c>IMustHaveTenant</c>.</para>
/// </summary>
[Serializable]
public class TimeEngineJobArgs
{
    /// <summary>Tenant the run belongs to. No legacy equivalent -- legacy was single-tenant.</summary>
    public int TenantId { get; set; }

    /// <summary>Legacy <c>args[0]</c>: comma-separated employee codes; empty = all.</summary>
    public string EmployeeCodes { get; set; }

    /// <summary>Legacy <c>args[1]</c>: null = engine falls back to the 1st of the current month.</summary>
    public DateTime? StartDate { get; set; }

    /// <summary>Legacy <c>args[2]</c>: required.</summary>
    public DateTime EndDate { get; set; }

    /// <summary>Legacy <c>args[3]</c>: matched against <c>PayrollArea.Name</c>.</summary>
    public string PayrollAreaName { get; set; }

    /// <summary>Legacy <c>args[4]</c>: recorded on the <c>ExecutionLog</c> row.</summary>
    public string CurrentUserName { get; set; }

    /// <summary>Legacy <c>args[5]</c>. Always false for runs started from this module.</summary>
    public bool IsRanFromPayrollEngine { get; set; }

    /// <summary>
    /// PORT ADAPTATION -- see <c>TimeEngineRunInput.EmployeeSubGroupIds</c>. Caller-supplied because no
    /// entity in this port links an Employee to an EmployeeSubGroup.
    /// </summary>
    public Dictionary<int, int> EmployeeSubGroupIds { get; set; }
}
