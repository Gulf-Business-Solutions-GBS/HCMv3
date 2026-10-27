using System;
using Abp.Application.Services.Dto;
using HCMv3.GeneralMasterInfo;
using HCMv3.GridReports.Dto;

namespace HCMv3.GridReports.MasterReport.Dto;

/// <summary>
/// Filter input for the Asset analysis grid report (legacy
/// <c>MasterReport/uscRptAsset.ascx</c>). Legacy hides the date-range, employee-status and employee-code
/// filter boxes on this screen (<c>Visible="false"</c> on all three, uscRptAsset.ascx:11-15) and never
/// applies row-level security (its <c>ApplyRowLevelSecurity</c> override is entirely commented out --
/// <c>Asset</c> has no employee dimension to restrict by), so those inherited fields are unused here and
/// the Angular screen hides their inputs too.
/// </summary>
public class GetAssetReportInput : GridReportInputBase
{
    /// <summary>Legacy <c>cmbAssetType</c> (<see cref="HCMv3.GeneralMasterInfo.AssetType"/> code).</summary>
    public string AssetTypeCode { get; set; }

    /// <summary>Legacy <c>cmbEmpStatus</c> (actually an <see cref="AssetStatus"/> combo on this screen).</summary>
    public AssetStatus? AssetStatus { get; set; }

    protected override string DefaultSorting => "Code ASC";
}

/// <summary>
/// One grid row. Mirrors legacy <c>Table1()</c>'s column set (uscRptAsset.ascx.cs:502-641) column-for-column.
/// </summary>
public class AssetReportRowDto : EntityDto<int>
{
    public string Code { get; set; }

    public string NameEn { get; set; }

    public string NameAr { get; set; }

    public string AssetType { get; set; }

    public string Status { get; set; }

    public string AssetAllocationStatus { get; set; }

    public string Brand { get; set; }

    public string Model { get; set; }

    /// <summary>Legacy wrote <c>Year.ToString()</c> into a string-typed DataColumn -- reproduced as a string.</summary>
    public string Year { get; set; }

    public string Color { get; set; }

    public string VinNumber { get; set; }

    public string Serial { get; set; }

    public string PlateNumber { get; set; }

    public string Barcode { get; set; }

    public DateTime ExpiryDate { get; set; }

    public double Value { get; set; }
}
