using System;
using Abp.Application.Services.Dto;

namespace HCMv3.PayrollManagement.Dto;

/// <summary>
/// Read/list DTO for <see cref="EmployeeWageAccGroup"/>.
/// </summary>
public class EmployeeWageAccGroupDto : EntityDto<int>
{
    public int EmployeeId { get; set; }

    public string EmployeeName { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public int WageAccGroupId { get; set; }

    public string WageAccGroupName { get; set; }
}
