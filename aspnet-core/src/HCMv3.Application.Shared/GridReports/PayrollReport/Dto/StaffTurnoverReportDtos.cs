using HCMv3.GridReports.Dto;

namespace HCMv3.GridReports.PayrollReport.Dto;

/// <summary>
/// Filter input for the Staff Turnover grid report (legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/PayrollReport/uscRptStaffTurnover.ascx</c>).
/// </summary>
public class GetStaffTurnoverReportInput : GridReportInputBase
{
    protected override string DefaultSorting => "AvgNoOfEmp ASC";
}

/// <summary>
/// One grid row of legacy <c>Table1()</c> (lines 486-543).
///
/// LEGACY DEFECT REPRODUCED, NOT A DATA-LOSS DEFECT: this screen never computes an actual turnover rate.
/// <c>Table1()</c> and the unreachable <c>Table2()</c> both just dump every employee who joined in the
/// requested window into three oddly-named columns -- <c>dr["Period"] = rec.Oid</c>,
/// <c>dr["AvgNoOfEmp"] = rec.Code</c>, <c>dr["Turnover"] = rec.Code</c> (lines 520-533) -- i.e. "Period" is
/// the employee's surrogate id and "AvgNoOfEmp"/"Turnover" both just repeat the employee CODE. There is no
/// average headcount and no turnover percentage anywhere in the control. Reproduced verbatim (renamed to
/// legacy-faithful column names) rather than "fixed", per the legacy-fidelity rule; pinned by
/// <c>Should_Reproduce_The_Legacy_Meaningless_Placeholder_Columns</c>.
/// </summary>
public class StaffTurnoverReportRowDto
{
    public int Id { get; set; }

    /// <summary>Legacy "Period" column -- literally the employee's surrogate id, not a period.</summary>
    public string Period { get; set; }

    /// <summary>Legacy "AvgNoOfEmp" column -- literally the employee code, not an average headcount.</summary>
    public string AvgNoOfEmp { get; set; }

    /// <summary>Legacy "Turnover" column -- literally the employee code again, not a turnover rate.</summary>
    public string Turnover { get; set; }
}
