using System.Collections.Generic;
using HCMv3.GridReports.Dto;

namespace HCMv3.GridReports.PayrollReport.Dto;

/// <summary>
/// Filter input for the Salary Deduction grid report (legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/PayrollReport/uscRptSalaryDeduction.ascx</c>). Legacy panel:
/// Month / Year / Payroll Area only -- no date range, employee code or status.
/// </summary>
public class GetSalaryDeductionReportInput : GridReportInputBase
{
    public int? Month { get; set; }

    public int? Year { get; set; }

    public int? PayrollAreaId { get; set; }

    protected override string DefaultSorting => "EmployeeCode ASC";
}

/// <summary>
/// One grid row of legacy <c>Table1()</c> (lines 530-822): a per-employee earning/deduction pivot for the
/// reported month, PLUS a 10%-of-salary max-deduction check.
///
/// DATA-DRIVEN COLUMNS: legacy adds one <c>DataColumn</c> PER DISTINCT WAGE TYPE actually posted that
/// month (earnings first, then deductions, both ordered by <c>PayrollOrder</c>) -- the column set differs
/// tenant to tenant and period to period. Reproduced the same way: <see cref="WageTypeAmounts"/> carries
/// the pivot values keyed by wage type name, and <see cref="SalaryDeductionReportOutput"/> ships the
/// ordered column list the Angular grid renders and the export follows -- see
/// <see cref="HCMv3.GridReports.Exporting.GridReportColumn{TRow}.FromLiteralHeader"/>.
/// </summary>
public class SalaryDeductionReportRowDto
{
    public int Id { get; set; }

    public string EmployeeCode { get; set; }

    public string EmployeeName { get; set; }

    public string PayrollMonth { get; set; }

    /// <summary>Sum of wage type 1000 from <c>EmployeeBasicPayDetail</c> as of the reported month end.</summary>
    public double ActualBasicSalary { get; set; }

    /// <summary>Sum of every OTHER Earning wage type from the same basic-pay snapshot.</summary>
    public double ActualAllowances { get; set; }

    public double ActualTotalSalary { get; set; }

    /// <summary>10% of <see cref="ActualTotalSalary"/> -- the legal max-deduction cap.</summary>
    public double MaxAllowedDeduction { get; set; }

    /// <summary>Number of wage type 1000 in the salary register (days paid).</summary>
    public double WorkingDays { get; set; }

    /// <summary>Pivot values keyed by wage type display name -- see class remarks.</summary>
    public Dictionary<string, double> WageTypeAmounts { get; set; } = new();

    public double TotalEarning { get; set; }

    public double TotalDeduction { get; set; }

    /// <summary><see cref="MaxAllowedDeduction"/> minus <see cref="TotalDeduction"/>; negative is a breach.</summary>
    public double Difference { get; set; }

    /// <summary>Legacy free-text summary of every non-suppressed deduction line, comma-separated.</summary>
    public string Narration { get; set; }
}

/// <summary>
/// Wrapper carrying the paged rows plus the two ordered, data-driven column lists (earning wage type
/// names, then deduction wage type names) that both the Angular grid and the Excel export use to read
/// <see cref="SalaryDeductionReportRowDto.WageTypeAmounts"/>.
/// </summary>
public class SalaryDeductionReportOutput
{
    public int TotalCount { get; set; }

    public List<SalaryDeductionReportRowDto> Items { get; set; } = new();

    public List<string> EarningColumns { get; set; } = new();

    public List<string> DeductionColumns { get; set; } = new();
}
