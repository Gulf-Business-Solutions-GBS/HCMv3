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
using HCMv3.Ess.Dto;
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
    private readonly ILeaveBalanceService _leaveBalanceService;
    private readonly IEmployeeScopeProvider _employeeScopeProvider;
    private readonly IRequestMasterResolver _requestMasterResolver;
    private readonly IWorkflowApprovalService _workflowApprovalService;
    private readonly IUnitOfWorkManager _unitOfWorkManager;
    private readonly IWorkflowAmendmentService _workflowAmendmentService;
    private readonly IEmployeeAbsenceCalculationService _absenceCalculationService;
    private readonly ISysSettingsAccessor _sysSettingsAccessor;

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
        ISysSettingsAccessor sysSettingsAccessor)
    {
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
                .WhereIf(!input.IncludeHistory, a => a.EndDate >= today || a.WorkflowStatus == RequestStatus.Open);

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderBy(input.Sorting)
                .PageBy(input)
                .ToListAsync();

            var dtos = items.Select(ToDto).ToList();

            await FillAmendmentStateAsync(dtos);

            return new PagedResultDto<MyLeaveDto>(totalCount, dtos);
        }
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
        var approved = leaves.Where(l => l.WorkflowStatus == RequestStatus.Approved).ToList();
        if (approved.Count == 0)
        {
            return;
        }

        var amendable = await _workflowAmendmentService.IsAmendmentEnabledAsync(
            LeaveWorkflowTransactionHandler.LeaveRequestType);

        var leaveIds = approved.Select(l => l.Id).ToList();

        var inFlight = (await _requestRepository.GetAll()
            .Where(r => r.IsAmendment
                        && r.RequestType == LeaveWorkflowTransactionHandler.LeaveRequestType
                        && r.TargetEntityId.HasValue
                        && leaveIds.Contains(r.TargetEntityId.Value)
                        && (r.RequestStatus == RequestStatus.Open || r.RequestStatus == RequestStatus.Draft))
            .Select(r => new { TargetId = r.TargetEntityId.Value, r.Id })
            .ToListAsync())
            .GroupBy(r => r.TargetId)
            .ToDictionary(g => g.Key, g => g.First().Id);

        foreach (var leave in approved)
        {
            if (inFlight.TryGetValue(leave.Id, out var requestId))
            {
                leave.HasPendingAmendment = true;
                leave.PendingAmendmentRequestId = requestId;
                continue;
            }

            leave.CanAmend = amendable;
            leave.CanRequestCancellation = amendable;
            leave.CanReturn = amendable && !leave.IsReturned;
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

        var amendment = new LeaveAmendment
        {
            AbsenceTypeId = input.AbsenceTypeId,
            StartDate = input.StartDate.Date,
            EndDate = input.EndDate.Date,
            // Legacy EmployeeAbsence.UpdateQuotaInformation: calendar days less the days the type excludes.
            TotalLeaveDays = await _absenceCalculationService.CalculateTotalLeaveDaysAsync(
                employeeId, input.AbsenceTypeId, input.StartDate.Date, input.EndDate.Date, 0),
            Comments = input.Reason
        };

        return await _workflowAmendmentService.CreateAndSubmitAsync(
            LeaveWorkflowTransactionHandler.LeaveRequestType,
            leave.Id,
            leave.EmployeeId,
            JsonSerializer.Serialize(amendment),
            employeeId,
            input.AbsenceTypeId);
    }

    public async Task<ReturnFromLeaveResultDto> ReturnFromMyLeave(ReturnFromLeaveInput input)
    {
        var employeeId = await _employeeScopeProvider.GetRequiredCurrentEmployeeIdAsync();

        var leave = await GetMyApprovedLeaveForReturnAsync(input.EmployeeAbsenceId, employeeId);

        var plan = await PlanReturnAsync(leave, input, useProposedSplitWhenEmpty: true);

        var amendment = new LeaveAmendment
        {
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
            UnpaidEndDate = plan.UnpaidEndDate
        };

        // The absence type's AllowToReturnEarly rule and every late-return rule are enforced inside the leave
        // handler, not here, so they apply at submit AND again at approval.
        var requestId = await _workflowAmendmentService.CreateAndSubmitAsync(
            LeaveWorkflowTransactionHandler.LeaveRequestType,
            leave.Id,
            leave.EmployeeId,
            JsonSerializer.Serialize(amendment),
            employeeId,
            leave.AbsenceTypeId);

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

        var daysDifference = await _absenceCalculationService.GetReturnDaysDifferenceAsync(
            leave.EmployeeId, leave.AbsenceTypeId, leave.EndDate.Date, rejoiningDate);

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

    public async Task<int> RequestCancellationOfMyLeave(EntityDto input)
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

        // The current values ride along so the approver's summary shows exactly what is being removed.
        var cancellation = new LeaveAmendment
        {
            AbsenceTypeId = leave.AbsenceTypeId,
            StartDate = leave.StartDate.Date,
            EndDate = leave.EndDate.Date,
            TotalLeaveDays = leave.TotalLeaveDays,
            Comments = leave.Comments,
            IsCancellation = true
        };

        return await _workflowAmendmentService.CreateAndSubmitAsync(
            LeaveWorkflowTransactionHandler.LeaveRequestType,
            leave.Id,
            leave.EmployeeId,
            JsonSerializer.Serialize(cancellation),
            employeeId,
            leave.AbsenceTypeId);
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

        var days = (input.EndDate.Date - input.StartDate.Date).TotalDays + 1;

        // What the quota is charged: calendar days less the week-offs, public holidays and rest days the type
        // excludes (its Include* flags), as legacy EmployeeAbsence.UpdateQuotaInformation computed it.
        var totalLeaveDays = await _absenceCalculationService.CalculateTotalLeaveDaysAsync(
            employeeId, input.AbsenceTypeId, input.StartDate.Date, input.EndDate.Date, 0);

        var leave = new EmployeeAbsence
        {
            TenantId = AbpSession.TenantId ?? 0,
            EmployeeId = employeeId,
            AbsenceTypeId = input.AbsenceTypeId,
            StartDate = input.StartDate.Date,
            EndDate = input.EndDate.Date,
            InitialEndDate = input.EndDate.Date,
            CalendarDays = days,
            TotalLeaveDays = totalLeaveDays,
            Comments = input.Comments
        };

        var workflowEnabled = await _requestMasterResolver.IsWorkflowEnabledAsync(LeaveWorkflowTransactionHandler.LeaveRequestType);

        // Draft while workflow-managed, so the row is invisible to payroll and reports from the moment it
        // exists. With workflow off it is left null, i.e. immediately effective -- the pre-workflow
        // behaviour, and the reason turning workflow off for a type is a real option rather than a
        // half-working one.
        leave.WorkflowStatus = workflowEnabled ? RequestStatus.Draft : (RequestStatus?)null;

        var leaveId = await _absenceRepository.InsertAndGetIdAsync(leave);

        if (!workflowEnabled)
        {
            return new ApplyForLeaveResultDto
            {
                EmployeeAbsenceId = leaveId,
                SubmittedForApproval = false
            };
        }

        var request = new WorkflowRequest
        {
            TenantId = AbpSession.TenantId ?? 0,
            EmployeeId = employeeId,
            RequestType = LeaveWorkflowTransactionHandler.LeaveRequestType,
            TargetEntityId = leaveId,
            RequestSubTypeId = input.AbsenceTypeId,
            RequestNumber = await GenerateRequestNumberAsync(),
            RequestStatus = RequestStatus.Draft,
            WorkflowStep = 0
        };

        var requestId = await _requestRepository.InsertAndGetIdAsync(request);

        // Saved before submitting so the engine's own lookups and the handler's status stamp both see rows
        // that actually exist.
        await CurrentUnitOfWork.SaveChangesAsync();

        // Created and submitted in one step: an employee should never be left holding a draft they have to
        // remember to send. Validation inside the handler (overlap, bookable balance) throws from here, and
        // because it is one unit of work the half-created leave rolls back with it.
        await _workflowApprovalService.SubmitAsync(requestId, employeeId);

        return new ApplyForLeaveResultDto
        {
            EmployeeAbsenceId = leaveId,
            WorkflowRequestId = requestId,
            SubmittedForApproval = true
        };
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

    private async Task<string> GenerateRequestNumberAsync()
    {
        var year = DateTime.Today.Year;
        var prefix = "LV-" + year + "-";

        var lastNumber = await _requestRepository.GetAll()
            .Where(r => r.RequestNumber.StartsWith(prefix))
            .OrderByDescending(r => r.RequestNumber)
            .Select(r => r.RequestNumber)
            .FirstOrDefaultAsync();

        var next = 1;
        if (!string.IsNullOrEmpty(lastNumber) && int.TryParse(lastNumber.Substring(prefix.Length), out var parsed))
        {
            next = parsed + 1;
        }

        return prefix + next.ToString("D5");
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
            IsReturned = entity.IsReturned
        };
    }
}
