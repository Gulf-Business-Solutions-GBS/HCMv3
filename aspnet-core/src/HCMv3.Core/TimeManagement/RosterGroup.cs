using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.TimeManagement;

/// <summary>
/// Roster Group header (ported from legacy XAF <c>HCM10.Module.BusinessObjects.TimeManagement.RosterGroup</c>).
/// Its child is <see cref="RosterGroupDetail"/>, listed separately (flat parent+child pattern) and linked
/// back via FK, rather than as a nested-list DTO.
/// </summary>
public class RosterGroup : FullAuditedEntity, IMustHaveTenant
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
}
