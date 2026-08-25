using System;
using Abp.Application.Services.Dto;
using HCMv3.GridReports.Dto;

namespace HCMv3.GridReports.SurveyReport.Dto;

/// <summary>
/// Filter input for the Survey Status analysis grid report (legacy <c>uscRptSurveyStatus.ascx</c>).
/// Common FromDate/ToDate window (overlap against the Survey's own StartDate/EndDate) plus the
/// report-specific <c>cmbSurvey</c> combo (matched on Code). No employee-code/employee-status filters
/// exist in legacy -- hidden on the Angular filter panel. This report has no RLS in legacy: its rows are
/// Surveys, not employees.
/// </summary>
public class GetSurveyStatusReportInput : GridReportInputBase
{
    public string SurveyCode { get; set; }

    protected override string DefaultSorting => "Code ASC";
}

/// <summary>
/// One grid row. Mirrors the legacy <c>DataTable</c> built in <c>uscRptSurveyStatus.ascx.cs</c>
/// (<c>Table1()</c>). LEGACY COLUMN SET: OID, Code, Name, StartDate, EndDate, Complete.
///
/// <see cref="CompletePercent"/> is legacy's integer-truncated percentage of ACTIVE employees who have an
/// <c>EmployeeSurvey</c> row for this survey: <c>EmployeeSurveyCount * 100 / ActiveEmployeeCount</c> using
/// INTEGER division (legacy line: <c>empSurveyList.Count() * 100 / empCount</c>), reproduced as-is
/// including truncation-toward-zero and the "0 when no active employees" guard.
/// </summary>
public class SurveyStatusReportRowDto : EntityDto<int>
{
    public string Code { get; set; }

    public string Name { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public int CompletePercent { get; set; }
}
