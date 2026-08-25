using Abp.Application.Services.Dto;
using HCMv3.GridReports.Dto;

namespace HCMv3.GridReports.PerformanceReport.Dto;

/// <summary>
/// Legacy <c>cmbReportType</c> variant selector (uscRptPASummary.ascx.cs:244-248). Determines which extra
/// columns the legacy DataTable carried; this port always returns every field and lets the Angular screen
/// show/hide columns per variant instead (non-data-loss simplification -- see the row DTO remarks).
/// </summary>
public enum PASummaryReportVariant
{
    OverallScore = 1,
    FeedbackReport = 2,
    GoalReport = 3,
    OverallAssessment = 4
}

/// <summary>
/// Filter input for the PA Summary analysis grid report (legacy
/// <c>PerformanceReport/uscRptPASummary.ascx</c>). Legacy's <c>dtFrom</c>/<c>dtTo</c> select the
/// <see cref="HCMv3.PerformanceManagement.PAPeriod"/>s whose <see cref="HCMv3.PerformanceManagement.PATimeline"/>
/// window overlaps the requested date range -- blank dates default to "today" via the shared base class,
/// same as every other report in this family.
/// </summary>
public class GetPASummaryReportInput : GridReportInputBase
{
    /// <summary>Legacy <c>cmbPAPeriod</c> (matched against <see cref="HCMv3.PerformanceManagement.PAPeriod.Name"/>).</summary>
    public string PeriodName { get; set; }

    public PASummaryReportVariant Variant { get; set; } = PASummaryReportVariant.OverallScore;

    protected override string DefaultSorting => "WorkLocation ASC";
}

/// <summary>
/// One grid row. Legacy sorted the whole result by "Location" ascending (<c>dv.Sort</c>,
/// uscRptPASummary.ascx.cs:690) rather than any grid column -- reproduced as the default sort here.
///
/// DEVIATION (non-data-loss): legacy read <c>Employee.CurrentDepartment</c>/<c>CurrentWorkLocation</c>
/// (XAF computed aliases with no HCMv3 equivalent, since org levels collapsed into one
/// <c>OrganizationUnit</c> tree -- see <c>EmployeeOrgLevelNameResolver</c>). This port instead resolves
/// "Department" as the employee's current org-assignment's leaf <c>OrganizationUnit</c> display name and
/// "Location" as the current org-assignment's <c>WorkLocation</c> (a field that DOES still exist directly
/// on <c>EmployeeOrgAssignment</c>).
/// </summary>
public class PASummaryReportRowDto : EntityDto<int>
{
    public string EmployeeCode { get; set; }

    public string EmployeeName { get; set; }

    public string JobTitle { get; set; }

    public string Department { get; set; }

    public string WorkLocation { get; set; }

    public double OverallScore { get; set; }

    public double ModeratedScore { get; set; }

    public string AppraiseeStrength { get; set; }

    public string AreasOfImprovement { get; set; }

    public string NewlySetGoals { get; set; }

    public string OverallComments { get; set; }
}
