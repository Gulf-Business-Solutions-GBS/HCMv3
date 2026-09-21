using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.Integrations;

/// <summary>
/// One attempt to reach an external system: either an administrator pressing "Test Connection" or a real
/// data exchange. This is what lets an Integration Center card say "last sync 06:00, 412 punches" or
/// "failed 2 hours ago" instead of only showing whether settings happen to be filled in.
///
/// Append-only. <c>CreationTime</c> is when the attempt happened.
/// </summary>
public class IntegrationConnectionLog : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    /// <summary>One of the <see cref="IntegrationKeys"/> constants.</summary>
    [Required]
    [StringLength(IntegrationSetting.MaxIntegrationKeyLength)]
    public virtual string IntegrationKey { get; set; }

    public virtual IntegrationAttemptType AttemptType { get; set; }

    public virtual bool Succeeded { get; set; }

    /// <summary>
    /// Outcome detail shown on the card and in the history list. Truncated on write rather than allowed
    /// to overflow, because a failing external system can return a very long error body.
    /// </summary>
    [StringLength(MaxMessageLength)]
    public virtual string Message { get; set; }

    /// <summary>Rows exchanged, when the attempt was a sync. Null for connection tests.</summary>
    public virtual int? ItemCount { get; set; }

    public virtual int DurationMs { get; set; }

    public const int MaxMessageLength = 2000;
}
