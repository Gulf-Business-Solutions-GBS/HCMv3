using System;
using Abp.Application.Services.Dto;

namespace HCMv3.ProjectManagement.Dto;

/// <summary>
/// Read/list DTO for <see cref="Project"/>.
/// </summary>
public class ProjectDto : EntityDto<int>
{
    public string Code { get; set; }

    public string NameEn { get; set; }

    public string NameAr { get; set; }

    public int ProjectTypeId { get; set; }

    public string ProjectTypeNameEn { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public string Comments { get; set; }
}
