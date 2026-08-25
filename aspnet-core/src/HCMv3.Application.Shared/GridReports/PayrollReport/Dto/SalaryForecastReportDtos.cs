using System.Collections.Generic;
using HCMv3.GridReports.Dto;

namespace HCMv3.GridReports.PayrollReport.Dto;

public enum SalaryForecastType
{
    All = 0,
    BasicPay = 1
}

/// <summary>
/// Filter input for the Salary Forecast grid report (legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/PayrollReport/uscRptSalaryForecast.ascx</c>).
///
/// LEGACY QUIRK REPRODUCED: <c>dtFrom</c>/<c>dtTo</c> are logged in <c>GetParameterStr</c> and stamp the
/// (unreachable, drill-down-only) <c>Table2()</c>'s <c>EffectiveDate</c> column, but the LIVE
/// <c>Table1()</c> (the only table this port renders -- see remarks below) filters
/// <c>EmployeeBasicPayDetail</c> by <c>DateTime.Now</c>, not the picked date. The date range is exposed
/// here only so the logged parameter string matches legacy.
/// </summary>
public class GetSalaryForecastReportInput : GridReportInputBase
{
    /// <summary>Legacy <c>cmbForecastType</c>: ALL wage types or Basic Pay (code 1000) only.</summary>
    public SalaryForecastType? ForecastType { get; set; }

    /// <summary>Legacy <c>txtEmpCode</c>, repurposed on THIS screen as "Salary Change (%)" per year.</summary>
    public double? SalaryChangePercent { get; set; }

    protected override string DefaultSorting => "CompanyName ASC, DepartmentName ASC";
}

/// <summary>
/// One grid row of legacy <c>Table1()</c> (lines 479-603): a Company/Department roll-up of CURRENT basic
/// pay, compounded forward 5 years at <see cref="GetSalaryForecastReportInput.SalaryChangePercent"/> per
/// year.
///
/// NOT PORTED -- <c>Table2()</c> (lines 604-762), the employee-level drill-down reached only by
/// double-clicking a Table1 row: the drill-down/drill-up session stack that reaches it is out of scope for
/// this entire report family (see <c>EmployeeContractReportAppService</c> remarks), so its data is
/// unreachable in this port.
/// </summary>
public class SalaryForecastReportRowDto
{
    public string CompanyName { get; set; }

    public string DepartmentName { get; set; }

    public double CurrentAmount { get; set; }

    /// <summary>5 compounding forecast amounts, current-year + 1 .. current-year + 5.</summary>
    public double[] ForecastAmounts { get; set; } = new double[5];
}

/// <summary>Wrapper carrying the rows plus the 5 forecast year numbers the columns are labelled with.</summary>
public class SalaryForecastReportOutput
{
    public int TotalCount { get; set; }

    public List<SalaryForecastReportRowDto> Items { get; set; } = new();

    public List<int> ForecastYears { get; set; } = new();
}
