using HCMv3.Ess;
using HCMv3.Ess.Dto;
using HCMv3.Maui.Core.Components;
using HCMv3.Maui.Core.Helpers;
using HCMv3.Maui.Core.Threading;
using HCMv3.Maui.Services.Navigation;

namespace HCMv3.Maui.Pages.Ess;

/// <summary>
/// The approver's inbox: every request waiting on the signed-in user, of every type. Tapping a card opens
/// <see cref="InboxDetail"/>, which shows the request the way the web's detail dialog does and carries the
/// full decision (approve / reject / send back / get feedback) or, for a feedback ask, the feedback box.
/// Approve stays on the card as a one-tap shortcut, as it does on the web inbox's row menu, because its
/// comment is optional; reject and send back require a reason and so live on the detail page.
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

    /// <summary>
    /// Opens the request. A feedback ask says so on the query string, because the detail DTO does not know
    /// which kind of inbox row it was reached from.
    /// </summary>
    private void OpenDetail(EssInboxItemDto item)
    {
        var url = $"{NavigationUrlConsts.InboxDetail}/{item.Id}";

        if (IsFeedback(item))
        {
            url += "?kind=" + InboxDetail.FeedbackKind;
        }

        NavigationService.NavigateTo(url);
    }

    private async Task ApproveAsync(EssInboxItemDto item)
    {
        // Optional comment: null means the dialog was cancelled, an empty string means "approve, no comment".
        // A step that requires a comment is refused by the server, whose message is shown as usual.
        var comment = await UserDialogsService.Prompt(L("Comments"), L("DoYouApproveThisRequest"), L("Approve"), true, L("Cancel"), true);
        if (comment == null)
        {
            return;
        }

        var done = false;

        await SetBusyAsync(async () =>
        {
            await WebRequestExecuter.Execute(
                async () => await EssInboxAppService.Approve(new ApproveRequestInput
                {
                    WorkflowRequestId = item.Id,
                    Comment = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim()
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

        await UserDialogsService.AlertSuccess(L("RequestApproved"));
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
