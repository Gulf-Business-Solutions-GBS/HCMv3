using System.ComponentModel.DataAnnotations;

namespace HCMv3.TrainingManagement.Dto;

/// <summary>
/// Create/Update input DTO for <see cref="EmployeeTraining"/>. <c>Id == null</c> means create; otherwise
/// update.
/// </summary>
public class CreateOrEditEmployeeTrainingDto
{
    public int? Id { get; set; }

    [Required]
    public int EmployeeId { get; set; }

    [Required]
    public int TrainingCourseId { get; set; }

    [Required]
    public int TrainingScheduleId { get; set; }

    public TrainingCompletionStatus CompletionStatus { get; set; }

    public double Score { get; set; }

    [StringLength(500)]
    public string Comments { get; set; }
}
