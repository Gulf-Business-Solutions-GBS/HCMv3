using System.ComponentModel.DataAnnotations;
using Abp.Application.Services.Dto;
using Abp.Extensions;
using Abp.Runtime.Validation;

namespace HCMv3.GeneralMasterInfo.Dto;

/// <summary>
/// Read/list DTO for <c>AssetType</c>.
/// </summary>
public class AssetTypeDto : EntityDto<int>
{
    public string Code { get; set; }

    public string NameEn { get; set; }

    public string NameAr { get; set; }

    public bool IncludeInWorkflow { get; set; }

    public int? WageTypeId { get; set; }

    public string WageTypeName { get; set; }
}

/// <summary>
/// Create/Update input for <c>AssetType</c>. <c>Id == null</c> means create; otherwise update.
/// </summary>
public class CreateOrEditAssetTypeDto
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

    /// <summary>Legacy <c>AssetType.IncludeInWorkflow</c>: assets of this type can be requested in self-service.</summary>
    public bool IncludeInWorkflow { get; set; }

    /// <summary>Legacy <c>AssetType.WageType</c>. Optional.</summary>
    public int? WageTypeId { get; set; }
}

public class GetAssetTypeForEditOutput
{
    public CreateOrEditAssetTypeDto AssetType { get; set; }
}

public class GetAllAssetTypesInput : PagedAndSortedResultRequestDto, IShouldNormalize
{
    /// <summary>Free-text filter matched against Code / NameEn / NameAr.</summary>
    public string Filter { get; set; }

    public void Normalize()
    {
        if (Sorting.IsNullOrWhiteSpace())
        {
            Sorting = "Code ASC";
        }
    }
}
