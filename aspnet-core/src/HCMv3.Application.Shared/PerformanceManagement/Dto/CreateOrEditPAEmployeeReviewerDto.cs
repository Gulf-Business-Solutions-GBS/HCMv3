using System.ComponentModel.DataAnnotations;

namespace HCMv3.PerformanceManagement.Dto;

public class CreateOrEditPAEmployeeReviewerDto
{
    public int? Id { get; set; }

    [Required]
    public int PAEmployeeId { get; set; }

    [Required]
    public int EmployeeId { get; set; }

    public bool IsPeer { get; set; }

    public int? PeerAddedById { get; set; }

    [StringLength(4000)]
    public string CustomTextField1 { get; set; }

    [StringLength(4000)]
    public string CustomTextField2 { get; set; }

    [StringLength(4000)]
    public string CustomTextField3 { get; set; }
}
