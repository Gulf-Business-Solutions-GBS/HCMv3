using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.HealthAndSafetyManagement;

/// <summary>
/// Safety checklist item scoped to a <see cref="SafetyAreaAsset"/> (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.HealthAndSafetyManagement.SafetyChecklist</c>). Legacy
/// <c>PerformValidation</c> required Code (auto-assigned if blank, unique) and SafetyAreaAsset -- ported
/// as-is in <c>SafetyChecklistsAppService</c>. Extends the Code shape with a FK and extra fields, so it
/// can't derive from <see cref="SimpleLookupEntity"/> (same bespoke pattern as
/// <c>AccommodationManagement.Building</c>).
/// </summary>
public class SafetyChecklist : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    [StringLength(SimpleLookupConsts.MaxCodeLength)]
    public virtual string Code { get; set; }

    public virtual int SafetyAreaAssetId { get; set; }

    public virtual SafetyAreaAsset SafetyAreaAsset { get; set; }

    public virtual string Description { get; set; }

    public virtual bool IsActive { get; set; }
}
