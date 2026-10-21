using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp;
using Abp.Dependency;
using Abp.Domain.Repositories;
using Abp.Extensions;
using Abp.Linq.Extensions;
using Abp.Runtime.Session;
using Abp.Timing;
using Abp.UI;
using HCMv3.PayrollManagement.Costing;
using HCMv3.PayrollManagement.Dto;
using HCMv3.PayrollManagement.Services;
using HCMv3.PersonnelAdministration;
using HCMv3.Reports.Dto;
using MiniExcelLibs;

namespace HCMv3.PayrollManagement;

/// <summary>
/// Everything the Costing File and Post JV screens share. Legacy had ONE screen for both
/// (<c>uscCostingFile.ascx</c>, with Generate Costing File and Post JV side by side); HCMv3 has two screens
/// with their own permissions, so the shared behaviour lives here once and each app service exposes it under
/// its own permission. (Calling one app service from the other would demand BOTH permissions, because ABP
/// checks the callee's too.)
/// </summary>
public class CostingScreenService : AbpServiceBase, ITransientDependency
{
    private readonly IRepository<PayrollArea> _payrollAreaRepository;
    private readonly IRepository<PayrollControlRecord> _payrollControlRecordRepository;
    private readonly IRepository<Employee> _employeeRepository;
    private readonly IRepository<CostRegister> _costRegisterRepository;
    private readonly IEmployeePayrollAreaResolver _payrollAreaResolver;
    private readonly IJournalVoucherBuilder _journalVoucherBuilder;
    private readonly IFinanceRunLog _runLog;
    private readonly IAbpSession _session;

    public CostingScreenService(
        IRepository<PayrollArea> payrollAreaRepository,
        IRepository<PayrollControlRecord> payrollControlRecordRepository,
        IRepository<Employee> employeeRepository,
        IRepository<CostRegister> costRegisterRepository,
        IEmployeePayrollAreaResolver payrollAreaResolver,
        IJournalVoucherBuilder journalVoucherBuilder,
        IFinanceRunLog runLog,
        IAbpSession session)
    {
        _payrollAreaRepository = payrollAreaRepository;
        _payrollControlRecordRepository = payrollControlRecordRepository;
        _employeeRepository = employeeRepository;
        _costRegisterRepository = costRegisterRepository;
        _payrollAreaResolver = payrollAreaResolver;
        _journalVoucherBuilder = journalVoucherBuilder;
        _runLog = runLog;
        _session = session;
        LocalizationSourceName = HCMv3Consts.LocalizationSourceName;
    }

    #region << Inputs >>

    /// <summary>
    /// Legacy input guards (<c>ExecuteLongOperation</c> lines 243-247, <c>Main</c> lines 330-337,
    /// <c>ExportGrid</c> lines 1580-1596).
    /// </summary>
    public void ValidateInput(CostingRunInputDto input)
    {
        if (input == null || (string.IsNullOrWhiteSpace(input.PayrollAreaName) && string.IsNullOrWhiteSpace(input.EmployeeCode)))
        {
            throw new UserFriendlyException(L("NoPayrollAreaOrEmployeeCodeEntered"));
        }

        if (input.PayInDate == default)
        {
            throw new UserFriendlyException(L("NoPayDateDefinedForJvPost"));
        }

        if (input.PostingDate == default)
        {
            throw new UserFriendlyException(L("PostingDateNotProperFormat"));
        }
    }

    /// <summary>Legacy <c>BindPayrollArea</c>.</summary>
    public List<PayrollAreaLookupDto> GetPayrollAreasForLookup()
    {
        return _payrollAreaRepository.GetAll()
            .OrderBy(a => a.Name)
            .Select(a => new PayrollAreaLookupDto
            {
                Id = a.Id,
                Code = a.Code,
                Name = a.Name,
                IsFiConfigured = a.FICompanyId != null && a.FIBankId != null
            })
            .ToList();
    }

    /// <summary>
    /// Legacy <c>SetCurrentDates</c> (lines 1510-1525): month, year, start and end from the area's control
    /// record, and the posting date defaulted to the period END when the box is empty -- what both the live
    /// D365 screen and the older AX screen (<c>uscPostJV.ascx.cs</c> lines 937-940) did. The same lookup as
    /// the Payroll Engine screen's (<c>PayrollRunSetupAppService.GetPayrollRunContext</c>); it is repeated
    /// here because that method demands the Payroll Engine permission, which a finance user need not have.
    /// </summary>
    public CostingPeriodContextDto GetPeriodContext(string payrollAreaName)
    {
        var context = new CostingPeriodContextDto { PayrollAreaName = payrollAreaName };

        if (payrollAreaName.IsNullOrWhiteSpace())
        {
            context.Message = L("PayrollRunPleaseSelectPayrollArea");
            return context;
        }

        var record = _payrollControlRecordRepository.GetAll()
            .Where(r => r.PayrollArea != null && r.PayrollArea.Name == payrollAreaName)
            .OrderBy(r => r.Id)
            .Select(r => new { r.PayrollMonth, r.PayrollYear, r.CalendarMonthStart, r.CalendarMonthEnd })
            .FirstOrDefault();

        if (record == null)
        {
            context.Message = L("PayrollRunControlRecordIsEmpty");
            return context;
        }

        context.HasControlRecord = true;
        context.PayrollMonth = record.PayrollMonth;
        context.PayrollYear = record.PayrollYear;
        context.CalendarMonthStart = record.CalendarMonthStart.Date;
        context.CalendarMonthEnd = record.CalendarMonthEnd.Date;
        context.DefaultPostingDate = record.CalendarMonthEnd.Date;
        return context;
    }

    /// <summary>
    /// Legacy <c>SetDates</c> (lines 1499-1508): <c>PayrollControlRecord.GetStartEndDateFromPayrollPeriod</c>
    /// for the typed month and year, through the same calculator the Payroll Engine screen uses.
    /// </summary>
    public CostingPeriodDatesDto GetPeriodDates(string payrollAreaName, int payrollMonth, int payrollYear)
    {
        var payrollPeriodCode = _payrollAreaRepository.GetAll()
            .Where(a => a.Name == payrollAreaName && a.PayrollPeriod != null)
            .Select(a => a.PayrollPeriod.Code)
            .FirstOrDefault();

        if (payrollPeriodCode.IsNullOrWhiteSpace() || payrollMonth < 1 || payrollMonth > 12 || payrollYear < 1900)
        {
            throw new UserFriendlyException(L("IncorrectPayrollPeriod"));
        }

        try
        {
            var (start, end) = PayrollPeriodShiftCalculator.GetCalendarMonthRange(payrollPeriodCode, payrollMonth, payrollYear);
            return new CostingPeriodDatesDto
            {
                PayrollMonth = payrollMonth,
                PayrollYear = payrollYear,
                CalendarMonthStart = start,
                CalendarMonthEnd = end
            };
        }
        catch (ArgumentException)
        {
            throw new UserFriendlyException(L("IncorrectPayrollPeriod"));
        }
    }

    #endregion

    #region << Employee popup (legacy BuildEmployeePopupList) >>

    public async Task<PayrollRunCandidatesOutput> GetCandidatesAsync(GetCostingFileCandidatesInput input)
    {
        var all = await BuildCandidatesAsync(input);
        var filtered = ApplyCandidateFilters(all, input);

        var page = filtered
            .AsQueryable()
            .OrderBy(input.Sorting.IsNullOrWhiteSpace() ? "EmployeeCode" : input.Sorting)
            .PageBy(input)
            .ToList();

        // Legacy UpdateFilterCounts (lines 2858-2884): counted over the whole list, whatever is filtered.
        return new PayrollRunCandidatesOutput
        {
            TotalCount = filtered.Count,
            Items = page,
            AllCount = all.Count,
            ProcessedCount = all.Count(c => c.PayrollStatus == PayrollRunCandidateStatus.Processed),
            PendingCount = all.Count(c => c.PayrollStatus == PayrollRunCandidateStatus.Pending),
            SelectedCount = all.Count(c => c.IsSelected),
            PayrollDate = input == null || input.PayInDate == default
                ? null
                : CostEngineService.NormalizeToEndOfMonth(input.PayInDate)
        };
    }

    public async Task<List<string>> GetCandidateCodesAsync(GetCostingFileCandidatesInput input)
    {
        var all = await BuildCandidatesAsync(input);

        return ApplyCandidateFilters(all, input)
            .OrderBy(c => c.EmployeeCode, StringComparer.Ordinal)
            .Select(c => c.EmployeeCode)
            .ToList();
    }

    /// <summary>
    /// Legacy lines 2632-2775. The employees of the area on the period end date; for a Regular run the ACTIVE
    /// ones, each "Processed" when a cost register already exists for the period and run type, otherwise
    /// "Pending".
    ///
    /// <para><b>OffCycle lists nobody, as in legacy.</b> The whole off-cycle branch of legacy's popup is
    /// commented out (lines 2700-2770), so its eligible set stays empty and the popup shows no rows for an
    /// off-cycle run. Kept as-is; the code box can still be typed in.</para>
    /// </summary>
    private async Task<List<PayrollRunCandidateDto>> BuildCandidatesAsync(GetCostingFileCandidatesInput input)
    {
        var empty = new List<PayrollRunCandidateDto>();

        if (input == null || input.PayrollAreaName.IsNullOrWhiteSpace() || input.PayInDate == default)
        {
            return empty;
        }

        if (input.PayrollRunType != PayrollRunType.Regular)
        {
            return empty;
        }

        var payrollDate = CostEngineService.NormalizeToEndOfMonth(input.PayInDate);

        var areaEmployeeIds = await _payrollAreaResolver.GetEmployeeIdsInPayrollAreaAsync(input.PayrollAreaName, payrollDate);
        if (!areaEmployeeIds.Any())
        {
            return empty;
        }

        var employees = _employeeRepository.GetAll()
            .Where(e => areaEmployeeIds.Contains(e.Id) && e.Status == EmployeeStatus.Active)
            .Select(e => new { e.Id, e.Code, e.FullNameEn })
            .ToList();

        var activeIds = employees.Select(e => e.Id).ToList();

        var processed = new HashSet<int>(_costRegisterRepository.GetAll()
            .Where(r => activeIds.Contains(r.EmployeeId)
                        && r.PayrollRunType == input.PayrollRunType
                        && r.Period == payrollDate)
            .Select(r => r.EmployeeId)
            .ToList());

        var selectedCodes = SplitCodes(input.SelectedEmployeeCodes);

        return employees
            .Select(e => new PayrollRunCandidateDto
            {
                EmployeeId = e.Id,
                EmployeeCode = e.Code,
                EmployeeName = e.FullNameEn,
                PayrollStatus = processed.Contains(e.Id) ? PayrollRunCandidateStatus.Processed : PayrollRunCandidateStatus.Pending,
                IsSelected = e.Code != null && selectedCodes.Contains(e.Code)
            })
            .ToList();
    }

    /// <summary>Legacy <c>gridEmployees_DataBinding</c> (lines 2777-2805) plus the grid's own text filter.</summary>
    private static List<PayrollRunCandidateDto> ApplyCandidateFilters(List<PayrollRunCandidateDto> all,
                                                                     GetCostingFileCandidatesInput input)
    {
        IEnumerable<PayrollRunCandidateDto> query = all;

        switch (input?.StatusFilter)
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

        if (input != null && !input.Filter.IsNullOrWhiteSpace())
        {
            var filter = input.Filter.Trim();
            query = query.Where(c =>
                (c.EmployeeCode ?? "").Contains(filter, StringComparison.OrdinalIgnoreCase)
                || (c.EmployeeName ?? "").Contains(filter, StringComparison.OrdinalIgnoreCase));
        }

        return query.ToList();
    }

    private static HashSet<string> SplitCodes(string codes)
    {
        // Legacy lines 2662-2668: split on ',' and trim, case-insensitive.
        return codes.IsNullOrWhiteSpace()
            ? new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            : new HashSet<string>(codes.Split(',').Select(c => c.Trim()).Where(c => c.Length > 0),
                                  StringComparer.OrdinalIgnoreCase);
    }

    #endregion

    #region << JV preview / validate >>

    /// <summary>The JV preview grid, with the fingerprint Post JV checks against.</summary>
    public async Task<JournalVoucherPreviewDto> PreviewAsync(CostingRunInputDto input)
    {
        ValidateInput(input);

        var domainInput = CostingMapper.ToDomain(input);
        var result = await _journalVoucherBuilder.BuildAsync(domainInput);

        var dto = CostingMapper.ToDto(result);
        dto.PreviewFingerprint = JournalVoucherFingerprint.Compute(domainInput, result);
        return dto;
    }

    /// <summary>Legacy "Validate JV" (<c>ExportGrid(onlyValidate: true)</c>, lines 1618-1634).</summary>
    public async Task<JournalVoucherValidationResultDto> ValidateAsync(CostingRunInputDto input)
    {
        ValidateInput(input);

        var domainInput = CostingMapper.ToDomain(input);
        var result = await _journalVoucherBuilder.BuildAsync(domainInput);

        var validation = BuildValidationResult(result);
        validation.PreviewFingerprint = JournalVoucherFingerprint.Compute(domainInput, result);
        return validation;
    }

    /// <summary>Legacy validation block: no rows, then debit/credit after WHOLE-unit rounding.</summary>
    public JournalVoucherValidationResultDto BuildValidationResult(JournalVoucherResult result)
    {
        var messages = new List<string>();

        if (!result.Lines.Any())
        {
            messages.Add(L("NoDataFoundToPostJV"));
        }
        else if (!result.IsBalanced)
        {
            messages.Add(L("DebitCreditNotMatched"));
        }

        return new JournalVoucherValidationResultDto
        {
            IsValid = !messages.Any(),
            Message = messages.Any() ? string.Join(Environment.NewLine, messages) : L("JVValidatedSuccessfully"),
            TotalDebit = result.TotalDebit,
            TotalCredit = result.TotalCredit,
            LineCount = result.Lines.Count
        };
    }

    #endregion

    #region << Runs >>

    /// <summary>
    /// Legacy Generate / Post JV buttons (lines 2204-2212 / 2235-2243): empty the log, then start the long
    /// operation in the background.
    ///
    /// <para><b>One run at a time across both screens (not in legacy).</b> Both runs write the same
    /// <c>ProjectLog</c>, and starting one empties it, so a second run started while the first is going
    /// destroys the first one's log and stop flag; legacy let that happen. A run that has gone silent for
    /// <see cref="FinanceRunLog.StaleAfterMinutes"/> minutes no longer blocks.</para>
    /// </summary>
    public async Task<FinanceRunStartedDto> StartRunAsync(FinanceRunProcess process, CostingRunInput input, string userName,
                                                          string previewFingerprint, Func<FinanceRunJobArgs, Task> enqueue)
    {
        var active = await _runLog.GetActiveRunAsync();
        if (active != null)
        {
            throw new UserFriendlyException(L("FinanceRunAlreadyRunning",
                L(active.Process == FinanceRunProcess.PostJv ? "PostJv" : "CostingFile"),
                active.StartedBy,
                active.StartedAt.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture)));
        }

        await _runLog.ClearAsync();

        var executionLogId = await _runLog.OpenAsync(process, input, userName);

        await enqueue(FinanceRunJobArgs.From(input, _session.GetTenantId(), _session.UserId, executionLogId,
                                             previewFingerprint));

        return new FinanceRunStartedDto
        {
            ExecutionLogId = executionLogId,
            Message = L(process == FinanceRunProcess.PostJv ? "PostJvRunQueued" : "CostingFileRunQueued")
        };
    }

    /// <summary>Legacy Export Log button (lines 2964-2973): the grid as filtered, every page.</summary>
    public async Task<ReportPdfOutput> ExportLogAsync(GetFinanceRunLogInput input)
    {
        var rows = await _runLog.GetLogRowsAsync(input);

        var sheet = rows.Select(r => new Dictionary<string, object>
        {
            [L("EmployeeCode")] = r.EmpCode,
            [L("Employee")] = r.Employee,
            [L("RunTimeStamp")] = r.RunTimeStamp,
            [L("Status")] = r.Status == 0 ? L("Success") : L("Failed"),
            [L("Message")] = r.Message,
            [L("InPeriod")] = r.InPeriod
        }).ToList();

        using var stream = new MemoryStream();
        MiniExcel.SaveAs(stream, sheet, printHeader: true, excelType: ExcelType.XLSX);

        var fileName = "CostEngineExecutionLog_" + Clock.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture) + ".xlsx";
        return new ReportPdfOutput(fileName, Convert.ToBase64String(stream.ToArray()));
    }

    #endregion
}
