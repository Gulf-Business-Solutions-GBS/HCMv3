using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.GeneralMasterInfo;

/// <summary>
/// PersonalActionReason master (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.GeneralMasterInfo.PersonalActionReason</c>).
/// Extends the Code/NameEn/NameAr shape with a <see cref="PersonalAction"/> FK, so it can't derive from
/// <see cref="SimpleLookupEntity"/> (see <see cref="AccessUnit"/> for the same bespoke pattern).
/// Legacy <c>NotificationConfig</c> FK is <see cref="NotificationConfigId"/>: the email sent when a personnel
/// action with this reason is approved through workflow (legacy <c>EmployeeActionRequest</c>, see
/// <c>EmployeeActionWorkflowTransactionHandler.OnApprovedAsync</c>).
/// </summary>
public class PersonalActionReason : FullAuditedEntity, IMustHaveTenant
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

    public virtual int? PersonalActionId { get; set; }

    public virtual PersonalAction PersonalAction { get; set; }

    /// <summary>
    /// Legacy <c>PersonalActionReason.NotificationConfig</c>. Optional; when set, approving a personnel action with
    /// this reason emails the employee (and whoever else the configuration addresses) using its template.
    /// </summary>
    public virtual int? NotificationConfigId { get; set; }

    public virtual NotificationConfig NotificationConfig { get; set; }
}
