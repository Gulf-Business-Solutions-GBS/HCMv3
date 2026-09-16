using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.Runtime.Session;
using Abp.Timing;
using Abp.UI;
using HCMv3.AiAgent.Chat;
using HCMv3.AiAgent.Dto;
using HCMv3.AiAgent.Tools;
using HCMv3.Authorization;
using HCMv3.PersonnelAdministration.Services;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.AiAgent;

/// <inheritdoc cref="IAiAgentAppService"/>
[AbpAuthorize(AppPermissions.Ess_AiAgent)]
public class AiAgentAppService : HCMv3AppServiceBase, IAiAgentAppService
{
    private const int MaxConversationsListed = 100;

    private static readonly Regex Whitespace = new(@"\s+", RegexOptions.Compiled);

    private readonly IRepository<AiConversation> _conversationRepository;
    private readonly IRepository<AiMessage> _messageRepository;
    private readonly IAiChatClient _chatClient;
    private readonly AiAgentToolRegistry _toolRegistry;
    private readonly IEmployeeSessionService _employeeSessionService;
    private readonly IUnitOfWorkManager _unitOfWorkManager;

    public AiAgentAppService(
        IRepository<AiConversation> conversationRepository,
        IRepository<AiMessage> messageRepository,
        IAiChatClient chatClient,
        AiAgentToolRegistry toolRegistry,
        IEmployeeSessionService employeeSessionService,
        IUnitOfWorkManager unitOfWorkManager)
    {
        _conversationRepository = conversationRepository;
        _messageRepository = messageRepository;
        _chatClient = chatClient;
        _toolRegistry = toolRegistry;
        _employeeSessionService = employeeSessionService;
        _unitOfWorkManager = unitOfWorkManager;
    }

    public async Task<AiAgentStartupDto> GetStartupInfo()
    {
        var employee = await _employeeSessionService.GetCurrentEmployeeAsync();

        return new AiAgentStartupDto
        {
            IsLinkedToEmployee = employee != null,
            FirstNameEn = employee?.FirstNameEn,
            FirstNameAr = employee?.FirstNameAr,
            CanUseLeave = await PermissionChecker.IsGrantedAsync(AppPermissions.Ess_MyLeave),
            CanViewTeam = await PermissionChecker.IsGrantedAsync(AppPermissions.Ess_MyTeam)
        };
    }

    /// <remarks>
    /// <para>
    /// <b>No unit of work around the whole method, deliberately.</b> A model reply can take minutes on a
    /// CPU-only host. Holding one transaction open across that would keep the conversation rows locked and a
    /// pooled connection checked out for the entire wait, and one slow answer would degrade everyone's
    /// requests. Instead each database step is its own short unit of work, and each tool call opens its own
    /// through the service it calls -- which also means a tool that fails rolls back only itself.
    /// </para>
    /// <para>
    /// The user's message is saved BEFORE the model is called, so a model outage loses the reply, never the
    /// question.
    /// </para>
    /// </remarks>
    [UnitOfWork(IsDisabled = true)]
    public async Task<SendAiMessageOutput> SendMessage(SendAiMessageInput input)
    {
        var text = input.Message?.Trim();
        if (string.IsNullOrEmpty(text))
        {
            throw new UserFriendlyException(L("AiAgentMessageRequired"));
        }

        int conversationId;
        string conversationTitle;
        AiMessage userMessage;
        List<AiChatMessage> history;
        List<IAiAgentTool> tools;
        string systemPrompt;

        using (var uow = _unitOfWorkManager.Begin())
        {
            var conversation = input.ConversationId.HasValue
                ? await GetOwnConversationAsync(input.ConversationId.Value)
                : await StartConversationAsync(text);

            conversationId = conversation.Id;
            conversationTitle = conversation.Title;

            // Read before the new message is inserted, so the history is everything BEFORE it.
            history = await LoadHistoryAsync(conversationId);

            userMessage = new AiMessage
            {
                TenantId = AbpSession.GetTenantId(),
                ConversationId = conversationId,
                Role = AiMessageRole.User,
                Content = text
            };
            await _messageRepository.InsertAsync(userMessage);

            conversation.LastMessageTime = Clock.Now;

            tools = await _toolRegistry.GetAvailableToolsAsync();
            systemPrompt = AiAgentSystemPrompt.Build(await BuildPromptContextAsync());

            await uow.CompleteAsync();
        }

        var reply = await RunModelAsync(conversationId, systemPrompt, history, text, tools);

        AiMessage assistantMessage;
        using (var uow = _unitOfWorkManager.Begin())
        {
            assistantMessage = new AiMessage
            {
                TenantId = AbpSession.GetTenantId(),
                ConversationId = conversationId,
                Role = AiMessageRole.Assistant,
                Content = reply
            };
            await _messageRepository.InsertAsync(assistantMessage);

            var conversation = await _conversationRepository.GetAsync(conversationId);
            conversation.LastMessageTime = Clock.Now;

            await uow.CompleteAsync();
        }

        return new SendAiMessageOutput
        {
            ConversationId = conversationId,
            ConversationTitle = conversationTitle,
            UserMessage = ToDto(userMessage),
            AssistantMessage = ToDto(assistantMessage)
        };
    }

    public async Task<ListResultDto<AiConversationListDto>> GetMyConversations()
    {
        var userId = AbpSession.GetUserId();

        var conversations = await _conversationRepository.GetAll()
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.LastMessageTime)
            .Take(MaxConversationsListed)
            .Select(c => new AiConversationListDto
            {
                Id = c.Id,
                Title = c.Title,
                LastMessageTime = c.LastMessageTime
            })
            .ToListAsync();

        return new ListResultDto<AiConversationListDto>(conversations);
    }

    public async Task<AiConversationDto> GetConversation(EntityDto input)
    {
        var conversation = await GetOwnConversationAsync(input.Id);

        var messages = await VisibleMessages(conversation.Id)
            .OrderBy(m => m.Id)
            .ToListAsync();

        return new AiConversationDto
        {
            Id = conversation.Id,
            Title = conversation.Title,
            Messages = messages.Select(ToDto).ToList()
        };
    }

    public async Task DeleteConversation(EntityDto input)
    {
        var conversation = await GetOwnConversationAsync(input.Id);

        // Soft delete: it disappears for the user, and the audit trail of what data the model was shown stays.
        await _conversationRepository.DeleteAsync(conversation);
    }

    /// <summary>
    /// Runs the model until it replies in words, executing the tools it asks for along the way.
    /// </summary>
    private async Task<string> RunModelAsync(
        int conversationId, string systemPrompt, List<AiChatMessage> history, string text, List<IAiAgentTool> tools)
    {
        var definitions = tools.Select(AiAgentToolRegistry.ToDefinition).ToList();

        var request = new AiChatRequest();
        request.Messages.Add(AiChatMessage.System(systemPrompt));
        request.Messages.AddRange(history);
        request.Messages.Add(AiChatMessage.User(text));

        for (var call = 1; call <= AiAgentConsts.MaxModelCallsPerMessage; call++)
        {
            // On the last allowed call no tools are offered, so the model has to answer with what it already
            // has instead of asking for one more lookup that would never run.
            request.Tools = call < AiAgentConsts.MaxModelCallsPerMessage ? definitions : new List<AiToolDefinition>();

            var response = await CompleteAsync(request);

            if (response.ToolCalls.Count == 0 || request.Tools.Count == 0)
            {
                return string.IsNullOrWhiteSpace(response.Content)
                    ? L(response.ToolCalls.Count == 0 ? "AiAgentEmptyReply" : "AiAgentCouldNotComplete")
                    : response.Content;
            }

            request.Messages.Add(AiChatMessage.Assistant(response.Content, response.ToolCalls));

            var steps = new List<AiMessage>
            {
                new()
                {
                    Role = AiMessageRole.Assistant,
                    Content = string.IsNullOrWhiteSpace(response.Content) ? null : response.Content,
                    ToolCallsJson = JsonSerializer.Serialize(response.ToolCalls, AiAgentToolRegistry.ResultJsonOptions)
                }
            };

            foreach (var toolCall in response.ToolCalls)
            {
                var result = await _toolRegistry.ExecuteAsync(toolCall, tools);

                request.Messages.Add(AiChatMessage.Tool(toolCall.Id, toolCall.Name, result));

                steps.Add(new AiMessage
                {
                    Role = AiMessageRole.Tool,
                    ToolCallId = Truncate(toolCall.Id, AiAgentConsts.MaxToolNameLength),
                    ToolName = Truncate(toolCall.Name, AiAgentConsts.MaxToolNameLength),
                    Content = result
                });
            }

            await SaveStepsAsync(conversationId, steps);
        }

        return L("AiAgentCouldNotComplete");
    }

    private async Task<AiChatResponse> CompleteAsync(AiChatRequest request)
    {
        try
        {
            return await _chatClient.CompleteAsync(request);
        }
        catch (AiChatClientException ex)
        {
            // The detail names the model host and may quote its response -- log it, do not show it.
            Logger.Warn("AI agent model call failed: " + ex.Message, ex);
            throw new UserFriendlyException(L(ex.IsTimeout ? "AiAgentModelTimedOut" : "AiAgentModelUnavailable"));
        }
    }

    private async Task SaveStepsAsync(int conversationId, List<AiMessage> steps)
    {
        using var uow = _unitOfWorkManager.Begin();

        foreach (var step in steps)
        {
            step.TenantId = AbpSession.GetTenantId();
            step.ConversationId = conversationId;
            await _messageRepository.InsertAsync(step);
        }

        await uow.CompleteAsync();
    }

    private async Task<AiConversation> StartConversationAsync(string firstMessage)
    {
        var conversation = new AiConversation
        {
            TenantId = AbpSession.GetTenantId(),
            UserId = AbpSession.GetUserId(),
            Title = MakeTitle(firstMessage),
            LastMessageTime = Clock.Now
        };

        conversation.Id = await _conversationRepository.InsertAndGetIdAsync(conversation);
        return conversation;
    }

    /// <summary>
    /// Owner-checked load. A conversation belonging to someone else is reported as not found rather than
    /// forbidden, so the API does not confirm which ids exist.
    /// </summary>
    private async Task<AiConversation> GetOwnConversationAsync(int id)
    {
        var userId = AbpSession.GetUserId();

        var conversation = await _conversationRepository.FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);

        return conversation ?? throw new UserFriendlyException(L("AiAgentConversationNotFound"));
    }

    /// <summary>
    /// Only what the user saw is replayed: their messages and the assistant's replies. Earlier tool calls and
    /// results are left out, so a trimmed history can never hand the model a tool result without the call it
    /// answers -- and a follow-up like "cancel it" simply looks the data up again, which is also fresher.
    /// </summary>
    private async Task<List<AiChatMessage>> LoadHistoryAsync(int conversationId)
    {
        var recent = await VisibleMessages(conversationId)
            .OrderByDescending(m => m.Id)
            .Take(AiAgentConsts.MaxHistoryMessages)
            .Select(m => new { m.Role, m.Content })
            .ToListAsync();

        recent.Reverse();

        return recent
            .Select(m => m.Role == AiMessageRole.User ? AiChatMessage.User(m.Content) : AiChatMessage.Assistant(m.Content))
            .ToList();
    }

    private IQueryable<AiMessage> VisibleMessages(int conversationId) =>
        _messageRepository.GetAll()
            .Where(m => m.ConversationId == conversationId)
            .Where(m => m.Role == AiMessageRole.User
                        || (m.Role == AiMessageRole.Assistant && m.ToolCallsJson == null && m.Content != null));

    private async Task<AiAgentPromptContext> BuildPromptContextAsync()
    {
        var employee = await _employeeSessionService.GetCurrentEmployeeAsync();

        return new AiAgentPromptContext
        {
            Today = Clock.Now.Date,
            IsLinkedToEmployee = employee != null,
            EmployeeNameEn = employee?.FullNameEn,
            EmployeeNameAr = employee?.FullNameAr,
            CanUseLeave = await PermissionChecker.IsGrantedAsync(AppPermissions.Ess_MyLeave),
            CanViewTeam = await PermissionChecker.IsGrantedAsync(AppPermissions.Ess_MyTeam)
        };
    }

    public static string MakeTitle(string firstMessage)
    {
        const int maxLength = 60;

        var title = Whitespace.Replace(firstMessage, " ").Trim();

        return title.Length <= maxLength ? title : title.Substring(0, maxLength).TrimEnd() + "…";
    }

    private static AiMessageDto ToDto(AiMessage message) => new()
    {
        Id = message.Id,
        Role = message.Role,
        Content = message.Content,
        CreationTime = message.CreationTime
    };

    private static string Truncate(string value, int length) =>
        value == null || value.Length <= length ? value : value.Substring(0, length);
}
