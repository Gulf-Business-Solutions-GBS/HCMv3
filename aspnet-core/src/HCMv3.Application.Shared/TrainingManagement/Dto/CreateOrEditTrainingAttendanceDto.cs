using System;
using System.ComponentModel.DataAnnotations;

namespace HCMv3.TrainingManagement.Dto;

/// <summary>
/// Create/Update input DTO for <see cref="TrainingAttendance"/>. <c>Id == null</c> means create; otherwise
/// update.
/// </summary>
public class CreateOrEditTrainingAttendanceDto
{
    public int? Id { get; set; }

    [Required]
    public int TrainingScheduleId { get; set; }

    [Required]
    public int EmployeeId { get; set; }

    [Required]
    public DateTime DayDate { get; set; }

    public TrainingAttendanceStatus Attendance { get; set; }
}
