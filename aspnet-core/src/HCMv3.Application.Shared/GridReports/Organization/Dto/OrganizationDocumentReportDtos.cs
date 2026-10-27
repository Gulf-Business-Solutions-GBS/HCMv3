using System;
using Abp.Application.Services.Dto;
using HCMv3.GridReports.Dto;

namespace HCMv3.GridReports.Organization.Dto;

/// <summary>
/// Filter input for the Organization Document analysis grid report (legacy
/// <c>uscRptOrganizationDocument.ascx</c>). Legacy filtered on <c>DocumentType.Code</c> off a combo box;
/// this port filters by <see cref="DocumentTypeId"/> instead -- the standard lookup-filter convention in
/// this codebase (same effective filter, cosmetic key difference). This report has no employee dimension
/// (<c>OrganizationDocument</c> is org-unit-scoped, not employee-scoped), so the inherited
/// <c>EmployeeCode</c>/<c>EmployeeStatus</c> fields exist but are simply never referenced.
/// </summary>
public class GetOrganizationDocumentReportInput : GridReportInputBase
{
    public int? DocumentTypeId { get; set; }

    /// <summary>Legacy: <c>newds.OrderBy(r =&gt; r.IssueDate).ThenBy(r =&gt; r.ExpiryDate)</c> (ascx.cs:557).</summary>
    protected override string DefaultSorting => "IssueDate ASC, ExpiryDate ASC";
}

/// <summary>
/// One grid row. Mirrors the legacy <c>DataTable</c> built in <c>uscRptOrganizationDocument.ascx.cs:484-586</c>:
/// OID, OrgUnit, DocumentType, IssueDate, ExpiryDate, DocumentPlace, DocumentNumber, Description, DaysLeft.
/// </summary>
public class OrganizationDocumentReportRowDto : EntityDto<int>
{
    public string OrganizationUnitName { get; set; }

    public string DocumentTypeName { get; set; }

    public DateTime IssueDate { get; set; }

    public DateTime ExpiryDate { get; set; }

    public string DocumentPlace { get; set; }

    public string DocumentNum { get; set; }

    /// <summary>Legacy "Description" column -- <c>rec.Comments</c>.</summary>
    public string Description { get; set; }

    /// <summary>
    /// Legacy computed <c>OrganizationDocument.DaysLeft</c> (dropped from the ported entity, see its
    /// class remarks) -- recomputed here as <c>(ExpiryDate.Date - Today).TotalDays</c>.
    /// </summary>
    public int DaysLeft { get; set; }
}
