using System.ComponentModel.DataAnnotations;

namespace HCMv3.PerformanceManagement.Dto;

public class CreateOrEditPAEmployeeDto
{
    public int? Id { get; set; }

    [Required]
    public int EmployeeId { get; set; }

    [Required]
    public int PACycleId { get; set; }

    [Required]
    public int FormTypeId { get; set; }

    [StringLength(1000)]
    public string RequestComments { get; set; }

    [StringLength(1000)]
    public string WorkflowComments { get; set; }

    [StringLength(256)]
    public string ProposedIncrement { get; set; }

    [StringLength(256)]
    public string ProposedJobTitle { get; set; }

    public string AreaOfImprovement { get; set; }
}
