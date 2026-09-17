using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.AiAgent;

/// <summary>
/// One chat thread between a user and the AI assistant.
///
/// <para>
/// Owned by a USER, not an employee. The assistant is reached through a login, and a conversation must stay
/// private to the login that had it even if that login is later re-linked to a different employee record.
/// Every read filters on <see cref="UserId"/> on top of the tenant filter; nothing -- not even a tenant
/// administrator -- reads another user's conversations through the assistant's API.
/// </para>
/// </summary>
public class AiConversation : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual long UserId { get; set; }

    /// <summary>Taken from the first message, so the conversation list reads like the questions asked.</summary>
    [Required]
    [StringLength(AiAgentConsts.MaxTitleLength)]
    public virtual string Title { get; set; }

    /// <summary>Drives the conversation list's order -- most recently used first.</summary>
    public virtual DateTime LastMessageTime { get; set; }

    public virtual ICollection<AiMessage> Messages { get; set; }
}
