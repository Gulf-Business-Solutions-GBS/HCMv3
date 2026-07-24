using System.ComponentModel.DataAnnotations;

namespace HCMv3.PerformanceManagement.Dto;

public class CreateOrEditPAPeriodDto
{
    public int? Id { get; set; }

    [Required]
    [StringLength(256)]
    public string Name { get; set; }

    public bool Closed { get; set; }
}
