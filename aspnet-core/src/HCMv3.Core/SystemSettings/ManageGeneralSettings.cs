using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.SystemSettings;

/// <summary>
/// Tenant-wide general/display configuration (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.SysSettings.ManageGeneralSettings</c>). Legacy <c>OnDeleting</c>
/// unconditionally throws -- enforced in <c>ManageGeneralSettingsAppService.DeleteGeneralSettings</c>, not
/// here.
/// </summary>
public class ManageGeneralSettings : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual bool DisableAutoEmpcode { get; set; }

    public virtual string DateEditMask { get; set; }

    public virtual string DateDisplayFormat { get; set; }

    public virtual string TimeEditMask { get; set; }

    public virtual string TimeDisplayFormat { get; set; }

    public virtual string StartupDashboard { get; set; }

    public virtual bool NoticePeriodDaysExcludingSubmissionDate { get; set; }

    public virtual bool EnableGridDataAccessModeServer { get; set; }

    public virtual bool StartHangfire { get; set; }

    public virtual AutoFilterColumnCondition AutoFilterColumnCondition { get; set; }
}
