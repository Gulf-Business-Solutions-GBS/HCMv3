using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.AiAgent.Dto;

namespace HCMv3.AiAgent;

/// <summary>
/// The AI assistant: a user asks in plain English or Arabic and the model answers using tools that call the
/// ordinary self-service services on the user's behalf.
///
/// <para>
/// Conversations are private to the login that had them. Nothing here reads another user's conversation,
/// and every tool runs as the calling user with that user's permissions, so the assistant can never show
/// someone data their own screens would not.
/// </para>
/// </summary>
public interface IAiAgentAppService : IApplicationService
{
    Task<AiAgentStartupDto> GetStartupInfo();

    /// <summary>
    /// Sends a message and waits for the reply. On a CPU-only model host this can take a minute or more;
    /// the page shows progress while it waits.
    /// </summary>
    Task<SendAiMessageOutput> SendMessage(SendAiMessageInput input);

    /// <summary>The caller's own conversations, most recently used first.</summary>
    Task<ListResultDto<AiConversationListDto>> GetMyConversations();

    Task<AiConversationDto> GetConversation(EntityDto input);

    Task DeleteConversation(EntityDto input);
}
