using System;
using System.ComponentModel.DataAnnotations;
using HCMv3.PayrollManagement;

namespace HCMv3.ProjectManagement.Dto;

/// <summary>
/// Input for the Project Cost Engine screen (ported from legacy
/// <c>HCM10.Web.UserControls.ProjectManagement.uscProjectCosting</c>'s form fields: Payroll Area,
/// Employee Code, End Date, Run Type, Off Payment Date, Off Reason Type).
///
/// <para>Legacy's screen HID the run type and both off-cycle fields (<c>uscProjectCosting.ascx</c> lines 52-57,
/// <c>Visible="False"</c>), so it always ran Regular with no off-cycle values. The fields stay on the contract;
/// the app service refuses an off-cycle run and ignores the off-cycle values of a regular one, as legacy's hidden
/// controls did.</para>
/// </summary>
public class RunProjectCostEngineInput
{
    /// <summary>Legacy <c>cmbPayrollRunType</c> (hidden; always Regular). OffCycle is refused.</summary>
    public PayrollRunType PayrollRunType { get; set; }

    /// <summary>Legacy <c>txtEmpCode</c> -- comma separated employee codes. Blank means "whole payroll area".</summary>
    [StringLength(4000)]
    public string EmployeeCodes { get; set; }

    /// <summary>Legacy <c>txtEndDate</c> -- the period end the run is for (Current or Other period).</summary>
    [Required]
    public DateTime PayInDate { get; set; }

    /// <summary>Legacy <c>cmbPayrollArea</c>.</summary>
    [Required]
    [StringLength(256)]
    public string PayrollAreaName { get; set; }

    /// <summary>Legacy <c>txtOffPaymentDate</c> (hidden). Ignored for a regular run.</summary>
    public DateTime? OffCyclePaymentDate { get; set; }

    /// <summary>
    /// Legacy <c>cmbReasonType</c> (hidden). Legacy normalised it to either "End Of Service" or "" (line 140); only
    /// "End Of Service" changes behaviour. Ignored for a regular run.
    /// </summary>
    [StringLength(128)]
    public string OffCycleReason { get; set; }
}

/// <summary>Result of queueing a Project Cost Engine run.</summary>
public class RunProjectCostEngineOutput
{
    public bool Queued { get; set; }
}
