using System;
using Abp.Application.Services.Dto;

namespace HCMv3.ProjectManagement.Dto;

/// <summary>
/// Read/list DTO for <see cref="EmployeeProjectDetail"/>.
/// </summary>
public class EmployeeProjectDetailDto : EntityDto<int>
{
    public int EmployeeProjectId { get; set; }

    public int ProjectId { get; set; }

    public string ProjectNameEn { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public double Percentage { get; set; }
}
