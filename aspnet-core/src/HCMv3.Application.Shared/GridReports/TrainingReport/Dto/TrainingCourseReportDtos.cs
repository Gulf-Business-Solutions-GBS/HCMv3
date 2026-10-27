using Abp.Application.Services.Dto;
using HCMv3.GridReports.Dto;

namespace HCMv3.GridReports.TrainingReport.Dto;

/// <summary>
/// Filter input for the Training Course analysis grid report (legacy <c>uscRptTrainingCourse.ascx</c>).
/// This screen has NO date/employee-code/employee-status filters -- the master's own <c>cmbTraining</c>
/// combo (matched on Code) is the only filter (uscRptTrainingCourse.ascx.cs:572-575). The common
/// FromDate/ToDate/EmployeeCode/EmployeeStatus fields from <see cref="GridReportInputBase"/> are kept for
/// shape consistency but are not applied and are hidden on the Angular filter panel.
/// </summary>
public class GetTrainingCourseReportInput : GridReportInputBase
{
    /// <summary>Legacy <c>cmbTraining</c>: matches the course CODE. Blank means "all courses".</summary>
    public string TrainingCourseCode { get; set; }

    protected override string DefaultSorting => "Code ASC";
}

/// <summary>
/// One grid row. Mirrors the legacy <c>DataTable</c> built in <c>uscRptTrainingCourse.ascx.cs:528-613</c>.
/// LEGACY COLUMN SET: OID, Code, NameEn, NameAr, DeliveryMethod, Fees, Active.
/// </summary>
public class TrainingCourseReportRowDto : EntityDto<int>
{
    public string Code { get; set; }

    public string NameEn { get; set; }

    public string NameAr { get; set; }

    public TrainingManagement.TrainingDeliveryMethod DeliveryMethod { get; set; }

    public double Fees { get; set; }

    public bool Active { get; set; }
}
