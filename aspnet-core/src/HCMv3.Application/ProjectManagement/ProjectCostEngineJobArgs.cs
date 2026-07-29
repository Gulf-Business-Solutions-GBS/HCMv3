using System;
using HCMv3.PayrollManagement;

namespace HCMv3.ProjectManagement;

/// <summary>Serialisable arguments for <see cref="ProjectCostEngineBackgroundJob"/>.</summary>
[Serializable]
public class ProjectCostEngineJobArgs
{
    public int TenantId { get; set; }

    public PayrollRunType PayrollRunType { get; set; }

    public string EmployeeCodes { get; set; }

    public DateTime PayInDate { get; set; }

    public string PayrollAreaName { get; set; }

    public DateTime OffCyclePaymentDate { get; set; }

    public string OffCycleReason { get; set; }
}
