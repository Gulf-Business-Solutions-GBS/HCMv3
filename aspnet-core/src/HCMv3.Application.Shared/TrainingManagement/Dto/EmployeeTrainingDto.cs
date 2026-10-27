using Abp.Application.Services.Dto;

namespace HCMv3.TrainingManagement.Dto;

/// <summary>
/// Read/list DTO for <see cref="EmployeeTraining"/>.
/// </summary>
public class EmployeeTrainingDto : EntityDto<int>
{
    public int EmployeeId { get; set; }

    public string EmployeeCode { get; set; }

    public int TrainingCourseId { get; set; }

    public string TrainingCourseNameEn { get; set; }

    public int TrainingScheduleId { get; set; }

    public string TrainingScheduleNameEn { get; set; }

    public TrainingCompletionStatus CompletionStatus { get; set; }

    public double Score { get; set; }

    public string Comments { get; set; }

    public int? EmployeeCertificationId { get; set; }

    public string EmployeeCertificationName { get; set; }
}
