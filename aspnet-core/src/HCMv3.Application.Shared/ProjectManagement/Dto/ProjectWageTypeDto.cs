using Abp.Application.Services.Dto;

namespace HCMv3.ProjectManagement.Dto;

/// <summary>
/// Read/list DTO for <see cref="ProjectWageType"/>.
/// </summary>
public class ProjectWageTypeDto : EntityDto<int>
{
    public int ProjectId { get; set; }

    public string ProjectNameEn { get; set; }

    public int WageTypeId { get; set; }

    public string WageTypeNameEn { get; set; }
}
