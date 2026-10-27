using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.SystemSettings;

/// <summary>
/// Tenant-wide security/audit configuration (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.SysSettings.ManageSecuritySettings</c>). Legacy <c>OnDeleting</c>
/// unconditionally throws -- enforced in <c>ManageSecuritySettingsAppService.DeleteSecuritySettings</c>, not
/// here. Legacy <c>PerformValidation</c> was empty (no real business rule to replicate).
/// </summary>
public class ManageSecuritySettings : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual bool EnableRowLevelSecurity { get; set; }

    public virtual bool EnableAuditLogInTimeEngine { get; set; }

    public virtual bool EnableAuditLogInPayrollEngine { get; set; }

    public virtual bool EnableAuditLogInProjectCostEngine { get; set; }

    public virtual bool EnableAuditLogInCostEngine { get; set; }

    public virtual bool EnableAuditLogInGridReport { get; set; }

    public virtual bool EnableAuditLogInPrintReport { get; set; }

    public virtual bool EnableReportExportPwd { get; set; }
}
