using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Extensions;
using Abp.Linq.Extensions;
using Abp.Timing;
using Abp.UI;
using HCMv3.Authorization;
using HCMv3.Dto;
using HCMv3.EngineRuns;
using HCMv3.EngineRuns.Dto;
using HCMv3.PayrollManagement.Dto;
using HCMv3.PayrollManagement.Exporting;
using HCMv3.PayrollManagement.Services;
using HCMv3.SystemSettings;
using HCMv3.SystemSettings.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.PayrollManagement;

/// <inheritdoc cref="IPayrollRunSetupAppService"/>
/// <remarks>
/// Port of the guidance half of legacy <c>HCM10.Web/UserControls/PayrollManagement/uscPayrollEngine.ascx.cs</c>:
/// <c>SetCurrentDates</c> (lines 333-420), <c>SetDates</c> (322-331), <c>BindOffReasonType</c> (156-168),
/// <c>BuildEmployeePopupList</c> (788-1020), the popup filters / select all (1093-1313) and
/// <c>btnExportPayrollLog_Click</c> (2166). Every method is gated on the Payroll Engine VIEW permission: nothing here
/// changes data.
/// </remarks>
public class PayrollRunSetupAppService : HCMv3AppServiceBase, IPayrollRunSetupAppService
{
    private readonly IRepository<PayrollControlRecord> _payrollControlRecordRepository;
    private readonly IRepository<PayrollArea> _payrollAreaRepository;
    private readonly IRepository<PayrollReasonType> _payrollReasonTypeRepository;
    private readonly IPayrollRunCandidateSelector _candidateSelector;
    private readonly ISysSettingsAccessor _sysSettingsAccessor;
    private readonly EngineRunProgressReader _progressReader;
    private readonly IPayrollRunEmployeesExcelExporter _excelExporter;

    public PayrollRunSetupAppService(
        IRepository<PayrollControlRecord> payrollControlRecordRepository,
        IRepository<PayrollArea> payrollAreaRepository,
        IRepository<PayrollReasonType> payrollReasonTypeRepository,
        IPayrollRunCandidateSelector candidateSelector,
        ISysSettingsAccessor sysSettingsAccessor,
        EngineRunProgressReader progressReader,
        IPayrollRunEmployeesExcelExporter excelExporter)
    {
        _payrollControlRecordRepository = payrollControlRecordRepository;
        _payrollAreaRepository = payrollAreaRepository;
        _payrollReasonTypeRepository = payrollReasonTypeRepository;
        _candidateSelector = candidateSelector;
        _sysSettingsAccessor = sysSettingsAccessor;
        _progressReader = progressReader;
        _excelExporter = excelExporter;
    }

    #region << Context (legacy SetCurrentDates) >>

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_PayrollEngine)]
    public async Task<PayrollRunContextDto> GetPayrollRunContext(string payrollAreaName)
    {
        var timeSettings = await _sysSettingsAccessor.GetTimeSettingsAsync();
        var autorun = timeSettings?.EnableTimeEngineAutorun ?? false;

        var context = new PayrollRunContextDto
        {
            PayrollAreaName = payrollAreaName,
            EnableTimeEngineAutorun = autorun,
            PeriodStatus = PayrollRunPeriodStatus.NoControlRecord
        };

        // Legacy lines 337-346.
        if (payrollAreaName.IsNullOrWhiteSpace())
        {
            context.Message = L("PayrollRunPleaseSelectPayrollArea");
            return context;
        }

        var record = await FindControlRecordAsync(payrollAreaName);

        // Legacy lines 411-419.
        if (record == null)
        {
            context.Message = L("PayrollRunControlRecordIsEmpty");
            return context;
        }

        context.HasControlRecord = true;
        context.PayrollMonth = record.PayrollMonth;
        context.PayrollYear = record.PayrollYear;
        context.CalendarMonthStart = record.CalendarMonthStart;
        context.CalendarMonthEnd = record.CalendarMonthEnd;
        context.PeriodLocked = record.PeriodLocked;
        context.PeriodClosed = record.PeriodClosed;
        context.PeriodStatus = GetPeriodStatus(record);
        context.PeriodStatusText = BuildPeriodStatusText(record, context.PeriodStatus);

        // Legacy lines 368-409: locked => Regular (green), closed => OffCycle (yellow), neither => no choice made.
        context.SuggestedRunType = context.PeriodStatus switch
        {
            PayrollRunPeriodStatus.Regular => PayrollRunType.Regular,
            PayrollRunPeriodStatus.OffCycle => PayrollRunType.OffCycle,
            _ => null
        };

        // The engine's own gates (PayrollControlRecordGuard), so the screen says up front what the run will accept.
        context.RegularRunAllowed = record.PeriodLocked && !record.PeriodClosed;
        context.OffCycleRunAllowed = record.PeriodClosed;
        context.RegularPayInDate = record.CalendarMonthEnd.Date;
        context.OffCyclePayInDate = PayrollControlRecordGuard.NextPeriodPayInDate(record);

        // Legacy Setup (lines 92-109): the payment date is not asked for when time-engine autorun is on -- the
        // engine ignores it then (PayrollControlRecordGuard.ResolveOffCyclePayInDate).
        context.ShowOffCyclePaymentDate = !autorun;

        var nextPeriodEnd = context.OffCyclePayInDate.Value;
        var nextPeriodStart = new DateTime(nextPeriodEnd.Year, nextPeriodEnd.Month, 1);
        context.OffCyclePaymentDateFrom = nextPeriodStart;
        context.OffCyclePaymentDateTo = nextPeriodEnd;

        var payrollSettings = await _sysSettingsAccessor.GetPayrollSettingsAsync();
        context.DefaultOffCyclePaymentDate = PayrollRunCandidateSelector.PickUpToDate(
            payrollSettings?.PickOffcyclePayment ?? PickOffcyclePayment.CurrentDate,
            nextPeriodStart,
            nextPeriodEnd,
            Clock.Now);

        return context;
    }

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_PayrollEngine)]
    public async Task<PayrollRunPeriodDatesDto> GetPayrollRunPeriodDates(string payrollAreaName, int payrollMonth, int payrollYear)
    {
        var (start, end) = await GetPeriodRangeAsync(payrollAreaName, payrollMonth, payrollYear);

        return new PayrollRunPeriodDatesDto
        {
            PayrollMonth = payrollMonth,
            PayrollYear = payrollYear,
            CalendarMonthStart = start,
            CalendarMonthEnd = end
        };
    }

    #endregion

    #region << Off-cycle reasons (legacy BindOffReasonType) >>

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_PayrollEngine)]
    public async Task<List<PayrollRunOffCycleReasonDto>> GetOffCycleReasons()
    {
        var arabic = IsArabic();

        var rows = await _payrollReasonTypeRepository.GetAll()
            .OrderBy(r => r.Id)
            .Select(r => new { r.Id, r.Code, r.NameEn, r.NameAr })
            .ToListAsync();

        if (rows.Any())
        {
            return rows.Select(r => new PayrollRunOffCycleReasonDto
            {
                Id = r.Id,
                Code = r.Code,
                Value = r.NameEn,
                DisplayName = arabic && !r.NameAr.IsNullOrWhiteSpace() ? r.NameAr : r.NameEn,
                IsKnownToEngine = IsKnownToEngine(r.NameEn)
            }).ToList();
        }

        // A tenant that has not filled the table yet still gets the three reasons the engine understands, so an
        // off-cycle can be run at all. (The register then carries no reason id, as the engine finds none.)
        return new List<PayrollRunOffCycleReasonDto>
        {
            Fallback(PayrollOffCycleReasons.EndOfService, "EndOfService"),
            Fallback(PayrollOffCycleReasons.AdvanceLeave, "AdvanceLeave"),
            Fallback(PayrollOffCycleReasons.LeaveEncashment, "LeaveEncashment")
        };
    }

    #endregion

    #region << Employee picker (legacy BuildEmployeePopupList + filters) >>

    [HttpPost]
    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_PayrollEngine)]
    public async Task<PayrollRunCandidatesOutput> GetPayrollRunCandidates(GetPayrollRunCandidatesInput input)
    {
        var (all, payrollDate, statusText) = await BuildCandidateListAsync(input);

        var filtered = ApplyFilters(all, input);

        var page = filtered
            .AsQueryable()
            .OrderBy(input.Sorting.IsNullOrWhiteSpace() ? "EmployeeCode" : input.Sorting)
            .PageBy(input)
            .ToList();

        // Legacy UpdateFilterCounts (lines 1265-1290): counted over the whole list, whatever is filtered.
        return new PayrollRunCandidatesOutput
        {
            TotalCount = filtered.Count,
            Items = page,
            AllCount = all.Count,
            ProcessedCount = all.Count(c => c.PayrollStatus == PayrollRunCandidateStatus.Processed),
            PendingCount = all.Count(c => c.PayrollStatus == PayrollRunCandidateStatus.Pending),
            SelectedCount = all.Count(c => c.IsSelected),
            PayrollDate = payrollDate,
            PeriodStatusText = statusText
        };
    }

    [HttpPost]
    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_PayrollEngine)]
    public async Task<List<string>> GetPayrollRunCandidateCodes(GetPayrollRunCandidatesInput input)
    {
        var (all, _, _) = await BuildCandidateListAsync(input);

        return ApplyFilters(all, input)
            .OrderBy(c => c.EmployeeCode)
            .Select(c => c.EmployeeCode)
            .ToList();
    }

    #endregion

    #region << Export (legacy btnExportPayrollLog_Click) >>

    [HttpPost]
    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_PayrollEngine)]
    public async Task<FileDto> GetPayrollRunEmployeesToExcel(GetPayrollRunEmployeesToExcelInput input)
    {
        input ??= new GetPayrollRunEmployeesToExcelInput();

        // Same reader and same filters as the on-screen grid (IPayrollEngineAppService.GetPayrollEngineRunEmployees),
        // so the file holds exactly what the operator was looking at -- every page of it.
        var result = await _progressReader.GetEmployeesAsync(EngineType.PayrollEngine, new GetEngineRunEmployeesInput
        {
            RunId = input.RunId,
            Filter = input.Filter,
            OnlyFailed = input.OnlyFailed,
            Sorting = "SequenceNo",
            SkipCount = 0,
            MaxResultCount = int.MaxValue
        });

        return await _excelExporter.ExportToFile(result.Items.ToList());
    }

    #endregion

    #region << Helpers >>

    private async Task<PayrollControlRecord> FindControlRecordAsync(string payrollAreaName)
    {
        // Legacy: PayrollControlRecord.Where(r => r.PayrollArea.Name == name).FirstOrDefault() -- the engine's lookup too.
        return await _payrollControlRecordRepository.GetAll()
            .Where(r => r.PayrollArea != null && r.PayrollArea.Name == payrollAreaName)
            .OrderBy(r => r.Id)
            .FirstOrDefaultAsync();
    }

    private async Task<(DateTime Start, DateTime End)> GetPeriodRangeAsync(string payrollAreaName, int payrollMonth, int payrollYear)
    {
        var payrollPeriodCode = await _payrollAreaRepository.GetAll()
            .Where(a => a.Name == payrollAreaName && a.PayrollPeriod != null)
            .Select(a => a.PayrollPeriod.Code)
            .FirstOrDefaultAsync();

        if (payrollPeriodCode.IsNullOrWhiteSpace())
        {
            throw new UserFriendlyException(L("IncorrectPayrollPeriod"));
        }

        try
        {
            return PayrollPeriodShiftCalculator.GetCalendarMonthRange(payrollPeriodCode, payrollMonth, payrollYear);
        }
        catch (ArgumentException)
        {
            throw new UserFriendlyException(L("IncorrectPayrollPeriod"));
        }
    }

    private async Task<(List<PayrollRunCandidateDto> All, DateTime? PayrollDate, string StatusText)> BuildCandidateListAsync(
        GetPayrollRunCandidatesInput input)
    {
        var empty = new List<PayrollRunCandidateDto>();

        if (input == null || input.PayrollAreaName.IsNullOrWhiteSpace())
        {
            return (empty, null, null);
        }

        DateTime payrollDate;
        string statusText = null;

        if (input.SimulationMonth.HasValue && input.SimulationYear.HasValue)
        {
            // Legacy: the Other-period radio replaces txtEndDate with that month's end (SetDates), and the popup
            // reads txtEndDate.
            var (_, end) = await GetPeriodRangeAsync(input.PayrollAreaName, input.SimulationMonth.Value, input.SimulationYear.Value);
            payrollDate = end;
        }
        else
        {
            var record = await FindControlRecordAsync(input.PayrollAreaName);
            if (record == null)
            {
                return (empty, null, L("PayrollRunControlRecordIsEmpty"));
            }

            payrollDate = record.CalendarMonthEnd;
            statusText = BuildPeriodStatusText(record, GetPeriodStatus(record));
        }

        var payrollSettings = await _sysSettingsAccessor.GetPayrollSettingsAsync();

        var candidates = await _candidateSelector.GetCandidatesAsync(
            input.PayrollAreaName,
            input.PayrollRunType,
            input.PayrollRunType == PayrollRunType.OffCycle ? input.OffCycleReason : null,
            payrollDate,
            payrollSettings?.PickOffcyclePayment ?? PickOffcyclePayment.CurrentDate,
            Clock.Now);

        var selectedCodes = SplitCodes(input.SelectedEmployeeCodes);
        var arabic = IsArabic();

        var all = candidates.Select(c => new PayrollRunCandidateDto
        {
            EmployeeId = c.EmployeeId,
            EmployeeCode = c.EmployeeCode,
            EmployeeName = arabic && !c.FullNameAr.IsNullOrWhiteSpace() ? c.FullNameAr : c.FullNameEn,
            PayrollStatus = c.IsProcessed ? PayrollRunCandidateStatus.Processed : PayrollRunCandidateStatus.Pending,
            IsSelected = c.EmployeeCode != null && selectedCodes.Contains(c.EmployeeCode)
        }).ToList();

        return (all, payrollDate.Date, statusText);
    }

    /// <summary>Legacy gridEmployees_DataBinding (lines 1022-1053) plus the grid's own text filter.</summary>
    private static List<PayrollRunCandidateDto> ApplyFilters(List<PayrollRunCandidateDto> all, GetPayrollRunCandidatesInput input)
    {
        IEnumerable<PayrollRunCandidateDto> query = all;

        switch (input.StatusFilter)
        {
            case PayrollRunCandidateFilter.Processed:
                query = query.Where(c => c.PayrollStatus == PayrollRunCandidateStatus.Processed);
                break;
            case PayrollRunCandidateFilter.Pending:
                query = query.Where(c => c.PayrollStatus == PayrollRunCandidateStatus.Pending);
                break;
            case PayrollRunCandidateFilter.Selected:
                query = query.Where(c => c.IsSelected);
                break;
        }

        if (!input.Filter.IsNullOrWhiteSpace())
        {
            var filter = input.Filter.Trim();
            query = query.Where(c =>
                (c.EmployeeCode ?? string.Empty).Contains(filter, StringComparison.OrdinalIgnoreCase)
                || (c.EmployeeName ?? string.Empty).Contains(filter, StringComparison.OrdinalIgnoreCase));
        }

        return query.ToList();
    }

    private static HashSet<string> SplitCodes(string codes)
    {
        // Legacy lines 823-829: split on ',' and trim, case-insensitive.
        return codes.IsNullOrWhiteSpace()
            ? new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            : new HashSet<string>(
                codes.Split(',').Select(c => c.Trim()).Where(c => c.Length > 0),
                StringComparer.OrdinalIgnoreCase);
    }

    private static PayrollRunPeriodStatus GetPeriodStatus(PayrollControlRecord record)
    {
        if (!record.PeriodLocked && !record.PeriodClosed)
        {
            return PayrollRunPeriodStatus.DataCorrection;
        }

        if (record.PeriodLocked && !record.PeriodClosed)
        {
            return PayrollRunPeriodStatus.Regular;
        }

        if (!record.PeriodLocked && record.PeriodClosed)
        {
            return PayrollRunPeriodStatus.OffCycle;
        }

        return PayrollRunPeriodStatus.Unknown;
    }

    /// <summary>
    /// Legacy <c>PayrollControlRecord.PayrollStatus</c> (HCM10.Module PayrollControlRecord.cs lines 116-172): the month
    /// comes from <c>CalendarMonthStart</c>; a closed period names the NEXT month, which is where its off-cycle lands.
    /// </summary>
    private string BuildPeriodStatusText(PayrollControlRecord record, PayrollRunPeriodStatus status)
    {
        if (record.CalendarMonthStart == default(DateTime))
        {
            return string.Empty;
        }

        var start = record.CalendarMonthStart;

        return status switch
        {
            PayrollRunPeriodStatus.DataCorrection => L("PayrollRunStatusDataCorrection", MonthYear(start)),
            PayrollRunPeriodStatus.Regular => L("PayrollRunStatusRegular", MonthYear(start)),
            PayrollRunPeriodStatus.OffCycle => L("PayrollRunStatusOffCycle", MonthYear(start.AddMonths(1))),
            _ => string.Empty
        };
    }

    private static string MonthYear(DateTime date)
    {
        return CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(date.Month) + "-" + date.Year;
    }

    private static bool IsKnownToEngine(string nameEn)
    {
        return nameEn == PayrollOffCycleReasons.EndOfService
               || nameEn == PayrollOffCycleReasons.AdvanceLeave
               || nameEn == PayrollOffCycleReasons.LeaveEncashment;
    }

    private PayrollRunOffCycleReasonDto Fallback(string value, string localizationKey)
    {
        return new PayrollRunOffCycleReasonDto
        {
            Value = value,
            DisplayName = L(localizationKey),
            IsKnownToEngine = true
        };
    }

    private static bool IsArabic()
    {
        return CultureInfo.CurrentUICulture.Name.StartsWith("ar", StringComparison.OrdinalIgnoreCase);
    }

    #endregion
}
