using System;
using Abp.Application.Services.Dto;

namespace HCMv3.TimeManagement.Dto;

/// <summary>
/// Read/list DTO for <see cref="TimeLog"/>.
/// </summary>
public class TimeLogDto : EntityDto<int>
{
    public string Message { get; set; }

    public DateTime RunTimeStamp { get; set; }

    public int? EmployeeId { get; set; }

    public string EmployeeCode { get; set; }

    public string EmployeeFullNameEn { get; set; }

    public int Status { get; set; }

    public DateTime DayDate { get; set; }

    public bool StopFlag { get; set; }

    public int NoOfEmployee { get; set; }
}
