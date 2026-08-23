using System;
using Abp.Application.Services.Dto;
using HCMv3.GridReports.Dto;

namespace HCMv3.GridReports.Employees.Dto;

/// <summary>
/// Filter input for the Employee Asset analysis grid report (legacy
/// <c>uscRptEmployeeAsset.ascx</c>). The legacy filter panel is exactly dtFrom/dtTo/cmbEmpStatus/
/// txtEmpCode (uscRptEmployeeAsset.ascx), i.e. the four common filters and nothing report-specific,
/// so like the reference report this class adds no fields of its own.
/// </summary>
public class GetEmployeeAssetReportInput : GridReportInputBase
{
    /// <summary>
    /// Legacy applied no sort at all in <c>Table1()</c> (uscRptEmployeeAsset.ascx.cs:462-660 fills the
    /// DataTable in raw query order). Employee code ascending is used here because server-side paging
    /// requires a deterministic order, which the legacy in-memory grid did not.
    /// </summary>
    protected override string DefaultSorting => "EmployeeCode ASC";
}

/// <summary>
/// One grid row. Mirrors the legacy <c>DataTable</c> built in
/// <c>uscRptEmployeeAsset.ascx.cs:462-660</c>: OID, Code, Name, StartDate, EndDate, OrgLevel1..7,
/// Asset, AssetType, Brand, Model, Year, Color, VIN_Number, Serial, PlateNumber, Barcode, Status,
/// ExpiryDate, Value.
///
/// DEVIATION (structural, applies to every report in this family): the legacy seven
/// <c>OrgLevel1</c>..<c>OrgLevel7</c> columns do not exist in HCMv3 -- Wave B collapsed them into the
/// single <c>EmployeeOrgAssignment.OrganizationUnitId</c> FK into Zero's hierarchical
/// <c>OrganizationUnit</c> tree (see EmployeeOrgAssignment class remarks). They are therefore
/// represented by ONE <see cref="OrganizationUnitName"/> column rather than seven.
/// </summary>
public class EmployeeAssetReportRowDto : EntityDto<int>
{
    /// <summary>Legacy "Code" column -- <c>rec.Employee.Code</c>.</summary>
    public string EmployeeCode { get; set; }

    /// <summary>Legacy "Name" column -- FullNameAr when Arabic, else FullNameEn.</summary>
    public string EmployeeName { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    /// <summary>Stands in for the legacy OrgLevel1..7 columns (see class remarks). Null when the
    /// employee has no org assignment current TODAY -- legacy used <c>DateTime.Now</c> here, NOT the
    /// report's from/to dates (uscRptEmployeeAsset.ascx.cs:566-568).</summary>
    public string OrganizationUnitName { get; set; }

    /// <summary>Legacy "Asset" column -- empty string (not null) when the row has no asset.</summary>
    public string AssetName { get; set; }

    public string AssetTypeName { get; set; }

    public string Brand { get; set; }

    public string Model { get; set; }

    /// <summary>Legacy rendered Year via <c>Year.ToString()</c> into a string column, so it stays a string here.</summary>
    public string Year { get; set; }

    public string Color { get; set; }

    public string VinNumber { get; set; }

    public string Serial { get; set; }

    public string PlateNumber { get; set; }

    public string Barcode { get; set; }

    /// <summary>Legacy "Status" column -- <c>Asset.Status.ToString()</c>, i.e. the ASSET status, not the employee's.</summary>
    public string AssetStatus { get; set; }

    public DateTime ExpiryDate { get; set; }

    public double Value { get; set; }
}
