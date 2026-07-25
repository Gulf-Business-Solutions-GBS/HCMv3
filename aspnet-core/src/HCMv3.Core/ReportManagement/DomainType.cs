using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.ReportManagement;

/// <summary>
/// Report-builder domain/entity-type lookup (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.ReportManagement.DomainType</c>). Unlike the pure bilingual lookups in
/// GeneralMasterInfo, the legacy object has no <c>NameAr</c> field, so it is NOT derived from
/// <see cref="SimpleLookupEntity"/>.
/// </summary>
public class DomainType : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    [StringLength(SimpleLookupConsts.MaxCodeLength)]
    public virtual string Code { get; set; }

    [Required]
    [StringLength(SimpleLookupConsts.MaxNameLength)]
    public virtual string NameEn { get; set; }

    /// <summary>Fully-qualified legacy business-object type name this domain represents.</summary>
    [StringLength(512)]
    public virtual string EntityType { get; set; }
}
