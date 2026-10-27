using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.PerformanceManagement;

/// <summary>
/// KPI category master (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.PerformanceManagement.KpiCategory</c>). Extends the Code/NameEn/NameAr
/// shape with a display <see cref="HexColor"/>, so it can't derive from <see cref="SimpleLookupEntity"/>.
/// </summary>
public class KpiCategory : FullAuditedEntity, IMustHaveTenant
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

    [StringLength(32)]
    public virtual string HexColor { get; set; }
}
