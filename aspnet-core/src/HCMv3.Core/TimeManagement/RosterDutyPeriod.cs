using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.TimeManagement;

/// <summary>
/// Roster subsystem's header, parallel to <see cref="DutyPeriod"/> but for the Roster subsystem (ported
/// from legacy XAF <c>HCM10.Module.BusinessObjects.TimeManagement.RosterDutyPeriod</c>). Its child is
/// <see cref="RosterDutyPeriodDetail"/>, listed separately (flat parent+child pattern) and linked back via
/// FK, rather than as a nested-list DTO.
/// </summary>
public class RosterDutyPeriod : FullAuditedEntity, IMustHaveTenant
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
