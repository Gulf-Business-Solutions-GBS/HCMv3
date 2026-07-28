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

    /// <summary>Legacy <c>args[0]</c>.</summary>
    public PayrollRunType PayrollRunType { get; set; }

    /// <summary>Legacy <c>args[1]</c>: comma-separated employee codes; empty = the whole payroll area.</summary>
    public string EmployeeCodes { get; set; }

    /// <summary>Legacy <c>args[2]</c>: required.</summary>
    public DateTime PayInDate { get; set; }

    /// <summary>Legacy <c>args[4]</c>: matched against <c>PayrollArea.Name</c>.</summary>
    public string PayrollAreaName { get; set; }

    /// <summary>Legacy <c>args[5]</c>.</summary>
    public DateTime OffCyclePaymentDate { get; set; }

    /// <summary>Legacy <c>args[6]</c>.</summary>
    public string OffCycleReason { get; set; }

    /// <summary>
    /// Legacy <c>args[3]</c> ("0" = live, "1" = simulation). Carried as the typed
    /// <see cref="PayrollRegisterTarget"/> so wave P7 is a parameter flip. Runs arriving through
    /// <c>IPayrollEngineAppService</c> are always <see cref="PayrollRegisterTarget.Live"/> today -- the
    /// simulation screen is P7's to add.
    /// </summary>
    public PayrollRegisterTarget RegisterTarget { get; set; } = PayrollRegisterTarget.Live;
}
