using System;
using Abp.Application.Services.Dto;

namespace HCMv3.TrainingManagement.Dto;

/// <summary>
/// Read/list DTO for <see cref="TrainingAttendance"/>.
/// </summary>
public class TrainingAttendanceDto : EntityDto<int>
{
    public int TrainingScheduleId { get; set; }

    public string TrainingScheduleNameEn { get; set; }

    public int EmployeeId { get; set; }

    public string EmployeeCode { get; set; }

    public DateTime DayDate { get; set; }

    public TrainingAttendanceStatus Attendance { get; set; }
}
