using HCMv3.GridReports.Dto;
using HCMv3.PayrollManagement;

namespace HCMv3.GridReports.PayrollReport.Dto;

/// <summary>
/// Filter input for the Pre-Payroll Report grid report (legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/PayrollReport/uscRptPrePayrollReport.ascx</c>).
///
/// LEGACY QUIRK REPRODUCED: <c>Table1()</c> (lines 560-748) never applies <c>Session[RunType]</c> or
/// <c>Session[payrollArea]</c> to the query -- both combos are logged in <c>GetParameterStr</c> and stamped
/// into Session by the callback handler, but the data query only reads Month/Year (via
/// <see cref="Month"/>/<see cref="Year"/>, which become the EndDate every date-scoped sub-lookup is as-of)
/// and Employee.Status == Active. They are kept here purely so the parameter string matches legacy; they
/// have NO filtering effect. See <c>Should_Not_Filter_By_PayrollRunType_Or_PayrollArea_Reproducing_Legacy_Behavior</c>.
/// </summary>
public class GetPrePayrollReportInput : GridReportInputBase
{
    /// <summary>Legacy <c>txtMonth</c>. Blank means the current month.</summary>
    public int? Month { get; set; }

    /// <summary>Legacy <c>txtYear</c>. Blank means the current year.</summary>
    public int? Year { get; set; }

    /// <summary>Legacy <c>cmbRunType</c> -- logged only, never filters. See class remarks.</summary>
    public PayrollRunType? PayrollRunType { get; set; }

    /// <summary>Legacy <c>cmbPayrollArea</c> -- logged only, never filters. See class remarks.</summary>
    public int? PayrollAreaId { get; set; }

    protected override string DefaultSorting => "EmployeeCode ASC";
}

/// <summary>
/// One grid row of legacy <c>Table1()</c> -- a per-employee readiness checklist for running payroll, all
/// columns booleans/dates rather than amounts.
/// </summary>
public class PrePayrollReportRowDto
{
    public int Id { get; set; }

    public string EmployeeCode { get; set; }

    public string EmployeeName { get; set; }

    /// <summary>Legacy "AccessId" -- true when <c>Employee.AccessID</c> is non-blank (line 666).</summary>
    public bool HasAccessId { get; set; }

    /// <summary>Legacy "Basic Pay" -- an <c>EmployeeBasicPayDetail</c> for wage type 1000 exists as of EndDate.</summary>
    public bool HasBasicPay { get; set; }

    /// <summary>Legacy "Contract" -- an <c>EmployeeContract</c> is current as of EndDate.</summary>
    public bool HasContract { get; set; }

    /// <summary>Legacy "Org Assignment" -- an <c>EmployeeOrgAssignment</c> is current as of EndDate.</summary>
    public bool HasOrgAssignment { get; set; }

    /// <summary>
    /// Legacy "Salary Profile" -- true when the current org assignment has an EmployeeSubGroup
    /// (legacy: <c>EmployeeSubGroup != null</c>, line 707 in the sibling SalaryDeduction screen's dead
    /// code and line 707 here).
    /// </summary>
    public bool HasSalaryProfile { get; set; }

    public bool HasTimePolicy { get; set; }

    public bool HasDutyPeriod { get; set; }

    public System.DateTime? EarliestMasterDataChange { get; set; }

    /// <summary>
    /// Legacy "BackDated Amount" -- true when <see cref="EarliestMasterDataChange"/> is set and falls
    /// before the first day of the reported month (line 718).
    /// </summary>
    public bool IsBackDatedAmount { get; set; }

    /// <summary>
    /// Legacy "Loan Recovery" -- true when the employee has a loan current as of EndDate with either a
    /// positive balance or a recovery already posted this month, and no exemption this month (lines
    /// 721-735).
    /// </summary>
    public bool HasLoanRecovery { get; set; }
}
