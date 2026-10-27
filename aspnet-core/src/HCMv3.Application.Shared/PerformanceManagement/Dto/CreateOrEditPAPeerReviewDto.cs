using System.ComponentModel.DataAnnotations;

namespace HCMv3.PerformanceManagement.Dto;

public class CreateOrEditPAPeerReviewDto
{
    public int? Id { get; set; }

    [Required]
    public int PAFormId { get; set; }

    [Required]
    public int PeerId { get; set; }

    [Required]
    public int KpiInfoId { get; set; }

    public int? RatingId { get; set; }

    [StringLength(500)]
    public string Comments { get; set; }
}
