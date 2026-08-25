using Abp.Application.Services.Dto;
using HCMv3.GridReports.Dto;
using HCMv3.GridReports.Employees.Dto;

namespace HCMv3.GridReports.TrainingReport.Dto;

/// <summary>
/// Filter input for the Employee Training analysis grid report (legacy
/// <c>uscRptEmployeeTraining.ascx</c>). Beyond the three common filters, legacy adds one report-specific
/// combo, <c>cmbTraining</c>, matched on the TrainingCourse CODE (uscRptEmployeeTraining.ascx.cs:603).
///
/// LEGACY QUIRK, faithfully reproduced: legacy captures <c>dtFrom</c>/<c>dtTo</c> and logs them in the
/// parameter string, but <c>Table1()</c> never applies them as a filter on <see cref="TrainingManagement.EmployeeTraining"/>
/// (uscRptEmployeeTraining.ascx.cs:589-611 has no date predicate at all). So here too FromDate/ToDate are
/// accepted and echoed in <see cref="GridReportAppServiceBase.BuildParameterString"/>, but do not narrow
/// the result set. See the pinning test.
/// </summary>
public class GetEmployeeTrainingReportInput : GridReportInputBase
{
    /// <summary>Legacy <c>cmbTraining</c>: filters on the TrainingCourse CODE. Blank means "all courses".</summary>
    public string TrainingCourseCode { get; set; }

    /// <summary>Legacy ordered the fill loop by employee code implicitly (row source order); explicit here.</summary>
    protected override string DefaultSorting => "EmployeeCode ASC";
}

/// <summary>
/// One grid row. Mirrors the legacy <c>DataTable</c> built in <c>uscRptEmployeeTraining.ascx.cs:528-682</c>.
///
/// LEGACY COLUMN SET: OID, Code, Name, JobTitle, WorkLocation, OrgLevel1..OrgLevel7, TrainingCourse,
/// CompletionStatus, Score. JobTitle/WorkLocation/OrgLevel1..7 all come from the employee's org assignment
/// current AS OF TODAY (<c>DateTime.Now</c>, lines 615-617), NOT from any date supplied by the filter.
/// </summary>
public class EmployeeTrainingReportRowDto : EntityDto<int>, IEmployeeOrgLevelRow
{
    public string EmployeeCode { get; set; }

    public string EmployeeName { get; set; }

    /// <summary>From the assignment current as of today (legacy lines 615-617/658).</summary>
    public string JobTitleName { get; set; }

    /// <summary>From the assignment current as of today (legacy lines 615-617/659).</summary>
    public string WorkLocationName { get; set; }

    public string OrgLevel1 { get; set; }

    public string OrgLevel2 { get; set; }

    public string OrgLevel3 { get; set; }

    public string OrgLevel4 { get; set; }

    public string OrgLevel5 { get; set; }

    public string OrgLevel6 { get; set; }

    public string OrgLevel7 { get; set; }

    /// <summary>Leaf org unit the seven levels are resolved from. See <see cref="IEmployeeOrgLevelRow"/>.</summary>
    public long? OrganizationUnitId { get; set; }

    public string TrainingCourseName { get; set; }

    public TrainingManagement.TrainingCompletionStatus CompletionStatus { get; set; }

    public double Score { get; set; }
}
