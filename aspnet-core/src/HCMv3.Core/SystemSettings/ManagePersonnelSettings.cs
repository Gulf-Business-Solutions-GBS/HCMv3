using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.SystemSettings;

/// <summary>
/// Tenant-wide personnel administration configuration (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.SysSettings.ManagePersonnelSettings</c>). Legacy <c>OnDeleting</c>
/// unconditionally throws ("You can't delete record. Please contact administrator.") -- enforced in
/// <c>ManagePersonnelSettingsAppService.DeletePersonnelSettings</c>, not here. Legacy
/// <c>PerformValidation</c> was empty (no real business rule to replicate).
/// </summary>
public class ManagePersonnelSettings : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual bool EnableSponsorGovtJobTitleQuota { get; set; }

    public virtual int MaxPhotoSize { get; set; }

    public virtual ProbationDaysCalculation ProbationDaysCalculation { get; set; }

    public virtual bool AutoWageAssignOnHire { get; set; }
}
