using Abp.Application.Services.Dto;
using HCMv3.Ess;
using HCMv3.Ess.Dto;
using HCMv3.GeneralMasterInfo.Dto;
using HCMv3.Maui.Core.Components;
using HCMv3.Maui.Core.Helpers;
using HCMv3.Maui.Core.Threading;
using HCMv3.PersonnelAdministration;

namespace HCMv3.Maui.Pages.Ess;

/// <summary>
/// Self-service leave: balances, the employee's own leaves, and applying for a new one. Every rule --
/// balance, overlap, workflow routing -- is enforced by the server; this page only shows and submits.
/// Amend / return / split preview are not in v1.
/// </summary>
public partial class MyLeave : HCMv3MainLayoutPageComponentBase
{
    protected IMyLeaveAppService MyLeaveAppService { get; set; }
    protected IEssLookupAppService EssLookupAppService { get; set; }
    protected IEssSessionAppService EssSessionAppService { get; set; }

    private bool _loaded;
    private bool _notLinked;

    private List<MyLeaveBalanceDto> _balances = new();
    private List<MyLeaveDto> _leaves = new();
    private List<SimpleLookupDto> _absenceTypes = new();
    private LeaveFormSettingsDto _settings;

    private bool _includeHistory;

    private bool _showForm;
    private int? _absenceTypeId;
    private DateTime? _startDate;
    private DateTime? _endDate;
    private string _comments;

    public MyLeave()
    {
        MyLeaveAppService = Resolve<IMyLeaveAppService>();
        EssLookupAppService = Resolve<IEssLookupAppService>();
        EssSessionAppService = Resolve<IEssSessionAppService>();
    }

    protected override async Task OnInitializedAsync()
    {
        await SetPageHeader(L("EssMyLeave"));

        await SetBusyAsync(async () =>
        {
            await WebRequestExecuter.Execute(
                async () => await EssSessionAppService.GetEssContext(),
                result =>
                {
                    _notLinked = result != null && !result.IsLinkedToEmployee;
                    return Task.CompletedTask;
                });

            if (_notLinked)
            {
                return;
            }

            await WebRequestExecuter.Execute(
                async () => await EssLookupAppService.GetAbsenceTypes(),
                result =>
                {
                    _absenceTypes = result ?? new List<SimpleLookupDto>();
                    return Task.CompletedTask;
                });

            await WebRequestExecuter.Execute(
                async () => await MyLeaveAppService.GetLeaveFormSettings(),
                result =>
                {
                    _settings = result;
                    return Task.CompletedTask;
                });

            await LoadListsAsync();
        });

        _loaded = true;
    }

    private async Task ReloadAsync()
    {
        await SetBusyAsync(LoadListsAsync);
        StateHasChanged();
    }

    private async Task LoadListsAsync()
    {
        await WebRequestExecuter.Execute(
            async () => await MyLeaveAppService.GetMyBalances(),
            result =>
            {
                _balances = result ?? new List<MyLeaveBalanceDto>();
                return Task.CompletedTask;
            });

        await WebRequestExecuter.Execute(
            async () => await MyLeaveAppService.GetMyLeaves(new GetMyLeavesInput
            {
                MaxResultCount = 50,
                SkipCount = 0,
                IncludeHistory = _includeHistory
            }),
            result =>
            {
                _leaves = result?.Items?.ToList() ?? new List<MyLeaveDto>();
                return Task.CompletedTask;
            });
    }

    private async Task ToggleHistoryAsync()
    {
        _includeHistory = !_includeHistory;
        await ReloadAsync();
    }

    private void OpenForm()
    {
        _absenceTypeId = null;
        _startDate = DateTime.Today;
        _endDate = DateTime.Today;
        _comments = string.Empty;
        _showForm = true;
    }

    private void CloseForm()
    {
        _showForm = false;
    }

    /// <summary>
    /// The tenant's default payment type, or the DTO's own default (pay with payroll) when the setting is
    /// unset. Advance / pay-full choices are not offered on the phone in v1.
    /// </summary>
    private LeavePaymentType PaymentType =>
        _settings != null && _settings.DefaultLeavePaymentType != LeavePaymentType.None
            ? _settings.DefaultLeavePaymentType
            : LeavePaymentType.PayWithPayroll;

    private async Task ApplyAsync()
    {
        if (!_absenceTypeId.HasValue || !_startDate.HasValue || !_endDate.HasValue)
        {
            await UserDialogsService.AlertWarn(L("FormIsNotValidMessage"));
            return;
        }

        var input = new ApplyForLeaveInput
        {
            AbsenceTypeId = _absenceTypeId.Value,
            StartDate = _startDate.Value.Date,
            EndDate = _endDate.Value.Date,
            Comments = _comments,
            LeavePaymentType = PaymentType
        };

        ApplyForLeaveResultDto applied = null;

        await SetBusyAsync(async () =>
        {
            await WebRequestExecuter.Execute(
                async () => await MyLeaveAppService.ApplyForLeave(input),
                result =>
                {
                    applied = result;
                    return Task.CompletedTask;
                });
        });

        if (applied == null)
        {
            return;
        }

        await UserDialogsService.AlertSuccess(L(applied.SubmittedForApproval ? "LeaveSubmittedForApproval" : "LeaveRecorded"));
        _showForm = false;
        await ReloadAsync();
    }

    private async Task CancelLeaveAsync(MyLeaveDto leave)
    {
        var confirmed = await UserDialogsService.Confirm(L("LeaveCancelWarningMessage"), L("AreYouSure"));
        if (!confirmed)
        {
            return;
        }

        var done = false;

        await SetBusyAsync(async () =>
        {
            await WebRequestExecuter.Execute(
                async () => await MyLeaveAppService.CancelMyLeave(new EntityDto(leave.Id)),
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

        await UserDialogsService.AlertSuccess(L("SuccessfullyDeleted"));
        await ReloadAsync();
    }

    private static string BalanceTypeName(MyLeaveBalanceDto balance) =>
        EssText.Pick(balance.AbsenceTypeNameEn, balance.AbsenceTypeNameAr);

    private static string LeaveTypeName(MyLeaveDto leave) =>
        EssText.Pick(leave.AbsenceTypeNameEn, leave.AbsenceTypeNameAr);

    private static string LookupName(SimpleLookupDto item) =>
        EssText.Pick(item.NameEn, item.NameAr);
}
