using System.ComponentModel.DataAnnotations;

namespace HCMv3.PerformanceManagement.Dto;

public class CreateOrEditKpiByJobTitleDetailDto
{
    public int? Id { get; set; }

    [Required]
    public int KpiByJobTitleId { get; set; }

    [Required]
    public int KpiInfoId { get; set; }

    [Range(1, 100)]
    public double Weight { get; set; }

    public string Target { get; set; }
}
