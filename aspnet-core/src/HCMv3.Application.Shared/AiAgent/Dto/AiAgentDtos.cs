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
}
