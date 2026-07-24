using System.ComponentModel.DataAnnotations;

namespace HCMv3.PerformanceManagement.Dto;

public class CreateOrEditKpiCategoryDto
{
    public int? Id { get; set; }

    [StringLength(64)]
    public string Code { get; set; }

    [Required]
    [StringLength(256)]
    public string NameEn { get; set; }

    [Required]
    [StringLength(256)]
    public string NameAr { get; set; }

    [StringLength(32)]
    public string HexColor { get; set; }
}
