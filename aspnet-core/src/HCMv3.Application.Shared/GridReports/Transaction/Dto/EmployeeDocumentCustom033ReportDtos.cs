using System;
using Abp.Application.Services.Dto;
using HCMv3.GridReports.Dto;
using HCMv3.GridReports.Employees.Dto;
using HCMv3.PersonnelAdministration;

namespace HCMv3.GridReports.Transaction.Dto;

/// <summary>
/// Filter input for the customer-specific Employee Document (Custom033) analysis grid report (legacy
/// <c>uscRptEmployeeDocument_Custom033.ascx</c>). Diffed against the base
/// <c>uscRptEmployeeDocument.ascx</c>: identical filter surface (Document Type dropdown, the three shared
/// filters); the delta is entirely in the row shape (see <see cref="EmployeeDocumentCustom033ReportRowDto"/>).
/// </summary>
public class GetEmployeeDocumentCustom033ReportInput : GridReportInputBase
{
    /// <summary>Legacy <c>cmbDocumentType</c> -- filters by <c>DocumentType.Code</c>.</summary>
    public string DocumentTypeCode { get; set; }

    /// <summary>Legacy line 636: <c>OrderBy(r =&gt; r.Code).ThenBy(r =&gt; r.IssueDate).ThenBy(r =&gt; r.ExpiryDate)</c>.</summary>
    protected override string DefaultSorting => "EmployeeCode ASC, IssueDate ASC, ExpiryDate ASC";
}

/// <summary>
/// One grid row. Diff against the base <c>uscRptEmployeeDocument.ascx.cs</c> Table1 (verified via
/// <c>diff</c> of both code-behinds):
/// <list type="bullet">
/// <item><description>ADDS <see cref="CivilId"/> (<c>rec.Employee.CivilID</c>, Custom033 lines 601/648/717/787/830).</description></item>
/// <item><description>RENAMES the base's "Sponsor" column to <see cref="SponsorName"/> (same value, cosmetic
/// header rename only, Custom033 lines 621/659).</description></item>
/// <item><description>DROPS the base's separate "DocumentStatus" text column (Custom033 keeps only
/// <c>IsActive</c>) -- moot in this port since the base row DTO already collapsed that redundant duplicate
/// column (see <c>EmployeeDocumentReportRowDto</c> remarks).</description></item>
/// </list>
/// Everything else (Employee/Status/JobTitle/OrgLevel1..7/CostCenter/PayrollArea/Country/DocumentType/
/// IssueDate/ExpiryDate/DocumentNum/Description/DaysLeft) is identical to the base report.
/// </summary>
public class EmployeeDocumentCustom033ReportRowDto : EntityDto<int>, IEmployeeOrgLevelRow
{
    public string EmployeeCode { get; set; }

    public string EmployeeName { get; set; }

    /// <summary>Custom033-only column: <c>rec.Employee.CivilID</c>.</summary>
    public long CivilId { get; set; }

    public EmployeeStatus EmployeeStatus { get; set; }

    public string JobTitle { get; set; }

    public string OrgLevel1 { get; set; }

    public string OrgLevel2 { get; set; }

    public string OrgLevel3 { get; set; }

    public string OrgLevel4 { get; set; }

    public string OrgLevel5 { get; set; }

    public string OrgLevel6 { get; set; }

    public string OrgLevel7 { get; set; }

    /// <summary>Leaf org unit of the assignment current as of today (legacy: <c>DateTime.Now</c>).</summary>
    public long? OrganizationUnitId { get; set; }

    public string CostCenter { get; set; }

    public string PayrollArea { get; set; }

    public string Country { get; set; }

    /// <summary>Legacy "Sponsor" column, renamed "SponsorName" in Custom033.</summary>
    public string SponsorName { get; set; }

    public string DocumentTypeName { get; set; }

    public DateTime IssueDate { get; set; }

    public DateTime ExpiryDate { get; set; }

    /// <summary>Simplified from legacy's full computed rule -- see base report's DTO remarks.</summary>
    public bool IsActive { get; set; }

    public string DocumentNum { get; set; }

    public string Description { get; set; }

    /// <summary>Simplified from legacy's full computed rule -- see base report's DTO remarks.</summary>
    public double DaysLeft { get; set; }
}
