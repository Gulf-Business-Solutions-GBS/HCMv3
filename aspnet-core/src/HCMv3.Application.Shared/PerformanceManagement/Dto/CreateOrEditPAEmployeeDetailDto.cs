using System.ComponentModel.DataAnnotations;

namespace HCMv3.PerformanceManagement.Dto;

public class CreateOrEditPAEmployeeDetailDto
{
    public int? Id { get; set; }

    [Required]
    public int PAEmployeeId { get; set; }

    [Required]
    public int KpiInfoId { get; set; }

    [Range(0, 100)]
    public double Weight { get; set; }

    public string Target { get; set; }
}
