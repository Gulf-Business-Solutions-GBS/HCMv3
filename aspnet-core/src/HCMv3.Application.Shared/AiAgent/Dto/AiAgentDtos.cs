using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Abp.Application.Services.Dto;

namespace HCMv3.AiAgent.Dto;

public class SendAiMessageInput
{
    /// <summary>Null starts a new conversation.</summary>
    public int? ConversationId { get; set; }

    [Required]
    [StringLength(AiAgentConsts.MaxUserMessageLength)]
    public string Message { get; set; }
}

public class SendAiMessageOutput
{
    public int ConversationId { get; set; }

    public string ConversationTitle { get; set; }

    public AiMessageDto UserMessage { get; set; }

    public AiMessageDto AssistantMessage { get; set; }
}

/// <summary>A message as the page shows it. Tool calls and tool results are never sent to the browser.</summary>
public class AiMessageDto : EntityDto
{
    public AiMessageRole Role { get; set; }

    public string Content { get; set; }

    public DateTime CreationTime { get; set; }

    /// <summary>The confirmation card shown under this reply, if the assistant prepared something.</summary>
    public AiPendingActionDto PendingAction { get; set; }
}

/// <summary>
/// Something prepared for the user to confirm, as the card shows it. Title and details are already in the
/// user's language; the page renders them as given.
/// </summary>
public class AiPendingActionDto : EntityDto
{
    public string ActionType { get; set; }

    public AiPendingActionStatus Status { get; set; }

    public string Title { get; set; }

    public List<AiActionDetailDto> Details { get; set; } = new();

    /// <summary>What happened once confirmed, or why it was refused.</summary>
    public string ResultMessage { get; set; }
}

public class AiActionDetailDto
{
    public string Label { get; set; }

    public string Value { get; set; }
}

/// <summary>The outcome of pressing Confirm or Cancel: the card's new state, and the line added to the chat.</summary>
public class AiActionResultDto
{
    public AiPendingActionDto Action { get; set; }

    public AiMessageDto Message { get; set; }
}

public class AiConversationListDto : EntityDto
{
    public string Title { get; set; }

    public DateTime LastMessageTime { get; set; }
}

public class AiConversationDto : EntityDto
{
    public string Title { get; set; }

    public List<AiMessageDto> Messages { get; set; } = new();
}

/// <summary>What the page needs before the first message, so it can greet the user and suggest questions
/// that will actually work for them.</summary>
public class AiAgentStartupDto
{
    /// <summary>
    /// False for a login with no employee record -- including the tenant admin, deliberately. The page says so
    /// up front instead of letting every question fail.
    /// </summary>
    public bool IsLinkedToEmployee { get; set; }

    public string FirstNameEn { get; set; }

    public string FirstNameAr { get; set; }

    public bool CanUseLeave { get; set; }

    public bool CanViewTeam { get; set; }

    /// <summary>Holds a pay administration permission the assistant's pay tools use, so the page can suggest them.</summary>
    public bool CanManagePay { get; set; }
}
