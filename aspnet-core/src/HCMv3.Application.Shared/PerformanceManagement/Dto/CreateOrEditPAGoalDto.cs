using System.ComponentModel.DataAnnotations;

namespace HCMv3.PerformanceManagement.Dto;

public class CreateOrEditPAGoalDto
{
    public int? Id { get; set; }

    [Required]
    public int PAFormId { get; set; }

    [StringLength(2000)]
    public string Goal { get; set; }
}
