using System.ComponentModel.DataAnnotations;

namespace HCMv3.PerformanceManagement.Dto;

public class CreateOrEditPARatingDto
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

    public bool IsCommentRequired { get; set; }

    public bool Status { get; set; }

    public int MaxLimit { get; set; }

    [StringLength(4000)]
    public string DescriptionEn { get; set; }

    [StringLength(4000)]
    public string DescriptionAr { get; set; }

    public double FromRange { get; set; }

    public double ToRange { get; set; }
}
