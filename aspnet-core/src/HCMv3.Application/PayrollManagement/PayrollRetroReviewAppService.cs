using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Extensions;
using Abp.Linq.Extensions;
using Abp.Runtime.Session;
using Abp.Timing;
using Abp.UI;
using HCMv3.Authorization;
using HCMv3.PayrollManagement.Dto;
using HCMv3.PayrollManagement.Services;
using HCMv3.PersonnelAdministration;
using HCMv3.SystemSecurity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.PayrollManagement;

/// <inheritdoc cref="IPayrollRetroReviewAppService"/>
/// <remarks>
/// <para><b>Which employees.</b> Legacy <c>BuildRetroPopupList</c>: employees whose org assignment covering the
/// payroll date is in the area, narrowed by <c>GetAllowedRetroEmployeeOids</c> (the same rule as
/// <c>EmployeePayment.GetAllowedEmployeesForPayment</c>): every active employee, plus an inactive one whose latest
/// un-revoked separation has no later rejoin and who either has no final settlement since that separation or whose
/// settlement falls in the current period of their payroll area (the next period when that one is closed). The
/// area is resolved through <see cref="IEmployeePayrollAreaResolver"/>, the same seam the engine uses.</para>
///
/// <para><b>Which period.</b> The area's control record: start = <c>CalendarMonthStart</c>, payroll date =
/// <c>CalendarMonthEnd</c> (legacy <c>txtStartDate</c>/<c>txtEndDate</c> in Current mode). With no control record,
/// legacy fell back to today and the first of today's month; so does this.</para>
///
/// <para><b>Validation, as legacy.</b> Legacy checks only that a reason was given and that something changed. It
/// does NOT limit the date against the period, the joining date or <c>EarliestRetroAccountingDate</c>, and allows
/// clearing it. Not added here (legacy-parity rule).</para>
/// </remarks>
public class PayrollRetroReviewAppService : HCMv3AppServiceBase, IPayrollRetroReviewAppService
{
    /// <summary>Legacy <c>SysService.AddRetroReasonLog(..., "PayrollEngine", 0, ...)</c>. Stored data, not translated.</summary>
    public const string LogEntityObjectName = "PayrollEngine";

    /// <summary>Legacy's log wording, kept as-is: existing Retro Reason Log reports read it.</summary>
    public const string LogReasonFormat = "Manual Retro Update From Payroll Engine. Reason: {0}";

    /// <summary>Legacy's "never set" date, <c>default(DateTime)</c>.</summary>
    private static readonly DateTime NeverSet = DateTime.MinValue;

    private readonly IRepository<PayrollArea> _payrollAreaRepository;
    private readonly IRepository<PayrollControlRecord> _controlRecordRepository;
    private readonly IRepository<Employee> _employeeRepository;
    private readonly IRepository<EmployeeSeparation> _separationRepository;
    private readonly IRepository<EmployeeRejoin> _rejoinRepository;
    private readonly IRepository<EmployeeFnFPayment> _fnfPaymentRepository;
    private readonly IRepository<EmployeePayrollStatus> _payrollStatusRepository;
    private readonly IRepository<RetroReasonLog> _retroReasonLogRepository;
    private readonly IEmployeePayrollAreaResolver _payrollAreaResolver;

    public PayrollRetroReviewAppService(
        IRepository<PayrollArea> payrollAreaRepository,
        IRepository<PayrollControlRecord> controlRecordRepository,
        IRepository<Employee> employeeRepository,
        IRepository<EmployeeSeparation> separationRepository,
        IRepository<EmployeeRejoin> rejoinRepository,
        IRepository<EmployeeFnFPayment> fnfPaymentRepository,
        IRepository<EmployeePayrollStatus> payrollStatusRepository,
        IRepository<RetroReasonLog> retroReasonLogRepository,
        IEmployeePayrollAreaResolver payrollAreaResolver)
    {
        _payrollAreaRepository = payrollAreaRepository;
        _controlRecordRepository = controlRecordRepository;
        _employeeRepository = employeeRepository;
        _separationRepository = separationRepository;
        _rejoinRepository = rejoinRepository;
        _fnfPaymentRepository = fnfPaymentRepository;
        _payrollStatusRepository = payrollStatusRepository;
        _retroReasonLogRepository = retroReasonLogRepository;
        _payrollAreaResolver = payrollAreaResolver;
    }

    /// <summary>Legacy <c>GetRetroEmployeeCountForSelectedPayrollArea</c>.</summary>
    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_PayrollEngine)]
    public async Task<int> GetRetroCount(string payrollAreaName)
    {
        if (payrollAreaName.IsNullOrWhiteSpace() || !await PayrollAreaExistsAsync(payrollAreaName))
        {
            return 0;
        }

        var review = await BuildReviewAsync(payrollAreaName);
        return review.Rows.Count(r => r.HasRetro);
    }

    /// <summary>
    /// Legacy <c>BuildRetroPopupList</c> + <c>gridRetro_DataBinding</c>. <c>[HttpPost]</c> per the repo's NSwag rule
    /// (custom input DTO).
    /// </summary>
    [HttpPost]
    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_PayrollEngine)]
    public async Task<GetPayrollRetroReviewEmployeesOutput> GetRetroReviewEmployees(GetPayrollRetroReviewEmployeesInput input)
    {
        await EnsurePayrollAreaAsync(input.PayrollAreaName);

        var review = await BuildReviewAsync(input.PayrollAreaName);
        var rows = review.Rows;

        var search = input.Filter?.Trim() ?? string.Empty;

        // In memory: the retro status is worked out per row, not stored.
        var filtered = rows
            .AsQueryable()
            .WhereIf(input.RetroFilter == PayrollRetroReviewFilter.Retro, r => r.HasRetro)
            .WhereIf(input.RetroFilter == PayrollRetroReviewFilter.NoRetro, r => !r.HasRetro)
            .WhereIf(search.Length > 0, r =>
                (r.EmployeeCode ?? string.Empty).Contains(search, StringComparison.OrdinalIgnoreCase)
                || (r.EmployeeName ?? string.Empty).Contains(search, StringComparison.OrdinalIgnoreCase));

        var page = filtered
            .OrderBy(input.Sorting.IsNullOrWhiteSpace() ? "EmployeeCode" : input.Sorting)
            .PageBy(input)
            .ToList();

        return new GetPayrollRetroReviewEmployeesOutput
        {
            TotalCount = filtered.Count(),
            Items = page,
            AllCount = rows.Count,
            RetroCount = rows.Count(r => r.HasRetro),
            NoRetroCount = rows.Count(r => !r.HasRetro),
            PeriodStartDate = review.PeriodStart,
            PeriodEndDate = review.PayrollDate,
        };
    }

    /// <summary>
    /// Legacy <c>PrepareRetroSaveConfirmation</c> (its refusals) + <c>SaveRetroChanges</c>. Every refusal is checked
    /// before the first write, and the method runs in ABP's per-call unit of work, so a save is all or nothing.
    /// </summary>
    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_PayrollEngine_Run)]
    public async Task<int> UpdateRetroDates(UpdatePayrollRetroDatesInput input)
    {
        await EnsurePayrollAreaAsync(input.PayrollAreaName);

        var changes = input.Changes ?? new List<PayrollRetroDateChangeDto>();

        if (changes.Count == 0)
        {
            throw new UserFriendlyException(L("PayrollRetroNoChanges"));
        }

        // Legacy: "Reason is required for update." -- per changed employee.
        if (changes.Any(c => c == null || c.Reason.IsNullOrWhiteSpace()))
        {
            throw new UserFriendlyException(L("PayrollRetroReasonRequired"));
        }

        var review = await BuildReviewAsync(input.PayrollAreaName);
        var rowsById = review.Rows.ToDictionary(r => r.EmployeeId);

        var duplicate = changes.GroupBy(c => c.EmployeeId).FirstOrDefault(g => g.Count() > 1);
        if (duplicate != null)
        {
            throw new UserFriendlyException(L("PayrollRetroDuplicateEmployee", await EmployeeLabelAsync(duplicate.Key)));
        }

        // Legacy could only save rows of its own list; here the client could send anything, so refuse an employee
        // who is not on this area's review list rather than silently skipping (legacy `continue`).
        var stranger = changes.FirstOrDefault(c => !rowsById.ContainsKey(c.EmployeeId));
        if (stranger != null)
        {
            throw new UserFriendlyException(L(
                "PayrollRetroEmployeeNotInArea",
                await EmployeeLabelAsync(stranger.EmployeeId),
                input.PayrollAreaName));
        }

        // Legacy IsChanged: only rows whose date differs from the saved one are saved.
        var changed = changes
            .Select(c => new { Change = c, NewDate = c.RetroFromDate?.Date })
            .Where(x => x.NewDate != rowsById[x.Change.EmployeeId].RetroFromDate)
            .ToList();

        if (changed.Count == 0)
        {
            throw new UserFriendlyException(L("PayrollRetroNoChanges"));
        }

        var tenantId = AbpSession.GetTenantId();
        var employeeIds = changed.Select(x => x.Change.EmployeeId).ToList();

        var statuses = (await _payrollStatusRepository.GetAll()
                .Where(s => employeeIds.Contains(s.EmployeeId))
                .OrderBy(s => s.Id)
                .ToListAsync())
            .GroupBy(s => s.EmployeeId)
            .ToDictionary(g => g.Key, g => g.First());

        var joiningDates = await _employeeRepository.GetAll()
            .Where(e => employeeIds.Contains(e.Id))
            .Select(e => new { e.Id, e.JoiningDate })
            .ToDictionaryAsync(e => e.Id, e => e.JoiningDate);

        foreach (var item in changed)
        {
            var employeeId = item.Change.EmployeeId;
            var newDate = item.NewDate ?? NeverSet;

            if (!statuses.TryGetValue(employeeId, out var status))
            {
                // Legacy creates the row with the joining date as the floor.
                status = new EmployeePayrollStatus
                {
                    TenantId = tenantId,
                    EmployeeId = employeeId,
                    EarliestRetroAccountingDate = joiningDates[employeeId].Date,
                    NextTimeEvaluationFrom = newDate,
                    EarliestMasterDataChange = newDate,
                };
                await _payrollStatusRepository.InsertAsync(status);
            }
            else
            {
                status.EarliestMasterDataChange = newDate;
                await _payrollStatusRepository.UpdateAsync(status);
            }

            await _retroReasonLogRepository.InsertAsync(new RetroReasonLog
            {
                TenantId = tenantId,
                EmployeeId = employeeId,
                ChangedOn = Clock.Now,
                RetroDate = newDate,
                EntityObjectName = LogEntityObjectName,
                EntityObjectId = 0,
                RetroReason = string.Format(LogReasonFormat, item.Change.Reason.Trim()),
            });
        }

        return changed.Count;
    }

    // ------------------------------------------------------------------ the list

    private sealed class Review
    {
        public DateTime PeriodStart { get; init; }

        public DateTime PayrollDate { get; init; }

        public List<PayrollRetroReviewEmployeeDto> Rows { get; init; }
    }

    private async Task<Review> BuildReviewAsync(string payrollAreaName)
    {
        // Legacy txtStartDate / txtEndDate, defaulting to the first of this month / today.
        var controlRecord = await _controlRecordRepository.GetAll()
            .Where(r => r.PayrollArea.Name == payrollAreaName)
            .OrderBy(r => r.Id)
            .Select(r => new { r.CalendarMonthStart, r.CalendarMonthEnd })
            .FirstOrDefaultAsync();

        var payrollDate = controlRecord?.CalendarMonthEnd.Date ?? Clock.Now.Date;
        var periodStart = controlRecord?.CalendarMonthStart.Date ?? new DateTime(payrollDate.Year, payrollDate.Month, 1);

        var inArea = await _payrollAreaResolver.GetEmployeeIdsInPayrollAreaAsync(payrollAreaName, payrollDate);
        var allowed = await GetAllowedRetroEmployeeIdsAsync(inArea);
        var employeeIds = inArea.Where(allowed.Contains).ToList();

        var employees = await _employeeRepository.GetAll()
            .Where(e => employeeIds.Contains(e.Id))
            .Select(e => new { e.Id, e.Code, e.FullNameEn })
            .OrderBy(e => e.Code)
            .ToListAsync();

        var retroDates = (await _payrollStatusRepository.GetAll()
                .Where(s => employeeIds.Contains(s.EmployeeId))
                .OrderBy(s => s.Id)
                .Select(s => new { s.EmployeeId, s.EarliestMasterDataChange })
                .ToListAsync())
            .GroupBy(s => s.EmployeeId)
            .ToDictionary(g => g.Key, g => g.First().EarliestMasterDataChange);

        var withDate = retroDates.Where(kv => kv.Value != NeverSet).Select(kv => kv.Key).ToList();
        var reasons = await GetLatestReasonsAsync(withDate);

        var rows = employees.Select(e =>
        {
            DateTime? retroFrom = retroDates.TryGetValue(e.Id, out var date) && date != NeverSet ? date.Date : null;
            var hasRetro = retroFrom.HasValue && retroFrom.Value < periodStart;

            return new PayrollRetroReviewEmployeeDto
            {
                EmployeeId = e.Id,
                EmployeeCode = e.Code,
                EmployeeName = e.FullNameEn,
                RetroFromDate = retroFrom,
                HasRetro = hasRetro,
                RetroMonths = hasRetro ? MonthsBetween(retroFrom.Value, payrollDate) : 0,
                Reason = retroFrom.HasValue && reasons.TryGetValue(e.Id, out var reason) ? reason : string.Empty,
            };
        }).ToList();

        return new Review { PeriodStart = periodStart, PayrollDate = payrollDate, Rows = rows };
    }

    /// <summary>Legacy <c>RetroPopupModel.TotalRetroMonth</c>: month difference, never negative.</summary>
    private static int MonthsBetween(DateTime retroFrom, DateTime payrollDate)
    {
        var months = (payrollDate.Year - retroFrom.Year) * 12 + payrollDate.Month - retroFrom.Month;
        return months > 0 ? months : 0;
    }

    /// <summary>Legacy <c>latestReasons</c>: the reason on each employee's most recent log line (by ChangedOn).</summary>
    private async Task<Dictionary<int, string>> GetLatestReasonsAsync(List<int> employeeIds)
    {
        if (employeeIds.Count == 0)
        {
            return new Dictionary<int, string>();
        }

        var logs = _retroReasonLogRepository.GetAll();
        var latest = await logs
            .Where(l => employeeIds.Contains(l.EmployeeId)
                        && l.ChangedOn == logs.Where(x => x.EmployeeId == l.EmployeeId).Max(x => x.ChangedOn))
            .Select(l => new { l.EmployeeId, l.Id, l.RetroReason })
            .ToListAsync();

        return latest
            .GroupBy(l => l.EmployeeId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(l => l.Id).First().RetroReason);
    }

    /// <summary>
    /// Legacy <c>GetAllowedRetroEmployeeOids</c>, evaluated for <paramref name="candidateIds"/> only (legacy computed
    /// it for every employee and then intersected, which gives the same answer).
    /// </summary>
    private async Task<HashSet<int>> GetAllowedRetroEmployeeIdsAsync(List<int> candidateIds)
    {
        var allowed = new HashSet<int>(await _employeeRepository.GetAll()
            .Where(e => candidateIds.Contains(e.Id) && e.Status == EmployeeStatus.Active)
            .Select(e => e.Id)
            .ToListAsync());

        var separations = await _separationRepository.GetAll()
            .Where(s => candidateIds.Contains(s.EmployeeId)
                        && s.Employee.Status == EmployeeStatus.Inactive
                        && s.RevokeDate == null)
            .Select(s => new { s.EmployeeId, s.SubmissionDate })
            .ToListAsync();

        if (separations.Count == 0)
        {
            return allowed;
        }

        var latestSeparation = separations
            .GroupBy(s => s.EmployeeId)
            .ToDictionary(g => g.Key, g => g.Max(s => s.SubmissionDate));

        var inactiveIds = latestSeparation.Keys.ToList();
        var rejoins = (await _rejoinRepository.GetAll()
                .Where(r => inactiveIds.Contains(r.EmployeeId))
                .Select(r => new { r.EmployeeId, r.RejoinDate })
                .ToListAsync())
            .GroupBy(r => r.EmployeeId)
            .ToDictionary(g => g.Key, g => g.Select(r => r.RejoinDate).ToList());

        // Legacy finalSeparations: no rejoin after the latest separation. (Legacy's StartDate = RejoinDate here.)
        var finalSeparations = latestSeparation
            .Where(kv => !rejoins.TryGetValue(kv.Key, out var dates) || !dates.Any(d => d > kv.Value))
            .ToDictionary(kv => kv.Key, kv => kv.Value);

        if (finalSeparations.Count == 0)
        {
            return allowed;
        }

        var finalIds = finalSeparations.Keys.ToList();
        var settlements = (await _fnfPaymentRepository.GetAll()
                .Where(f => finalIds.Contains(f.EmployeeId))
                .Select(f => new { f.EmployeeId, f.CompensationDate })
                .ToListAsync())
            .GroupBy(f => f.EmployeeId)
            .ToDictionary(g => g.Key, g => g.Select(f => f.CompensationDate).ToList());

        var controlRecords = (await _controlRecordRepository.GetAll()
                .OrderBy(r => r.Id)
                .Select(r => new { r.PayrollAreaId, r.CalendarMonthStart, r.CalendarMonthEnd, r.PeriodClosed })
                .ToListAsync())
            .GroupBy(r => r.PayrollAreaId)
            .ToDictionary(g => g.Key, g => g.First());

        foreach (var (employeeId, submissionDate) in finalSeparations)
        {
            DateTime? latestSettlement = settlements.TryGetValue(employeeId, out var dates)
                ? dates.Where(d => d >= submissionDate).OrderByDescending(d => d).Cast<DateTime?>().FirstOrDefault()
                : null;

            if (latestSettlement == null)
            {
                allowed.Add(employeeId);
                continue;
            }

            // Legacy Employee.CurrentPayrollArea: the org assignment covering today.
            var currentArea = await _payrollAreaResolver.GetEmployeePayrollAreaAsync(employeeId, Clock.Now.Date);
            if (currentArea == null || !controlRecords.TryGetValue(currentArea.Id, out var control))
            {
                continue;
            }

            var compensationDate = latestSettlement.Value;
            var inPeriod = control.PeriodClosed
                ? compensationDate >= control.CalendarMonthStart.AddMonths(1) && compensationDate <= control.CalendarMonthEnd.AddMonths(1)
                : compensationDate >= control.CalendarMonthStart && compensationDate <= control.CalendarMonthEnd;

            if (inPeriod)
            {
                allowed.Add(employeeId);
            }
        }

        return allowed;
    }

    // ------------------------------------------------------------------ helpers

    private Task<bool> PayrollAreaExistsAsync(string payrollAreaName) =>
        _payrollAreaRepository.GetAll().AnyAsync(a => a.Name == payrollAreaName);

    /// <summary>Legacy "Payroll Area is required." / "Selected Payroll Area was not found."</summary>
    private async Task EnsurePayrollAreaAsync(string payrollAreaName)
    {
        if (payrollAreaName.IsNullOrWhiteSpace())
        {
            throw new UserFriendlyException(L("PayrollRetroPayrollAreaRequired"));
        }

        if (!await PayrollAreaExistsAsync(payrollAreaName))
        {
            throw new UserFriendlyException(L("PayrollRetroPayrollAreaNotFound"));
        }
    }

    private async Task<string> EmployeeLabelAsync(int employeeId)
    {
        var code = await _employeeRepository.GetAll()
            .Where(e => e.Id == employeeId)
            .Select(e => e.Code)
            .FirstOrDefaultAsync();

        return code.IsNullOrWhiteSpace() ? employeeId.ToString() : code;
    }
}
