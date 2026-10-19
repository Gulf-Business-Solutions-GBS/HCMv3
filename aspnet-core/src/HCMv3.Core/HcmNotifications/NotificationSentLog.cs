using System;
using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.SystemSecurity;

namespace HCMv3.HcmNotifications;

/// <summary>
/// One record a notification has been sent about, by one notification: what stops a notice from repeating.
///
/// <para>
/// Replaces legacy's single <c>NotificationSent</c> flag on the record itself. That flag was shared by every
/// Notification Master rule and every fixed notification reading the same table, so the first one to fire
/// stopped every other from ever firing about that record. A row here is keyed by WHICH notice was sent, so
/// two rules on the same table each fire once, and the source tables are not touched at all.
/// </para>
///
/// <para>
/// Also the "sent" history the Notification Master screen shows, and the bookkeeping the employee document
/// expiry notice needs for its repeat count and last-sent date (legacy kept those in three columns this port
/// never added to <c>EmployeeDocument</c>).
/// </para>
/// </summary>
public class NotificationSentLog : FullAuditedEntity<long>, IMustHaveTenant
{
    public int TenantId { get; set; }

    /// <summary>The Notification Master rule that sent it; null for the fixed daily notifications.</summary>
    public virtual int? NotificationMasterId { get; set; }

    public virtual NotificationMaster NotificationMaster { get; set; }

    /// <summary>The Zero notification definition it went out under (<c>HcmNotificationNames</c>).</summary>
    [Required]
    [StringLength(NotificationRuleConsts.MaxNotificationNameLength)]
    public virtual string NotificationName { get; set; }

    /// <summary>What kind of record <see cref="RecordId"/> is (<c>NotificationRuleSourceCodes</c>).</summary>
    [Required]
    [StringLength(NotificationRuleConsts.MaxSourceCodeLength)]
    public virtual string SourceCode { get; set; }

    /// <summary>The id of the record the notice was about.</summary>
    public virtual long RecordId { get; set; }

    /// <summary>The tenant's date the notice was sent (or marked) on.</summary>
    public virtual DateTime SentDate { get; set; }

    /// <summary>How many people it reached, for the history screen. Zero when nobody was addressed.</summary>
    public virtual int RecipientCount { get; set; }

    /// <summary>
    /// False when nothing was actually sent: the record was marked so it will not fire (the "mark existing as
    /// sent" action when a rule is switched on, or a legacy rule that closes a record without sending).
    /// Repeat counts only count rows where this is true.
    /// </summary>
    public virtual bool WasSent { get; set; }

    /// <summary>
    /// True when this notice is finished with this record and must never fire about it again -- legacy's
    /// <c>NotificationSent = true</c>. Every rule row is final; the employee document expiry notice writes
    /// non-final rows while its repeat count is still running.
    /// </summary>
    public virtual bool IsFinal { get; set; }
}
