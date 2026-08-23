using System;
using Abp.Application.Services.Dto;
using HCMv3.GridReports.Dto;

namespace HCMv3.GridReports.Employees.Dto;

/// <summary>
/// Filter input for the Employee History analysis grid report (legacy
/// <c>uscRptEmployeeHistory.ascx</c>).
///
/// Legacy HID both date pickers (uscRptEmployeeHistory.ascx:19-22, <c>Visible="false"</c>) and
/// <c>GetParameterStr</c> (lines 432-440) logs only the employee code, so this report has NO date range.
/// The inherited <c>FromDate</c>/<c>ToDate</c> are unused and the Angular screen hides them.
/// </summary>
public class GetEmployeeHistoryReportInput : GridReportInputBase
{
    /// <summary>
    /// Legacy iterated employees ordered by Code and, within an employee, the change dates ascending
    /// (lines 604/614) -- reproduced here as the default sort so server-side paging matches.
    /// </summary>
    protected override string DefaultSorting => "EmployeeCode ASC, StartDate ASC";
}

/// <summary>
/// One grid row of legacy <c>Table1()</c> (uscRptEmployeeHistory.ascx.cs:444-670).
///
/// ATYPICAL ROW GRAIN: this is not one row per employee. Legacy produced one row per employee PER CHANGE
/// DATE -- the distinct union of the employee's basic-pay start dates and ALL of the employee's org
/// assignment start dates (lines 606-611) -- and then showed the org assignment and basic salary that were
/// in force ON that date.
///
/// Legacy column set: OID, Code, Employee, FullNameAr, StartDate, JobTitle, JobTitleAr, PayrollArea,
/// EmployeeGroup, EmployeeSubGroup, BasicSalary, WorkLocation, CostCenter, WorkflowGroup,
/// OrgLevel1..OrgLevel7.
/// </summary>
public class EmployeeHistoryReportRowDto : EntityDto<int>, IEmployeeOrgLevelRow
{
    /// <summary>Legacy "Code" column.</summary>
    public string EmployeeCode { get; set; }

    /// <summary>Legacy "Employee" column -- always <c>FullNameEn</c> (line 621); the Arabic name has its own column.</summary>
    public string EmployeeName { get; set; }

    /// <summary>Legacy "FullNameAr" column (line 622).</summary>
    public string EmployeeNameAr { get; set; }

    /// <summary>Legacy "StartDate" column: the change date this row describes.</summary>
    public DateTime StartDate { get; set; }

    /// <summary>Legacy "JobTitle" column -- always the English name (line 632).</summary>
    public string JobTitle { get; set; }

    /// <summary>Legacy "JobTitleAr" column (line 633).</summary>
    public string JobTitleAr { get; set; }

    /// <summary>Legacy "PayrollArea" column.</summary>
    public string PayrollAreaName { get; set; }

    /// <summary>Legacy "EmployeeGroup" column.</summary>
    public string EmployeeGroupName { get; set; }

    /// <summary>Legacy "EmployeeSubGroup" column.</summary>
    public string EmployeeSubGroupName { get; set; }

    /// <summary>
    /// Legacy "BasicSalary" column: the amount of the wage-type-1000 basic pay detail in force on
    /// <see cref="StartDate"/>, 0 when there is none (legacy's <c>FirstOrDefault()</c> on a double).
    /// </summary>
    public double BasicSalary { get; set; }

    /// <summary>Legacy "WorkLocation" column.</summary>
    public string WorkLocationName { get; set; }

    /// <summary>Legacy "CostCenter" column.</summary>
    public string CostCenterName { get; set; }

    public string OrgLevel1 { get; set; }
    public string OrgLevel2 { get; set; }
    public string OrgLevel3 { get; set; }
    public string OrgLevel4 { get; set; }
    public string OrgLevel5 { get; set; }
    public string OrgLevel6 { get; set; }
    public string OrgLevel7 { get; set; }

    public long? OrganizationUnitId { get; set; }
}
