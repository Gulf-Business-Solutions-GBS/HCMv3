using System.ComponentModel.DataAnnotations;

namespace HCMv3.ManpowerPlanningManagement.Dto;

/// <summary>
/// Create/Update input DTO for <see cref="ManpowerPlanningDetail"/>. <c>Id == null</c> means create;
/// otherwise update.
/// </summary>
public class CreateOrEditManpowerPlanningDetailDto
{
    public int? Id { get; set; }

    [Required]
    public int ManpowerPlanningId { get; set; }

    [Required]
    public int JobTitleId { get; set; }

    public int? HolderId { get; set; }

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
}
