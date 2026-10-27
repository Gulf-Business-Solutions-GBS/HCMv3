using System.ComponentModel.DataAnnotations;

namespace HCMv3.CompetencyDevelopment.Dto;

/// <summary>Create/Update input DTO for <see cref="CDForm"/>. <c>Id == null</c> means create.</summary>
public class CreateOrEditCDFormDto
{
    public int? Id { get; set; }

    [Required]
    public int CDPeriodId { get; set; }

    [Required]
    public int EmployeeId { get; set; }

    public double OverallScore { get; set; }

    public double ModeratedScore { get; set; }

    public bool EmployeeSignedOff { get; set; }

    public bool ManagerSignedOff { get; set; }
}
