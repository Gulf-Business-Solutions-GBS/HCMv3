using System;
using System.ComponentModel.DataAnnotations;
using HCMv3.PayrollManagement;

namespace HCMv3.Administration.Tools.Dto;

/// <summary>
/// Input for the legacy uscPayrollConfigPanel "Generate Payroll Calendar" panel
/// (<c>ASPxCallbackPanel1_Callback</c>, line 98).
///
/// DEVIATION: legacy bound a combo of <c>PayrollArea.Name</c> strings and matched on the display text.
/// This port takes the PayrollArea <b>Id</b> instead — matching on a non-unique display name is a latent
/// data bug, and the Angular screen has the id available from the lookup.
/// </summary>
public class GeneratePayrollCalendarInput
{
    [Range(1, int.MaxValue)]
    public int PayrollAreaId { get; set; }

    /// <summary>Legacy <c>dtstartDate</c>. Normalised to the 1st of its month, exactly as legacy did.</summary>
    public DateTime StartDate { get; set; }

    /// <summary>Legacy <c>dtEndDate</c>. Loop is inclusive of this date's month.</summary>
    public DateTime EndDate { get; set; }
}

/// <summary>
/// Shared input for the legacy "Delete Salary Register" (<c>ASPxCallbackPanel7_Callback</c>, line 1253) and
/// "Unlock Salary Register" (<c>ASPxCallbackPanel8_Callback</c>, line 1397) panels — legacy duplicated the
/// same filter block for both, so one DTO serves both here.
/// </summary>
public class SalaryRegisterMaintenanceInput
{
    /// <summary>Legacy <c>txtMonth_SalRegDel</c> / <c>txtMonth_UnlockSalReg</c>.</summary>
    [Range(1, 12)]
    public int Month { get; set; }

    /// <summary>Legacy <c>txtYear_SalRegDel</c> / <c>txtYear_UnlockSalReg</c>.</summary>
    [Range(1900, 2999)]
    public int Year { get; set; }

    /// <summary>
    /// Legacy <c>cmbPayrollArea_*</c>, matched by name. Ported as an Id. Null means "no payroll-area
    /// filter" — legacy required at least one of PayrollArea / EmployeeCodes to be supplied.
    /// </summary>
    public int? PayrollAreaId { get; set; }

    /// <summary>
    /// Legacy <c>txtEmpCode_*</c>: a comma-separated employee-code list, ported as a real list. Empty means
    /// "all active employees" (legacy default).
    /// </summary>
    public string[] EmployeeCodes { get; set; }

    public PayrollRunType PayrollRunType { get; set; }
}

/// <summary>
/// Input for the legacy uscPayrollConfigPanel "Fix Forfeit Quota" panel
/// (<c>ASPxCallbackPanel3_Callback</c>, line 1171).
/// </summary>
public class FixForfeitQuotaInput
{
    /// <summary>
    /// Legacy <c>dtForfeitQuotaStartDate</c>. Clamped up to
    /// <c>ManageTimeSettings.EarliestEvaluationDate</c> when unset or earlier, exactly as legacy did
    /// (lines 1198-1200).
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>Legacy <c>txtEmpListForfeitQuota</c>. Empty means "all active employees".</summary>
    public string[] EmployeeCodes { get; set; }
}
