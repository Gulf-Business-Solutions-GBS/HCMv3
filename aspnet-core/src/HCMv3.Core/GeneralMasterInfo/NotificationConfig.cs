using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.SystemSecurity;

namespace HCMv3.GeneralMasterInfo;

/// <summary>
/// NotificationConfig (ported from legacy XAF <c>HCM10.Module.BusinessObjects.GeneralMasterInfo.NotificationConfig</c>).
/// No Code/NameEn/NameAr per legacy (just <see cref="Name"/>) plus notification-routing bool flags.
/// The aggregated <c>NotificationGroup</c> collection is ported separately as its own entity
/// (<see cref="NotificationGroup"/>).
/// </summary>
public class NotificationConfig : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    [Required]
    [StringLength(256)]
    public virtual string Name { get; set; }

    public virtual bool SendNotification { get; set; }

    public virtual bool SendEmail { get; set; }

    public virtual bool ToEmployee { get; set; }

    public virtual bool ToRequestCreator { get; set; }

    public virtual bool ToManager { get; set; }

    public virtual bool SendToAll { get; set; }

    public virtual bool SendToAllApproved { get; set; }

    public virtual bool SendToApprover { get; set; }

    /// <summary>
    /// What to send. Restored on 2026-09-22 (the first pass of this port left it out and made every caller
    /// pass a template of its own): a per-step workflow configuration has no other way to say which template
    /// belongs to it, since one step's approval mail and another's rejection mail differ only by the
    /// configuration attached to them. Optional — a configuration with no template sends the in-app
    /// notification only.
    /// </summary>
    public virtual int? EmailTemplateId { get; set; }

    /// <inheritdoc cref="EmailTemplateId"/>
    public virtual EmailTemplate EmailTemplate { get; set; }

    /// <summary>
    /// Extra recipient groups: everyone holding a given workflow role, optionally narrowed to the requester's
    /// own company/department/branch/cost centre.
    /// </summary>
    public virtual ICollection<NotificationGroup> NotificationGroups { get; set; }
}
