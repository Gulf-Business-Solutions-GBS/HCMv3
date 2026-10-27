using System;
using Abp.Application.Services.Dto;

namespace HCMv3.TimeManagement.Dto;

/// <summary>
/// Read/list DTO for <see cref="EmployeeDutySchedule"/>.
/// </summary>
public class EmployeeDutyScheduleDto : EntityDto<int>
{
    public int EmployeeId { get; set; }

    public string EmployeeCode { get; set; }

    public string EmployeeFullNameEn { get; set; }

    public int RosterDutyScheduleId { get; set; }

    public string RosterDutyScheduleName { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }
}
