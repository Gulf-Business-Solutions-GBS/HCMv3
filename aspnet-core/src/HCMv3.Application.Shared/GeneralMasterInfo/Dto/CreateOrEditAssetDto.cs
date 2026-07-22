using System;
using System.ComponentModel.DataAnnotations;

namespace HCMv3.GeneralMasterInfo.Dto;

/// <summary>
/// Create/Update input DTO for <see cref="Asset"/>. <c>Id == null</c> means create; otherwise update.
/// </summary>
public class CreateOrEditAssetDto
{
    public int? Id { get; set; }

    /// <summary>
    /// Optional. When left blank the server auto-generates the next numeric code (legacy <c>GetMaxCode</c> parity).
    /// </summary>
    [StringLength(SimpleLookupConsts.MaxCodeLength)]
    public string Code { get; set; }

    [Required]
    [StringLength(SimpleLookupConsts.MaxNameLength)]
    public string NameEn { get; set; }

    [Required]
    [StringLength(SimpleLookupConsts.MaxNameLength)]
    public string NameAr { get; set; }

    [StringLength(256)]
    public string Brand { get; set; }

    [StringLength(256)]
    public string Model { get; set; }

    [StringLength(64)]
    public string Color { get; set; }

    [StringLength(64)]
    public string VIN_Number { get; set; }

    [StringLength(64)]
    public string Serial { get; set; }

    [StringLength(64)]
    public string PlateNumber { get; set; }

    [StringLength(64)]
    public string Barcode { get; set; }

    public int Year { get; set; }

    public DateTime ExpiryDate { get; set; }

    public double Value { get; set; }

    public int? AssetTypeId { get; set; }

    public AssetStatus Status { get; set; }

    public AssetAllocationStatus AssetAllocationStatus { get; set; }
}
