using System;
using Abp.Application.Services.Dto;
using HCMv3.GridReports.Dto;

namespace HCMv3.GridReports.PerformanceReport.Dto;

/// <summary>
/// Filter input for the PA Employee analysis grid report (legacy
/// <c>PerformanceReport/uscRptPAEmployee.ascx</c>). Legacy's <c>dtFrom</c>/<c>dtTo</c> and
/// <c>cmbEmpStatus</c> exist on the filter panel but are never consulted inside <c>Table1()</c>
/// (uscRptPAEmployee.ascx.cs:635-654) -- only <c>FormTypeCode</c>, <c>PeriodCode</c> and the shared
/// employee-code box are. Those inherited fields are therefore kept on the DTO for shape parity but are
/// not applied as filters, and the Angular screen hides their inputs.
/// </summary>
public class GetPAEmployeeReportInput : GridReportInputBase
{
    /// <summary>Legacy <c>cmbFormType</c> (<see cref="HCMv3.PerformanceManagement.FormType"/> code).</summary>
    public string FormTypeCode { get; set; }

    /// <summary>Legacy <c>cmbPeriod</c> (<see cref="HCMv3.PerformanceManagement.PACycle"/> code).</summary>
    public string PeriodCode { get; set; }

    protected override string DefaultSorting => "EmployeeCode ASC";
}

/// <summary>
/// One grid row. Mirrors legacy <c>Table1()</c>'s column set (uscRptPAEmployee.ascx.cs:600-632). Legacy
/// LEFT-JOINs <c>PAEmployeeReviewer</c> onto each <c>PAEmployee</c>, so a PAEmployee with N reviewers
/// produces N rows here (fan-out), and one with zero reviewers still produces one row with the reviewer
/// columns blank -- reproduced faithfully rather than aggregated.
/// </summary>
public class PAEmployeeReportRowDto : EntityDto<int>
{
    public string EmployeeCode { get; set; }

    public string EmployeeName { get; set; }

    public string JobTitle { get; set; }

    /// <summary>Backing id for the seven OrgLevel columns; stamped by <c>EmployeeOrgLevelNameResolver</c>.</summary>
    public long? OrganizationUnitId { get; set; }

    public string OrgLevel1 { get; set; }
    public string OrgLevel2 { get; set; }
    public string OrgLevel3 { get; set; }
    public string OrgLevel4 { get; set; }
    public string OrgLevel5 { get; set; }
    public string OrgLevel6 { get; set; }
    public string OrgLevel7 { get; set; }

    public string Period { get; set; }

    public string FormType { get; set; }

    public string Manager { get; set; }

    public string ManagerJobTitle { get; set; }

    public string OverallAverageRating { get; set; }

    public double OverallAverageScore { get; set; }

    public string PeerAverageRating { get; set; }

    public double PeerAverageScore { get; set; }

    public string EmployeeAverageRating { get; set; }

    public double EmployeeAverageScore { get; set; }

    public string ProposedIncrement { get; set; }

    public string ProposedJobTitle { get; set; }

    public string AreaOfImprovement { get; set; }

    /// <summary>Legacy "Person" column -- the reviewer, blank when the PAEmployee has no reviewer row.</summary>
    public string Person { get; set; }

    public string ReviewerAverageRating { get; set; }

    public double ReviewerAverageScore { get; set; }
}
