using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.UI;
using HCMv3.GeneralMasterInfo;
using HCMv3.Workflow;
using HCMv3.Workflow.Services;

namespace HCMv3.PersonnelAdministration.Services;

/// <inheritdoc cref="IClearanceChecklistService"/>
public class ClearanceChecklistService : HCMv3DomainServiceBase, IClearanceChecklistService
{
    private readonly IRepository<EmployeeClearance> _clearanceRepository;
    private readonly IRepository<EmployeeClearanceDetail> _detailRepository;
    private readonly IRepository<ClearanceRequestField> _fieldRepository;
    private readonly IRepository<ClearanceRequestFieldDetail> _fieldDetailRepository;
    private readonly IRepository<EmployeeWorkflowRole> _employeeWorkflowRoleRepository;
    private readonly IRepository<WorkflowRequest> _requestRepository;
    private readonly IRepository<RequestDetail> _requestDetailRepository;
    private readonly IRequestMasterResolver _requestMasterResolver;
    private readonly IUnitOfWorkManager _unitOfWorkManager;

    public ClearanceChecklistService(
        IRepository<EmployeeClearance> clearanceRepository,
        IRepository<EmployeeClearanceDetail> detailRepository,
        IRepository<ClearanceRequestField> fieldRepository,
        IRepository<ClearanceRequestFieldDetail> fieldDetailRepository,
        IRepository<EmployeeWorkflowRole> employeeWorkflowRoleRepository,
        IRepository<WorkflowRequest> requestRepository,
        IRepository<RequestDetail> requestDetailRepository,
        IRequestMasterResolver requestMasterResolver,
        IUnitOfWorkManager unitOfWorkManager)
    {
        _clearanceRepository = clearanceRepository;
        _detailRepository = detailRepository;
        _fieldRepository = fieldRepository;
        _fieldDetailRepository = fieldDetailRepository;
        _employeeWorkflowRoleRepository = employeeWorkflowRoleRepository;
        _requestRepository = requestRepository;
        _requestDetailRepository = requestDetailRepository;
        _requestMasterResolver = requestMasterResolver;
        _unitOfWorkManager = unitOfWorkManager;
    }

    public Task<List<ClearanceChecklistLine>> GetLinesForApproverAsync(int employeeClearanceId, int approverEmployeeId)
    {
        var clearance = GetClearance(employeeClearanceId);

        var lines = BuildLines(clearance, approverEmployeeId);

        return Task.FromResult(lines);
    }

    public async Task SaveAnswersForApproverAsync(
        int employeeClearanceId,
        int approverEmployeeId,
        IReadOnlyCollection<ClearanceChecklistAnswer> answers)
    {
        var clearance = GetClearance(employeeClearanceId);

        await EnsureOnAParallelStepAsync(clearance);

        if (answers == null || answers.Count == 0)
        {
            return;
        }

        // Recomputed here rather than trusting the ids the screen posted back. The screen showing only the
        // approver's own lines is a convenience; this is the rule.
        var ownedFieldIds = BuildLines(clearance, approverEmployeeId)
            .Select(l => l.ClearanceRequestFieldId)
            .ToHashSet();

        var existing = _detailRepository.GetAll()
            .Where(d => d.EmployeeClearanceId == clearance.Id)
            .ToList();

        foreach (var answer in answers)
        {
            if (!ownedFieldIds.Contains(answer.ClearanceRequestFieldId))
            {
                throw new UserFriendlyException(L("ThisClearanceLineBelongsToAnotherDepartment"));
            }

            var row = existing.FirstOrDefault(d => d.ClearanceRequestFieldId == answer.ClearanceRequestFieldId);

            if (row == null)
            {
                await _detailRepository.InsertAsync(new EmployeeClearanceDetail
                {
                    TenantId = clearance.TenantId,
                    EmployeeClearanceId = clearance.Id,
                    ClearanceRequestFieldId = answer.ClearanceRequestFieldId,
                    Value = answer.Value,
                    ApproverId = approverEmployeeId
                });
            }
            else
            {
                // Upsert rather than insert-only: a department correcting its own answer before the step
                // closes is ordinary, and a second row for the same line would make "what did IT say" a
                // question with two answers.
                row.Value = answer.Value;
                row.ApproverId = approverEmployeeId;
                await _detailRepository.UpdateAsync(row);
            }
        }
    }

    /// <summary>
    /// The narrowing itself: the roles this approver currently holds, intersected with the roles that own
    /// the checklist lines of this clearance's type, merged with whatever has already been answered.
    /// </summary>
    private List<ClearanceChecklistLine> BuildLines(EmployeeClearance clearance, int approverEmployeeId)
    {
        var today = DateTime.Today;

        // Date-bounded on purpose: an EmployeeWorkflowRole is an assignment for a period, so somebody who
        // covered IT last quarter is not shown IT's questions today.
        var roleIds = _employeeWorkflowRoleRepository.GetAll()
            .Where(r => r.EmployeeId == approverEmployeeId
                        && r.StartDate <= today
                        && r.EndDate >= today)
            .Select(r => r.WorkflowRoleId)
            .Distinct()
            .ToList();

        if (roleIds.Count == 0)
        {
            return new List<ClearanceChecklistLine>();
        }

        var ownership = _fieldDetailRepository.GetAll()
            .Where(d => d.ClearanceRequestFieldId.HasValue && roleIds.Contains(d.WorkflowRoleId))
            .Select(d => new { FieldId = d.ClearanceRequestFieldId.Value, d.WorkflowRoleId })
            .ToList();

        if (ownership.Count == 0)
        {
            return new List<ClearanceChecklistLine>();
        }

        var ownedFieldIds = ownership.Select(o => o.FieldId).Distinct().ToList();

        // A field with no type belongs to every checklist -- that is how legacy's shared lines ("ID card
        // returned") avoid being duplicated once per clearance type.
        var fields = _fieldRepository.GetAll()
            .Where(f => ownedFieldIds.Contains(f.Id)
                        && f.IsActive
                        && (f.ClearanceRequestTypeId == null
                            || f.ClearanceRequestTypeId == clearance.ClearanceRequestTypeId))
            .ToList();

        var answers = _detailRepository.GetAll()
            .Where(d => d.EmployeeClearanceId == clearance.Id)
            .ToList();

        return fields
            .OrderBy(f => f.Code)
            .ThenBy(f => f.Id)
            .Select(f =>
            {
                var answer = answers.FirstOrDefault(a => a.ClearanceRequestFieldId == f.Id);

                return new ClearanceChecklistLine
                {
                    ClearanceRequestFieldId = f.Id,
                    Code = f.Code,
                    NameEn = f.NameEn,
                    NameAr = f.NameAr,
                    WorkflowRoleId = ownership.First(o => o.FieldId == f.Id).WorkflowRoleId,
                    Value = answer?.Value,
                    AnsweredByEmployeeId = answer?.ApproverId
                };
            })
            .ToList();
    }

    public async Task<bool> IsAwaitingParallelApprovalAsync(int employeeClearanceId)
    {
        var clearance = GetClearance(employeeClearanceId);

        return await GetBlockingReasonAsync(clearance) == null;
    }

    private async Task EnsureOnAParallelStepAsync(EmployeeClearance clearance)
    {
        var reason = await GetBlockingReasonAsync(clearance);

        if (reason != null)
        {
            throw new UserFriendlyException(L(reason));
        }
    }

    /// <summary>
    /// Null when answers may be recorded, otherwise the localization key saying why not. Read from the
    /// resolved <c>RequestMaster</c>'s <c>RequestDetail</c> for the request's CURRENT step, so the form
    /// closes when the parallel step does rather than staying writable for the life of the record.
    /// </summary>
    private async Task<string> GetBlockingReasonAsync(EmployeeClearance clearance)
    {
        if (!clearance.WorkflowRequestId.HasValue)
        {
            return "ThisClearanceIsNotAwaitingApproval";
        }

        var request = _requestRepository.FirstOrDefault(clearance.WorkflowRequestId.Value);

        if (request == null || request.RequestStatus != RequestStatus.Open)
        {
            return "ThisClearanceIsNotAwaitingApproval";
        }

        var requestMaster = await _requestMasterResolver.ResolveAsync(request);

        var requestDetail = _requestDetailRepository.GetAll()
            .FirstOrDefault(d => d.RequestMasterId == requestMaster.Id && d.Step == request.WorkflowStep);

        if (requestDetail == null || requestDetail.ApprovalRequire != ApprovalRequire.Parallel)
        {
            return "ClearanceAnswersCanOnlyBeRecordedOnAParallelStep";
        }

        return null;
    }

    private EmployeeClearance GetClearance(int employeeClearanceId)
    {
        // The ApprovedOnly filter hides a clearance while it is in flight -- which is precisely when
        // departments are filling it in -- so the lookup has to opt out of it.
        using (_unitOfWorkManager.Current.DisableFilter(HCMv3DataFilters.ApprovedOnly))
        {
            var clearance = _clearanceRepository.GetAll().FirstOrDefault(c => c.Id == employeeClearanceId);

            if (clearance == null)
            {
                throw new UserFriendlyException(L("RequestNotFound"));
            }

            return clearance;
        }
    }
}
