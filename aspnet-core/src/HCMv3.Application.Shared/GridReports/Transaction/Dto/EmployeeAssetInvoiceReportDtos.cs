using System;
using Abp.Application.Services.Dto;
using HCMv3.GridReports.Dto;
using HCMv3.GridReports.Employees.Dto;

namespace HCMv3.GridReports.Transaction.Dto;

/// <summary>
/// Filter input for the Employee Asset Invoice analysis grid report (legacy
/// <c>uscRptEmployeeAssetInvoice.ascx</c>). Adds the report-specific Asset Type dropdown filter
/// (legacy <c>cmbAssetType</c> / <c>Session[AssetTypeCode]</c>). Legacy never exposed an Employee Status
/// filter for this report, so <see cref="GridReportInputBase.EmployeeStatus"/> is inherited but unused.
/// </summary>
public class GetEmployeeAssetInvoiceReportInput : GridReportInputBase
{
    /// <summary>Legacy <c>cmbAssetType</c> -- filters by <c>Asset.AssetType.Code</c>. Null/empty means "all".</summary>
    public string AssetTypeCode { get; set; }

    /// <summary>Legacy <c>newds.OrderBy(r =&gt; r.Code).ThenBy(r =&gt; r.InvoiceDate)</c> (line 643).</summary>
    protected override string DefaultSorting => "EmployeeCode ASC, InvoiceDate ASC";
}

/// <summary>
/// One grid row. Mirrors legacy <c>Table1()</c> column-for-column (uscRptEmployeeAssetInvoice.ascx.cs:
/// 495-677): OID, Code, Name, JobTitle, OrgLevel1..7, Asset, AssetType, InvoiceDate, InvoiceAmount,
/// EmployeePlanAmount, AssetDeductionMode, DeductionAmount, DeductFromEmployee, Comments.
/// </summary>
public class EmployeeAssetInvoiceReportRowDto : EntityDto<int>, IEmployeeOrgLevelRow
{
    public string EmployeeCode { get; set; }

    public string EmployeeName { get; set; }

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

    public string AssetName { get; set; }

    public string AssetTypeName { get; set; }

    public DateTime InvoiceDate { get; set; }

    public double InvoiceAmount { get; set; }

    /// <summary>
    /// Legacy line 668: forced to 0 when <see cref="AssetDeductionMode"/> is "Full" -- the employee's
    /// installment plan amount is meaningless once the whole invoice is deducted at once.
    /// </summary>
    public double EmployeePlanAmount { get; set; }

    /// <summary>
    /// Legacy lines 632-640: the <c>EmployeeAsset</c> assignment (same employee+asset) whose
    /// Start/EndDate window contains the invoice date, or null when none matches (legacy showed "").
    /// Kept as the enum (not legacy's <c>ToString()</c>'d text) so the Angular grid/localization pipe
    /// controls the display text.
    /// </summary>
    public PersonnelAdministration.AssetDeductionMode? AssetDeductionMode { get; set; }

    public double DeductionAmount { get; set; }

    public bool DeductFromEmployee { get; set; }

    public string Comments { get; set; }
}
