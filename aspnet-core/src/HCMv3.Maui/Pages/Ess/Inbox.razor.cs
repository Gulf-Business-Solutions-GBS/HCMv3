using HCMv3.Ess;
using HCMv3.Ess.Dto;
using HCMv3.Maui.Core.Components;
using HCMv3.Maui.Core.Helpers;
using HCMv3.Maui.Core.Threading;

namespace HCMv3.Maui.Pages.Ess;

/// <summary>
/// The approver's inbox: every request waiting on the signed-in user, of every type. Approve takes an
/// optional comment; reject and send back require one (as on the Angular inbox and on the server). A
/// feedback request is not a decision -- it offers only "submit feedback", with a required comment.
/// </summary>
public partial class Inbox : HCMv3MainLayoutPageComponentBase
{
    protected IEssInboxAppService EssInboxAppService { get; set; }

    private bool _loaded;
    private List<EssInboxItemDto> _items = new();

    public Inbox()
    {
        EssInboxAppService = Resolve<IEssInboxAppService>();
    }

    protected override async Task OnInitializedAsync()
    {
        await SetPageHeader(L("EssInbox"));
        await SetBusyAsync(LoadAsync);
        _loaded = true;
    }

    private async Task ReloadAsync()
    {
        await SetBusyAsync(LoadAsync);
        StateHasChanged();
    }

    private async Task LoadAsync()
    {
        await WebRequestExecuter.Execute(
            async () => await EssInboxAppService.GetMyInbox(new GetMyInboxInput
            {
                MaxResultCount = 50,
                SkipCount = 0
            }),
            result =>
            {
                _items = result?.Items?.ToList() ?? new List<EssInboxItemDto>();
                return Task.CompletedTask;
            });
    }

    private async Task ApproveAsync(EssInboxItemDto item)
    {
        // Optional comment: null means the dialog was cancelled, an empty string means "approve, no comment".
        var comment = await UserDialogsService.Prompt(L("Comments"), L("DoYouApproveThisRequest"), L("Approve"), true, L("Cancel"), true);
        if (comment == null)
        {
            return;
        }

        await ActAsync(
            () => EssInboxAppService.Approve(new ApproveRequestInput
            {
                WorkflowRequestId = item.Id,
                Comment = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim()
            }),
            "RequestApproved");
    }

    private async Task RejectAsync(EssInboxItemDto item)
    {
        var comment = await UserDialogsService.Prompt(L("Comments"), L("Reject"), L("Reject"), true, L("Cancel"), true);
        if (comment == null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(comment))
        {
            await UserDialogsService.AlertWarn(L("AReasonIsRequiredToReject"));
            return;
        }

        await ActAsync(
            () => EssInboxAppService.Reject(new RejectRequestInput
            {
                WorkflowRequestId = item.Id,
                Comment = comment.Trim()
            }),
            "RequestRejected");
    }

    private async Task SendBackAsync(EssInboxItemDto item)
    {
        var comment = await UserDialogsService.Prompt(L("Comments"), L("SendBack"), L("SendBack"), true, L("Cancel"), true);
        if (comment == null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(comment))
        {
            await UserDialogsService.AlertWarn(L("AReasonIsRequiredToSendBack"));
            return;
        }

        await ActAsync(
            () => EssInboxAppService.SendBack(new SendBackRequestInput
            {
                WorkflowRequestId = item.Id,
                Comment = comment.Trim()
            }),
            "RequestSentBack");
    }

    private async Task SubmitFeedbackAsync(EssInboxItemDto item)
    {
        var comment = await UserDialogsService.Prompt(L("YouWereAskedForFeedbackNotApproval"), L("YourFeedback"), L("SubmitFeedback"), true, L("Cancel"), true);
        if (comment == null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(comment))
        {
            await UserDialogsService.AlertWarn(L("FeedbackIsRequired"));
            return;
        }

        await ActAsync(
            () => EssInboxAppService.SubmitFeedback(new SubmitFeedbackInput
            {
                WorkflowRequestId = item.Id,
                Comment = comment.Trim()
            }),
            "FeedbackSubmitted");
    }

    private async Task ActAsync(Func<Task> call, string successKey)
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
        await ReloadAsync();
    }

    private static bool IsFeedback(EssInboxItemDto item) => item.Kind == EssInboxItemKind.FeedbackRequest;

    private static string TypeName(EssInboxItemDto item)
    {
        var name = EssText.Pick(item.RequestTypeNameEn, item.RequestTypeNameAr);
        return string.IsNullOrWhiteSpace(name) ? item.RequestType : name;
    }

    private static string Summary(EssInboxItemDto item) => EssText.Pick(item.SummaryEn, item.SummaryAr);

    private static string RequesterName(EssInboxItemDto item) => EssText.Pick(item.RequesterNameEn, item.RequesterNameAr);

    private static string FeedbackRequestedByName(EssInboxItemDto item) =>
        EssText.Pick(item.FeedbackRequestedByNameEn, item.FeedbackRequestedByNameAr);
}
