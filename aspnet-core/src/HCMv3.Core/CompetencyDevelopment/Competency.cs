using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.CompetencyDevelopment;

/// <summary>
/// A single competency an employee can be assessed against (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.CompetencyDevelopment.Competency</c>).
///
/// Note: <c>HCMv3.PerformanceManagement.KpiInfo</c> (already-ported PerformanceManagement module)
/// deliberately DROPPED its legacy FK to this business object since CompetencyDevelopment did not exist yet
/// in HCMv3 at the time -- see the remarks on <c>KpiInfo</c>. Retrofitting that link is out of scope for this
/// dispatch.
/// </summary>
public class Competency : FullAuditedEntity, IMustHaveTenant
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

    public virtual int CompetencyTypeId { get; set; }

    public virtual CompetencyType CompetencyType { get; set; }

    [StringLength(500)]
    public virtual string Comments { get; set; }

    public virtual bool Active { get; set; }
}
