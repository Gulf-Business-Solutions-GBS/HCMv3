using System;
using Abp.Application.Services.Dto;

namespace HCMv3.TrainingManagement.Dto;

/// <summary>
/// Read/list DTO for <see cref="TrainingSchedule"/>.
/// </summary>
public class TrainingScheduleDto : EntityDto<int>
{
    public string Code { get; set; }

    public string NameEn { get; set; }

    public string NameAr { get; set; }

    public int TrainingCourseId { get; set; }

    public string TrainingCourseNameEn { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }
}
