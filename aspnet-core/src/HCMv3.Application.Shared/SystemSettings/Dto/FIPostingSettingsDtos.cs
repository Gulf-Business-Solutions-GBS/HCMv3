using Abp.Application.Services.Dto;

namespace HCMv3.SystemSettings.Dto;

public class FIPostingSettingsListDto : EntityDto
{
    public bool SeparateProvisionPosting { get; set; }

    public bool EnableAutoWageAccountEntry { get; set; }

    public bool AllowDuplicateCodeInCostElement { get; set; }

    public bool FiPostingAfterPayrollExit { get; set; }
}

public class FIPostingSettingsEditDto : EntityDto
{
    public bool SeparateProvisionPosting { get; set; }

    public bool EnableAutoWageAccountEntry { get; set; }

    public bool AllowDuplicateCodeInCostElement { get; set; }

    public bool FiPostingAfterPayrollExit { get; set; }
}
