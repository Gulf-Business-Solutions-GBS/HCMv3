using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.SystemSettings;

/// <summary>
/// Tenant-wide project costing configuration (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.SysSettings.ManageProjectCostingSettings</c>). Legacy <c>OnDeleting</c>
/// unconditionally throws -- enforced in <c>ManageProjectCostingSettingsAppService.DeleteProjectCostingSettings</c>,
/// not here. Legacy <c>PerformValidation</c> was empty (no real business rule to replicate).
/// </summary>
public class ManageProjectCostingSettings : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual bool CalculateNetPayProjectWise { get; set; }
}
