using Abp.Application.Services.Dto;

namespace HCMv3.SystemSettings.Dto;

public class ManageSecuritySettingsListDto : EntityDto
{
    public bool EnableRowLevelSecurity { get; set; }

    public bool EnableAuditLogInTimeEngine { get; set; }

    public bool EnableAuditLogInPayrollEngine { get; set; }

    public bool EnableAuditLogInProjectCostEngine { get; set; }

    public bool EnableAuditLogInCostEngine { get; set; }

    public bool EnableAuditLogInGridReport { get; set; }

    public bool EnableAuditLogInPrintReport { get; set; }

    public bool EnableReportExportPwd { get; set; }
}

public class ManageSecuritySettingsEditDto : EntityDto
{
    public bool EnableRowLevelSecurity { get; set; }

    public bool EnableAuditLogInTimeEngine { get; set; }

    public bool EnableAuditLogInPayrollEngine { get; set; }

    public bool EnableAuditLogInProjectCostEngine { get; set; }

    public bool EnableAuditLogInCostEngine { get; set; }

    public bool EnableAuditLogInGridReport { get; set; }

    public bool EnableAuditLogInPrintReport { get; set; }

    public bool EnableReportExportPwd { get; set; }
}
