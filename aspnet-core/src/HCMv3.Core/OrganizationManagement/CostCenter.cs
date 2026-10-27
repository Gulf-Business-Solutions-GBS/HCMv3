using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.OrganizationManagement;

/// <summary>
/// Cost center master (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.OrganizationManagement.CostCenter</c>).
/// Extends the Code/NameEn/NameAr shape with a <see cref="CostCenterGroup"/> FK, so it can't derive
/// from <see cref="SimpleLookupEntity"/> (see <see cref="City"/> for the same bespoke pattern).
/// </summary>
public class CostCenter : FullAuditedEntity, IMustHaveTenant, IBilingualLookup
{
    public int TenantId { get; set; }

    [StringLength(SimpleLookupConsts.MaxCodeLength)]
    public virtual string Code { get; set; }

    [Required]
    [StringLength(SimpleLookupConsts.MaxNameLength)]
    public virtual string NameEn { get; set; }

    [Required]
    [StringLength(SimpleLookupConsts.MaxNameLength)]
    public virtual string NameAr { get; set; }

    public virtual int? CostCenterGroupId { get; set; }

    public virtual CostCenterGroup CostCenterGroup { get; set; }
}
