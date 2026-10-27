using System.ComponentModel.DataAnnotations;

namespace HCMv3.PerformanceManagement.Dto;

public class CreateOrEditPAPlanDto
{
    public int? Id { get; set; }

    [Required]
    public int PAFormId { get; set; }

    [Required]
    public int KpiInfoId { get; set; }

    [Range(0, 100)]
    public double Weight { get; set; }

    public string Target { get; set; }

    public int? AppraiseeRatingId { get; set; }

    public int? RatingId { get; set; }

    [StringLength(500)]
    public string AppraiseeComments { get; set; }

    [StringLength(500)]
    public string AppraiserComments { get; set; }
}
