using System;
using System.Collections.Generic;
using Abp.Application.Services.Dto;
using HCMv3.GridReports.Dto;

namespace HCMv3.GridReports.PerformanceReport.Dto;

/// <summary>
/// Legacy <c>cmbRunType</c> ("Summary"/"Detail") on
/// <c>PerformanceReport/uscRptPerformanceAppraisal_Custom035.ascx</c>.
/// </summary>
public enum PerformanceAppraisalManagerEvaluationRunType
{
    Summary = 1,
    Detail = 2
}

/// <summary>
/// Filter input for the Performance Appraisal Manager Evaluation analysis grid report (legacy customer
/// variant <c>PerformanceReport/uscRptPerformanceAppraisal_Custom035.ascx</c>). Filters by
/// <see cref="HCMv3.PerformanceManagement.PACycle"/> (legacy <c>cmbPACycle</c>), same as Custom012.
/// </summary>
public class GetPerformanceAppraisalManagerEvaluationReportInput : GridReportInputBase
{
    public string CycleName { get; set; }

    public PerformanceAppraisalManagerEvaluationRunType RunType { get; set; } =
        PerformanceAppraisalManagerEvaluationRunType.Summary;

    protected override string DefaultSorting => "EmployeeCode ASC";
}

/// <summary>One employee's marks for one KPI -- legacy Custom035 Detail mode's "KPI1"/"KPI1 - Mark" pivot pair.</summary>
public class PerformanceAppraisalManagerEvaluationKpiDto
{
    public string KpiName { get; set; }

    public double Marks { get; set; }
}

/// <summary>
/// One grid row: the CURRENT MANAGER's evaluation of the employee for the selected PACycle. Mirrors
/// legacy Custom035 <c>Table1()</c>'s "Summary" columns, plus (Detail mode) the per-KPI marks pivot in
/// <see cref="Kpis"/> -- exposed as a nested list rather than legacy's dynamically-numbered "KPI1"/"KPI2"..
/// DataTable columns, which Angular renders as one column per list entry and the Excel export turns back
/// into <c>FromLiteralHeader</c> columns (see <see cref="HCMv3.GridReports.Exporting.GridReportColumn{TRow}"/>).
/// </summary>
public class PerformanceAppraisalManagerEvaluationReportRowDto : EntityDto<int>
{
    public string EmployeeCode { get; set; }

    public string EmployeeName { get; set; }

    public DateTime JoiningDate { get; set; }

    public string JobTitle { get; set; }

    public long? OrganizationUnitId { get; set; }

    public string OrgLevel1 { get; set; }
    public string OrgLevel2 { get; set; }
    public string OrgLevel3 { get; set; }
    public string OrgLevel4 { get; set; }
    public string OrgLevel5 { get; set; }
    public string OrgLevel6 { get; set; }
    public string OrgLevel7 { get; set; }

    public string WorkLocation { get; set; }

    public string Manager { get; set; }

    /// <summary>Legacy "Average Mark" -- current manager's average review score. Manager-evaluation only.</summary>
    public double AverageMark { get; set; }

    /// <summary>Legacy "Overall Evaluation" -- the <see cref="HCMv3.PerformanceManagement.PARating"/> whose range contains <see cref="AverageMark"/>.</summary>
    public string OverallEvaluation { get; set; }

    /// <summary>Populated only in <see cref="PerformanceAppraisalManagerEvaluationRunType.Detail"/> mode.</summary>
    public List<PerformanceAppraisalManagerEvaluationKpiDto> Kpis { get; set; } = new();
}
