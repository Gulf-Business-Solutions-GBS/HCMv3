using System;
using Abp.Application.Services.Dto;

namespace HCMv3.ProjectManagement.Dto;

/// <summary>
/// Read/list DTO for <see cref="ProjectLog"/>.
/// </summary>
public class ProjectLogDto : EntityDto<int>
{
    public string Message { get; set; }

    public DateTime RunTimeStamp { get; set; }

    public int EmployeeId { get; set; }

    public string EmployeeName { get; set; }

    public int Status { get; set; }

    public DateTime InPeriod { get; set; }

    public bool StopFlag { get; set; }

    public int NoOfEmployee { get; set; }
}
