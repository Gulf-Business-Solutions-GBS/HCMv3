using System.ComponentModel.DataAnnotations;

namespace HCMv3.PerformanceManagement.Dto;

public class CreateOrEditFormTypeDto
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

    public int MinReviewerLimit { get; set; }

    public int MaxReviewerLimit { get; set; }
}
