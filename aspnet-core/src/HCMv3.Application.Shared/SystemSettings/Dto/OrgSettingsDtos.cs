using Abp.Application.Services.Dto;

namespace HCMv3.SystemSettings.Dto;

public class OrgSettingsListDto : EntityDto
{
    public bool EnablePostingGroup { get; set; }

    public bool EnableBudgetingGroup { get; set; }

    public bool EnablePosition { get; set; }

    public bool EnableOrgChart3Levels { get; set; }
}

public class OrgSettingsEditDto : EntityDto
{
    public bool EnablePostingGroup { get; set; }

    public bool EnableBudgetingGroup { get; set; }

    public bool EnablePosition { get; set; }

    public bool EnableOrgChart3Levels { get; set; }
}
