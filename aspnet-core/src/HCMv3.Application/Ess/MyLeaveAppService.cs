using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Text.Json;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.Linq.Extensions;
using Abp.Timing;
using Abp.UI;
using HCMv3.Authorization;
using HCMv3.Dto;
using HCMv3.Ess.Dto;
using HCMv3.Ess.Exporting;
using HCMv3.GeneralMasterInfo;
using HCMv3.Storage;
using HCMv3.Storage.Dto;
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration;
using HCMv3.PersonnelAdministration.Services;
using HCMv3.PersonnelAdministration.Workflow;
using HCMv3.SystemSettings.Services;
using HCMv3.Workflow;
using HCMv3.Workflow.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.Ess;

/// <inheritdoc cref="IMyLeaveAppService"/>
[AbpAuthorize(AppPermissions.Ess_MyLeave)]
public class MyLeaveAppService : HCMv3AppServiceBase, IMyLeaveAppService
{
    private readonly IRepository<EmployeeAbsence> _absenceRepository;
    private readonly IRepository<AbsenceType> _absenceTypeRepository;
    private readonly IRepository<WorkflowRequest> _requestRepository;
    private readonly IRequestNumberGenerator _requestNumberGenerator;
    private readonly ILeaveBalanceService _leaveBalanceService;
    private readonly IEmployeeScopeProvider _employeeScopeProvider;
    private readonly IRequestMasterResolver _requestMasterResolver;
    private readonly IWorkflowApprovalService _workflowApprovalService;
    private readonly IUnitOfWorkManager _unitOfWorkManager;
    private readonly IWorkflowAmendmentService _workflowAmendmentService;
    private readonly IEmployeeAbsenceCalculationService _absenceCalculationService;
    private readonly ISysSettingsAccessor _sysSettingsAccessor;
    private readonly ISickLeaveDeductionPreview _sickLeaveDeductionPreview;
    private readonly HCMv3.Storage.IAttachmentAppService _attachmentAppService;
    private readonly IAttachmentManager _attachmentManager;
    private readonly IRepository<Employee> _employeeRepository;
    private readonly IRepository<Religion> _religionRepository;
    private readonly IRepository<EmployeeOrgAssignment> _orgAssignmentRepository;
    private readonly IRepository<EmployeeSubGroupAbsenceType> _subGroupAbsenceTypeRepository;
    private readonly IRepository<EmployeeManager> _employeeManagerRepository;
    private readonly IRepository<EmployeeWorkflowRole> _employeeWorkflowRoleRepository;
    private readonly IRepository<RequestLog> _requestLogRepository;
    private readonly HCMv3.Storage.ITempFileCacheManager _tempFileCacheManager;

    public MyLeaveAppService(
        IRepository<EmployeeAbsence> absenceRepository,
        IRepository<AbsenceType> absenceTypeRepository,
        IRepository<WorkflowRequest> requestRepository,
        ILeaveBalanceService leaveBalanceService,
        IEmployeeScopeProvider employeeScopeProvider,
        IRequestMasterResolver requestMasterResolver,
        IWorkflowApprovalService workflowApprovalService,
        IUnitOfWorkManager unitOfWorkManager,
        IWorkflowAmendmentService workflowAmendmentService,
        IEmployeeAbsenceCalculationService absenceCalculationService,
        ISysSettingsAccessor sysSettingsAccessor,
        ISickLeaveDeductionPreview sickLeaveDeductionPreview,
        HCMv3.Storage.IAttachmentAppService attachmentAppService,
        IRequestNumberGenerator requestNumberGenerator,
        IAttachmentManager attachmentManager,
        IRepository<Employee> employeeRepository,
        IRepository<Religion> religionRepository,
        IRepository<EmployeeOrgAssignment> orgAssignmentRepository,
        IRepository<EmployeeSubGroupAbsenceType> subGroupAbsenceTypeRepository,
        IRepository<EmployeeManager> employeeManagerRepository,
        IRepository<EmployeeWorkflowRole> employeeWorkflowRoleRepository,
        IRepository<RequestLog> requestLogRepository,
        HCMv3.Storage.ITempFileCacheManager tempFileCacheManager)
    {
        _attachmentManager = attachmentManager;
        _employeeRepository = employeeRepository;
        _religionRepository = religionRepository;
        _orgAssignmentRepository = orgAssignmentRepository;
        _subGroupAbsenceTypeRepository = subGroupAbsenceTypeRepository;
        _employeeManagerRepository = employeeManagerRepository;
        _employeeWorkflowRoleRepository = employeeWorkflowRoleRepository;
        _requestLogRepository = requestLogRepository;
        _tempFileCacheManager = tempFileCacheManager;
        _requestNumberGenerator = requestNumberGenerator;
        _absenceRepository = absenceRepository;
        _absenceTypeRepository = absenceTypeRepository;
        _requestRepository = requestRepository;
        _leaveBalanceService = leaveBalanceService;
        _employeeScopeProvider = employeeScopeProvider;
        _requestMasterResolver = requestMasterResolver;
        _workflowApprovalService = workflowApprovalService;
        _unitOfWorkManager = unitOfWorkManager;
        _workflowAmendmentService = workflowAmendmentService;
        _absenceCalculationService = absenceCalculationService;
        _sysSettingsAccessor = sysSettingsAccessor;
        _sickLeaveDeductionPreview = sickLeaveDeductionPreview;
        _attachmentAppService = attachmentAppService;
    }

    public async Task<List<MyLeaveBalanceDto>> GetMyBalances()
    {
        var employeeId = await _employeeScopeProvider.GetRequiredCurrentEmployeeIdAsync();

        var balances = await _leaveBalanceService.GetBalancesAsync(employeeId);

        return balances.Select(b => new MyLeaveBalanceDto
        {
            AbsenceTypeId = b.AbsenceTypeId,
            AbsenceTypeNameEn = b.AbsenceTypeNameEn,
            AbsenceTypeNameAr = b.AbsenceTypeNameAr,
            Accrued = b.Accrued,
            Used = b.Used,
            Pending = b.Pending,
            Remaining = b.Remaining,
            Bookable = b.Bookable
        }).ToList();
    }

    public async Task<LeaveFormSettingsDto> GetLeaveFormSettings()
    {
        var timeSettings = await _sysSettingsAccessor.GetTimeSettingsAsync();
        var payrollSettings = await _sysSettingsAccessor.GetPayrollSettingsAsync();
        var workflowSettings = await _sysSettingsAccessor.GetWorkflowSettingsAsync();

        return new LeaveFormSettingsDto
        {
            EnableAutoHalfDayLeave = timeSettings?.EnableAutoHalfDayLeave ?? false,
            EnablePayFullWithPayroll = timeSettings?.EnablePayFullWithPayroll ?? false,
            EnableIsTravelling = timeSettings?.EnableIsTravelling ?? false,
            EnableBookingQuotaInLeaveRequest = timeSettings?.EnableBookingQuotaInLeaveRequest ?? false,
            DefaultLeavePaymentType = payrollSettings?.DefaultLeavePaymentType ?? LeavePaymentType.None,
            HideLeavePayDetailsInWorkflow = workflowSettings?.HideLeavePayDetailsInWorkflow ?? false,
            CanDelegateApprovals = await HasApprovalsToDelegateAsync()
        };
    }

    /// <summary>
    /// Legacy <c>GetEmployeesRole</c> (<c>UnifiedLeaveRequestController</c> 3066-3110): the approver-delegate
    /// field is for somebody who actually approves things -- a manager, or the holder of a workflow role today.
    /// An employee with no employee record is simply not one of them; the screen says so elsewhere.
    /// </summary>
    private async Task<bool> HasApprovalsToDelegateAsync()
    {
        var employeeId = await _employeeScopeProvider.GetCurrentEmployeeIdAsync();
        if (!employeeId.HasValue)
        {
            return false;
        }

        var today = DateTime.Today;

        return await _employeeManagerRepository.GetAll()
                   .AnyAsync(m => m.ManagerId == employeeId.Value
                                  && m.ManagerId != m.EmployeeId
                                  && m.StartDate <= today
                                  && m.EndDate >= today)
               || await _employeeWorkflowRoleRepository.GetAll()
                   .AnyAsync(r => r.EmployeeId == employeeId.Value && r.StartDate <= today && r.EndDate >= today);
    }

    public async Task<List<MyLeaveTypeDto>> GetMyLeaveTypes()
    {
        var employeeId = await _employeeScopeProvider.GetRequiredCurrentEmployeeIdAsync();
        var today = Clock.Now.Date;

        var employee = await _employeeRepository.GetAll()
            .Where(e => e.Id == employeeId)
            .Select(e => new { e.Gender, e.ReligionId })
            .FirstAsync();

        var religionName = employee.ReligionId.HasValue
            ? await _religionRepository.GetAll()
                .Where(r => r.Id == employee.ReligionId.Value)
                .Select(r => r.NameEn)
                .FirstOrDefaultAsync()
            : null;

        var subGroupId = await _orgAssignmentRepository.GetAll()
            .Where(a => a.EmployeeId == employeeId && a.StartDate <= today && a.EndDate >= today)
            .OrderByDescending(a => a.StartDate)
            .Select(a => a.EmployeeSubGroupId)
            .FirstOrDefaultAsync();

        var profileTypeIds = subGroupId.HasValue
            ? await _subGroupAbsenceTypeRepository.GetAll()
                .Where(a => a.EmployeeSubGroupId == subGroupId.Value)
                .Select(a => a.AbsenceTypeId)
                .ToListAsync()
            : new List<int>();

        var types = await _absenceTypeRepository.GetAll()
            .Where(t => t.IsActive)
            .OrderBy(t => t.NameEn)
            .ToListAsync();

        var quotaTypeIds = (await _leaveBalanceService.GetBalancesAsync(employeeId))
            .Select(b => b.AbsenceTypeId)
            .ToHashSet();

        var result = new List<MyLeaveTypeDto>();

        foreach (var type in types)
        {
            // The same test the server refuses with, so the list never offers a type the submit would reject.
            if (!LeaveRuleValidator.LeaveTypeSuits(
                    type.LeaveGender, type.LeaveReligion, employee.Gender, employee.ReligionId.HasValue, religionName))
            {
                continue;
            }

            // Legacy applied the salary-profile filter only when the employee HAS a sub group; without one
            // every type is offered and the submit rule decides.
            if (subGroupId.HasValue && type.IncludeInSalaryProfile && !profileTypeIds.Contains(type.Id))
            {
                continue;
            }

            var policy = type.RequirePolicyAcceptance
                ? (await _attachmentManager.GetListAsync(
                    AbsenceTypePolicyAttachmentOwnerPolicy.Type,
                    type.Id.ToString(System.Globalization.CultureInfo.InvariantCulture)))
                .OrderByDescending(a => a.Id)
                .FirstOrDefault()
                : null;

            result.Add(new MyLeaveTypeDto
            {
                Id = type.Id,
                Code = type.Code,
                NameEn = type.NameEn,
                NameAr = type.NameAr,
                AllowPartialLeave = type.AllowPartialLeave && type.LeaveDurationType != LeaveDurationType.FullDay,
                LeaveDurationType = type.LeaveDurationType,
                IsAttachmentRequired = type.IsAttachmentRequired,
                RequirePolicyAcceptance = type.RequirePolicyAcceptance,
                PolicyAttachmentId = policy?.Id,
                PolicyFileName = policy?.FileName,
                EarliestStartDate = type.AllowBackDatedDays >= 0 ? today.AddDays(-type.AllowBackDatedDays) : null,
                LatestStartDate = type.MaxFutureDays > 0 ? today.AddDays(type.MaxFutureDays) : null,
                HasQuota = quotaTypeIds.Contains(type.Id)
            });
        }

        return result;
    }

    [HttpPost]
    public async Task<LeaveBalancePreviewDto> PreviewMyLeaveBalance(PreviewLeaveBalanceInput input)
    {
        var employeeId = await _employeeScopeProvider.GetRequiredCurrentEmployeeIdAsync();

        var start = input.StartDate.Date;
        var end = input.EndDate.Date;

        var balance = (await _leaveBalanceService.GetBalancesAsync(employeeId, start == default ? null : start))
            .FirstOrDefault(b => b.AbsenceTypeId == input.AbsenceTypeId);

        if (balance == null)
        {
            return new LeaveBalancePreviewDto { HasQuota = false };
        }

        var preview = new LeaveBalancePreviewDto
        {
            HasQuota = true,
            CurrentBalance = Math.Round(balance.Remaining, 3),
            Bookable = Math.Round(balance.Bookable, 3)
        };

        if (start == default || end == default || end < start)
        {
            preview.BalanceAfter = preview.Bookable;
            return preview;
        }

        var times = ResolveTimes(input.StartTime, input.EndTime, start);

        var days = await _absenceCalculationService.CalculateTotalLeaveDaysAsync(
            employeeId, input.AbsenceTypeId, start, end, times.AbsenceHours);

        // A change is charged only for the difference: the approved leave's days are already in Used.
        if (input.EmployeeAbsenceId.HasValue)
        {
            EmployeeAbsence leave;
            using (CurrentUnitOfWork.DisableFilter(HCMv3DataFilters.ApprovedOnly))
            {
                leave = await _absenceRepository.FirstOrDefaultAsync(a => a.Id == input.EmployeeAbsenceId.Value);
            }

            if (leave != null && leave.EmployeeId == employeeId && leave.AbsenceTypeId == input.AbsenceTypeId)
            {
                days -= leave.TotalLeaveDays;
            }
        }

        preview.DaysRequested = Math.Round(days, 3);
        preview.BalanceAfter = Math.Round(preview.Bookable - days, 3);

        return preview;
    }

    private readonly struct LeaveTimes
    {
        public LeaveTimes(DateTime startTime, DateTime endTime, double absenceHours)
        {
            StartTime = startTime;
            EndTime = endTime;
            AbsenceHours = absenceHours;
        }

        public DateTime StartTime { get; }

        public DateTime EndTime { get; }

        public double AbsenceHours { get; }
    }

    /// <summary>
    /// Turns the form's "HH:mm" pair into what the leave row stores. Legacy <c>UpdateAbsenceHours</c>
    /// (<c>AbsenceRequest</c> 2565-2571): hours are end minus start, to three places, and zero unless both
    /// times are given.
    /// </summary>
    private static LeaveTimes ResolveTimes(string startTime, string endTime, DateTime day)
    {
        if (string.IsNullOrWhiteSpace(startTime) || string.IsNullOrWhiteSpace(endTime))
        {
            return new LeaveTimes(default, default, 0);
        }

        var start = day.Date + TimeSpan.ParseExact(startTime, "hh\\:mm", System.Globalization.CultureInfo.InvariantCulture);
        var end = day.Date + TimeSpan.ParseExact(endTime, "hh\\:mm", System.Globalization.CultureInfo.InvariantCulture);

        return new LeaveTimes(start, end, Math.Round((end - start).TotalHours, 3));
    }

    /// <summary>
    /// Legacy <c>RecalculateDutyTimes</c> (<c>AbsenceRequest</c> 2550-2563): a half-day leave takes its times
    /// from the day's duty -- the first half of the shift, or the second.
    /// </summary>
    private async Task<LeaveTimes> ResolveHalfDayTimesAsync(int employeeId, DateTime day, HalfdayType halfdayType)
    {
        TimeManagement.Duty duty;
        try
        {
            duty = await _absenceCalculationService.GetEmployeeDutyAsync(employeeId, day);
        }
        catch (InvalidOperationException)
        {
            throw new UserFriendlyException(L("DutyNotFoundForTheDay", day.ToString("dd/MM/yyyy")));
        }

        var shiftStart = day.Date + duty.StartTime;
        var shiftEnd = duty.IsNightShift ? day.Date.AddDays(1) + duty.EndTime : day.Date + duty.EndTime;
        var half = TimeSpan.FromMinutes((shiftEnd - shiftStart).TotalMinutes / 2);

        var start = halfdayType == HalfdayType.StartOfDuty ? shiftStart : shiftEnd - half;
        var end = halfdayType == HalfdayType.StartOfDuty ? shiftStart + half : shiftEnd;

        return new LeaveTimes(start, end, Math.Round((end - start).TotalHours, 3));
    }

    [HttpPost]
    public async Task<PagedResultDto<MyLeaveDto>> GetMyLeaves(GetMyLeavesInput input)
    {
        input.Normalize();

        var employeeId = await _employeeScopeProvider.GetRequiredCurrentEmployeeIdAsync();

        // The employee must see their OWN in-flight requests -- that is the main reason they come here at
        // all -- so the ApprovedOnly filter is off. It stays scoped to them by employee id regardless.
        using (CurrentUnitOfWork.DisableFilter(HCMv3DataFilters.ApprovedOnly))
        {
            var today = DateTime.Today;

            var query = _absenceRepository.GetAll()
                .Include(a => a.AbsenceType)
                .Where(a => a.EmployeeId == employeeId)

                // One row per leave. Days that were added to a leave are rows of their own for payroll -- the
                // paid and unpaid days a late return books, or the unpaid tail of a leave applied for beyond
                // the balance -- but to the employee they are part of the leave they asked for, not more leave.
                .Where(a => !_requestRepository.GetAll().Any(r => r.Id == a.WorkflowRequestId && r.IsAmendment))
                .Where(a => a.WorkflowRequestId == null
                            || !_absenceRepository.GetAll().Any(b => b.WorkflowRequestId == a.WorkflowRequestId && b.Id < a.Id))
                // Besides what is current or waiting: a leave sent back to the employee, and (legacy's "Return"
                // tab) an approved leave of a type that needs a return date and has none yet -- a return is
                // reported AFTER the leave has ended, which is exactly when it would otherwise drop off the list.
                .WhereIf(!input.IncludeHistory, a => a.EndDate >= today
                                                     || a.WorkflowStatus == RequestStatus.Open
                                                     || a.WorkflowStatus == RequestStatus.Draft
                                                     || (a.WorkflowStatus == RequestStatus.Approved
                                                         && !a.IsReturned
                                                         && a.AbsenceType.IsReturnDateRequired));

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderBy(input.Sorting)
                .PageBy(input)
                .ToListAsync();

            var dtos = items.Select(ToDto).ToList();

            await FillAmendmentStateAsync(dtos);

            // Every request raised against each leave, for the lines under its row.
            var requests = await BuildHistoryAsync(items);
            foreach (var dto in dtos)
            {
                dto.Requests = requests[dto.Id];
            }

            return new PagedResultDto<MyLeaveDto>(totalCount, dtos);
        }
    }

    /// <summary>
    /// The leave list as a workbook: one line per request, the leave repeated on each. Built from the same
    /// rows the list shows, so a row that is collapsed or on another page of the screen is still in it.
    /// </summary>
    [HttpPost]
    public async Task<FileDto> GetMyLeavesToExcel(ExportMyLeavesInput input)
    {
        var all = await GetMyLeaves(new GetMyLeavesInput { IncludeHistory = true, MaxResultCount = int.MaxValue });

        // In the order the screen had them, when it says which ones it is showing.
        var leaves = input?.LeaveIds == null
            ? all.Items.ToList()
            : input.LeaveIds
                .Select(id => all.Items.FirstOrDefault(l => l.Id == id))
                .Where(l => l != null)
                .ToList();

        var arabic = System.Globalization.CultureInfo.CurrentUICulture.TextInfo.IsRightToLeft;

        using var book = EssListExcel.BuildLeaves(leaves, key => L(key), arabic);
        using var stream = new System.IO.MemoryStream();
        book.SaveAs(stream);

        var file = new FileDto(
            EssListExportAppService.FileNameOf(L("EssMyLeave")),
            Abp.AspNetZeroCore.Net.MimeTypeNames.ApplicationVndOpenxmlformatsOfficedocumentSpreadsheetmlSheet);
        _tempFileCacheManager.SetFile(file.FileToken, stream.ToArray());

        return file;
    }

    /// <summary>
    /// Marks which rows can be changed and which already have a change in flight.
    ///
    /// <para>
    /// Needed as a separate pass because an amendment deliberately leaves the leave row untouched -- the row
    /// looks exactly the same before and after asking for the change. Without this the employee would submit
    /// an amendment, see nothing happen, and submit it again.
    /// </para>
    /// </summary>
    private async Task FillAmendmentStateAsync(List<MyLeaveDto> leaves)
    {
        var ownRequestIds = leaves.Where(l => l.WorkflowRequestId.HasValue).Select(l => l.WorkflowRequestId.Value).ToList();

        var ownRequests = ownRequestIds.Count == 0
            ? new Dictionary<int, WorkflowRequest>()
            : await _requestRepository.GetAll()
                .Where(r => ownRequestIds.Contains(r.Id))
                .ToDictionaryAsync(r => r.Id);

        // Until a change says otherwise, the row shows the leave's own request.
        foreach (var leave in leaves)
        {
            leave.RequestAction = leave.WorkflowStatus == RequestStatus.Cancelled
                ? WorkflowRequestAction.Cancellation
                : WorkflowRequestAction.New;
            leave.DisplayStatus = OwnStatusOf(leave);

            // A draft that was never sent has no request, and so no number yet.
            if (leave.WorkflowRequestId.HasValue && ownRequests.TryGetValue(leave.WorkflowRequestId.Value, out var own))
            {
                leave.RequestNumber = own.RequestNumber;
            }
        }

        // A cancelled leave is read too: its row shows the cancellation that was granted.
        var decided = leaves
            .Where(l => l.WorkflowStatus == RequestStatus.Approved || l.WorkflowStatus == RequestStatus.Cancelled)
            .ToList();
        if (decided.Count == 0)
        {
            return;
        }

        var amendable = await _workflowAmendmentService.IsAmendmentEnabledAsync(
            LeaveWorkflowTransactionHandler.LeaveRequestType);

        var leaveIds = decided.Select(l => l.Id).ToList();

        // Every change ever raised against these leaves, newest first: the newest one decides what the row
        // shows -- something in flight, a rejection nobody has followed up, or nothing.
        var allChanges = (await _requestRepository.GetAll()
            .Where(r => r.IsAmendment
                        && r.RequestType == LeaveWorkflowTransactionHandler.LeaveRequestType
                        && r.TargetEntityId.HasValue
                        && leaveIds.Contains(r.TargetEntityId.Value))
            .OrderByDescending(r => r.Id)
            .ToListAsync())
            .GroupBy(r => r.TargetEntityId.Value)
            .ToDictionary(g => g.Key, g => g.ToList());

        var changes = allChanges.ToDictionary(g => g.Key, g => g.Value.First());

        // A Draft request is either the employee's own unsent draft or one an approver sent back; only the
        // log can tell them apart, and the screen words the two differently.
        var draftIds = changes.Values.Where(r => r.RequestStatus == RequestStatus.Draft).Select(r => r.Id).ToList();

        var sentBackIds = draftIds.Count == 0
            ? new HashSet<int>()
            : (await _requestLogRepository.GetAll()
                .Where(l => draftIds.Contains(l.WorkflowRequestId)
                            && (l.ActionType == RequestActionType.SentBack || l.ActionType == RequestActionType.SentBackToCreator))
                .Select(l => l.WorkflowRequestId)
                .Distinct()
                .ToListAsync()).ToHashSet();

        // The days a late return added, by the return that added them: shown on the leave's own row.
        var returnIds = allChanges.Values
            .SelectMany(raised => raised)
            .Where(r => r.RequestStatus == RequestStatus.Approved)
            .Select(r => r.Id)
            .ToList();

        var addedDays = returnIds.Count == 0
            ? new List<EmployeeAbsence>()
            : await _absenceRepository.GetAll()
                .Where(a => a.WorkflowRequestId.HasValue && returnIds.Contains(a.WorkflowRequestId.Value))
                .ToListAsync();

        foreach (var leave in decided)
        {
            if (allChanges.TryGetValue(leave.Id, out var mine))
            {
                var mineIds = mine.Select(r => r.Id).ToHashSet();

                foreach (var added in addedDays.Where(a => a.Id != leave.Id && mineIds.Contains(a.WorkflowRequestId.Value)))
                {
                    leave.EndDate = added.EndDate > leave.EndDate ? added.EndDate : leave.EndDate;
                    leave.TotalLeaveDays += added.TotalLeaveDays;
                }
            }

            // What the row shows: the newest change the employee did not withdraw. A withdrawn one changed
            // nothing, so the row goes back to what it showed before; it is still in the leave's history.
            var shown = allChanges.TryGetValue(leave.Id, out var raised)
                ? raised.FirstOrDefault(r => r.RequestStatus != RequestStatus.Closed)
                : null;

            if (shown != null)
            {
                leave.RequestAction = ActionOf(shown);
                leave.DisplayStatus = shown.RequestStatus switch
                {
                    RequestStatus.Open => MyLeaveRequestStatus.Open,
                    RequestStatus.Rejected => MyLeaveRequestStatus.Rejected,
                    RequestStatus.Draft => sentBackIds.Contains(shown.Id)
                        ? MyLeaveRequestStatus.SentBack
                        : MyLeaveRequestStatus.Draft,
                    _ => MyLeaveRequestStatus.Approved
                };
            }

            // A cancelled leave is finished with: nothing further can be raised against it.
            if (leave.WorkflowStatus == RequestStatus.Cancelled)
            {
                continue;
            }

            changes.TryGetValue(leave.Id, out var latest);

            if (latest != null && (latest.RequestStatus == RequestStatus.Open || latest.RequestStatus == RequestStatus.Draft))
            {
                leave.HasPendingAmendment = true;
                leave.PendingAmendmentRequestId = latest.Id;
                leave.PendingAction = ActionOf(latest);
                leave.PendingIsDraft = latest.RequestStatus == RequestStatus.Draft && !sentBackIds.Contains(latest.Id);
                leave.PendingIsSentBack = latest.RequestStatus == RequestStatus.Draft && !leave.PendingIsDraft;

                if (!string.IsNullOrWhiteSpace(latest.AmendmentJson))
                {
                    var proposed = JsonSerializer.Deserialize<LeaveAmendment>(latest.AmendmentJson);
                    leave.ProposedStartDate = proposed.StartDate;
                    leave.ProposedEndDate = proposed.EndDate;
                    leave.ProposedReturnDate = proposed.ReturnDate;
                    leave.ProposedAbsenceTypeId = proposed.AbsenceTypeId;
                    leave.ProposedPaidStartDate = proposed.PaidStartDate;
                    leave.ProposedPaidEndDate = proposed.PaidEndDate;
                    leave.ProposedUnpaidStartDate = proposed.UnpaidStartDate;
                    leave.ProposedUnpaidEndDate = proposed.UnpaidEndDate;
                    leave.PendingReason = proposed.Reason;
                }

                continue;
            }

            if (latest != null && latest.RequestStatus == RequestStatus.Rejected)
            {
                leave.LastRejectedAction = ActionOf(latest);
            }

            // Once the employee is back, the leave's dates are a fact: no change, no cancellation, no second
            // return. Anything further is HR's to correct on the leave itself.
            leave.CanAmend = amendable && !leave.IsReturned;
            leave.CanRequestCancellation = amendable && !leave.IsReturned;

            // A return can only be reported for a leave that has begun (legacy's Return tab listed leave whose
            // start date had passed). Offering it earlier only leads to "the return date cannot be in the
            // future".
            leave.CanReturn = amendable && !leave.IsReturned && leave.StartDate.Date <= Clock.Now.Date;
        }
    }

    public async Task<int> AmendMyLeave(AmendMyLeaveInput input)
    {
        var employeeId = await _employeeScopeProvider.GetRequiredCurrentEmployeeIdAsync();

        if (input.EndDate.Date < input.StartDate.Date)
        {
            throw new UserFriendlyException(L("EndDateCannotBeEarlierThanStartDate"));
        }

        var absenceTypeExists = await _absenceTypeRepository.GetAll()
            .AnyAsync(t => t.Id == input.AbsenceTypeId);

        if (!absenceTypeExists)
        {
            throw new UserFriendlyException(L("AbsenceTypeNotFound"));
        }

        EmployeeAbsence leave;

        using (CurrentUnitOfWork.DisableFilter(HCMv3DataFilters.ApprovedOnly))
        {
            leave = await _absenceRepository.FirstOrDefaultAsync(a => a.Id == input.EmployeeAbsenceId);
        }

        if (leave == null)
        {
            throw new UserFriendlyException(L("LeaveRequestNotFound"));
        }

        // Ownership re-checked on the write, exactly as in CancelMyLeave: a scoped list paired with an
        // unscoped write is how one employee ends up amending another's leave.
        if (leave.EmployeeId != employeeId)
        {
            throw new AbpAuthorizationException(L("YouCanOnlyCancelYourOwnRequests"));
        }

        // Only an approved leave is worth amending. While it is still Open the employee can withdraw and
        // re-apply, which costs nobody an approval round-trip; and changing something no one has agreed to
        // yet is editing a request, not amending a fact.
        if (leave.WorkflowStatus != RequestStatus.Approved)
        {
            throw new UserFriendlyException(L("OnlyAnApprovedLeaveCanBeAmended"));
        }

        EnsureNotReturned(leave);

        var amendment = await BuildAmendmentAsync(leave, input.AbsenceTypeId, input.StartDate, input.EndDate, input.Reason);

        return await _workflowAmendmentService.CreateAndSubmitAsync(
            LeaveWorkflowTransactionHandler.LeaveRequestType,
            leave.Id,
            leave.EmployeeId,
            JsonSerializer.Serialize(amendment),
            employeeId,
            input.AbsenceTypeId,
            WorkflowRequestAction.Amendment);
    }

    /// <summary>
    /// Legacy blocked an amendment once a return date existed (<c>LeaveRequest.js</c> 1870-1885), and the same
    /// goes for a cancellation: once the employee is back, the dates are a fact, and correcting them is HR's
    /// to do on the leave itself.
    /// </summary>
    private void EnsureNotReturned(EmployeeAbsence leave)
    {
        if (leave.IsReturned)
        {
            throw new UserFriendlyException(L("ThisLeaveHasAlreadyBeenReturnedFrom"));
        }
    }

    private async Task<LeaveAmendment> BuildAmendmentAsync(
        EmployeeAbsence leave, int absenceTypeId, DateTime startDate, DateTime endDate, string reason)
    {
        if (endDate.Date < startDate.Date)
        {
            throw new UserFriendlyException(L("EndDateCannotBeEarlierThanStartDate"));
        }

        // Legacy "No changes detected!" (LeaveRequest.js 1870): an amendment that changes nothing is not one.
        if (absenceTypeId == leave.AbsenceTypeId
            && startDate.Date == leave.StartDate.Date
            && endDate.Date == leave.EndDate.Date)
        {
            throw new UserFriendlyException(L("NoChangesDetected"));
        }

        return new LeaveAmendment
        {
            AbsenceTypeId = absenceTypeId,
            StartDate = startDate.Date,
            EndDate = endDate.Date,
            // Legacy EmployeeAbsence.UpdateQuotaInformation: calendar days less the days the type excludes.
            TotalLeaveDays = await _absenceCalculationService.CalculateTotalLeaveDaysAsync(
                leave.EmployeeId, absenceTypeId, startDate.Date, endDate.Date, leave.AbsenceHours),
            Comments = reason,
            Reason = reason
        };
    }

    public async Task<ReturnFromLeaveResultDto> ReturnFromMyLeave(ReturnFromLeaveInput input)
    {
        var employeeId = await _employeeScopeProvider.GetRequiredCurrentEmployeeIdAsync();

        var leave = await GetMyApprovedLeaveForReturnAsync(input.EmployeeAbsenceId, employeeId);

        var (amendment, plan) = await BuildReturnAmendmentAsync(leave, input);

        // The absence type's AllowToReturnEarly rule and every late-return rule are enforced inside the leave
        // handler, not here, so they apply at submit AND again at approval.
        var requestId = await _workflowAmendmentService.CreateAndSubmitAsync(
            LeaveWorkflowTransactionHandler.LeaveRequestType,
            leave.Id,
            leave.EmployeeId,
            JsonSerializer.Serialize(amendment),
            employeeId,
            leave.AbsenceTypeId,
            WorkflowRequestAction.Return);

        return new ReturnFromLeaveResultDto
        {
            EmployeeAbsenceId = leave.Id,
            WorkflowRequestId = requestId,
            NewEndDate = plan.NewEndDate,
            IsEarlyReturn = plan.Kind == LeaveReturnKind.Early,
            DaysReturned = plan.Kind == LeaveReturnKind.Early ? Math.Round(leave.TotalLeaveDays - plan.TotalLeaveDays, 6) : 0,
            Kind = plan.Kind,
            PaidCalendarDays = CalendarDaysOf(plan.PaidStartDate, plan.PaidEndDate),
            UnpaidCalendarDays = CalendarDaysOf(plan.UnpaidStartDate, plan.UnpaidEndDate)
        };
    }

    private async Task<(LeaveAmendment Amendment, ReturnPlan Plan)> BuildReturnAmendmentAsync(
        EmployeeAbsence leave, ReturnFromLeaveInput input)
    {
        var plan = await PlanReturnAsync(leave, input, useProposedSplitWhenEmpty: true);

        var amendment = new LeaveAmendment
        {
            Reason = input.Reason,
            AbsenceTypeId = leave.AbsenceTypeId,
            StartDate = leave.StartDate.Date,
            EndDate = plan.NewEndDate,
            TotalLeaveDays = plan.TotalLeaveDays,
            Comments = leave.Comments,
            ReturnDate = input.RejoiningDate.Date,
            IsReturned = true,

            // Only on an early return, and only the ORIGINAL end date -- the days granted but not taken stay
            // visible after the change. On time and late have nothing to preserve.
            InitialEndDate = plan.Kind == LeaveReturnKind.Early ? leave.EndDate.Date : (DateTime?)null,

            PaidStartDate = plan.PaidStartDate,
            PaidEndDate = plan.PaidEndDate,
            UnpaidStartDate = plan.UnpaidStartDate,
            UnpaidEndDate = plan.UnpaidEndDate,
            AdditionalLeaveAction = plan.AdditionalLeaveAction,
            AdditionalLeaveEndDate = plan.AdditionalLeaveEndDate
        };

        return (amendment, plan);
    }

    public async Task<ReturnFromLeavePreviewDto> PreviewReturnFromMyLeave(ReturnFromLeaveInput input)
    {
        var employeeId = await _employeeScopeProvider.GetRequiredCurrentEmployeeIdAsync();

        var leave = await GetMyApprovedLeaveForReturnAsync(input.EmployeeAbsenceId, employeeId);

        // The preview always shows the PROPOSED split, whatever the screen currently holds, so the employee
        // can always get back to the default.
        var plan = await PlanReturnAsync(
            leave,
            new ReturnFromLeaveInput { EmployeeAbsenceId = input.EmployeeAbsenceId, RejoiningDate = input.RejoiningDate },
            useProposedSplitWhenEmpty: true);

        return new ReturnFromLeavePreviewDto
        {
            Kind = plan.Kind,
            DaysDifference = plan.DaysDifference,
            NewEndDate = plan.NewEndDate,
            Bookable = plan.Bookable,
            PaidStartDate = plan.PaidStartDate,
            PaidEndDate = plan.PaidEndDate,
            UnpaidStartDate = plan.UnpaidStartDate,
            UnpaidEndDate = plan.UnpaidEndDate
        };
    }

    private sealed class ReturnPlan
    {
        public LeaveReturnKind Kind { get; set; }
        public double DaysDifference { get; set; }
        public DateTime NewEndDate { get; set; }
        public double TotalLeaveDays { get; set; }
        public double? Bookable { get; set; }
        public DateTime? PaidStartDate { get; set; }
        public DateTime? PaidEndDate { get; set; }
        public DateTime? UnpaidStartDate { get; set; }
        public DateTime? UnpaidEndDate { get; set; }
        public AdditionalLeaveAction AdditionalLeaveAction { get; set; }
        public DateTime? AdditionalLeaveEndDate { get; set; }
    }

    /// <summary>
    /// Classifies a rejoining date the way legacy <c>AbsenceRequest.DaysDifference</c> did and works out what
    /// the leave becomes. Shared by the preview and the submit so the two can never disagree.
    /// </summary>
    private async Task<ReturnPlan> PlanReturnAsync(EmployeeAbsence leave, ReturnFromLeaveInput input, bool useProposedSplitWhenEmpty)
    {
        var rejoiningDate = input.RejoiningDate.Date;

        // The rejoining date is the first day BACK, so the last day of leave is the day before it. A rejoining
        // date on or before the start would mean the leave never happened, which is a cancellation, not a
        // return -- legacy refused it too ("ReturnDate should be greater than Start Date.").
        if (rejoiningDate <= leave.StartDate.Date)
        {
            throw new UserFriendlyException(L("RejoiningDateMustBeAfterTheLeaveStartDate"));
        }

        // Legacy: "ReturnDate can't be future date." unless TimeSettings.AllowFutureDatedReturnFromLeave.
        var timeSettings = await _sysSettingsAccessor.GetTimeSettingsAsync();
        if (rejoiningDate > Clock.Now.Date && timeSettings?.AllowFutureDatedReturnFromLeave != true)
        {
            throw new UserFriendlyException(L("RejoiningDateCannotBeInTheFuture"));
        }

        // With additional unpaid leave attached, the leave really ends when THAT ends -- legacy measured the
        // return against ExtEndDate (AbsenceRequest 380-383).
        var lastDate = leave.ExtEndDate?.Date ?? leave.EndDate.Date;

        var daysDifference = await _absenceCalculationService.GetReturnDaysDifferenceAsync(
            leave.EmployeeId, leave.AbsenceTypeId, lastDate, rejoiningDate);

        var plan = new ReturnPlan
        {
            DaysDifference = daysDifference,
            NewEndDate = leave.EndDate.Date,

            // Unchanged dates keep the stored figure rather than recomputing it: an HR-entered leave may carry
            // a day count that is not simply its calendar span, and an on-time return has no business revising
            // it.
            TotalLeaveDays = leave.TotalLeaveDays
        };

        if (daysDifference == 0)
        {
            // ON TIME -- including a gap made up only of week-offs and holidays. The end date does not move.
            plan.Kind = LeaveReturnKind.OnTime;
            return plan;
        }

        if (daysDifference < 0)
        {
            // EARLY. Legacy ran GetRegularDate before letting an early return through: if another absence
            // already starts on the day this leave would now end, the rejoining date cannot be right.
            var regularDate = await _absenceCalculationService.GetRegularDateAsync(
                leave.EmployeeId, rejoiningDate, excludeEmployeeAbsenceId: leave.Id);

            if (regularDate == null)
            {
                throw new UserFriendlyException(L("RejoiningDateConflictsWithAnotherLeave", rejoiningDate.AddDays(-1).ToString("dd MMM yyyy")));
            }

            plan.Kind = LeaveReturnKind.Early;
            plan.NewEndDate = regularDate.Value;

            // Legacy EarlyJoinType (AbsenceRequest 1660-1754), when additional unpaid leave is attached:
            // returning before it starts removes it and shortens the paid leave; returning during it leaves the
            // paid leave alone and cuts the unpaid row back to the day before they came back.
            if (leave.ExtStartDate != null)
            {
                if (rejoiningDate <= leave.ExtStartDate.Value.Date)
                {
                    plan.AdditionalLeaveAction = AdditionalLeaveAction.Delete;
                }
                else
                {
                    plan.AdditionalLeaveAction = AdditionalLeaveAction.Shorten;
                    plan.AdditionalLeaveEndDate = regularDate.Value;
                    plan.NewEndDate = leave.EndDate.Date;
                }
            }

            // Measured the way legacy's EmployeeAbsence save measured it -- calendar days less the days the
            // type excludes -- so the refund equals what was charged for those days.
            plan.TotalLeaveDays = await _absenceCalculationService.CalculateTotalLeaveDaysAsync(
                leave.EmployeeId, leave.AbsenceTypeId, leave.StartDate.Date, plan.NewEndDate, leave.AbsenceHours);

            return plan;
        }

        // LATE. The gap is booked on approval as further paid leave and/or unpaid leave.
        plan.Kind = LeaveReturnKind.Late;

        var balances = await _leaveBalanceService.GetBalancesAsync(leave.EmployeeId, leave.EndDate.Date.AddDays(1));
        plan.Bookable = balances.FirstOrDefault(b => b.AbsenceTypeId == leave.AbsenceTypeId)?.Bookable;

        var anyEntered = input.PaidStartDate.HasValue || input.PaidEndDate.HasValue
                         || input.UnpaidStartDate.HasValue || input.UnpaidEndDate.HasValue;

        if (anyEntered || !useProposedSplitWhenEmpty)
        {
            plan.PaidStartDate = input.PaidStartDate?.Date;
            plan.PaidEndDate = input.PaidEndDate?.Date;
            plan.UnpaidStartDate = input.UnpaidStartDate?.Date;
            plan.UnpaidEndDate = input.UnpaidEndDate?.Date;
            return plan;
        }

        // Legacy ESS's proposal (LeaveRequest.js OnLeaveReturnDateChange): paid up to the whole days of
        // balance, the rest unpaid, in calendar days from the day after the leave. A type with no quota row at
        // all is not quota-controlled, so the whole gap is proposed as paid.
        var gapStart = leave.EndDate.Date.AddDays(1);
        var gapEnd = rejoiningDate.AddDays(-1);
        var gapDays = (int)daysDifference;
        var paidDays = plan.Bookable.HasValue
            ? Math.Max(0, Math.Min(gapDays, (int)Math.Floor(plan.Bookable.Value)))
            : gapDays;

        if (paidDays > 0)
        {
            plan.PaidStartDate = gapStart;
            plan.PaidEndDate = gapStart.AddDays(paidDays - 1);
        }

        if (paidDays < gapDays)
        {
            plan.UnpaidStartDate = gapStart.AddDays(paidDays);
            plan.UnpaidEndDate = gapEnd;
        }

        return plan;
    }

    private static double CalendarDaysOf(DateTime? start, DateTime? end)
    {
        return start.HasValue && end.HasValue ? (end.Value.Date - start.Value.Date).TotalDays + 1 : 0;
    }

    public async Task<int> RequestCancellationOfMyLeave(RequestLeaveCancellationInput input)
    {
        var employeeId = await _employeeScopeProvider.GetRequiredCurrentEmployeeIdAsync();

        EmployeeAbsence leave;

        using (CurrentUnitOfWork.DisableFilter(HCMv3DataFilters.ApprovedOnly))
        {
            leave = await _absenceRepository.FirstOrDefaultAsync(a => a.Id == input.Id);
        }

        if (leave == null)
        {
            throw new UserFriendlyException(L("LeaveRequestNotFound"));
        }

        if (leave.EmployeeId != employeeId)
        {
            throw new AbpAuthorizationException(L("YouCanOnlyCancelYourOwnRequests"));
        }

        // A leave still awaiting approval is withdrawn with CancelMyLeave, which costs nobody an approval.
        if (leave.WorkflowStatus != RequestStatus.Approved)
        {
            throw new UserFriendlyException(L("OnlyAnApprovedLeaveCanBeCancelledThroughApproval"));
        }

        EnsureNotReturned(leave);

        return await _workflowAmendmentService.CreateAndSubmitAsync(
            LeaveWorkflowTransactionHandler.LeaveRequestType,
            leave.Id,
            leave.EmployeeId,
            JsonSerializer.Serialize(BuildCancellation(leave, input.Reason)),
            employeeId,
            leave.AbsenceTypeId,
            WorkflowRequestAction.Cancellation);
    }

    /// <summary>The current values ride along so the approver's summary shows exactly what is being removed.</summary>
    private static LeaveAmendment BuildCancellation(EmployeeAbsence leave, string reason)
    {
        return new LeaveAmendment
        {
            AbsenceTypeId = leave.AbsenceTypeId,
            StartDate = leave.StartDate.Date,
            EndDate = leave.EndDate.Date,
            TotalLeaveDays = leave.TotalLeaveDays,
            Comments = leave.Comments,
            Reason = reason,
            IsCancellation = true
        };
    }

    public async Task<ApplyForLeaveResultDto> ResubmitMyLeave(ResubmitMyLeaveInput input)
    {
        var employeeId = await _employeeScopeProvider.GetRequiredCurrentEmployeeIdAsync();

        if (input.EndDate.Date < input.StartDate.Date)
        {
            throw new UserFriendlyException(L("EndDateCannotBeEarlierThanStartDate"));
        }

        EmployeeAbsence leave;

        using (CurrentUnitOfWork.DisableFilter(HCMv3DataFilters.ApprovedOnly))
        {
            leave = await _absenceRepository.FirstOrDefaultAsync(a => a.Id == input.EmployeeAbsenceId);
        }

        if (leave == null)
        {
            throw new UserFriendlyException(L("LeaveRequestNotFound"));
        }

        if (leave.EmployeeId != employeeId)
        {
            throw new AbpAuthorizationException(L("YouCanOnlyCancelYourOwnRequests"));
        }

        // Draft WITH a request is a leave an approver sent back: applying creates and submits in one step, so
        // nothing else ever rests in that state.
        if (leave.WorkflowStatus != RequestStatus.Draft || leave.WorkflowRequestId == null)
        {
            throw new UserFriendlyException(L("OnlyARequestSentBackToYouCanBeResubmitted"));
        }

        await FillLeaveAsync(leave, input, employeeId);

        using (CurrentUnitOfWork.DisableFilter(HCMv3DataFilters.ApprovedOnly))
        {
            await _absenceRepository.UpdateAsync(leave);
        }

        var request = await _requestRepository.GetAsync(leave.WorkflowRequestId.Value);
        request.RequestSubTypeId = input.AbsenceTypeId;
        await _requestRepository.UpdateAsync(request);

        await CurrentUnitOfWork.SaveChangesAsync();

        await AttachUploadedFilesAsync(leave.Id, input.AttachmentFileTokens);

        await _workflowApprovalService.SubmitAsync(request.Id, employeeId);

        return new ApplyForLeaveResultDto
        {
            EmployeeAbsenceId = leave.Id,
            WorkflowRequestId = request.Id,
            SubmittedForApproval = true
        };
    }

    public async Task<int> ResubmitMyLeaveChange(ResubmitMyLeaveChangeInput input)
    {
        var employeeId = await _employeeScopeProvider.GetRequiredCurrentEmployeeIdAsync();
        var (leave, request) = await GetMyLeaveWithChangeInFlightAsync(input.EmployeeAbsenceId, employeeId);

        if (request.RequestStatus != RequestStatus.Draft)
        {
            throw new UserFriendlyException(L("OnlyARequestSentBackToYouCanBeResubmitted"));
        }

        var previous = JsonSerializer.Deserialize<LeaveAmendment>(request.AmendmentJson);
        var corrected = await BuildChangeAsync(leave, ActionOf(request), previous, input);

        if (!corrected.IsCancellation && !corrected.IsReturned)
        {
            request.RequestSubTypeId = corrected.AbsenceTypeId;
        }

        await _workflowAmendmentService.ResubmitAsync(request.Id, JsonSerializer.Serialize(corrected), employeeId);

        return request.Id;
    }

    /// <summary>
    /// The proposed values for an amendment, cancellation or return, from what the form sent. Anything the
    /// form left out falls back to what the request already held, so a draft reopened and saved untouched
    /// stays as it was.
    /// </summary>
    private async Task<LeaveAmendment> BuildChangeAsync(
        EmployeeAbsence leave, WorkflowRequestAction action, LeaveAmendment previous, ResubmitMyLeaveChangeInput input)
    {
        switch (action)
        {
            case WorkflowRequestAction.Cancellation:
                return BuildCancellation(leave, input.Reason ?? previous?.Reason);

            case WorkflowRequestAction.Return:
                var (returned, _) = await BuildReturnAmendmentAsync(leave, new ReturnFromLeaveInput
                {
                    EmployeeAbsenceId = leave.Id,
                    RejoiningDate = input.RejoiningDate ?? previous?.ReturnDate ?? default,
                    Reason = input.Reason ?? previous?.Reason,
                    PaidStartDate = input.PaidStartDate,
                    PaidEndDate = input.PaidEndDate,
                    UnpaidStartDate = input.UnpaidStartDate,
                    UnpaidEndDate = input.UnpaidEndDate
                });

                return returned;

            default:
                return await BuildAmendmentAsync(
                    leave,
                    input.AbsenceTypeId ?? previous?.AbsenceTypeId ?? leave.AbsenceTypeId,
                    input.StartDate ?? previous?.StartDate ?? leave.StartDate,
                    input.EndDate ?? previous?.EndDate ?? leave.EndDate,
                    input.Reason ?? previous?.Reason);
        }
    }

    public async Task<int> SaveMyLeaveChangeDraft(SaveMyLeaveChangeDraftInput input)
    {
        var employeeId = await _employeeScopeProvider.GetRequiredCurrentEmployeeIdAsync();

        EmployeeAbsence leave;

        using (CurrentUnitOfWork.DisableFilter(HCMv3DataFilters.ApprovedOnly))
        {
            leave = await _absenceRepository.FirstOrDefaultAsync(a => a.Id == input.EmployeeAbsenceId);
        }

        if (leave == null)
        {
            throw new UserFriendlyException(L("LeaveRequestNotFound"));
        }

        if (leave.EmployeeId != employeeId)
        {
            throw new AbpAuthorizationException(L("YouCanOnlyCancelYourOwnRequests"));
        }

        if (leave.WorkflowStatus != RequestStatus.Approved)
        {
            throw new UserFriendlyException(L("OnlyAnApprovedLeaveCanBeAmended"));
        }

        EnsureNotReturned(leave);

        var existing = await _workflowAmendmentService.GetInFlightAsync(
            LeaveWorkflowTransactionHandler.LeaveRequestType, leave.Id);

        if (existing != null)
        {
            // Already drafted (or sent back): the same request is saved again, whatever action it was for.
            if (existing.RequestStatus != RequestStatus.Draft)
            {
                throw new UserFriendlyException(L("AnAmendmentIsAlreadyAwaitingApprovalForThisRecord"));
            }

            var previous = JsonSerializer.Deserialize<LeaveAmendment>(existing.AmendmentJson);
            var saved = await BuildChangeAsync(leave, ActionOf(existing), previous, input);

            await _workflowAmendmentService.UpdateDraftAsync(
                existing.Id, JsonSerializer.Serialize(saved), employeeId,
                saved.IsCancellation || saved.IsReturned ? null : saved.AbsenceTypeId);

            return existing.Id;
        }

        var change = await BuildChangeAsync(leave, input.Action, null, input);

        return await _workflowAmendmentService.CreateDraftAsync(
            LeaveWorkflowTransactionHandler.LeaveRequestType,
            leave.Id,
            leave.EmployeeId,
            JsonSerializer.Serialize(change),
            employeeId,
            change.AbsenceTypeId,
            input.Action);
    }

    public async Task<ApplyForLeaveResultDto> SaveMyLeaveDraft(SaveMyLeaveDraftInput input)
    {
        var employeeId = await _employeeScopeProvider.GetRequiredCurrentEmployeeIdAsync();

        if (input.EndDate.Date < input.StartDate.Date)
        {
            throw new UserFriendlyException(L("EndDateCannotBeEarlierThanStartDate"));
        }

        EmployeeAbsence leave;

        if (input.Id.HasValue)
        {
            leave = await GetMyUnsentLeaveAsync(input.Id.Value, employeeId);
        }
        else
        {
            // Draft, with no request yet: invisible to payroll and to every approver, and not a claim on the
            // balance or on the dates. The leave rules are applied when it is submitted, not here -- a draft
            // is what somebody saves precisely because it is not ready.
            leave = new EmployeeAbsence { WorkflowStatus = RequestStatus.Draft };
        }

        await FillLeaveAsync(leave, input, employeeId);

        using (CurrentUnitOfWork.DisableFilter(HCMv3DataFilters.ApprovedOnly))
        {
            if (leave.Id == 0)
            {
                await _absenceRepository.InsertAndGetIdAsync(leave);
            }
            else
            {
                await _absenceRepository.UpdateAsync(leave);
            }
        }

        await CurrentUnitOfWork.SaveChangesAsync();
        await AttachUploadedFilesAsync(leave.Id, input.AttachmentFileTokens);

        return new ApplyForLeaveResultDto
        {
            EmployeeAbsenceId = leave.Id,
            WorkflowRequestId = leave.WorkflowRequestId,
            SubmittedForApproval = false
        };
    }

    public async Task<ApplyForLeaveResultDto> SubmitMyLeaveDraft(EntityDto input)
    {
        var employeeId = await _employeeScopeProvider.GetRequiredCurrentEmployeeIdAsync();
        var leave = await GetMyUnsentLeaveAsync(input.Id, employeeId);

        return await SendForApprovalAsync(leave, employeeId);
    }

    public async Task<MyLeaveDto> GetMyLeaveForEdit(EntityDto input)
    {
        var employeeId = await _employeeScopeProvider.GetRequiredCurrentEmployeeIdAsync();

        EmployeeAbsence leave;

        using (CurrentUnitOfWork.DisableFilter(HCMv3DataFilters.ApprovedOnly))
        {
            leave = await _absenceRepository.GetAll()
                .Include(a => a.AbsenceType)
                .FirstOrDefaultAsync(a => a.Id == input.Id);
        }

        if (leave == null)
        {
            throw new UserFriendlyException(L("LeaveRequestNotFound"));
        }

        if (leave.EmployeeId != employeeId)
        {
            throw new AbpAuthorizationException(L("YouCanOnlyCancelYourOwnRequests"));
        }

        var dto = ToDto(leave);
        await FillAmendmentStateAsync(new List<MyLeaveDto> { dto });
        await FillDelegateNamesAsync(dto);

        return dto;
    }

    /// <summary>The employee's own leave that has not been sent yet, or was sent back: the only kind they may edit.</summary>
    private async Task<EmployeeAbsence> GetMyUnsentLeaveAsync(int leaveId, int employeeId)
    {
        EmployeeAbsence leave;

        using (CurrentUnitOfWork.DisableFilter(HCMv3DataFilters.ApprovedOnly))
        {
            leave = await _absenceRepository.FirstOrDefaultAsync(a => a.Id == leaveId);
        }

        if (leave == null)
        {
            throw new UserFriendlyException(L("LeaveRequestNotFound"));
        }

        if (leave.EmployeeId != employeeId)
        {
            throw new AbpAuthorizationException(L("YouCanOnlyCancelYourOwnRequests"));
        }

        if (leave.WorkflowStatus != RequestStatus.Draft)
        {
            throw new UserFriendlyException(L("OnlyARequestSentBackToYouCanBeResubmitted"));
        }

        return leave;
    }

    private async Task FillDelegateNamesAsync(MyLeaveDto dto)
    {
        var ids = new[] { dto.PrimaryDelegateId, dto.SecondaryDelegateId, dto.ApproverDelegateId }
            .Where(id => id.HasValue)
            .Select(id => id.Value)
            .Distinct()
            .ToList();

        if (ids.Count == 0)
        {
            return;
        }

        var names = (await _employeeRepository.GetAll()
                .Where(e => ids.Contains(e.Id))
                .Select(e => new { e.Id, e.Code, e.FullNameEn })
                .ToListAsync())
            .ToDictionary(e => e.Id, e => e.Code + " - " + e.FullNameEn);

        string NameOf(int? id) => id.HasValue && names.TryGetValue(id.Value, out var name) ? name : null;

        dto.PrimaryDelegateName = NameOf(dto.PrimaryDelegateId);
        dto.SecondaryDelegateName = NameOf(dto.SecondaryDelegateId);
        dto.ApproverDelegateName = NameOf(dto.ApproverDelegateId);
    }

    public async Task WithdrawMyLeaveChange(WithdrawMyLeaveChangeInput input)
    {
        var employeeId = await _employeeScopeProvider.GetRequiredCurrentEmployeeIdAsync();
        var (_, request) = await GetMyLeaveWithChangeInFlightAsync(input.Id, employeeId);

        // The leave is not touched: it was never changed, so there is nothing to put back.
        await _workflowApprovalService.WithdrawAsync(request.Id, employeeId, input.Reason);
    }

    /// <summary>The employee's own approved leave and the change, cancellation or return in flight against it.</summary>
    private async Task<(EmployeeAbsence Leave, WorkflowRequest Request)> GetMyLeaveWithChangeInFlightAsync(int leaveId, int employeeId)
    {
        EmployeeAbsence leave;

        using (CurrentUnitOfWork.DisableFilter(HCMv3DataFilters.ApprovedOnly))
        {
            leave = await _absenceRepository.FirstOrDefaultAsync(a => a.Id == leaveId);
        }

        if (leave == null)
        {
            throw new UserFriendlyException(L("LeaveRequestNotFound"));
        }

        if (leave.EmployeeId != employeeId)
        {
            throw new AbpAuthorizationException(L("YouCanOnlyCancelYourOwnRequests"));
        }

        var request = await _workflowAmendmentService.GetInFlightAsync(
            LeaveWorkflowTransactionHandler.LeaveRequestType, leave.Id);

        if (request == null)
        {
            throw new UserFriendlyException(L("NoRequestIsAwaitingADecisionForThisLeave"));
        }

        return (leave, request);
    }

    public async Task<List<MyLeaveHistoryItemDto>> GetMyLeaveHistory(EntityDto input)
    {
        var employeeId = await _employeeScopeProvider.GetRequiredCurrentEmployeeIdAsync();

        EmployeeAbsence leave;

        using (CurrentUnitOfWork.DisableFilter(HCMv3DataFilters.ApprovedOnly))
        {
            leave = await _absenceRepository.FirstOrDefaultAsync(a => a.Id == input.Id);
        }

        if (leave == null)
        {
            throw new UserFriendlyException(L("LeaveRequestNotFound"));
        }

        if (leave.EmployeeId != employeeId)
        {
            throw new AbpAuthorizationException(L("YouCanOnlyCancelYourOwnRequests"));
        }

        return (await BuildHistoryAsync(new[] { leave }))[leave.Id];
    }

    /// <summary>
    /// Every request raised against each of these leaves -- the application itself and each later amendment,
    /// cancellation or return -- oldest first, with what it asked for and how it ended. Read for many leaves
    /// at once because the list shows them under every row.
    /// </summary>
    private async Task<Dictionary<int, List<MyLeaveHistoryItemDto>>> BuildHistoryAsync(IReadOnlyCollection<EmployeeAbsence> leaves)
    {
        var leaveIds = leaves.Select(l => l.Id).ToList();

        var allRequests = await _requestRepository.GetAll()
            .Where(r => r.RequestType == LeaveWorkflowTransactionHandler.LeaveRequestType
                        && r.TargetEntityId.HasValue
                        && leaveIds.Contains(r.TargetEntityId.Value))
            .OrderBy(r => r.Id)
            .ToListAsync();

        var requestIds = allRequests.Select(r => r.Id).ToList();

        var logs = await _requestLogRepository.GetAll()
            .Where(l => requestIds.Contains(l.WorkflowRequestId))
            .OrderBy(l => l.ActionDateTime)
            .Select(l => new
            {
                l.WorkflowRequestId,
                l.ActionType,
                l.ActionDateTime,
                l.RequestActionComment,
                ActionByNameEn = l.ActionBy.FullNameEn,
                ActionByNameAr = l.ActionBy.FullNameAr
            })
            .ToListAsync();

        var result = new Dictionary<int, List<MyLeaveHistoryItemDto>>();

        foreach (var leave in leaves)
        {
            var requests = allRequests.Where(r => r.TargetEntityId == leave.Id).ToList();
            var history = new List<MyLeaveHistoryItemDto>();
            result[leave.Id] = history;

            foreach (var request in requests)
            {
                var requestLogs = logs.Where(l => l.WorkflowRequestId == request.Id).ToList();
                var created = requestLogs.FirstOrDefault(l => l.ActionType == RequestActionType.Created);

                var item = new MyLeaveHistoryItemDto
                {
                    WorkflowRequestId = request.Id,
                    RequestNumber = request.RequestNumber,
                    Action = ActionOf(request),
                    SubmittedOn = created?.ActionDateTime ?? request.CreationTime,
                    Reason = created?.RequestActionComment,
                    WasSentBack = requestLogs.Any(l => l.ActionType == RequestActionType.SentBack
                                                       || l.ActionType == RequestActionType.SentBackToCreator)
                };

                // The entry that ended it, when it has ended: the last approval, the rejection, or the withdrawal.
                var deciding = request.RequestStatus switch
                {
                    RequestStatus.Approved => requestLogs.LastOrDefault(l => l.ActionType == RequestActionType.Approved),
                    RequestStatus.Rejected => requestLogs.LastOrDefault(l => l.ActionType == RequestActionType.Rejected),
                    RequestStatus.Closed => requestLogs.LastOrDefault(l => l.ActionType == RequestActionType.Closed),
                    RequestStatus.Draft => requestLogs.LastOrDefault(l => l.ActionType == RequestActionType.SentBack
                                                                          || l.ActionType == RequestActionType.SentBackToCreator),
                    _ => null
                };

                item.Outcome = request.RequestStatus switch
                {
                    RequestStatus.Approved => MyLeaveHistoryOutcome.Approved,
                    RequestStatus.Rejected => MyLeaveHistoryOutcome.Rejected,
                    RequestStatus.Closed => MyLeaveHistoryOutcome.Withdrawn,
                    // A Draft request with no send-back in its log is one the employee saved and has not sent.
                    RequestStatus.Draft => item.WasSentBack ? MyLeaveHistoryOutcome.SentBack : MyLeaveHistoryOutcome.Draft,
                    _ => MyLeaveHistoryOutcome.AwaitingApproval
                };

                if (deciding != null)
                {
                    item.DecidedOn = deciding.ActionDateTime;
                    item.DecidedByNameEn = deciding.ActionByNameEn;
                    item.DecidedByNameAr = deciding.ActionByNameAr;
                    item.DecisionRemark = deciding.RequestActionComment;
                }

                if (request.IsAmendment && !string.IsNullOrWhiteSpace(request.AmendmentJson))
                {
                    var proposed = JsonSerializer.Deserialize<LeaveAmendment>(request.AmendmentJson);

                    item.StartDate = proposed.StartDate;
                    item.EndDate = proposed.EndDate;
                    item.Days = proposed.TotalLeaveDays;
                    item.ReturnDate = proposed.ReturnDate;
                    item.PaidStartDate = proposed.PaidStartDate;
                    item.PaidEndDate = proposed.PaidEndDate;
                    item.UnpaidStartDate = proposed.UnpaidStartDate;
                    item.UnpaidEndDate = proposed.UnpaidEndDate;
                    item.Reason = proposed.Reason ?? item.Reason;

                    if (!string.IsNullOrWhiteSpace(request.SnapshotJson))
                    {
                        var before = JsonSerializer.Deserialize<LeaveAmendment>(request.SnapshotJson);
                        item.BeforeStartDate = before.StartDate;
                        item.BeforeEndDate = before.EndDate;
                        item.BeforeDays = before.TotalLeaveDays;
                    }
                }
                else
                {
                    // The application itself. Its values were written straight to the row, so what it ASKED for is
                    // the earliest "before" any later change recorded -- or the row as it stands, if nothing has
                    // changed it since.
                    var firstChange = requests
                        .Where(r => r.IsAmendment && r.Id > request.Id && !string.IsNullOrWhiteSpace(r.SnapshotJson))
                        .Select(r => JsonSerializer.Deserialize<LeaveAmendment>(r.SnapshotJson))
                        .FirstOrDefault();

                    item.StartDate = firstChange?.StartDate ?? leave.StartDate;
                    item.EndDate = firstChange?.EndDate ?? leave.EndDate;
                    item.Days = firstChange?.TotalLeaveDays ?? leave.TotalLeaveDays;
                }

                history.Add(item);
            }
        }

        return result;
    }

    /// <summary>
    /// Which action a request is. Read from the column, and for a request raised before the column existed --
    /// when every amendment was stored as the default -- from what its payload says it does.
    /// </summary>
    private static WorkflowRequestAction ActionOf(WorkflowRequest request)
    {
        if (!request.IsAmendment)
        {
            return WorkflowRequestAction.New;
        }

        if (request.RequestAction != WorkflowRequestAction.New && request.RequestAction != WorkflowRequestAction.Amendment)
        {
            return request.RequestAction;
        }

        if (string.IsNullOrWhiteSpace(request.AmendmentJson))
        {
            return WorkflowRequestAction.Amendment;
        }

        var proposed = JsonSerializer.Deserialize<LeaveAmendment>(request.AmendmentJson);

        return proposed.IsCancellation
            ? WorkflowRequestAction.Cancellation
            : proposed.IsReturned ? WorkflowRequestAction.Return : WorkflowRequestAction.Amendment;
    }

    /// <summary>
    /// The leave a return may be raised against: the employee's own, approved, and not already returned from.
    /// </summary>
    private async Task<EmployeeAbsence> GetMyApprovedLeaveForReturnAsync(int leaveId, int employeeId)
    {
        EmployeeAbsence leave;

        using (CurrentUnitOfWork.DisableFilter(HCMv3DataFilters.ApprovedOnly))
        {
            leave = await _absenceRepository.FirstOrDefaultAsync(a => a.Id == leaveId);
        }

        if (leave == null)
        {
            throw new UserFriendlyException(L("LeaveRequestNotFound"));
        }

        // Re-checked on the write, as everywhere else here: a scoped list paired with an unscoped write is
        // how one employee ends up reporting a return against another's leave.
        if (leave.EmployeeId != employeeId)
        {
            throw new AbpAuthorizationException(L("YouCanOnlyCancelYourOwnRequests"));
        }

        if (leave.WorkflowStatus != RequestStatus.Approved)
        {
            throw new UserFriendlyException(L("OnlyAnApprovedLeaveCanBeReturnedFrom"));
        }

        if (leave.IsReturned)
        {
            throw new UserFriendlyException(L("ThisLeaveHasAlreadyBeenReturnedFrom"));
        }

        return leave;
    }

    public async Task<ApplyForLeaveResultDto> ApplyForLeave(ApplyForLeaveInput input)
    {
        var employeeId = await _employeeScopeProvider.GetRequiredCurrentEmployeeIdAsync();

        if (input.EndDate.Date < input.StartDate.Date)
        {
            throw new UserFriendlyException(L("EndDateCannotBeEarlierThanStartDate"));
        }

        var absenceTypeExists = await _absenceTypeRepository.GetAll()
            .AnyAsync(t => t.Id == input.AbsenceTypeId);

        if (!absenceTypeExists)
        {
            throw new UserFriendlyException(L("AbsenceTypeNotFound"));
        }

        // Draft from the first write, so the row is invisible to payroll and reports from the moment it exists.
        var leave = new EmployeeAbsence { WorkflowStatus = RequestStatus.Draft };
        await FillLeaveAsync(leave, input, employeeId);

        await _absenceRepository.InsertAndGetIdAsync(leave);
        await CurrentUnitOfWork.SaveChangesAsync();

        // Files first: an absence type that REQUIRES a document is refused at submit without one, so the
        // employee's upload has to be on the leave before it goes for approval.
        await AttachUploadedFilesAsync(leave.Id, input.AttachmentFileTokens);

        // Created and submitted in one step. Validation inside the handler (overlap, bookable balance) throws
        // from here, and because it is one unit of work the half-created leave rolls back with it.
        return await SendForApprovalAsync(leave, employeeId);
    }

    /// <summary>
    /// Sends a leave row that is still the employee's own -- just created, a saved draft, or one sent back --
    /// into the approval chain. Shared by applying in one step and by submitting a draft, so the two cannot
    /// route differently.
    ///
    /// <para>
    /// With workflow off for leave there is nobody to send it to: the row is made effective at once (status
    /// cleared), which is the pre-workflow behaviour and what makes switching workflow off a real option.
    /// </para>
    /// </summary>
    private async Task<ApplyForLeaveResultDto> SendForApprovalAsync(EmployeeAbsence leave, int employeeId)
    {
        var workflowEnabled = await _requestMasterResolver.IsWorkflowEnabledAsync(LeaveWorkflowTransactionHandler.LeaveRequestType);

        if (!workflowEnabled)
        {
            leave.WorkflowStatus = null;

            using (CurrentUnitOfWork.DisableFilter(HCMv3DataFilters.ApprovedOnly))
            {
                await _absenceRepository.UpdateAsync(leave);
            }

            return new ApplyForLeaveResultDto
            {
                EmployeeAbsenceId = leave.Id,
                SubmittedForApproval = false
            };
        }

        int requestId;

        if (leave.WorkflowRequestId.HasValue)
        {
            // Sent back earlier: the same request goes round again, with its number, log and comments.
            var existing = await _requestRepository.GetAsync(leave.WorkflowRequestId.Value);
            existing.RequestSubTypeId = leave.AbsenceTypeId;
            await _requestRepository.UpdateAsync(existing);
            requestId = existing.Id;
        }
        else
        {
            requestId = await _requestRepository.InsertAndGetIdAsync(new WorkflowRequest
            {
                TenantId = AbpSession.TenantId ?? 0,
                EmployeeId = employeeId,
                RequestType = LeaveWorkflowTransactionHandler.LeaveRequestType,
                TargetEntityId = leave.Id,
                RequestSubTypeId = leave.AbsenceTypeId,
                // One central counter for every request type, with no prefix: the number identifies the
                // request, and the type is a column on it. A per-type scheme meant leave raised here and
                // leave recorded by HR carried different-looking numbers for the same transaction.
                RequestNumber = await _requestNumberGenerator.NextAsync("LV"),
                RequestStatus = RequestStatus.Draft,
                WorkflowStep = 0
            });
        }

        // Saved before submitting so the engine's own lookups and the handler's status stamp both see rows
        // that actually exist.
        await CurrentUnitOfWork.SaveChangesAsync();

        await _workflowApprovalService.SubmitAsync(requestId, employeeId);

        return new ApplyForLeaveResultDto
        {
            EmployeeAbsenceId = leave.Id,
            WorkflowRequestId = requestId,
            SubmittedForApproval = true
        };
    }

    /// <summary>
    /// Writes what the employee entered onto a leave row -- a new one, or one that was sent back and is being
    /// corrected. Shared by both so a resubmitted leave is built exactly as a first application is.
    /// </summary>
    private async Task FillLeaveAsync(EmployeeAbsence leave, ApplyForLeaveInput input, int employeeId)
    {
        var endDate = input.EndDate.Date;
        DateTime? unpaidStart = null;
        DateTime? unpaidEnd = null;
        int? unpaidTypeId = null;

        // Legacy's additional unpaid leave: the paid part is cut to what the balance covers and the rest is
        // booked as unpaid leave when the leave is approved.
        if (input.SplitExcessAsUnpaidLeave)
        {
            var split = await PlanSplitAsync(employeeId, input);

            if (split.UnpaidLeaveTypeMissing)
            {
                throw new UserFriendlyException(L("UnpaidLeaveTypeNotConfigured", LeaveWorkflowTransactionHandler.UnpaidLeaveTypeCode));
            }

            if (!split.FitsWithinBalance)
            {
                if (split.PaidEndDate == null)
                {
                    // No balance at all: the whole leave is unpaid, so it is simply a leave of that type.
                    throw new UserFriendlyException(L("NoPaidLeaveBalanceToSplit"));
                }

                endDate = split.PaidEndDate.Value;
                unpaidStart = split.UnpaidStartDate;
                unpaidEnd = split.UnpaidEndDate;
                unpaidTypeId = await _absenceTypeRepository.GetAll()
                    .Where(t => t.Code == LeaveWorkflowTransactionHandler.UnpaidLeaveTypeCode)
                    .Select(t => (int?)t.Id)
                    .FirstOrDefaultAsync();
            }
        }

        var days = (endDate - input.StartDate.Date).TotalDays + 1;

        // What the quota is charged: calendar days less the week-offs, public holidays and rest days the type
        // excludes (its Include* flags), as legacy EmployeeAbsence.UpdateQuotaInformation computed it.
        // Half day takes its times from the duty; otherwise they are what the employee entered, if anything.
        var times = input.HalfdayType != HalfdayType.None
            ? await ResolveHalfDayTimesAsync(employeeId, input.StartDate.Date, input.HalfdayType)
            : ResolveTimes(input.StartTime, input.EndTime, input.StartDate.Date);

        var totalLeaveDays = await _absenceCalculationService.CalculateTotalLeaveDaysAsync(
            employeeId, input.AbsenceTypeId, input.StartDate.Date, endDate, times.AbsenceHours);

        leave.TenantId = AbpSession.TenantId ?? 0;
        leave.EmployeeId = employeeId;
        leave.AbsenceTypeId = input.AbsenceTypeId;
        leave.StartDate = input.StartDate.Date;
        leave.EndDate = endDate;
        leave.InitialEndDate = endDate;
        leave.CalendarDays = days;
        leave.TotalLeaveDays = totalLeaveDays;
        leave.Comments = input.Comments;
        leave.LeavePaymentType = input.LeavePaymentType;
        leave.PaymentDate = input.PaymentDate?.Date ?? await DefaultPaymentDateAsync(input.StartDate.Date);
        leave.IsTravelling = input.IsTravelling;
        leave.HalfdayType = input.HalfdayType;
        leave.StartTime = times.StartTime;
        leave.EndTime = times.EndTime;
        leave.AbsenceHours = times.AbsenceHours;
        leave.PrimaryDelegateId = input.PrimaryDelegateId;
        leave.SecondaryDelegateId = input.SecondaryDelegateId;
        leave.ApproverDelegateId = input.ApproverDelegateId;

        // Stamped when ticked and kept once stamped: a leave sent back and corrected was already accepted.
        if (input.PolicyAccepted && leave.PolicyAcceptedOn == null)
        {
            leave.PolicyAcceptedOn = Clock.Now;
        }

        // The unpaid remainder rides on the same row until approval, exactly as legacy carried it on the
        // request: one application, one approval, two leave rows once it is granted.
        leave.ExtAbsenceTypeId = unpaidTypeId;
        leave.ExtStartDate = unpaidStart;
        leave.ExtEndDate = unpaidEnd;
        leave.ExtTotalLeaveDays = unpaidStart == null
            ? null
            : await _absenceCalculationService.CalculateTotalLeaveDaysAsync(
                employeeId, input.AbsenceTypeId, unpaidStart.Value, unpaidEnd!.Value, 0);
    }


    /// <summary>
    /// How a sick leave will be paid, slab by slab (legacy GetSickLeaveUnpaidInfo). Empty for any other type.
    /// </summary>
    public async Task<List<SickLeaveDeductionSegmentDto>> PreviewMySickLeaveDeduction(ApplyForLeaveInput input)
    {
        var employeeId = await _employeeScopeProvider.GetRequiredCurrentEmployeeIdAsync();

        var segments = await _sickLeaveDeductionPreview.GetSegmentsAsync(
            employeeId, input.AbsenceTypeId, input.StartDate.Date, input.EndDate.Date);

        return segments.Select(s => new SickLeaveDeductionSegmentDto
        {
            FromDate = s.FromDate,
            ToDate = s.ToDate,
            DeductionNumber = s.DeductionNumber,
            DeductionUnit = s.DeductionUnit,
            IsBeyondTheLastSlab = s.IsBeyondTheLastSlab,
        }).ToList();
    }

    public async Task<LeaveSplitPreviewDto> PreviewMyLeaveSplit(ApplyForLeaveInput input)
    {
        var employeeId = await _employeeScopeProvider.GetRequiredCurrentEmployeeIdAsync();

        return await PlanSplitAsync(employeeId, input);
    }

    /// <summary>
    /// Legacy's additional-unpaid-leave split (ESS <c>GetLeaveInfo</c>, <c>UnifiedLeaveRequestController</c>
    /// 2503-2553): the leave is paid for as long as the balance lasts, and the remainder becomes unpaid leave.
    ///
    /// <para>
    /// The paid part ends on the day that makes its COUNTABLE days equal the whole days of balance -- walking
    /// past week-offs and holidays, which is why it is a date walk rather than "start + balance". The unpaid
    /// part runs from the next day to the end the employee asked for.
    /// </para>
    /// </summary>
    private async Task<LeaveSplitPreviewDto> PlanSplitAsync(int employeeId, ApplyForLeaveInput input)
    {
        var start = input.StartDate.Date;
        var end = input.EndDate.Date;

        var requestedDays = await _absenceCalculationService.CalculateTotalLeaveDaysAsync(
            employeeId, input.AbsenceTypeId, start, end, 0);

        var balances = await _leaveBalanceService.GetBalancesAsync(employeeId, start);
        var bookable = balances.FirstOrDefault(b => b.AbsenceTypeId == input.AbsenceTypeId)?.Bookable ?? 0;

        var preview = new LeaveSplitPreviewDto { Bookable = bookable };

        if (requestedDays <= bookable)
        {
            preview.FitsWithinBalance = true;
            preview.PaidEndDate = end;
            preview.PaidLeaveDays = requestedDays;
            return preview;
        }

        var unpaidTypeId = await _absenceTypeRepository.GetAll()
            .Where(t => t.Code == LeaveWorkflowTransactionHandler.UnpaidLeaveTypeCode)
            .Select(t => (int?)t.Id)
            .FirstOrDefaultAsync();

        if (unpaidTypeId == null)
        {
            preview.UnpaidLeaveTypeMissing = true;
            return preview;
        }

        // Whole days only: legacy takes floor(balance), so a half day of balance is not split across two rows.
        var paidDays = Math.Max(0, Math.Floor(bookable));

        DateTime? paidEnd = null;
        if (paidDays > 0)
        {
            paidEnd = await _absenceCalculationService.CalcLeaveEndDateAsync(
                employeeId, input.AbsenceTypeId, start.AddDays(-1), paidDays);
        }

        if (paidEnd >= end)
        {
            preview.FitsWithinBalance = true;
            preview.PaidEndDate = end;
            preview.PaidLeaveDays = requestedDays;
            return preview;
        }

        preview.PaidEndDate = paidEnd;
        preview.PaidLeaveDays = paidEnd == null
            ? 0
            : await _absenceCalculationService.CalculateTotalLeaveDaysAsync(employeeId, input.AbsenceTypeId, start, paidEnd.Value, 0);

        preview.UnpaidStartDate = (paidEnd ?? start.AddDays(-1)).AddDays(1);
        preview.UnpaidEndDate = end;

        // Counted with the MAIN type's week-off and holiday rules, as legacy's SaveAdditionalUnpaidLeave did.
        preview.UnpaidLeaveDays = await _absenceCalculationService.CalculateTotalLeaveDaysAsync(
            employeeId, input.AbsenceTypeId, preview.UnpaidStartDate.Value, end, 0);

        return preview;
    }

    /// <summary>
    /// Legacy <c>CalculatePaymentDate</c> (<c>AbsenceRequest</c> 231-264): a leave starting on or before the
    /// 15th is paid on the 1st, one starting later on the 16th -- each only when the payroll settings say so,
    /// and otherwise left empty. The approval then moves it to the first working day from there.
    /// </summary>
    private async Task<DateTime> DefaultPaymentDateAsync(DateTime startDate)
    {
        var payrollSettings = await _sysSettingsAccessor.GetPayrollSettingsAsync();
        if (payrollSettings == null)
        {
            return default;
        }

        if (startDate.Day <= 15 && payrollSettings.LeaveAdvPaymentdateBefore15)
        {
            return new DateTime(startDate.Year, startDate.Month, 1);
        }

        if (startDate.Day > 15 && payrollSettings.LeaveAdvPaymentdateAfter15)
        {
            return new DateTime(startDate.Year, startDate.Month, 16);
        }

        return default;
    }

    /// <summary>Moves the employee's uploads onto the new leave, through the shared attachment store.</summary>
    private async Task AttachUploadedFilesAsync(int leaveId, List<string> fileTokens)
    {
        if (fileTokens == null || fileTokens.Count == 0)
        {
            return;
        }

        foreach (var token in fileTokens.Where(t => !string.IsNullOrWhiteSpace(t)))
        {
            await _attachmentAppService.AddAttachment(new AddAttachmentInput
            {
                EntityType = LeaveRuleValidator.LeaveAttachmentEntityType,
                EntityId = leaveId.ToString(System.Globalization.CultureInfo.InvariantCulture),
                FileToken = token,
            });
        }

        await CurrentUnitOfWork.SaveChangesAsync();
    }

    public async Task CancelMyLeave(EntityDto input)
    {
        var employeeId = await _employeeScopeProvider.GetRequiredCurrentEmployeeIdAsync();

        using (CurrentUnitOfWork.DisableFilter(HCMv3DataFilters.ApprovedOnly))
        {
            var leave = await _absenceRepository.FirstOrDefaultAsync(a => a.Id == input.Id);

            if (leave == null)
            {
                throw new UserFriendlyException(L("LeaveRequestNotFound"));
            }

            // Ownership is checked here as well as by the scoped query, because this is a write: a scoped
            // list paired with an unscoped write is how one employee ends up cancelling another's leave.
            if (leave.EmployeeId != employeeId)
            {
                throw new AbpAuthorizationException(L("YouCanOnlyCancelYourOwnRequests"));
            }

            if (!CanCancel(leave))
            {
                // Once approved it is a real transaction others may have planned around; withdrawing it is
                // an amendment, not a cancellation.
                throw new UserFriendlyException(L("ThisLeaveCanNoLongerBeCancelled"));
            }

            if (leave.WorkflowRequestId.HasValue)
            {
                var request = await _requestRepository.FirstOrDefaultAsync(r => r.Id == leave.WorkflowRequestId.Value);
                if (request != null)
                {
                    request.RequestStatus = RequestStatus.Closed;
                    await _requestRepository.UpdateAsync(request);
                }
            }

            await _absenceRepository.DeleteAsync(leave);
        }
    }

    /// <summary>
    /// Only while the request is still the employee's to withdraw. A row with no workflow status was
    /// entered by HR rather than requested, so it is not theirs to cancel either.
    /// </summary>
    private static bool CanCancel(EmployeeAbsence leave)
    {
        return leave.WorkflowStatus == RequestStatus.Draft || leave.WorkflowStatus == RequestStatus.Open;
    }

    /// <summary>
    /// The leave's own request in the standard set of statuses. A leave HR entered has no request and is in
    /// effect, so it reads as approved.
    /// </summary>
    private static MyLeaveRequestStatus OwnStatusOf(MyLeaveDto leave)
    {
        return leave.WorkflowStatus switch
        {
            RequestStatus.Draft => leave.IsSentBack ? MyLeaveRequestStatus.SentBack : MyLeaveRequestStatus.Draft,
            RequestStatus.Open or RequestStatus.Transferred => MyLeaveRequestStatus.Open,
            RequestStatus.Rejected => MyLeaveRequestStatus.Rejected,
            _ => MyLeaveRequestStatus.Approved
        };
    }

    private static MyLeaveDto ToDto(EmployeeAbsence entity)
    {
        return new MyLeaveDto
        {
            Id = entity.Id,
            AbsenceTypeId = entity.AbsenceTypeId,
            AbsenceTypeNameEn = entity.AbsenceType?.NameEn,
            AbsenceTypeNameAr = entity.AbsenceType?.NameAr,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate,
            TotalLeaveDays = entity.TotalLeaveDays,
            Comments = entity.Comments,
            WorkflowStatus = entity.WorkflowStatus,
            WorkflowRequestId = entity.WorkflowRequestId,
            CanCancel = CanCancel(entity),
            IsReturned = entity.IsReturned,
            ReturnDate = entity.ReturnDate == default ? null : entity.ReturnDate,

            // Only worth showing when it differs: every leave starts with the two equal.
            InitialEndDate = entity.InitialEndDate != default && entity.InitialEndDate.Date != entity.EndDate.Date
                ? entity.InitialEndDate
                : null,
            AbsenceHours = entity.AbsenceHours,
            StartTime = entity.AbsenceHours != 0 ? entity.StartTime.ToString("HH:mm") : null,
            EndTime = entity.AbsenceHours != 0 ? entity.EndTime.ToString("HH:mm") : null,
            HalfdayType = entity.HalfdayType,

            // Applying creates and submits in one step, so a Draft row that has a request is one an approver
            // sent back.
            IsSentBack = entity.WorkflowStatus == RequestStatus.Draft && entity.WorkflowRequestId.HasValue,

            // ...and one with no request yet is a draft the employee saved and has not sent.
            IsDraft = entity.WorkflowStatus == RequestStatus.Draft && !entity.WorkflowRequestId.HasValue,
            CalendarDays = entity.CalendarDays,
            LeavePaymentType = entity.LeavePaymentType,
            PaymentDate = entity.PaymentDate == default ? null : entity.PaymentDate,
            IsTravelling = entity.IsTravelling,
            PrimaryDelegateId = entity.PrimaryDelegateId,
            SecondaryDelegateId = entity.SecondaryDelegateId,
            ApproverDelegateId = entity.ApproverDelegateId,
            PolicyAccepted = entity.PolicyAcceptedOn.HasValue
        };
    }
}
