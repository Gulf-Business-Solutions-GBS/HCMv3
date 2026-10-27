using System;
using Abp.Application.Services.Dto;

namespace HCMv3.TimeManagement.Dto;

/// <summary>
/// Read/list DTO for <see cref="EmployeeDutyPeriod"/>.
/// </summary>
public class EmployeeDutyPeriodDto : EntityDto<int>
{
    public int EmployeeId { get; set; }

    public string EmployeeCode { get; set; }

    public string EmployeeFullNameEn { get; set; }

    public int DutyPeriodId { get; set; }

    public string DutyPeriodName { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }
}
