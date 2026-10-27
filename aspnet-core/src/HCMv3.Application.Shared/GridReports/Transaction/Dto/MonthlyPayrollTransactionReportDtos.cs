using System;
using Abp.Application.Services.Dto;
using HCMv3.GridReports.Dto;

namespace HCMv3.GridReports.Transaction.Dto;

/// <summary>
/// Filter input for the Monthly Payroll Transaction analysis grid report (legacy
/// <c>uscRptMonthlyPayrollTransaction.ascx</c>).
///
/// DEVIATION FROM THE SHARED FROM/TO CONTRACT: legacy's own filter panel has no date-range picker at all --
/// it has <c>txtMonth</c>/<c>txtYear</c> numeric boxes instead (uscRptMonthlyPayrollTransaction.ascx.cs:44-51,
/// 381-397). <see cref="Month"/>/<see cref="Year"/> are the report-specific fields carrying that; the base
/// <c>FromDate</c>/<c>ToDate</c> are unused by this report and hidden client-side.
/// </summary>
public class GetMonthlyPayrollTransactionReportInput : GridReportInputBase
{
    /// <summary>Legacy <c>txtMonth</c>. Null defaults to the current month, exactly as legacy did.</summary>
    public int? Month { get; set; }

    /// <summary>Legacy <c>txtYear</c>. Null defaults to the current year, exactly as legacy did.</summary>
    public int? Year { get; set; }

    protected override string DefaultSorting => "EmployeeCode ASC";

    public (DateTime Start, DateTime End) ResolvePeriod(DateTime today)
    {
        var year = Year ?? today.Year;
        var month = Month ?? today.Month;
        var start = new DateTime(year, month, 1);
        return (start, start.AddMonths(1).AddDays(-1));
    }
}

/// <summary>
/// One grid row: a single transaction line drawn from ANY of four source tables (leave pay estimate,
/// one-off payment, overtime, loan). Mirrors legacy <c>Table1()</c>'s UNION
/// (uscRptMonthlyPayrollTransaction.ascx.cs:447-625).
/// </summary>
public class MonthlyPayrollTransactionReportRowDto : EntityDto<int>
{
    public string EmployeeCode { get; set; }

    public string EmployeeName { get; set; }

    public string WageTypeName { get; set; }

    public DateTime TransactionDate { get; set; }

    public double ExpectedAmount { get; set; }

    public string SysCreator { get; set; }

    public string SysModifier { get; set; }
}
