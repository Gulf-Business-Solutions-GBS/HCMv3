using System;
using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.AiAgent;

/// <summary>
/// Something the assistant has prepared for the user to confirm -- "apply for 10 days of annual leave from
/// 1 October" -- and that has NOT happened yet.
///
/// <para>
/// This is the whole of the assistant's write access. The model cannot submit, cancel or change anything:
/// the most a tool can do is create one of these in <see cref="AiPendingActionStatus.Pending"/>. The real
/// request only runs when the user presses Confirm, and it runs through the same application service the
/// ordinary screen uses, with the same validation and approval routing. A model that misunderstands, or
/// is manipulated by text planted in a data field, can at worst produce a draft the user reads and cancels.
/// </para>
///
/// <para>
/// <see cref="PayloadJson"/> is what will be SUBMITTED, validated and normalised by the tool when the draft
/// was made -- never re-read from the model at confirmation time. What the user saw on the card is exactly
/// what runs.
/// </para>
/// </summary>
public class AiPendingAction : FullAuditedEntity, IMustHaveTenant
{
    public const int MaxActionTypeLength = 50;
    public const int MaxResultMessageLength = 1000;

    public int TenantId { get; set; }

    public virtual int ConversationId { get; set; }

    public virtual AiConversation Conversation { get; set; }

    /// <summary>Checked on confirmation as well as on read: confirming is a write.</summary>
    public virtual long UserId { get; set; }

    /// <summary>Which handler runs it, e.g. <c>ApplyForLeave</c>.</summary>
    [Required]
    [StringLength(MaxActionTypeLength)]
    public virtual string ActionType { get; set; }

    [Required]
    public virtual string PayloadJson { get; set; }

    public virtual AiPendingActionStatus Status { get; set; }

    /// <summary>What happened when it was confirmed -- the success text or the reason it was refused.</summary>
    [StringLength(MaxResultMessageLength)]
    public virtual string ResultMessage { get; set; }

    public virtual DateTime? ResolvedTime { get; set; }
}
