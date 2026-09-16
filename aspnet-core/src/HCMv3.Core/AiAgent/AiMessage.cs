using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.AiAgent;

/// <summary>
/// One entry in an <see cref="AiConversation"/>.
///
/// <para>
/// Every step is stored, not only what the user saw: the model's tool calls with their exact arguments, and
/// exactly what each tool returned. That is the audit trail for "why did the assistant tell me that" -- and for
/// the harder question of what employee data was put in front of the model. Only
/// <see cref="AiMessageRole.User"/> messages and assistant messages with <see cref="Content"/> are ever sent
/// back to the browser.
/// </para>
/// </summary>
public class AiMessage : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int ConversationId { get; set; }

    public virtual AiConversation Conversation { get; set; }

    public virtual AiMessageRole Role { get; set; }

    /// <summary>
    /// The text. For <see cref="AiMessageRole.Tool"/> this is the JSON the tool returned. Null on an assistant
    /// message that only called tools.
    /// </summary>
    public virtual string Content { get; set; }

    /// <summary>On an assistant message: the tool calls it made, as the model sent them.</summary>
    public virtual string ToolCallsJson { get; set; }

    /// <summary>On a tool message: which of the assistant's calls this answers.</summary>
    [StringLength(AiAgentConsts.MaxToolNameLength)]
    public virtual string ToolCallId { get; set; }

    /// <summary>On a tool message: which tool ran. Denormalised so the audit trail reads without parsing JSON.</summary>
    [StringLength(AiAgentConsts.MaxToolNameLength)]
    public virtual string ToolName { get; set; }

    /// <summary>Set when this reply offered the user an action to confirm, so the card renders beside it.</summary>
    public virtual int? PendingActionId { get; set; }
}
