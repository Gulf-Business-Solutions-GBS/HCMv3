using System;
using Abp.Application.Services.Dto;
using HCMv3.GridReports.Dto;
using HCMv3.GridReports.Employees.Dto;
using HCMv3.PersonnelAdministration;

namespace HCMv3.GridReports.Transaction.Dto;

/// <summary>
/// Filter input for the Employee Document analysis grid report (legacy
/// <c>uscRptEmployeeDocument.ascx</c>). Adds the report-specific Document Type dropdown filter (legacy
/// <c>Session[DocumentTypeCode]</c>). Shared by both <see cref="GetEmployeeDocumentReportInput"/> and the
/// Custom033 customer variant.
/// </summary>
public class GetEmployeeDocumentReportInput : GridReportInputBase
{
    /// <summary>Legacy <c>cmbDocumentType</c> -- filters by <c>DocumentType.Code</c>.</summary>
    public string DocumentTypeCode { get; set; }

    /// <summary>Legacy line 636: <c>OrderBy(r =&gt; r.Code).ThenBy(r =&gt; r.IssueDate).ThenBy(r =&gt; r.ExpiryDate)</c>.</summary>
    protected override string DefaultSorting => "EmployeeCode ASC, IssueDate ASC, ExpiryDate ASC";
}

/// <summary>
/// One grid row. Mirrors legacy <c>Table1()</c> column-for-column (uscRptEmployeeDocument.ascx.cs:
/// 495-677): OID, Code, Employee, EmployeeStatus, JobTitle, OrgLevel1..7, CostCenter, PayrollArea,
/// Country, Sponsor, DocumentType, IssueDate, ExpiryDate, IsActive, DocumentNum, Description, DaysLeft.
///
/// DELIBERATE SIMPLIFICATION: legacy's <c>IsActive</c>/<c>DaysLeft</c> came from the <c>EmployeeDocument</c>
/// business object's computed properties, which include a "superseded by a newer document of the same
/// type" rule (see <c>HCMv3.Application.PersonnelAdministration.EmployeeDocumentsAppService.ToDtoAsync</c>
/// for the full legacy-equivalent computation). Reproducing that per-row correlated rule here would need a
/// second correlated subquery per row on top of the seven already present for Org/Sponsor context; this
/// report computes the simpler "is the document currently within its Issue/Expiry window" check only. Not
/// a data-loss defect (the report still shows every document and its real dates), just a reduced-fidelity
/// display column -- flagged per porting convention rather than silently matched or silently dropped.
/// </summary>
public class EmployeeDocumentReportRowDto : EntityDto<int>, IEmployeeOrgLevelRow
{
    public string EmployeeCode { get; set; }

    public string EmployeeName { get; set; }

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

    public string Sponsor { get; set; }

    public string DocumentTypeName { get; set; }

    public DateTime IssueDate { get; set; }

    public DateTime ExpiryDate { get; set; }

    /// <summary>See class remarks -- simplified from legacy's full computed rule.</summary>
    public bool IsActive { get; set; }

    public string DocumentNum { get; set; }

    public string Description { get; set; }

    /// <summary>See class remarks -- simplified from legacy's full computed rule.</summary>
    public double DaysLeft { get; set; }
}
