using HCMv3.Ess;
using HCMv3.Ess.Dto;
using HCMv3.Maui.Core.Components;
using HCMv3.Maui.Core.Helpers;
using HCMv3.Maui.Core.Threading;
using HCMv3.Maui.Services.Navigation;

namespace HCMv3.Maui.Pages.Ess;

/// <summary>
/// The self-service landing page: who the employee is, what they can open, and what is waiting on whom.
/// </summary>
public partial class EssHome : HCMv3MainLayoutPageComponentBase
{
    protected EssContextCache EssContextCache { get; set; }

    private EssContextDto _context;

    private bool _loaded;

    public EssHome()
    {
        EssContextCache = Resolve<EssContextCache>();
    }

    protected override async Task OnInitializedAsync()
    {
        await SetPageHeader(L("EmployeeSelfService"));
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        await SetBusyAsync(async () =>
        {
            await WebRequestExecuter.Execute(
                async () => await EssContextCache.GetAsync(refresh: true),
                result =>
                {
                    _context = result;
                    return Task.CompletedTask;
                });
        });

        _loaded = true;
        StateHasChanged();
    }

    private bool ShowInbox => _context != null && (_context.IsManager || _context.PendingApprovalCount > 0);

    private string EmployeeName => _context == null
        ? string.Empty
        : EssText.Pick(_context.EmployeeFullNameEn, _context.EmployeeFullNameAr);

    private static string RequestTypeName(EssRecentRequestDto request)
    {
        var name = EssText.Pick(request.RequestTypeNameEn, request.RequestTypeNameAr);
        return string.IsNullOrWhiteSpace(name) ? request.RequestType : name;
    }

    private static string RequestSummary(EssRecentRequestDto request)
    {
        return EssText.Pick(request.SummaryEn, request.SummaryAr);
    }

    private void Open(string url)
    {
        NavigationService.NavigateTo(url);
    }
}
