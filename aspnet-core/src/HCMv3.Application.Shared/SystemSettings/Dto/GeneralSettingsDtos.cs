using Abp.Application.Services.Dto;

namespace HCMv3.SystemSettings.Dto;

public class ManageGeneralSettingsListDto : EntityDto
{
    public bool DisableAutoEmpcode { get; set; }

    public string DateEditMask { get; set; }

    public string DateDisplayFormat { get; set; }

    public string TimeEditMask { get; set; }

    public string TimeDisplayFormat { get; set; }

    public string StartupDashboard { get; set; }

    public bool EnableGridDataAccessModeServer { get; set; }

    public bool StartHangfire { get; set; }
}

public class ManageGeneralSettingsEditDto : EntityDto
{
    public bool DisableAutoEmpcode { get; set; }

    public string DateEditMask { get; set; }

    public string DateDisplayFormat { get; set; }

    public string TimeEditMask { get; set; }

    public string TimeDisplayFormat { get; set; }

    public string StartupDashboard { get; set; }

    public bool NoticePeriodDaysExcludingSubmissionDate { get; set; }

    public bool EnableGridDataAccessModeServer { get; set; }

    public bool StartHangfire { get; set; }

    public AutoFilterColumnCondition AutoFilterColumnCondition { get; set; }
}
