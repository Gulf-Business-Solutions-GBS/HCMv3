using System;
using Abp.Application.Services.Dto;

namespace HCMv3.TimeManagement.Dto;

/// <summary>
/// Read/list DTO for <see cref="RosterGroupDetail"/>.
/// </summary>
public class RosterGroupDetailDto : EntityDto<int>
{
    public int RosterGroupId { get; set; }

    public string RosterGroupName { get; set; }

    public int EmployeeId { get; set; }

    public string EmployeeName { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }
}
