using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.SystemSettings;

/// <summary>
/// Tenant-wide organization-management configuration (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.SysSettings.ManageOrgSettings</c>). Legacy <c>OnDeleting</c>
/// unconditionally throws -- enforced in <c>ManageOrgSettingsAppService.DeleteOrgSettings</c>, not here.
/// </summary>
public class ManageOrgSettings : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual bool EnablePostingGroup { get; set; }

    public virtual bool EnableBudgetingGroup { get; set; }

    public virtual bool EnablePosition { get; set; }

    public virtual bool EnableOrgChart3Levels { get; set; }
}
