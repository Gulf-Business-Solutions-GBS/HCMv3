using Abp.Application.Services.Dto;

namespace HCMv3.HealthAndSafetyManagement.Dto;

/// <summary>
/// Read/list DTO for <see cref="SafetyChecklist"/>.
/// </summary>
public class SafetyChecklistDto : EntityDto<int>
{
    public string Code { get; set; }

    public int SafetyAreaAssetId { get; set; }

    public string SafetyAreaAssetNameEn { get; set; }

    public string Description { get; set; }

    public bool IsActive { get; set; }
}
