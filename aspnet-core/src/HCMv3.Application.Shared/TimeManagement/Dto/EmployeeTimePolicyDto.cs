using System;
using Abp.Application.Services.Dto;

namespace HCMv3.TimeManagement.Dto;

/// <summary>
/// Read/list DTO for <see cref="EmployeeTimePolicy"/>.
/// </summary>
public class EmployeeTimePolicyDto : EntityDto<int>
{
    public int EmployeeId { get; set; }

    public string EmployeeCode { get; set; }

    public string EmployeeFullNameEn { get; set; }

    public int TimePolicyId { get; set; }

    public string TimePolicyName { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }
}
