using System.ComponentModel.DataAnnotations;

namespace HCMv3.PerformanceManagement.Dto;

public class CreateOrEditPACycleDto
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

    public bool IsDefault { get; set; }
}
