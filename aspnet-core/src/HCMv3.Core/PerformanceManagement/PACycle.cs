using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.PerformanceManagement;

/// <summary>
/// Appraisal cycle master for the newer <see cref="PAEmployee"/> model (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.PerformanceManagement.PACycle</c>). Extends the Code/NameEn/NameAr shape
/// with <see cref="IsDefault"/>, so it can't derive from <see cref="SimpleLookupEntity"/>.
/// </summary>
public class PACycle : FullAuditedEntity, IMustHaveTenant
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

    /// <summary>Marks this cycle as the default selection.</summary>
    public virtual bool IsDefault { get; set; }
}
