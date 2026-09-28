using System.Globalization;
using HCMv3.Ess;
using HCMv3.Ess.Dto;
using HCMv3.Maui.Core.Components;
using HCMv3.Maui.Core.Helpers;
using HCMv3.Maui.Core.Threading;
using HCMv3.Maui.Services.Navigation;
using HCMv3.Workflow;
using HCMv3.Workflow.Dto;
using Microsoft.AspNetCore.Components;

namespace HCMv3.Maui.Pages.Ess;

/// <summary>
/// One inbox request, read the way the web's request detail dialog shows it
/// (angular/src/app/main/ess/shared/ess-request-detail-modal.component.ts), in a single phone column:
/// the outcome banner, the question, who raised it, every field its workflow handler projects, the decision
/// (or, for somebody asked for their view, the feedback box), the conversation, and the approvers / read
/// log / delegates / action log panel.
///
/// <para>
/// The inbox row's kind is not on the detail DTO, so the inbox passes <c>?kind=feedback</c> for a feedback
/// ask. The asker's name and note are then read back from the inbox's feedback rows; if the row has gone
/// (answered elsewhere, withdrawn) the page still opens in feedback mode and the server decides whether the
/// submission is still allowed.
/// </para>
/// </summary>
public partial class InboxDetail : HCMv3MainLayoutPageComponentBase
{
    public const string FeedbackKind = "feedback";

    [Parameter]
    public int WorkflowRequestId { get; set; }

    /// <summary>
    /// Read off the URI by hand rather than with [SupplyParameterFromQuery]: that attribute depends on a
    /// query-value provider being registered, which a BlazorWebView host is not guaranteed to do.
    /// </summary>
    [Inject]
    protected NavigationManager NavigationManager { get; set; }

    private string Kind { get; set; }

    protected IWorkflowActivityAppService WorkflowActivityAppService { get; set; }

    protected IEssInboxAppService EssInboxAppService { get; set; }

    private bool _loaded;
    private int _loadedId;

    private WorkflowRequestDetailDto _detail;
    private WorkflowActivityFeedDto _feed;
    private WorkflowParticipantsDto _participants;
    private List<WorkflowRequestLogEntryDto> _logEntries;
    private string _participantsTab = "approvers";

    // Feedback mode: the inbox row this person was asked on.
    private EssInboxItemDto _feedbackRow;
    private string _feedbackComment = string.Empty;

    // Approver mode.
    private string _decisionComment = string.Empty;
    private List<FeedbackRequestDto> _feedbackRequests = new();

    // The "Get feedback" picker, inline rather than a modal.
    private bool _showFeedbackPicker;
    private List<FeedbackCandidateDto> _candidates = new();
    private readonly HashSet<int> _selected = new();
    private string _candidateFilter = string.Empty;
    private string _feedbackNote = string.Empty;

    public InboxDetail()
    {
        WorkflowActivityAppService = Resolve<IWorkflowActivityAppService>();
        EssInboxAppService = Resolve<IEssInboxAppService>();
    }

    private bool IsFeedbackMode => string.Equals(Kind, FeedbackKind, StringComparison.OrdinalIgnoreCase);

    private static string ReadQueryValue(string uri, string name)
    {
        var at = uri?.IndexOf('?') ?? -1;
        if (at < 0)
        {
            return null;
        }

        foreach (var pair in uri.Substring(at + 1).Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = pair.Split('=', 2);
            if (string.Equals(Uri.UnescapeDataString(parts[0]), name, StringComparison.OrdinalIgnoreCase))
            {
                return parts.Length > 1 ? Uri.UnescapeDataString(parts[1]) : string.Empty;
            }
        }

        return null;
    }

    protected override async Task OnParametersSetAsync()
    {
        Kind = ReadQueryValue(NavigationManager?.Uri, "kind");

        if (_loaded && _loadedId == WorkflowRequestId)
        {
            return;
        }

        await SetPageHeader(L("EssInbox"));

        _loaded = false;
        _loadedId = WorkflowRequestId;
        _detail = null;
        _feed = null;
        _participants = null;
        _logEntries = null;
        _feedbackRow = null;
        _feedbackRequests = new List<FeedbackRequestDto>();
        _participantsTab = "approvers";
        _decisionComment = string.Empty;
        _feedbackComment = string.Empty;
        CloseFeedbackPicker();

        await SetBusyAsync(LoadAsync);
        _loaded = true;
    }

    private async Task LoadAsync()
    {
        await WebRequestExecuter.Execute(
            async () => await WorkflowActivityAppService.GetRequestDetail(new GetWorkflowRequestDetailInput
            {
                WorkflowRequestId = WorkflowRequestId
            }),
            result =>
            {
                _detail = result;
                return Task.CompletedTask;
            });

        await LoadFeedAsync();

        await WebRequestExecuter.Execute(
            async () => await WorkflowActivityAppService.GetParticipants(new GetWorkflowParticipantsInput
            {
                WorkflowRequestId = WorkflowRequestId
            }),
            result =>
            {
                _participants = result;
                return Task.CompletedTask;
            });

        if (IsFeedbackMode)
        {
            await WebRequestExecuter.Execute(
                async () => await EssInboxAppService.GetMyInbox(new GetMyInboxInput
                {
                    Kind = EssInboxItemKind.FeedbackRequest,
                    MaxResultCount = 100,
                    SkipCount = 0
                }),
                result =>
                {
                    _feedbackRow = result?.Items?.FirstOrDefault(i => i.Id == WorkflowRequestId);
                    return Task.CompletedTask;
                });
        }
        else if (_detail?.IsCurrentApprover == true)
        {
            await LoadFeedbackRequestsAsync();
        }
    }

    private async Task LoadFeedAsync()
    {
        await WebRequestExecuter.Execute(
            async () => await WorkflowActivityAppService.GetActivityFeed(new GetWorkflowActivityFeedInput
            {
                WorkflowRequestId = WorkflowRequestId
            }),
            result =>
            {
                _feed = result;
                return Task.CompletedTask;
            });
    }

    private async Task LoadFeedbackRequestsAsync()
    {
        await WebRequestExecuter.Execute(
            async () => await EssInboxAppService.GetFeedbackRequests(new GetFeedbackCandidatesInput
            {
                WorkflowRequestId = WorkflowRequestId
            }),
            result =>
            {
                _feedbackRequests = result ?? new List<FeedbackRequestDto>();
                return Task.CompletedTask;
            });
    }

    // ------------------------------------------------------------------ header

    private string TypeName
    {
        get
        {
            var name = EssText.Pick(_detail?.RequestTypeNameEn, _detail?.RequestTypeNameAr);
            return string.IsNullOrWhiteSpace(name) ? _detail?.RequestType ?? L("EssInbox") : name;
        }
    }

    private string HeaderTitle =>
        string.IsNullOrWhiteSpace(_detail?.RequestNumber) ? TypeName : $"{TypeName} ({_detail.RequestNumber})";

    private string RequesterPlainName => EssText.Pick(_detail?.RequesterNameEn, _detail?.RequesterNameAr);

    /// <summary>"Ahmad Al Sayed (1042)", as the web names a person.</summary>
    private string RequesterName
    {
        get
        {
            var name = RequesterPlainName;
            var code = _detail?.RequesterCode;
            return !string.IsNullOrWhiteSpace(name) && !string.IsNullOrWhiteSpace(code) ? $"{name} ({code})" : name;
        }
    }

    private string RequesterJobTitle => EssText.Pick(_detail?.RequesterJobTitleEn, _detail?.RequesterJobTitleAr);

    private string InitiatedByText =>
        L("InitiatedBy", RequesterName, _detail?.SubmittedOn.HasValue == true ? EssText.Date(_detail.SubmittedOn) : string.Empty);

    /// <summary>The one-line verdict on a request that is no longer running; null while it is still open.</summary>
    private (string Text, string Css)? OutcomeBanner
    {
        get
        {
            switch (_detail?.RequestStatus)
            {
                case RequestStatus.Approved:
                    return (L("ThisRequestIsComplete"), "alert-success");
                case RequestStatus.Rejected:
                    return (L("ThisRequestWasRejected"), "alert-danger");
                case RequestStatus.Closed:
                    return (L("ThisRequestIsClosed"), "alert-secondary");
                default:
                    return null;
            }
        }
    }

    // ------------------------------------------------------------------ fields

    private List<WorkflowDetailFieldDto> Fields => _detail?.Fields ?? new List<WorkflowDetailFieldDto>();

    /// <summary>
    /// A transport value in the reader's terms, as the web's formatValue does: ISO dates as dd/MM/yyyy,
    /// invariant numbers grouped, money to three places, booleans as Yes/No. An unknown kind or an unparsable
    /// value falls through to the raw string, which is always readable.
    /// </summary>
    private string FormatValue(WorkflowDetailFieldDto field)
    {
        var raw = field?.Value;
        if (string.IsNullOrEmpty(raw))
        {
            return "-";
        }

        switch (field.Kind)
        {
            case WorkflowDetailFieldKind.Date:
                return TryParseDate(raw, out var date) ? EssText.Date(date) : raw;
            case WorkflowDetailFieldKind.DateTime:
                return TryParseDate(raw, out var moment) ? EssText.DateTimeText(moment) : raw;
            case WorkflowDetailFieldKind.Number:
                return TryParseNumber(raw, out var number) ? number.ToString("#,0.##", CultureInfo.InvariantCulture) : raw;
            case WorkflowDetailFieldKind.Money:
                return TryParseNumber(raw, out var money) ? money.ToString("#,0.000", CultureInfo.InvariantCulture) : raw;
            case WorkflowDetailFieldKind.Boolean:
                return L(string.Equals(raw, "true", StringComparison.OrdinalIgnoreCase) ? "Yes" : "No");
            case WorkflowDetailFieldKind.LongText:
            case WorkflowDetailFieldKind.Text:
            default:
                return raw;
        }
    }

    private static bool TryParseDate(string raw, out DateTime value)
    {
        return DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out value);
    }

    private static bool TryParseNumber(string raw, out decimal value)
    {
        return decimal.TryParse(raw, NumberStyles.Number | NumberStyles.AllowExponent, CultureInfo.InvariantCulture, out value);
    }

    // ------------------------------------------------------------------ decision

    private bool CanApprove => !IsFeedbackMode && _detail?.IsCurrentApprover == true && _detail.CanApprove;

    private bool CanReject => !IsFeedbackMode && _detail?.IsCurrentApprover == true && _detail.CanReject;

    private bool CanSendBack => !IsFeedbackMode && _detail?.IsCurrentApprover == true && _detail.CanSendBack;

    private bool HasActions => CanApprove || CanReject || CanSendBack;

    private bool IsCommentRequired => _detail?.IsCommentRequired == true;

    private async Task ApproveAsync()
    {
        var comment = (_decisionComment ?? string.Empty).Trim();

        // The step demands a comment on approval too; said before the round trip, as on the web.
        if (IsCommentRequired && comment.Length == 0)
        {
            await UserDialogsService.AlertWarn(L("CommentIsRequiredForThisStep"));
            return;
        }

        await DecideAsync(
            () => EssInboxAppService.Approve(new ApproveRequestInput
            {
                WorkflowRequestId = WorkflowRequestId,
                Comment = comment.Length == 0 ? null : comment
            }),
            "RequestApproved");
    }

    private async Task RejectAsync()
    {
        var comment = (_decisionComment ?? string.Empty).Trim();
        if (comment.Length == 0)
        {
            await UserDialogsService.AlertWarn(L("AReasonIsRequiredToReject"));
            return;
        }

        await DecideAsync(
            () => EssInboxAppService.Reject(new RejectRequestInput
            {
                WorkflowRequestId = WorkflowRequestId,
                Comment = comment
            }),
            "RequestRejected");
    }

    private async Task SendBackAsync()
    {
        var comment = (_decisionComment ?? string.Empty).Trim();
        if (comment.Length == 0)
        {
            await UserDialogsService.AlertWarn(L("AReasonIsRequiredToSendBack"));
            return;
        }

        await DecideAsync(
            () => EssInboxAppService.SendBack(new SendBackRequestInput
            {
                WorkflowRequestId = WorkflowRequestId,
                Comment = comment
            }),
            "RequestSentBack");
    }

    private async Task SubmitFeedbackAsync()
    {
        var comment = (_feedbackComment ?? string.Empty).Trim();
        if (comment.Length == 0)
        {
            await UserDialogsService.AlertWarn(L("FeedbackIsRequired"));
            return;
        }

        await DecideAsync(
            () => EssInboxAppService.SubmitFeedback(new SubmitFeedbackInput
            {
                WorkflowRequestId = WorkflowRequestId,
                Comment = comment
            }),
            "FeedbackSubmitted");
    }

    /// <summary>Runs one decision; on success says so with the web's key and returns to the inbox.</summary>
    private async Task DecideAsync(Func<Task> call, string successKey)
    {
        var done = false;

        await SetBusyAsync(async () =>
        {
            await WebRequestExecuter.Execute(
                async () => await call(),
                () =>
                {
                    done = true;
                    return Task.CompletedTask;
                });
        });

        if (!done)
        {
            return;
        }

        await UserDialogsService.AlertSuccess(L(successKey));
        BackToInbox();
    }

    private void BackToInbox()
    {
        NavigationService.NavigateTo(NavigationUrlConsts.Inbox);
    }

    // ------------------------------------------------------------------ feedback asks (approver side)

    private string FeedbackAskedByName =>
        EssText.Pick(_feedbackRow?.FeedbackRequestedByNameEn, _feedbackRow?.FeedbackRequestedByNameAr);

    private static string PersonName(string en, string ar, string code)
    {
        var name = EssText.Pick(en, ar);
        if (string.IsNullOrWhiteSpace(name))
        {
            return code ?? string.Empty;
        }

        return string.IsNullOrWhiteSpace(code) ? name : $"{name} ({code})";
    }

    private static string CandidateName(FeedbackCandidateDto c) => PersonName(c.NameEn, c.NameAr, c.Code);

    private static string CandidateJobTitle(FeedbackCandidateDto c) => EssText.Pick(c.JobTitleEn, c.JobTitleAr);

    private static string FeedbackRequestName(FeedbackRequestDto r) => PersonName(r.NameEn, r.NameAr, r.Code);

    private IEnumerable<FeedbackCandidateDto> VisibleCandidates
    {
        get
        {
            var needle = (_candidateFilter ?? string.Empty).Trim();
            if (needle.Length == 0)
            {
                return _candidates;
            }

            // Code as well as name: the code is how two colleagues with the same name are told apart.
            return _candidates.Where(c =>
                EssText.Pick(c.NameEn, c.NameAr).Contains(needle, StringComparison.OrdinalIgnoreCase)
                || (c.Code ?? string.Empty).Contains(needle, StringComparison.OrdinalIgnoreCase));
        }
    }

    private IEnumerable<FeedbackCandidateDto> SelectedCandidates => _candidates.Where(c => _selected.Contains(c.EmployeeId));

    private async Task OpenFeedbackPickerAsync()
    {
        _selected.Clear();
        _candidateFilter = string.Empty;
        _feedbackNote = string.Empty;
        _candidates = new List<FeedbackCandidateDto>();
        _showFeedbackPicker = true;

        await SetBusyAsync(LoadCandidatesAsync);
    }

    private async Task LoadCandidatesAsync()
    {
        await WebRequestExecuter.Execute(
            async () => await EssInboxAppService.GetFeedbackCandidates(new GetFeedbackCandidatesInput
            {
                WorkflowRequestId = WorkflowRequestId
            }),
            result =>
            {
                _candidates = result ?? new List<FeedbackCandidateDto>();
                return Task.CompletedTask;
            });
    }

    private void CloseFeedbackPicker()
    {
        _showFeedbackPicker = false;
        _selected.Clear();
        _candidates = new List<FeedbackCandidateDto>();
        _candidateFilter = string.Empty;
        _feedbackNote = string.Empty;
    }

    /// <summary>
    /// Ticks or unticks one colleague. Somebody already asked is left alone; somebody already in the approval
    /// chain is asked about first, exactly as the web does, because asking them is usually redundant but not
    /// always.
    /// </summary>
    private async Task ToggleCandidateAsync(FeedbackCandidateDto candidate)
    {
        if (candidate.EmployeeId == 0 || candidate.AlreadyAsked)
        {
            return;
        }

        if (_selected.Contains(candidate.EmployeeId))
        {
            _selected.Remove(candidate.EmployeeId);
            return;
        }

        if (candidate.IsAlreadyApprover)
        {
            var confirmed = await UserDialogsService.Confirm(
                L("AlreadyInApprovalCycleConfirm", CandidateName(candidate)),
                L("GetFeedback"));

            if (!confirmed)
            {
                StateHasChanged();
                return;
            }
        }

        _selected.Add(candidate.EmployeeId);
    }

    private void RemoveSelected(FeedbackCandidateDto candidate)
    {
        _selected.Remove(candidate.EmployeeId);
    }

    private async Task SendFeedbackRequestAsync()
    {
        if (_selected.Count == 0)
        {
            return;
        }

        var done = false;

        await SetBusyAsync(async () =>
        {
            await WebRequestExecuter.Execute(
                async () => await EssInboxAppService.RequestFeedback(new RequestFeedbackInput
                {
                    WorkflowRequestId = WorkflowRequestId,
                    EmployeeIds = _selected.ToList(),
                    Note = (_feedbackNote ?? string.Empty).Trim()
                }),
                () =>
                {
                    done = true;
                    return Task.CompletedTask;
                });
        });

        if (!done)
        {
            return;
        }

        await UserDialogsService.AlertSuccess(L("FeedbackRequestsSent"));
        CloseFeedbackPicker();

        await SetBusyAsync(async () =>
        {
            await LoadFeedbackRequestsAsync();
            await LoadFeedAsync();
        });
    }

    /// <summary>
    /// Takes back an unanswered ask, after a confirmation -- the colleague has already seen it. An answered
    /// one cannot be withdrawn: what they said is in the conversation.
    /// </summary>
    private async Task WithdrawAsync(int employeeId)
    {
        if (employeeId == 0)
        {
            return;
        }

        if (!await UserDialogsService.Confirm(L("AreYouSure"), L("WithdrawFeedbackRequest")))
        {
            return;
        }

        var done = false;

        await SetBusyAsync(async () =>
        {
            await WebRequestExecuter.Execute(
                async () => await EssInboxAppService.CancelFeedbackRequest(new CancelFeedbackInput
                {
                    WorkflowRequestId = WorkflowRequestId,
                    EmployeeId = employeeId
                }),
                () =>
                {
                    done = true;
                    return Task.CompletedTask;
                });
        });

        if (!done)
        {
            return;
        }

        await UserDialogsService.AlertSuccess(L("FeedbackRequestWithdrawn"));

        await SetBusyAsync(async () =>
        {
            await LoadFeedbackRequestsAsync();
            await LoadFeedAsync();

            if (_showFeedbackPicker)
            {
                await LoadCandidatesAsync();
            }
        });
    }

    // ------------------------------------------------------------------ conversation

    /// <summary>One post: a system line (an action with nothing said) or a bubble (anything with words).</summary>
    private sealed class FeedEntry
    {
        public string Key { get; init; }
        public bool IsSystem { get; init; }
        public string AuthorName { get; init; }
        public DateTime? OccurredAt { get; init; }
        public string Body { get; init; }
        public string ActionLabel { get; init; }
        public bool IsInternal { get; init; }
        public bool IsEdited { get; init; }
    }

    /// <summary>
    /// Newest first, as the web dialog shows it beside the decision. The requester's submission comment leads
    /// only when the Created entry does not already carry it -- otherwise it would print twice.
    /// </summary>
    private List<FeedEntry> FeedEntries
    {
        get
        {
            var activities = _feed?.Activities ?? new List<WorkflowActivityDto>();
            var entries = activities.Select(ToEntry).ToList();

            var lead = (_detail?.SubmissionComment ?? string.Empty).Trim();
            if (lead.Length > 0)
            {
                var created = activities.FirstOrDefault(a =>
                                  a.Kind == WorkflowActivityKind.Action && a.ActionType == RequestActionType.Created)
                              ?? activities.FirstOrDefault();

                if (string.IsNullOrWhiteSpace(created?.Body))
                {
                    entries.Add(new FeedEntry
                    {
                        Key = "lead",
                        IsSystem = false,
                        AuthorName = !string.IsNullOrWhiteSpace(RequesterName) ? RequesterName : created != null ? ActorName(created) : string.Empty,
                        OccurredAt = created?.OccurredAt,
                        Body = lead,
                        ActionLabel = string.Empty
                    });
                }
            }

            return entries
                .OrderByDescending(e => e.OccurredAt ?? DateTime.MinValue)
                .ToList();
        }
    }

    private FeedEntry ToEntry(WorkflowActivityDto activity)
    {
        var isAction = activity.Kind == WorkflowActivityKind.Action;

        string actionLabel;
        if (isAction)
        {
            actionLabel = activity.ActionType.HasValue ? L("WorkflowAction_" + activity.ActionType.Value) : string.Empty;
        }
        else if (activity.Kind == WorkflowActivityKind.FeedbackRequested)
        {
            actionLabel = L("FeedbackRequestedFrom", ActorName(activity));
        }
        else
        {
            actionLabel = string.Empty;
        }

        return new FeedEntry
        {
            Key = activity.Kind + "-" + activity.Id,
            IsSystem = isAction && string.IsNullOrWhiteSpace(activity.Body),
            AuthorName = ActorName(activity),
            OccurredAt = activity.OccurredAt,
            Body = activity.Body,
            ActionLabel = actionLabel,
            IsInternal = activity.IsInternal,
            IsEdited = activity.IsEdited
        };
    }

    private static string ActorName(WorkflowActivityDto activity) =>
        PersonName(activity.ActorNameEn, activity.ActorNameAr, activity.ActorCode);

    // ------------------------------------------------------------------ participants panel

    private async Task SelectParticipantsTabAsync(string tab)
    {
        _participantsTab = tab;

        // The log is fetched the first time it is asked for, as on the web.
        if (tab == "log" && _logEntries == null)
        {
            await SetBusyAsync(async () =>
            {
                await WebRequestExecuter.Execute(
                    async () => await WorkflowActivityAppService.GetRequestLog(new GetWorkflowRequestLogInput
                    {
                        WorkflowRequestId = WorkflowRequestId
                    }),
                    result =>
                    {
                        _logEntries = result ?? new List<WorkflowRequestLogEntryDto>();
                        return Task.CompletedTask;
                    });
            });
        }
    }

    private List<WorkflowApproverEntryDto> Approvers => _participants?.Approvers ?? new List<WorkflowApproverEntryDto>();

    private List<WorkflowReadLogEntryDto> ReadLog => _participants?.ReadLog ?? new List<WorkflowReadLogEntryDto>();

    private List<WorkflowDelegateEntryDto> Delegates => _participants?.Delegates ?? new List<WorkflowDelegateEntryDto>();

    private IEnumerable<IGrouping<int, WorkflowApproverEntryDto>> ApproverSteps =>
        Approvers.GroupBy(a => a.WorkflowStep).OrderBy(g => g.Key);

    private bool HasReconstructedApprovers => Approvers.Any(a => a.Origin == WorkflowApproverOrigin.ReconstructedFromLog);

    private string CurrentWorkflowRole =>
        EssText.Pick(_participants?.CurrentWorkflowRoleNameEn, _participants?.CurrentWorkflowRoleNameAr);

    private string ParticipantsStatus =>
        _participants == null ? "-" : L("WorkflowRequestStatus_" + _participants.RequestStatus);

    /// <summary>Stated only where it says something: more than one approver and not the creator step.</summary>
    private string StepRule(IGrouping<int, WorkflowApproverEntryDto> step)
    {
        var entries = step.ToList();
        if (entries.Count <= 1 || entries.All(e => e.Origin == WorkflowApproverOrigin.CreatorStep))
        {
            return string.Empty;
        }

        switch (entries[0].ApprovalRequire)
        {
            case ApprovalRequire.Single:
                return L("ApprovalRuleSingle");
            case ApprovalRequire.All:
                return L("ApprovalRuleAll");
            case ApprovalRequire.Parallel:
                return L("ApprovalRuleParallel");
            default:
                return string.Empty;
        }
    }

    private static string PersonDisplay(WorkflowPersonDto person) =>
        person == null ? string.Empty : PersonName(person.NameEn, person.NameAr, person.Code);

    private static string PersonJobTitle(WorkflowPersonDto person) =>
        person == null ? string.Empty : EssText.Pick(person.JobTitleEn, person.JobTitleAr);

    /// <summary>What this approver has done, or nothing at all where nothing is being asked of them.</summary>
    private string ApproverStatusLabel(WorkflowApproverEntryDto entry)
    {
        if (entry.Origin == WorkflowApproverOrigin.CreatorStep)
        {
            return L("Submitted");
        }

        if (entry.IsApproved)
        {
            return L("Approved");
        }

        if (entry.ActionType.HasValue)
        {
            return L("WorkflowAction_" + entry.ActionType.Value);
        }

        return entry.IsNotRequired ? string.Empty : L("Pending");
    }

    private static string ApproverStatusCss(WorkflowApproverEntryDto entry)
    {
        if (entry.IsApproved || entry.Origin == WorkflowApproverOrigin.CreatorStep)
        {
            return "badge-light-success";
        }

        if (entry.ActionType == RequestActionType.Rejected)
        {
            return "badge-light-danger";
        }

        if (entry.ActionType == RequestActionType.SentBack || entry.ActionType == RequestActionType.SentBackToCreator)
        {
            return "badge-light-warning";
        }

        return "badge-light-primary";
    }

    private string DelegationLabel(WorkflowDelegateEntryDto entry)
    {
        var other = PersonDisplay(entry.DelegatedBy);
        if (string.IsNullOrWhiteSpace(other))
        {
            other = "-";
        }

        return entry.Relation == WorkflowDelegationRelation.ApproverDelegatedOut
            ? L("CoverFor", other)
            : L("OnThisRequestInPlaceOf", other);
    }

    private static string ReadLogWho(WorkflowReadLogEntryDto entry)
    {
        var name = PersonDisplay(entry.Person);
        return !string.IsNullOrWhiteSpace(name) ? name : !string.IsNullOrWhiteSpace(entry.User) ? entry.User : "-";
    }

    private static string LogActorName(WorkflowRequestLogEntryDto entry) =>
        PersonName(entry.ActionByNameEn, entry.ActionByNameAr, entry.ActionByCode);

    private string LogActionLabel(WorkflowRequestLogEntryDto entry) => L("RequestActionType_" + entry.ActionType);

    private string PlatformLabel(WorkflowRequestLogEntryDto entry) => L("WorkflowPlatform_" + entry.Platform);
}
