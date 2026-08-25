using System;
using Abp.Application.Services.Dto;
using HCMv3.GridReports.Dto;
using HCMv3.GridReports.Employees.Dto;
using HCMv3.PersonnelAdministration;

namespace HCMv3.GridReports.Status.Dto;

/// <summary>
/// Filter input for the Employee Residency analysis grid report (legacy
/// <c>uscRptEmployeeResidency.ascx</c>). Adds the report-specific "Residency Type" dropdown (legacy
/// <c>cmbResidencyType</c>, applied as a <c>DataView.RowFilter</c> post-filter over the computed
/// ResidencyType column at lines 778-784; applied here as a normal query predicate instead since the
/// value comes from a correlated sub-query and both are equivalent).
/// </summary>
public class GetEmployeeResidencyReportInput : GridReportInputBase
{
    public ResidencyType? ResidencyType { get; set; }

    /// <summary>Legacy sorted the row set by CivilIdExpiryDate ascending (line 679).</summary>
    protected override string DefaultSorting => "CivilIdExpiryDate ASC";
}

/// <summary>
/// One grid row -- ONE PER CIVIL-ID DOCUMENT, not per employee (an employee with two CID/Civil-Id
/// documents overlapping the window produces two rows, exactly as legacy's <c>Table1()</c> iterated the
/// <c>EmployeeDocument</c> query directly). Mirrors the legacy <c>DataTable</c> built in
/// <c>uscRptEmployeeResidency.ascx.cs:493-793</c>: OID, Code, Name, JobTitle, JoiningDate, CivilId,
/// CivilIdIssueDate, CivilIdExpiryDate, PassportNo, PassportIssueDate, PassportExpiryDate,
/// ResidenceExtensionIssueDate, ResidenceExtensionExpiryDate, RenewedResidenceInfoIssueDate,
/// RenewedResidenceInfoExpiryDate, OrgLevel1..7, WorkLocation, ResidencyType, SalaryInIznamal,
/// GovtJobTitle, GovtJobTitleAr, StartDate, EndDate, Country.
/// </summary>
public class EmployeeResidencyReportRowDto : EntityDto<int>, IEmployeeOrgLevelRow
{
    public string EmployeeCode { get; set; }

    public string EmployeeName { get; set; }

    public string JobTitleName { get; set; }

    public DateTime JoiningDate { get; set; }

    public long CivilId { get; set; }

    public DateTime CivilIdIssueDate { get; set; }

    public DateTime CivilIdExpiryDate { get; set; }

    /// <summary>
    /// Legacy lines 694-716: among the employee's Passport documents currently valid (no expiry, or
    /// Issue&lt;=today&lt;=Expiry), the one with the latest IssueDate. Empty/null when none qualifies.
    /// </summary>
    public string PassportNo { get; set; }

    public DateTime? PassportIssueDate { get; set; }

    public DateTime? PassportExpiryDate { get; set; }

    /// <summary>Legacy lines 719-729: latest (by ExpiryDate desc) "Residence Extension" document, if any.</summary>
    public DateTime? ResidenceExtensionIssueDate { get; set; }

    public DateTime? ResidenceExtensionExpiryDate { get; set; }

    /// <summary>Legacy lines 732-742: latest (by ExpiryDate desc) "Renewed Residence Information" document, if any.</summary>
    public DateTime? RenewedResidenceInfoIssueDate { get; set; }

    public DateTime? RenewedResidenceInfoExpiryDate { get; set; }

    public string OrgLevel1 { get; set; }

    public string OrgLevel2 { get; set; }

    public string OrgLevel3 { get; set; }

    public string OrgLevel4 { get; set; }

    public string OrgLevel5 { get; set; }

    public string OrgLevel6 { get; set; }

    public string OrgLevel7 { get; set; }

    public long? OrganizationUnitId { get; set; }

    public string WorkLocationName { get; set; }

    /// <summary>Legacy lines 643-656/764: the sponsor record current TODAY (not the document's own dates).</summary>
    public ResidencyType? ResidencyType { get; set; }

    public double? SalaryInIznamal { get; set; }

    public string GovtJobTitleName { get; set; }

    public string GovtJobTitleNameAr { get; set; }

    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public string CountryName { get; set; }
}
