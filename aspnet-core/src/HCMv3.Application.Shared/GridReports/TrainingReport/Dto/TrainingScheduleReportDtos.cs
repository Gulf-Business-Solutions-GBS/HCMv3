using System;
using Abp.Application.Services.Dto;
using HCMv3.GridReports.Dto;

namespace HCMv3.GridReports.TrainingReport.Dto;

/// <summary>
/// Filter input for the Training Schedule analysis grid report (legacy <c>uscRptTrainingSchedule.ascx</c>).
/// Uses the common FromDate/ToDate window (overlap against the schedule's own StartDate/EndDate) plus the
/// report-specific <c>cmbTraining</c> combo. No employee-code/employee-status filters exist in legacy --
/// hidden on the Angular filter panel.
/// </summary>
public class GetTrainingScheduleReportInput : GridReportInputBase
{
    /// <summary>Legacy <c>cmbTraining</c>: matches the parent TrainingCourse CODE.</summary>
    public string TrainingCourseCode { get; set; }

    protected override string DefaultSorting => "Code ASC";
}

/// <summary>
/// One grid row. Mirrors the legacy <c>DataTable</c> built in <c>uscRptTrainingSchedule.ascx.cs:528-621</c>.
/// LEGACY COLUMN SET: OID, Code, NameEn, NameAr, TrainingCourse, StartDate, EndDate.
/// </summary>
public class TrainingScheduleReportRowDto : EntityDto<int>
{
    public string Code { get; set; }

    public string NameEn { get; set; }

    public string NameAr { get; set; }

    public string TrainingCourseName { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }
}
