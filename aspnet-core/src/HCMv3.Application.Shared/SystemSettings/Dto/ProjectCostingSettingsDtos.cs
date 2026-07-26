using Abp.Application.Services.Dto;

namespace HCMv3.SystemSettings.Dto;

public class ProjectCostingSettingsListDto : EntityDto
{
    public bool CalculateNetPayProjectWise { get; set; }
}

public class ProjectCostingSettingsEditDto : EntityDto
{
    public bool CalculateNetPayProjectWise { get; set; }
}
