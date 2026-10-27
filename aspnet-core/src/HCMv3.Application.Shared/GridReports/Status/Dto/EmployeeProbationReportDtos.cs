using System;
using Abp.Application.Services.Dto;
using HCMv3.GridReports.Dto;
using HCMv3.GridReports.Employees.Dto;

namespace HCMv3.GridReports.Status.Dto;

/// <summary>
/// Filter input for the Employee Probation analysis grid report (legacy
/// <c>uscRptEmployeeProbation.ascx</c>), filtered on <c>Employee.ProbationEndDate</c>.
/// </summary>
public class GetEmployeeProbationReportInput : GridReportInputBase
{
    protected override string DefaultSorting => "EmployeeCode ASC";
}

/// <summary>
/// One grid row. Mirrors the legacy <c>DataTable</c> built in
/// <c>uscRptEmployeeProbation.ascx.cs:460-654</c> (<c>Table1()</c>): OID, Code, Name, JobTitle,
/// Nationality, OrgLevel1..7, JoiningDate, ProbationDays, ProbationEndDate, IssueDate, ExpiryDate,
/// WorkLocation, Description, Country.
/// </summary>
public class EmployeeProbationReportRowDto : EntityDto<int>, IEmployeeOrgLevelRow
{
    public string EmployeeCode { get; set; }

    public string EmployeeName { get; set; }

    public string JobTitleName { get; set; }

    public string NationalityName { get; set; }

    public string OrgLevel1 { get; set; }

    public string OrgLevel2 { get; set; }

    public string OrgLevel3 { get; set; }

    public string OrgLevel4 { get; set; }

    public string OrgLevel5 { get; set; }

    public string OrgLevel6 { get; set; }

    public string OrgLevel7 { get; set; }

    public long? OrganizationUnitId { get; set; }

    public DateTime JoiningDate { get; set; }

    public int ProbationDays { get; set; }

    public DateTime ProbationEndDate { get; set; }

    /// <summary>
    /// Legacy lines 573-582/601: the employee's most recent CID/Civil Id <c>EmployeeDocument</c>
    /// (by ExpiryDate desc, no date-window filter on the document itself).
    /// </summary>
    public DateTime? IssueDate { get; set; }

    public DateTime? ExpiryDate { get; set; }

    public string WorkLocationName { get; set; }

    /// <summary>Legacy line 603: always empty string -- the legacy projection never assigns it.</summary>
    public string Description { get; set; }

    public string CountryName { get; set; }
}
