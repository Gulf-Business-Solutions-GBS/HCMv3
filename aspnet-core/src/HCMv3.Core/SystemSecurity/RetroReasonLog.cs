using System;
using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.PersonnelAdministration;

namespace HCMv3.SystemSecurity;

/// <summary>
/// Audit log of retroactive-change reasons entered against an arbitrary target entity (ported from legacy
/// XAF HCM10.Module.BusinessObjects.SysSecurity.RetroReasonLog). EntityObjectName/EntityObjectId are a
/// loose polymorphic reference (legacy pattern, no FK constraint), same as the source.
/// </summary>
public class RetroReasonLog : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int EmployeeId { get; set; }

    public virtual Employee Employee { get; set; }

    public virtual DateTime ChangedOn { get; set; }

    public virtual DateTime RetroDate { get; set; }

    [StringLength(256)]
    public virtual string EntityObjectName { get; set; }

    public virtual int EntityObjectId { get; set; }

    [StringLength(4000)]
    public virtual string RetroReason { get; set; }
}
