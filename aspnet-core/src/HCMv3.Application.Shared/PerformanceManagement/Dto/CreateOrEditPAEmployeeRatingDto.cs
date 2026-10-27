using System.ComponentModel.DataAnnotations;

namespace HCMv3.PerformanceManagement.Dto;

public class CreateOrEditPAEmployeeRatingDto
{
    public int? Id { get; set; }

    [Required]
    public int PAEmployeeId { get; set; }

    [Required]
    public int PAEmployeeReviewerId { get; set; }

    [Required]
    public int PAEmployeeDetailId { get; set; }

    public double Marks { get; set; }

    public int? RatingId { get; set; }

    [StringLength(4000)]
    public string Comments { get; set; }
}
