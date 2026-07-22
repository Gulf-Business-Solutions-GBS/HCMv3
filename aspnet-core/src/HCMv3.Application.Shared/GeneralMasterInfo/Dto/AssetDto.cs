using System;
using Abp.Application.Services.Dto;

namespace HCMv3.GeneralMasterInfo.Dto;

/// <summary>
/// Read/list DTO for <see cref="Asset"/>.
/// </summary>
public class AssetDto : EntityDto<int>
{
    public string Code { get; set; }

    public string NameEn { get; set; }

    public string NameAr { get; set; }

    public string Brand { get; set; }

    public string Model { get; set; }

    public string Color { get; set; }

    public string VIN_Number { get; set; }

    public string Serial { get; set; }

    public string PlateNumber { get; set; }

    public string Barcode { get; set; }

    public int Year { get; set; }

    public DateTime ExpiryDate { get; set; }

    public double Value { get; set; }

    public int? AssetTypeId { get; set; }

    public string AssetTypeName { get; set; }

    public AssetStatus Status { get; set; }

    public AssetAllocationStatus AssetAllocationStatus { get; set; }
}
