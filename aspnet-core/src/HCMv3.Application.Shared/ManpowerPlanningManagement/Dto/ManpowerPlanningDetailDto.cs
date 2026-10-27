using Abp.Application.Services.Dto;

namespace HCMv3.ManpowerPlanningManagement.Dto;

/// <summary>
/// Read/list DTO for <see cref="ManpowerPlanningDetail"/>.
/// </summary>
public class ManpowerPlanningDetailDto : EntityDto<int>
{
    public int ManpowerPlanningId { get; set; }

    public int JobTitleId { get; set; }

    public string JobTitleNameEn { get; set; }

    public int? PositionId { get; set; }

    public string PositionCodeWithNameEn { get; set; }

    public int? HolderId { get; set; }

    public string HolderFullNameEn { get; set; }

    public double January { get; set; }

    public double February { get; set; }

    public double March { get; set; }

    public double April { get; set; }

    public double May { get; set; }

    public double June { get; set; }

    public double July { get; set; }

    public double August { get; set; }

    public double September { get; set; }

    public double October { get; set; }

    public double November { get; set; }

    public double December { get; set; }

    public double TotalAmount { get; set; }
}
