using System.ComponentModel.DataAnnotations;

namespace HCMv3.PerformanceManagement.Dto;

public class CreateOrEditKpiInfoDto
{
    public int? Id { get; set; }

    [StringLength(64)]
    public string Code { get; set; }

    [Required]
    public string NameEn { get; set; }

    [Required]
    public string NameAr { get; set; }

    [Required]
    public int KpiCategoryId { get; set; }

    [StringLength(500)]
    public string Comments { get; set; }

    public bool Active { get; set; }
}
