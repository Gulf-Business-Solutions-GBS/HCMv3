using System;
using Abp.Application.Services.Dto;
using HCMv3.GridReports.Dto;
using HCMv3.GridReports.Employees.Dto;

namespace HCMv3.GridReports.Transaction.Dto;

/// <summary>
/// Filter input for the Employee Non Payroll Transaction analysis grid report (legacy
/// <c>uscRptEmployeeNonPayroll.ascx</c>). Adds the report-specific <c>cmbWageType</c> combo. Legacy has
/// no employee-status filter on this screen.
/// </summary>
public class GetEmployeeNonPayrollReportInput : GridReportInputBase
{
    /// <summary>Legacy <c>cmbWageType</c>: WageType.Code. Null/empty means "all wage types".</summary>
    public string WageTypeCode { get; set; }

    protected override string DefaultSorting => "EmployeeCode ASC";
}

/// <summary>One grid row. Mirrors legacy <c>Table1()</c> (uscRptEmployeeNonPayroll.ascx.cs:502-660).</summary>
public class EmployeeNonPayrollReportRowDto : EntityDto<int>, IEmployeeOrgLevelRow
{
    public string EmployeeCode { get; set; }

    public string EmployeeName { get; set; }

    public string JobTitleName { get; set; }

    public string OrgLevel1 { get; set; }
    public string OrgLevel2 { get; set; }
    public string OrgLevel3 { get; set; }
    public string OrgLevel4 { get; set; }
    public string OrgLevel5 { get; set; }
    public string OrgLevel6 { get; set; }
    public string OrgLevel7 { get; set; }
    public long? OrganizationUnitId { get; set; }

    public string WageTypeName { get; set; }

    public DateTime PaymentDate { get; set; }

    public double Amount { get; set; }

    public string CurrencyShortForm { get; set; }

    public bool IsLocked { get; set; }

    public string PayableBy { get; set; }

    public string PaymentMethod { get; set; }
}
