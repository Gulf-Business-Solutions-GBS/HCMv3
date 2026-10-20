using System;
using HCMv3.PayrollManagement.Services;

namespace HCMv3.PayrollManagement;

/// <summary>
/// Serialized payload of a queued Payroll Engine run. Mirrors legacy's <c>args[]</c> array
/// (<c>HCM10.PayrollEngine/Program.cs</c> lines 168-215) with <c>TenantId</c> added, because ABP background
/// jobs start outside any tenant scope and every payroll entity is <c>IMustHaveTenant</c>.
/// </summary>
[Serializable]
public class PayrollEngineJobArgs
{
    /// <summary>Tenant the run belongs to. No legacy equivalent -- legacy was single-tenant.</summary>
    public int TenantId { get; set; }

    /// <summary>
    /// Who started the run (legacy <c>args[8]</c>, the user name). The job runs as this user, so the units of
    /// work it opens per batch and the progress row get the tenant from the session. Null for runs queued before
    /// this was added; they run with the tenant only.
    /// </summary>
    public long? UserId { get; set; }

    /// <summary>Legacy <c>args[0]</c>.</summary>
    public PayrollRunType PayrollRunType { get; set; }

    /// <summary>Legacy <c>args[1]</c>: comma-separated employee codes; empty = the whole payroll area.</summary>
    public string EmployeeCodes { get; set; }

    /// <summary>Legacy <c>args[2]</c>: required. For a simulation of another period, the pay date the server worked out.</summary>
    public DateTime PayInDate { get; set; }

    /// <summary>Legacy <c>args[4]</c>: matched against <c>PayrollArea.Name</c>.</summary>
    public string PayrollAreaName { get; set; }

    /// <summary>Legacy <c>args[5]</c>.</summary>
    public DateTime OffCyclePaymentDate { get; set; }

    /// <summary>Legacy <c>args[6]</c>.</summary>
    public string OffCycleReason { get; set; }

    /// <summary>
    /// Legacy <c>args[3]</c> ("0" = live, "1" = simulation), carried as the typed
    /// <see cref="PayrollRegisterTarget"/>. <see cref="PayrollRegisterTarget.Simulation"/> when the screen's
    /// Simulation box was ticked.
    /// </summary>
    public PayrollRegisterTarget RegisterTarget { get; set; } = PayrollRegisterTarget.Live;
}
