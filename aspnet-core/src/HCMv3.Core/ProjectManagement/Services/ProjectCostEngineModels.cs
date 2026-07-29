using System;
using System.Collections.Generic;
using HCMv3.PayrollManagement;

namespace HCMv3.ProjectManagement.Services;

/// <summary>
/// Input to <see cref="IProjectCostEngineService.RunAsync"/>. Replaces the legacy screen's
/// <c>string[] args</c> array (<c>uscProjectCosting.ExecuteLongOperation</c>, legacy lines 124-166) with
/// typed values -- the <c>dd/MM/yyyy</c> string parsing legacy did in <c>Main</c> (lines 176-216) is gone.
/// </summary>
public class ProjectCostEngineRunInput
{
    /// <summary>Legacy args[0] ("R"/"O").</summary>
    public PayrollRunType PayrollRunType { get; set; }

    /// <summary>Legacy args[1]. Comma separated employee codes; blank means "use PayrollAreaName".</summary>
    public string EmployeeCodes { get; set; }

    /// <summary>Legacy args[2] -- the period end date the run is FOR.</summary>
    public DateTime PayInDate { get; set; }

    /// <summary>Legacy args[4].</summary>
    public string PayrollAreaName { get; set; }

    /// <summary>Legacy args[5].</summary>
    public DateTime OffCyclePaymentDate { get; set; }

    /// <summary>Legacy args[6] -- only "End Of Service" is ever meaningful (legacy line 152).</summary>
    public string OffCycleReason { get; set; }
}

/// <summary>Per-employee outcome, mirroring the <c>ProjectLog</c> row legacy writes for each employee.</summary>
public class ProjectCostEmployeeRunResult
{
    public int EmployeeId { get; set; }

    public string EmployeeCode { get; set; }

    public bool Succeeded { get; set; }

    public string Message { get; set; }
}

/// <summary>Aggregate result of one Project Cost Engine run.</summary>
public class ProjectCostEngineRunResult
{
    public DateTime PayInDate { get; set; }

    public bool Aborted { get; set; }

    public List<ProjectCostEmployeeRunResult> EmployeeResults { get; set; } = new List<ProjectCostEmployeeRunResult>();
}

/// <summary>
/// Settings snapshot read ONCE per run, replacing legacy's three static fields set by
/// <c>uscProjectCosting.GetInitialVariables</c> (legacy lines 1308-1320).
/// </summary>
public class ProjectCostEngineSettings
{
    /// <summary>Legacy <c>ManageTimeSettings.EnableTimeRegisterApproval</c> -- selects
    /// <c>ActualWorkingDay</c> vs <c>ApprovedActualWorkingDay</c> / <c>AbsentDay</c> vs
    /// <c>ApprovedAbsentDay</c> on every TimeRegister read.</summary>
    public bool EnableTimeRegisterApproval { get; set; }

    /// <summary>Legacy <c>ManageProjectCostingSettings.CalculateNetPayProjectWise</c>.</summary>
    public bool CalculateNetPayProjectWise { get; set; }

    /// <summary>
    /// Legacy <c>ManageSecuritySettings.EnableAuditLogInProjectCostEngine</c>. Carried for parity but
    /// UNUSED: the XAF <c>AuditTrailService</c> plumbing it gated (legacy lines 371-381, 1291-1304) is not
    /// ported -- ABP's <c>FullAuditedEntity</c> auditing is always on. Same decision as
    /// <c>PayrollEngineService</c>.
    /// </summary>
    public bool EnableAuditLogInProjectCostEngine { get; set; }

    /// <summary>Legacy <c>ManageTimeSettings.EarliestEvaluationDate</c> (legacy line 355). Read by legacy
    /// into <c>minStartDateLimit</c> and then never used in the live code path (the only consumer sits in
    /// the fully commented-out retro block, legacy lines 442-448). Carried, unused -- same as legacy.</summary>
    public DateTime EarliestEvaluationDate { get; set; }
}

/// <summary>
/// A TimeRegister row projected down to just the fields the cost engine reads, with the
/// <c>EnableTimeRegisterApproval</c> switch already applied. Mirrors legacy's anonymous-type projection
/// (legacy lines 713-724).
/// </summary>
public class ProjectCostTimeRegisterRow
{
    public double ActualWorkingDay { get; set; }

    public double PublicHoliday { get; set; }

    public double RestDay { get; set; }

    public double AbsentDay { get; set; }

    public double UnpaidLeaveDay { get; set; }

    public int LeaveTypeId { get; set; }

    public double LeaveDay { get; set; }

    public double LeaveAdjDay { get; set; }

    public double AnnualLeaveAccrualPerDay { get; set; }
}
