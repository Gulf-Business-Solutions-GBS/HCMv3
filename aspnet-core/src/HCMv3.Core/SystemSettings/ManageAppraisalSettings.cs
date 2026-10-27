using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.SystemSettings;

/// <summary>
/// Tenant-wide appraisal configuration (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.SysSettings.ManageAppraisalSettings</c>). Legacy <c>OnDeleting</c>
/// unconditionally throws ("You can't delete record. Please contact administrator.") -- enforced in
/// <c>ManageAppraisalSettingsAppService.DeleteAppraisalSettings</c>, not here. Legacy
/// <c>PerformValidation</c> was empty (no real business rule to replicate).
/// </summary>
public class ManageAppraisalSettings : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual AppraisalCycle AppraisalCycle { get; set; }

    public virtual bool EnablePAPeerReview { get; set; }

    public virtual int SetPeerMinLimit { get; set; }

    public virtual int SetPeerMaxLimit { get; set; }

    public virtual bool EnableMarksInPAReview { get; set; }

    public virtual MarksOn MarksOn { get; set; }
}
