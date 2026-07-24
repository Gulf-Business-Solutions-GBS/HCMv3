using System.ComponentModel.DataAnnotations;

namespace HCMv3.PerformanceManagement.Dto;

public class CreateOrEditPAPeerNomineeDto
{
    public int? Id { get; set; }

    [Required]
    public int PAPeriodId { get; set; }

    [Required]
    public int EmployeeId { get; set; }

    [Required]
    public int PeerId { get; set; }

    public bool Submitted { get; set; }
}
