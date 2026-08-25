using System;
using System.ComponentModel.DataAnnotations;
using HCMv3.GridReports.Dto;

namespace HCMv3.GridReports.QuotaProvision.Dto;

/// <summary>
/// Input for the End Of Service report -- port of legacy <c>uscRptEndOfService.ascx(.cs)</c>, <c>Table1()</c>.
/// Legacy filters by a Payroll Calendar month/year (not a free date range), so this report does NOT use
/// <see cref="GridReportInputBase.FromDate"/>/<see cref="GridReportInputBase.ToDate"/> -- <see cref="Month"/>
/// and <see cref="Year"/> are the legacy <c>txtMonth</c>/<c>txtYear</c> instead, defaulting (legacy
/// lines 111-122) to LAST month when blank.
/// </summary>
public class GetEndOfServiceReportInput : GridReportInputBase
{
    [Range(1, 12)]
    public int? Month { get; set; }

    [Range(1900, 3000)]
    public int? Year { get; set; }

    /// <summary>Legacy <c>cmbPayrollArea</c> selected Code. Null/blank means "all payroll areas".</summary>
    public string PayrollAreaCode { get; set; }

    protected override string DefaultSorting => "EmployeeCode asc";
}

public class EndOfServiceReportRowDto
{
    public int Id { get; set; }

    public string EmployeeCode { get; set; }

    public string EmployeeName { get; set; }

    public DateTime JoiningDate { get; set; }

    public bool Separation { get; set; }

    public bool FnfPayment { get; set; }

    public double BasicSalary { get; set; }

    public double AverageCommission { get; set; }

    public double AverageTicket { get; set; }

    public double AverageBonus { get; set; }

    public double MonthlyIndemnityAmount { get; set; }

    public double DailyIndemnityAmount { get; set; }

    public double GLProvisionAmount { get; set; }

    public double LoanAmount { get; set; }

    public double NetAvailableBalance { get; set; }
}
