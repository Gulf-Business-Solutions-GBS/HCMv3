using Abp.Application.Services.Dto;
using HCMv3.GridReports.Dto;
using HCMv3.GridReports.Employees.Dto;

namespace HCMv3.GridReports.SurveyReport.Dto;

/// <summary>
/// Filter input for the Employee Survey analysis grid report (legacy <c>uscRptEmployeeSurvey.ascx</c>).
/// Beyond the three common filters, legacy adds one report-specific combo, <c>cmbSurvey</c> (session key
/// <c>SurveyCode</c>), matched on the Survey CODE (uscRptEmployeeSurvey.ascx.cs "_SurveyCode" filter).
/// FromDate/ToDate are applied against the parent Survey's own StartDate/EndDate window (overlap).
/// </summary>
public class GetEmployeeSurveyReportInput : GridReportInputBase
{
    /// <summary>Legacy <c>cmbSurvey</c>: matches the Survey CODE. Blank means "all surveys".</summary>
    public string SurveyCode { get; set; }

    protected override string DefaultSorting => "EmployeeCode ASC";
}

/// <summary>
/// One grid row. Mirrors the legacy <c>DataTable</c> built in <c>uscRptEmployeeSurvey.ascx.cs</c>
/// (<c>Table1()</c>). LEGACY COLUMN SET: OID, Code, Name, Survey, StartDate, EndDate, JobTitle,
/// WorkLocation, OrgLevel1..OrgLevel7, Question, Answer. JobTitle/WorkLocation/OrgLevel1..7 all come from
/// the employee's org assignment current AS OF TODAY, exactly like <c>uscRptEmployeeTraining</c>/
/// <c>uscRptEmployeeWorkLocation</c>.
/// </summary>
public class EmployeeSurveyReportRowDto : EntityDto<int>, IEmployeeOrgLevelRow
{
    public string EmployeeCode { get; set; }

    public string EmployeeName { get; set; }

    public string SurveyName { get; set; }

    public System.DateTime StartDate { get; set; }

    public System.DateTime EndDate { get; set; }

    public string JobTitleName { get; set; }

    public string WorkLocationName { get; set; }

    public string OrgLevel1 { get; set; }

    public string OrgLevel2 { get; set; }

    public string OrgLevel3 { get; set; }

    public string OrgLevel4 { get; set; }

    public string OrgLevel5 { get; set; }

    public string OrgLevel6 { get; set; }

    public string OrgLevel7 { get; set; }

    public long? OrganizationUnitId { get; set; }

    /// <summary>Legacy "Question" column: empty string, not null, when the linked question is missing.</summary>
    public string Question { get; set; }

    public string Answer { get; set; }
}
