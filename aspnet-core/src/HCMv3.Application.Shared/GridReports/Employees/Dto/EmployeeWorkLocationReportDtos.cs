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
/// OrgLevel1..OrgLevel7 are kept as SEVEN columns, exactly as legacy rendered them. HCMv3's
/// <c>EmployeeOrgAssignment</c> replaced legacy's seven FKs with one FK into ABP's hierarchical
/// <c>OrganizationUnit</c> tree, so each level is resolved from that tree after materialization --
/// see <c>EmployeeOrgLevelNameResolver</c>.
///
/// DEVIATION 2 (naming only) -- legacy's column is literally called "Remarks" but is declared
/// <c>typeof(DateTime)</c> (line 570) and filled with the employee's last day in service (line 671).
/// It is exposed here under its real meaning, <see cref="LastDayInService"/>, and typed nullable so
/// "no separation" is null rather than legacy's <c>DateTime.MinValue</c>.
/// </summary>
public class EmployeeWorkLocationReportRowDto : EntityDto<int>, IEmployeeOrgLevelRow
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

    /// <summary>
    /// Legacy <c>OrgLevel1</c>..<c>OrgLevel7</c> columns. HCMv3 stores ONE
    /// <c>EmployeeOrgAssignment.OrganizationUnitId</c> into Zero's hierarchical OrganizationUnit tree, so
    /// each level is the ancestor-or-self of that leaf classified at the matching level. The values are
    /// stamped after materialization by <c>EmployeeOrgLevelNameResolver</c> and are therefore NOT
    /// server-sortable.
    /// </summary>
    public string OrgLevel1 { get; set; }

    public string OrgLevel2 { get; set; }

    public string OrgLevel3 { get; set; }

    public string OrgLevel4 { get; set; }

    public string OrgLevel5 { get; set; }

    public string OrgLevel6 { get; set; }

    public string OrgLevel7 { get; set; }

    /// <summary>Leaf org unit the seven levels are resolved from. See <see cref="IEmployeeOrgLevelRow"/>.</summary>
    public long? OrganizationUnitId { get; set; }

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
