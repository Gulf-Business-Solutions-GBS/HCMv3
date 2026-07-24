using System;
using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.PersonnelAdministration;

/// <summary>
/// Employee reward record (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.PersonnelAdministration.EmployeeReward</c>, Wave 4 deferred bucket).
///
/// <see cref="RewardType"/> is a plain string field in the legacy source (NOT a lookup FK) -- kept as a
/// plain string here, per instruction, rather than "improved" into a lookup FK.
/// </summary>
public class EmployeeReward : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int EmployeeId { get; set; }

    public virtual Employee Employee { get; set; }

    [StringLength(256)]
    public virtual string RewardType { get; set; }

    public virtual DateTime RewardDate { get; set; }

    [StringLength(256)]
    public virtual string Certificate { get; set; }

    public virtual string Comments { get; set; }
}
