using System;
using Abp.Application.Services.Dto;
using HCMv3.GridReports.Dto;

namespace HCMv3.GridReports.Employees.Dto;

/// <summary>
/// Filter input for the Employee Work Location analysis grid report (legacy
/// <c>uscRptEmployeeWorkLocation.ascx</c>). Beyond the four common filters legacy adds one
/// report-specific combo, <c>cmbWorkLocation</c>, which filters on the WorkLocation CODE
/// (uscRptEmployeeWorkLocation.ascx.cs:585-589).
/// </summary>
public class GetEmployeeWorkLocationReportInput : GridReportInputBase
{
    /// <summary>
    /// Legacy <c>cmbWorkLocation</c>: filters org-assignment rows to a single WorkLocation by its CODE
    /// (not its id/name). Null or blank means "all work locations".
    /// </summary>
    public string WorkLocationCode { get; set; }

    /// <summary>Legacy ordered the fill loop by employee code (line 646: <c>newds.OrderBy(r =&gt; r.Code)</c>).</summary>
    protected override string DefaultSorting => "EmployeeCode ASC";
}

/// <summary>
/// One grid row. Mirrors the legacy <c>DataTable</c> built in
/// <c>uscRptEmployeeWorkLocation.ascx.cs:513-683</c>.
///
/// LEGACY COLUMN SET (lines 555-570): OID, Code, Name, JobTitle, CivilId, Nationality,
/// OrgLevel1..OrgLevel7, WorkLocation, Country, Remarks.
///
/// DEVIATION 1 -- OrgLevel1..OrgLevel7 collapse to a single <see cref="OrganizationUnitName"/>; see
/// <see cref="EmployeeTransferReportRowDto"/> remarks for why (single <c>OrganizationUnitId</c> FK).
///
/// DEVIATION 2 (naming only) -- legacy's column is literally called "Remarks" but is declared
/// <c>typeof(DateTime)</c> (line 570) and filled with the employee's last day in service (line 671).
/// It is exposed here under its real meaning, <see cref="LastDayInService"/>, and typed nullable so
/// "no separation" is null rather than legacy's <c>DateTime.MinValue</c>.
/// </summary>
public class EmployeeWorkLocationReportRowDto : EntityDto<int>
{
    /// <summary>Legacy "Code" column (line 651).</summary>
    public string EmployeeCode { get; set; }

    /// <summary>Legacy "Name" column -- <c>FullNameAr</c> when Arabic, else <c>FullNameEn</c> (line 626).</summary>
    public string EmployeeName { get; set; }

    /// <summary>
    /// Legacy "JobTitle" column -- taken from the assignment current AS OF TODAY (the <c>empOrgAss</c>
    /// sub-query is filtered on <c>DateTime.Now</c>, lines 601-603), NOT from the row's own assignment.
    /// </summary>
    public string JobTitleName { get; set; }

    /// <summary>Legacy "CivilId" column -- <c>rec.Employee.CivilID</c> (line 627).</summary>
    public long CivilId { get; set; }

    /// <summary>Legacy "Nationality" column (line 628).</summary>
    public string NationalityName { get; set; }

    /// <summary>Stands in for legacy OrgLevel1..OrgLevel7 -- from the assignment current as of TODAY.</summary>
    public string OrganizationUnitName { get; set; }

    /// <summary>
    /// Legacy "WorkLocation" column -- from the ROW's own org assignment (<c>rec.WorkLocation</c>,
    /// line 640), unlike the other org columns which come from today's assignment.
    /// </summary>
    public string WorkLocationName { get; set; }

    /// <summary>Legacy "Country" column -- PayrollArea country of today's assignment (line 665).</summary>
    public string CountryName { get; set; }

    /// <summary>
    /// Legacy "Remarks" column (see class remarks): last day in service of the employee's most recent
    /// non-revoked separation submitted ON OR BEFORE the FROM date (lines 636-637).
    /// </summary>
    public DateTime? LastDayInService { get; set; }
}
