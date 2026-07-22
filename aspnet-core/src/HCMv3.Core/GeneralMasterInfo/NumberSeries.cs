using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.GeneralMasterInfo;

/// <summary>
/// NumberSeries (ported from legacy XAF <c>HCM10.Module.BusinessObjects.GeneralMasterInfo.NumberSeries</c>).
/// Pure Code/Name/Start/Prefix/Formatting record -- no NameEn/NameAr (confirmed against the legacy source).
/// DEFERRED (not ported): the legacy <c>EmploymentType</c> FK/enum (tied to modules not yet ported), along
/// with the duplicate-EmploymentType validation rules that depended on it.
/// </summary>
public class NumberSeries : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    [Required]
    [StringLength(SimpleLookupConsts.MaxCodeLength)]
    public virtual string Code { get; set; }

    [StringLength(SimpleLookupConsts.MaxNameLength)]
    public virtual string Name { get; set; }

    public virtual long Start { get; set; }

    [StringLength(64)]
    public virtual string Prefix { get; set; }

    [Required]
    [StringLength(64)]
    public virtual string Formatting { get; set; }
}
