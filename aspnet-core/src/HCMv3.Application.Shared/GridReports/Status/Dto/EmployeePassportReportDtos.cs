using System;
using Abp.Application.Services.Dto;
using HCMv3.GridReports.Dto;
using HCMv3.GridReports.Employees.Dto;

namespace HCMv3.GridReports.Status.Dto;

/// <summary>
/// Filter input for the Employee Passport analysis grid report (legacy
/// <c>uscRptEmployeePassport.ascx</c>).
///
/// LEGACY DEFECT FLAGGED FOR REVIEW / DROPPED FILTER: legacy's <c>cmbDocumentPlace</c> filter
/// (<c>uscRptEmployeePassport.ascx.cs:560-564</c>) filters on <c>EmployeeDocument.DocPlace</c>, but
/// HCMv3's <c>EmployeeDocument</c> (ported in an earlier wave) deliberately does not carry a
/// <c>DocPlace</c>/<c>DocumentPlace</c> FK -- see the "out of Wave A scope" note on that entity. This is
/// not something this report can restore without a schema change to an already-ported entity, which is
/// out of this wave's scope; the filter and its corresponding "DocumentPlace" export/grid column are
/// therefore OMITTED here rather than silently faked. No row data is lost -- every other legacy column is
/// present.
/// </summary>
public class GetEmployeePassportReportInput : GridReportInputBase
{
    protected override string DefaultSorting => "EmployeeCode ASC";
}

/// <summary>
/// One grid row. Mirrors the legacy <c>DataTable</c> built in
/// <c>uscRptEmployeePassport.ascx.cs:488-658</c> (<c>Table1()</c>): OID, Code, Name, JobTitle,
/// Nationality, OrgLevel1..7, IssueDate, ExpiryDate, DocumentPlace, Description, Country.
/// <c>DocumentPlace</c> is omitted -- see the input DTO remarks.
/// </summary>
public class EmployeePassportReportRowDto : EntityDto<int>, IEmployeeOrgLevelRow
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

    public DateTime IssueDate { get; set; }

    public DateTime ExpiryDate { get; set; }

    public string Description { get; set; }

    public string CountryName { get; set; }
}
