using Abp.Application.Services.Dto;
using HCMv3.GridReports.Dto;

namespace HCMv3.GridReports.ProjectReport.Dto;

/// <summary>
/// Filter input for the Project Wise Accrual analysis grid report (legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/ProjectReport/uscRptProjectWiseAccrual.ascx</c>).
///
/// Legacy filters by MONTH/YEAR (resolved to a <c>PayrollCalendar</c> period end date), not a date range,
/// so <see cref="GridReportInputBase.FromDate"/>/<see cref="GridReportInputBase.ToDate"/> are unused here
/// (kept on the base for filter-panel consistency, but ignored by the query -- same convention as
/// <c>GetEmployeeSalaryChangeReportInput</c> ignoring fields the legacy screen never showed).
/// </summary>
public class GetProjectWiseAccrualReportInput : GridReportInputBase
{
    /// <summary>Legacy <c>txtMonth</c> (1-12).</summary>
    public int Month { get; set; }

    /// <summary>Legacy <c>txtYear</c>.</summary>
    public int Year { get; set; }

    /// <summary>Legacy <c>cmbPayrollArea</c>, matched by Code.</summary>
    public string PayrollAreaCode { get; set; }

    protected override string DefaultSorting => "ProjectName ASC, EmployeeCode ASC";
}

/// <summary>
/// One (Project, Employee) row of the accrual grid. Ports legacy <c>Table2()</c>
/// (uscRptProjectWiseAccrual.ascx.cs:591-745) -- the employee-level drill target of the report; legacy's
/// project-only rollup (<c>Table1()</c>) is the same shape with the employee columns blank, so this port
/// exposes only the finer grain (drill-down navigation is UI plumbing, not ported -- see the app service
/// remarks).
/// </summary>
public class ProjectWiseAccrualReportRowDto : EntityDto<int>
{
    public string EmployeeCode { get; set; }

    public string EmployeeName { get; set; }

    public string ProjectCode { get; set; }

    public string ProjectName { get; set; }

    /// <summary>Legacy "BasicPay": sum of WageType code 1000.</summary>
    public double BasicPay { get; set; }

    /// <summary>Legacy "Allowance": sum of every wage type except 1000 flagged IncludeInBasicPay.</summary>
    public double Allowance { get; set; }

    /// <summary>Legacy "AnnualLeavePay": sum of WageType code 1003.</summary>
    public double AnnualLeavePay { get; set; }

    /// <summary>Legacy "SickLeavePay": sum of WageType code 1013.</summary>
    public double SickLeavePay { get; set; }

    /// <summary>Legacy "NormalOTPay": sum of WageType code 1007.</summary>
    public double NormalOTPay { get; set; }

    /// <summary>Legacy "WeekendOTPay": sum of WageType code 1008.</summary>
    public double WeekendOTPay { get; set; }

    /// <summary>Legacy "PublicHolidayOTPay": sum of WageType code 1009.</summary>
    public double PublicHolidayOTPay { get; set; }

    /// <summary>Legacy "SpecialOTPay": sum of WageType code 1042.</summary>
    public double SpecialOTPay { get; set; }

    /// <summary>Legacy "NetPay": the sum of the eight pay columns above.</summary>
    public double NetPay { get; set; }

    /// <summary>Legacy "ALCurrentMonthAccrual": sum of WageType code 4010 (Number, not Amount).</summary>
    public double ALCurrentMonthAccrual { get; set; }

    /// <summary>Legacy "AnnualLeaveProvision": sum of WageType code 3001.</summary>
    public double AnnualLeaveProvision { get; set; }
}
