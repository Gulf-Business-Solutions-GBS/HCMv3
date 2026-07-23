using System;
using Abp.Application.Services.Dto;

namespace HCMv3.TimeManagement.Dto;

/// <summary>
/// Read/list DTO for <see cref="EmployeeTimesheet"/>.
/// </summary>
public class EmployeeTimesheetDto : EntityDto<int>
{
    public int EmployeeId { get; set; }

    public string EmployeeCode { get; set; }

    public string EmployeeFullNameEn { get; set; }

    public DateTime TransDate { get; set; }

    public TimeSpan ActualStartTime { get; set; }

    public TimeSpan BreakStartTime { get; set; }

    public TimeSpan BreakEndTime { get; set; }

    public TimeSpan ActualEndTime { get; set; }

    public double TotalHours { get; set; }

    public int Approved { get; set; }

    public string Comments { get; set; }
}
