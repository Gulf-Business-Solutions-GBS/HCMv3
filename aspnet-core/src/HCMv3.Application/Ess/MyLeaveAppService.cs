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
using Abp.UI;
using HCMv3.Authorization;
using HCMv3.Ess.Dto;
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration;
using HCMv3.PersonnelAdministration.Services;
using HCMv3.PersonnelAdministration.Workflow;
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

    public MyLeaveAppService(
        IRepository<EmployeeAbsence> absenceRepository,
        IRepository<AbsenceType> absenceTypeRepository,
        IRepository<WorkflowRequest> requestRepository,
        ILeaveBalanceService leaveBalanceService,
        IEmployeeScopeProvider employeeScopeProvider,
        IRequestMasterResolver requestMasterResolver,
        IWorkflowApprovalService workflowApprovalService,
        IUnitOfWorkManager unitOfWorkManager,
        IWorkflowAmendmentService workflowAmendmentService)
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
            TotalLeaveDays = (input.EndDate.Date - input.StartDate.Date).TotalDays + 1,
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

        var leave = new EmployeeAbsence
        {
            TenantId = AbpSession.TenantId ?? 0,
            EmployeeId = employeeId,
            AbsenceTypeId = input.AbsenceTypeId,
            StartDate = input.StartDate.Date,
            EndDate = input.EndDate.Date,
            InitialEndDate = input.EndDate.Date,
            CalendarDays = days,
            TotalLeaveDays = days,
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
            CanCancel = CanCancel(entity)
        };
    }
}
