using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.GovtRelationsManagement;

/// <summary>
/// Authorized signatory master (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.GovtRelationsManagement.Signatory</c>). Extends the Code/NameEn/NameAr
/// shape with a <see cref="CivilId"/>, so it can't derive from <see cref="SimpleLookupEntity"/>.
///
/// NOTE: the legacy <c>NameAr</c> property was a broken auto-property (<c>get; set;</c> only, never
/// actually wired to XPO's <c>SetPropertyValue</c>/persistence plumbing like every other field on this
/// class) -- effectively a bug where NameAr was never really persisted. Ported here as a normal, correctly
/// persisted string field; the legacy bug is not replicated.
/// </summary>
public class Signatory : FullAuditedEntity, IMustHaveTenant
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

    public virtual long CivilId { get; set; }
}
