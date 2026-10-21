using System;
using HCMv3.PayrollManagement;

namespace HCMv3.ProjectManagement;

/// <summary>Serialisable arguments for <see cref="ProjectCostEngineBackgroundJob"/>.</summary>
[Serializable]
public class ProjectCostEngineJobArgs
{
    public int TenantId { get; set; }

    /// <summary>Who pressed Run. The job runs as them (their tenant and user), as the Payroll Engine job does.</summary>
    public long? UserId { get; set; }

    /// <summary>Shown on the run's progress row ("started by").</summary>
    public string CurrentUserName { get; set; }

    public PayrollRunType PayrollRunType { get; set; }

    public string EmployeeCodes { get; set; }

    public DateTime PayInDate { get; set; }

    public string PayrollAreaName { get; set; }

    public DateTime OffCyclePaymentDate { get; set; }

    public string OffCycleReason { get; set; }
}
