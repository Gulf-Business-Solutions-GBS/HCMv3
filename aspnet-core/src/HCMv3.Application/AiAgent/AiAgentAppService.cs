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
using Abp.Runtime.Validation;
using Abp.Timing;
using Abp.UI;
using HCMv3.AiAgent.Actions;
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

    /// <summary>Being offered any of these is what makes the prompt describe the user as an administrator.</summary>
    private static readonly HashSet<string> AdministrationToolNames = new(StringComparer.Ordinal)
    {
        "find_employee", "find_wage_types", "get_employee_basic_pay", "prepare_basic_pay_change", "prepare_one_time_payment"
    };

    private static readonly Regex Whitespace = new(@"\s+", RegexOptions.Compiled);

    private readonly IRepository<AiConversation> _conversationRepository;
    private readonly IRepository<AiMessage> _messageRepository;
    private readonly IRepository<AiPendingAction> _actionRepository;
    private readonly IAiChatClient _chatClient;
    private readonly AiAgentToolRegistry _toolRegistry;
    private readonly AiPendingActionHandlerResolver _actionHandlers;
    private readonly IEmployeeSessionService _employeeSessionService;
    private readonly IUnitOfWorkManager _unitOfWorkManager;

    public AiAgentAppService(
        IRepository<AiConversation> conversationRepository,
        IRepository<AiMessage> messageRepository,
        IRepository<AiPendingAction> actionRepository,
        IAiChatClient chatClient,
        AiAgentToolRegistry toolRegistry,
        AiPendingActionHandlerResolver actionHandlers,
        IEmployeeSessionService employeeSessionService,
        IUnitOfWorkManager unitOfWorkManager)
    {
        _conversationRepository = conversationRepository;
        _messageRepository = messageRepository;
        _actionRepository = actionRepository;
        _chatClient = chatClient;
        _toolRegistry = toolRegistry;
        _actionHandlers = actionHandlers;
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
            systemPrompt = AiAgentSystemPrompt.Build(await BuildPromptContextAsync(tools));

            await uow.CompleteAsync();
        }

        var toolContext = new AiToolContext { ConversationId = conversationId };

        var reply = await RunModelAsync(systemPrompt, history, text, tools, toolContext);

        using (var uow = _unitOfWorkManager.Begin())
        {
            var assistantMessage = new AiMessage
            {
                TenantId = AbpSession.GetTenantId(),
                ConversationId = conversationId,
                Role = AiMessageRole.Assistant,
                Content = reply,
                PendingActionId = toolContext.CreatedPendingActionId
            };
            await _messageRepository.InsertAsync(assistantMessage);

            var conversation = await _conversationRepository.GetAsync(conversationId);
            conversation.LastMessageTime = Clock.Now;

            await CurrentUnitOfWork.SaveChangesAsync();

            var dtos = await ToDtosAsync(new List<AiMessage> { userMessage, assistantMessage });

            await uow.CompleteAsync();

            return new SendAiMessageOutput
            {
                ConversationId = conversationId,
                ConversationTitle = conversationTitle,
                UserMessage = dtos[0],
                AssistantMessage = dtos[1]
            };
        }
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
            Messages = await ToDtosAsync(messages)
        };
    }

    public async Task DeleteConversation(EntityDto input)
    {
        var conversation = await GetOwnConversationAsync(input.Id);

        // Soft delete: it disappears for the user, and the audit trail of what data the model was shown stays.
        await _conversationRepository.DeleteAsync(conversation);
    }

    /// <remarks>
    /// Three short steps rather than one transaction, so that whatever the underlying service does, the card
    /// ends in a final state:
    /// <list type="number">
    /// <item><description>CLAIM the action with one conditional update (Pending -&gt; Processing). Two
    /// concurrent confirmations cannot both change the row, so a double-click submits once.</description></item>
    /// <item><description>RUN the handler with no surrounding unit of work. The leave service opens its own;
    /// if it refuses, only its work rolls back.</description></item>
    /// <item><description>RECORD the outcome on the action and as a line in the conversation, so the next
    /// question the user asks is answered knowing what happened.</description></item>
    /// </list>
    /// </remarks>
    [UnitOfWork(IsDisabled = true)]
    public async Task<AiActionResultDto> ConfirmAction(EntityDto input)
    {
        AiPendingAction action;
        bool expired;

        using (var uow = _unitOfWorkManager.Begin())
        {
            action = await GetOwnActionAsync(input.Id);

            if (action.Status != AiPendingActionStatus.Pending)
            {
                throw new UserFriendlyException(L("AiAgentActionNoLongerPending"));
            }

            var claimed = await _actionRepository.GetAll()
                .Where(a => a.Id == action.Id && a.Status == AiPendingActionStatus.Pending)
                .ExecuteUpdateAsync(setters => setters.SetProperty(a => a.Status, AiPendingActionStatus.Processing));

            await uow.CompleteAsync();

            if (claimed == 0)
            {
                throw new UserFriendlyException(L("AiAgentActionNoLongerPending"));
            }

            expired = action.CreationTime < Clock.Now.AddHours(-AiAgentConsts.PendingActionLifetimeHours);
        }

        if (expired)
        {
            return await FinishActionAsync(action.Id, AiPendingActionStatus.Expired, L("AiAgentActionExpired"));
        }

        AiPendingActionStatus outcome;
        string message;

        try
        {
            message = await _actionHandlers.Get(action.ActionType).ExecuteAsync(action.PayloadJson);
            outcome = AiPendingActionStatus.Completed;
        }
        catch (UserFriendlyException ex)
        {
            outcome = AiPendingActionStatus.Failed;
            message = L("AiAgentActionFailed", string.IsNullOrWhiteSpace(ex.Details) ? ex.Message : ex.Message + " " + ex.Details);
        }
        catch (AbpAuthorizationException ex)
        {
            outcome = AiPendingActionStatus.Failed;
            message = L("AiAgentActionFailed", ex.Message);
        }
        catch (AbpValidationException ex)
        {
            outcome = AiPendingActionStatus.Failed;
            message = L("AiAgentActionFailed", string.Join(" ", ex.ValidationErrors.Select(e => e.ErrorMessage)));
        }
        catch (Exception ex)
        {
            // Must not escape: the action is already claimed, and an escaping exception would strand it in
            // Processing, unconfirmable and uncancellable, with the user unsure whether leave was booked.
            Logger.Error($"AI agent action {action.Id} ({action.ActionType}) failed.", ex);
            outcome = AiPendingActionStatus.Failed;
            message = L("AiAgentActionFailedUnexpectedly");
        }

        return await FinishActionAsync(action.Id, outcome, message);
    }

    [UnitOfWork(IsDisabled = true)]
    public async Task<AiActionResultDto> CancelAction(EntityDto input)
    {
        using (var uow = _unitOfWorkManager.Begin())
        {
            var action = await GetOwnActionAsync(input.Id);

            var claimed = await _actionRepository.GetAll()
                .Where(a => a.Id == action.Id && a.Status == AiPendingActionStatus.Pending)
                .ExecuteUpdateAsync(setters => setters.SetProperty(a => a.Status, AiPendingActionStatus.Processing));

            await uow.CompleteAsync();

            if (claimed == 0)
            {
                throw new UserFriendlyException(L("AiAgentActionNoLongerPending"));
            }
        }

        return await FinishActionAsync(input.Id, AiPendingActionStatus.Cancelled, L("AiAgentActionCancelledByUser"));
    }

    /// <summary>
    /// Runs the model until it replies in words, executing the tools it asks for along the way.
    /// </summary>
    private async Task<string> RunModelAsync(
        string systemPrompt, List<AiChatMessage> history, string text, List<IAiAgentTool> tools, AiToolContext toolContext)
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
                var result = await _toolRegistry.ExecuteAsync(toolCall, tools, toolContext);

                request.Messages.Add(AiChatMessage.Tool(toolCall.Id, toolCall.Name, result));

                steps.Add(new AiMessage
                {
                    Role = AiMessageRole.Tool,
                    ToolCallId = Truncate(toolCall.Id, AiAgentConsts.MaxToolNameLength),
                    ToolName = Truncate(toolCall.Name, AiAgentConsts.MaxToolNameLength),
                    Content = result
                });
            }

            await SaveStepsAsync(toolContext.ConversationId, steps);
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

    private async Task<AiActionResultDto> FinishActionAsync(int actionId, AiPendingActionStatus status, string message)
    {
        using var uow = _unitOfWorkManager.Begin();

        var action = await _actionRepository.GetAsync(actionId);
        action.Status = status;
        action.ResultMessage = Truncate(message, AiPendingAction.MaxResultMessageLength);
        action.ResolvedTime = Clock.Now;

        // Written into the conversation as the assistant's line, so it shows in the thread and the model
        // knows the outcome if the user follows up ("did it go through?").
        var line = new AiMessage
        {
            TenantId = AbpSession.GetTenantId(),
            ConversationId = action.ConversationId,
            Role = AiMessageRole.Assistant,
            Content = message
        };
        await _messageRepository.InsertAsync(line);

        var conversation = await _conversationRepository.GetAsync(action.ConversationId);
        conversation.LastMessageTime = Clock.Now;

        await CurrentUnitOfWork.SaveChangesAsync();

        var result = new AiActionResultDto
        {
            Action = ToActionDto(action),
            Message = (await ToDtosAsync(new List<AiMessage> { line }))[0]
        };

        await uow.CompleteAsync();

        return result;
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

    /// <summary>Owner-checked, for the same reason -- and because confirming is a write.</summary>
    private async Task<AiPendingAction> GetOwnActionAsync(int id)
    {
        var userId = AbpSession.GetUserId();

        var action = await _actionRepository.FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);

        return action ?? throw new UserFriendlyException(L("AiAgentActionNotFound"));
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

    private async Task<AiAgentPromptContext> BuildPromptContextAsync(IReadOnlyCollection<IAiAgentTool> tools)
    {
        var employee = await _employeeSessionService.GetCurrentEmployeeAsync();

        return new AiAgentPromptContext
        {
            Today = Clock.Now.Date,
            IsLinkedToEmployee = employee != null,
            EmployeeNameEn = employee?.FullNameEn,
            EmployeeNameAr = employee?.FullNameAr,
            CanUseLeave = await PermissionChecker.IsGrantedAsync(AppPermissions.Ess_MyLeave),
            CanViewTeam = await PermissionChecker.IsGrantedAsync(AppPermissions.Ess_MyTeam),
            IsPayAdministrator = tools.Any(t => AdministrationToolNames.Contains(t.Name))
        };
    }

    private async Task<List<AiMessageDto>> ToDtosAsync(List<AiMessage> messages)
    {
        var actionIds = messages.Where(m => m.PendingActionId.HasValue).Select(m => m.PendingActionId!.Value).Distinct().ToList();

        var actions = actionIds.Count == 0
            ? new Dictionary<int, AiPendingActionDto>()
            : (await _actionRepository.GetAll().Where(a => actionIds.Contains(a.Id)).ToListAsync())
                .ToDictionary(a => a.Id, ToActionDto);

        return messages.Select(m => new AiMessageDto
        {
            Id = m.Id,
            Role = m.Role,
            Content = m.Content,
            CreationTime = m.CreationTime,
            PendingAction = m.PendingActionId.HasValue && actions.TryGetValue(m.PendingActionId.Value, out var action) ? action : null
        }).ToList();
    }

    private AiPendingActionDto ToActionDto(AiPendingAction action)
    {
        AiPendingActionPreview preview;
        try
        {
            preview = _actionHandlers.Get(action.ActionType).Describe(action.PayloadJson);
        }
        catch (Exception ex)
        {
            // A card that cannot be described still has to render -- with its status -- rather than break the
            // whole conversation for one unreadable row.
            Logger.Warn($"AI agent action {action.Id} could not be described.", ex);
            preview = new AiPendingActionPreview { Title = action.ActionType };
        }

        return new AiPendingActionDto
        {
            Id = action.Id,
            ActionType = action.ActionType,
            Status = action.Status,
            Title = preview.Title,
            Details = preview.Details,
            ResultMessage = action.ResultMessage
        };
    }

    public static string MakeTitle(string firstMessage)
    {
        const int maxLength = 60;

        var title = Whitespace.Replace(firstMessage, " ").Trim();

        return title.Length <= maxLength ? title : title.Substring(0, maxLength).TrimEnd() + "…";
    }

    private static string Truncate(string value, int length) =>
        value == null || value.Length <= length ? value : value.Substring(0, length);
}
