using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.CompetencyDevelopment;

/// <summary>
/// Competency category/grouping with UI color coding (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.CompetencyDevelopment.CompetencyType</c>). Not derived from
/// <see cref="SimpleLookupEntity"/> because of the extra <see cref="BackColor"/>/<see cref="ForeColor"/>
/// fields.
/// </summary>
public class CompetencyType : FullAuditedEntity, IMustHaveTenant
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

    [StringLength(50)]
    public virtual string BackColor { get; set; }

    [StringLength(50)]
    public virtual string ForeColor { get; set; }
}
