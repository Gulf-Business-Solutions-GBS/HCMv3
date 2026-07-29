using System;
using System.ComponentModel.DataAnnotations;
using HCMv3.PayrollManagement;

namespace HCMv3.ProjectManagement.Dto;

/// <summary>
/// Input for the Project Cost Engine screen (ported from legacy
/// <c>HCM10.Web.UserControls.ProjectManagement.uscProjectCosting</c>'s form fields: Payroll Area,
/// Employee Code, End Date, Run Type, Off Payment Date, Off Reason Type).
/// </summary>
public class RunProjectCostEngineInput
{
    /// <summary>Legacy <c>cmbPayrollRunType</c>.</summary>
    public PayrollRunType PayrollRunType { get; set; }

    /// <summary>Legacy <c>txtEmpCode</c> -- comma separated employee codes. Blank means "whole payroll area".</summary>
    [StringLength(2000)]
    public string EmployeeCodes { get; set; }

    /// <summary>Legacy <c>txtEndDate</c> -- the period the run is for.</summary>
    [Required]
    public DateTime PayInDate { get; set; }

    /// <summary>Legacy <c>cmbPayrollArea</c>.</summary>
    [Required]
    [StringLength(256)]
    public string PayrollAreaName { get; set; }

    /// <summary>Legacy <c>txtOffPaymentDate</c>.</summary>
    public DateTime? OffCyclePaymentDate { get; set; }

    /// <summary>
    /// Legacy <c>cmbReasonType</c>. Legacy normalised it to either "End Of Service" or "" (line 152); only
    /// "End Of Service" changes behaviour.
    /// </summary>
    [StringLength(128)]
    public string OffCycleReason { get; set; }
}

/// <summary>Result of queueing a Project Cost Engine run.</summary>
public class RunProjectCostEngineOutput
{
    public bool Queued { get; set; }
}

/// <summary>
/// Live progress figures for the engine screen, ported from legacy
/// <c>uscProjectCosting.CalculateProcessStatus</c> (lines 1446-1508).
/// </summary>
public class ProjectCostEngineStatusDto
{
    /// <summary>Legacy <c>txtSummaryAll</c>.</summary>
    public int TotalRecords { get; set; }

    /// <summary>Legacy <c>txtSummarySuccess</c>.</summary>
    public int SuccessRecords { get; set; }

    /// <summary>Legacy <c>txtSummaryFailed</c>.</summary>
    public int FailedRecords { get; set; }

    /// <summary>Legacy <c>lblEmpProcessed</c>, right-hand side -- the run's expected employee count.</summary>
    public int NoOfEmployee { get; set; }

    /// <summary>Legacy <c>lblEmpProcessed</c>, left-hand side.</summary>
    public int TotalEmployeesProcessed { get; set; }

    /// <summary>Legacy <c>lblEmpSpeed</c> -- employees per minute.</summary>
    public double SpeedPerMinute { get; set; }

    /// <summary>Legacy <c>lblEstimateTime</c> -- estimated minutes remaining.</summary>
    public double RemainingMinutes { get; set; }

    /// <summary>Whether a stop has been requested (legacy <c>ProjectLog.StopFlag</c>).</summary>
    public bool StopRequested { get; set; }

    /// <summary>Legacy <c>lblCurrntEmp</c> -- "Code - Name" of the most recently processed employee.</summary>
    public string CurrentEmployee { get; set; }
}
