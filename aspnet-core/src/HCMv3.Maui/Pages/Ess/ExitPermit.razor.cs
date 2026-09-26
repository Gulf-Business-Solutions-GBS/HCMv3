using Abp.Application.Services.Dto;
using HCMv3.Ess;
using HCMv3.Ess.Dto;
using HCMv3.GovtRelationsManagement;
using HCMv3.Maui.Core.Components;
using HCMv3.Maui.Core.Threading;

namespace HCMv3.Maui.Pages.Ess;

/// <summary>
/// Self-service exit permits: the employee's own permits, and recording a new one (submit now, or save as a
/// draft to finish later). The permit number is keyed in -- it is issued by the government, not here.
/// </summary>
public partial class ExitPermit : HCMv3MainLayoutPageComponentBase
{
    protected IMyExitPermitAppService MyExitPermitAppService { get; set; }
    protected IEssSessionAppService EssSessionAppService { get; set; }

    private static readonly TravelReason[] TravelReasons =
    {
        TravelReason.PlannedLeave,
        TravelReason.EmergencyLeave,
        TravelReason.PersonalTravel
    };

    private bool _loaded;
    private bool _notLinked;

    private List<MyExitPermitDto> _rows = new();

    private bool _showForm;
    private string _externalRequestNumber;
    private TravelReason _travelReason = TravelReason.PlannedLeave;
    private DateTime? _exitDate;
    private DateTime? _startDate;
    private string _comments;

    public ExitPermit()
    {
        MyExitPermitAppService = Resolve<IMyExitPermitAppService>();
        EssSessionAppService = Resolve<IEssSessionAppService>();
    }

    protected override async Task OnInitializedAsync()
    {
        await SetPageHeader(L("EssMyExitPermits"));

        await SetBusyAsync(async () =>
        {
            await WebRequestExecuter.Execute(
                async () => await EssSessionAppService.GetEssContext(),
                result =>
                {
                    _notLinked = result != null && !result.IsLinkedToEmployee;
                    return Task.CompletedTask;
                });

            if (!_notLinked)
            {
                await LoadListAsync();
            }
        });

        _loaded = true;
    }

    private async Task ReloadAsync()
    {
        await SetBusyAsync(LoadListAsync);
        StateHasChanged();
    }

    private async Task LoadListAsync()
    {
        await WebRequestExecuter.Execute(
            async () => await MyExitPermitAppService.GetMyExitPermits(new GetMyRequestsInput
            {
                MaxResultCount = 50,
                SkipCount = 0
            }),
            result =>
            {
                _rows = result?.Items?.ToList() ?? new List<MyExitPermitDto>();
                return Task.CompletedTask;
            });
    }

    private void OpenForm()
    {
        _externalRequestNumber = string.Empty;
        _travelReason = TravelReason.PlannedLeave;
        _exitDate = null;
        _startDate = null;
        _comments = string.Empty;
        _showForm = true;
    }

    private void CloseForm()
    {
        _showForm = false;
    }

    private async Task SubmitAsync()
    {
        // Checked here as well as by the server, so the employee is told before the round trip -- the same
        // three checks, in the same order, as the Angular form.
        if (string.IsNullOrWhiteSpace(_externalRequestNumber))
        {
            await UserDialogsService.AlertWarn(L("ExitPermitNumberIsRequired"));
            return;
        }

        if (!_exitDate.HasValue)
        {
            await UserDialogsService.AlertWarn(L("ExitDateIsRequired"));
            return;
        }

        if (!_startDate.HasValue)
        {
            await UserDialogsService.AlertWarn(L("StartDateIsRequired"));
            return;
        }

        var input = new RequestExitPermitInput
        {
            ExternalRequestNumber = _externalRequestNumber.Trim(),
            TravelReason = _travelReason,
            ExitDate = _exitDate.Value.Date,
            StartDate = _startDate.Value.Date,
            Comments = _comments
        };

        MyRequestSubmissionDto submitted = null;

        await SetBusyAsync(async () =>
        {
            await WebRequestExecuter.Execute(
                async () => await MyExitPermitAppService.RequestExitPermit(input),
                result =>
                {
                    submitted = result;
                    return Task.CompletedTask;
                });
        });

        if (submitted == null)
        {
            return;
        }

        await UserDialogsService.AlertSuccess(L(submitted.SubmittedForApproval ? "RequestSubmittedForApproval" : "RequestRecorded"));
        _showForm = false;
        await ReloadAsync();
    }

    /// <summary>Files what is on the form as a draft. Nothing is validated -- that happens at submit.</summary>
    private async Task SaveDraftAsync()
    {
        var input = new SaveExitPermitDraftInput
        {
            Id = null,
            ExternalRequestNumber = _externalRequestNumber?.Trim(),
            TravelReason = _travelReason,
            ExitDate = _exitDate?.Date,
            StartDate = _startDate?.Date,
            Comments = _comments
        };

        MyRequestSubmissionDto saved = null;

        await SetBusyAsync(async () =>
        {
            await WebRequestExecuter.Execute(
                async () => await MyExitPermitAppService.SaveMyExitPermitDraft(input),
                result =>
                {
                    saved = result;
                    return Task.CompletedTask;
                });
        });

        if (saved == null)
        {
            return;
        }

        await UserDialogsService.AlertInfo(L("RequestSavedAsDraft"));
        _showForm = false;
        await ReloadAsync();
    }

    /// <summary>Sends a saved draft into the approval chain; the server validates it at this point.</summary>
    private async Task SubmitDraftAsync(MyExitPermitDto row)
    {
        MyRequestSubmissionDto submitted = null;

        await SetBusyAsync(async () =>
        {
            await WebRequestExecuter.Execute(
                async () => await MyExitPermitAppService.SubmitMyExitPermitDraft(new EntityDto(row.Id)),
                result =>
                {
                    submitted = result;
                    return Task.CompletedTask;
                });
        });

        if (submitted == null)
        {
            return;
        }

        await UserDialogsService.AlertSuccess(L(submitted.SubmittedForApproval ? "RequestSubmittedForApproval" : "RequestRecorded"));
        await ReloadAsync();
    }

    private async Task CancelAsync(MyExitPermitDto row)
    {
        var confirmed = await UserDialogsService.Confirm(L("AreYouSure"), L(row.IsDraft ? "DiscardDraft" : "Withdraw"));
        if (!confirmed)
        {
            return;
        }

        var done = false;

        await SetBusyAsync(async () =>
        {
            await WebRequestExecuter.Execute(
                async () => await MyExitPermitAppService.CancelMyExitPermit(new EntityDto(row.Id)),
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

    private string TravelReasonLabel(TravelReason reason) => L("TravelReason_" + reason);
}
