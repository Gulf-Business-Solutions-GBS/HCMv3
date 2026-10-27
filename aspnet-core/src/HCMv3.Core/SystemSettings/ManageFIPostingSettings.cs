using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.SystemSettings;

/// <summary>
/// Tenant-wide FI (Finance) posting configuration (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.SysSettings.ManageFIPostingSettings</c>). Legacy <c>OnDeleting</c>
/// unconditionally throws -- enforced in <c>ManageFIPostingSettingsAppService.DeleteFIPostingSettings</c>,
/// not here.
/// </summary>
public class ManageFIPostingSettings : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual bool SeparateProvisionPosting { get; set; }

    public virtual bool EnableAutoWageAccountEntry { get; set; }

    public virtual bool AllowDuplicateCodeInCostElement { get; set; }

    public virtual bool FiPostingAfterPayrollExit { get; set; }
}
