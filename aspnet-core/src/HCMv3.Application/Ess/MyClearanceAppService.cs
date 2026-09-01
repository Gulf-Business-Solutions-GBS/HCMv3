using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Linq.Extensions;
using Abp.UI;
using HCMv3.Authorization;
using HCMv3.Ess.Dto;
using HCMv3.GeneralMasterInfo;
using HCMv3.PersonnelAdministration;
using HCMv3.PersonnelAdministration.Services;
using HCMv3.PersonnelAdministration.Workflow;
using HCMv3.Workflow;
using HCMv3.Workflow.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.Ess;

/// <inheritdoc cref="IMyClearanceAppService"/>
[AbpAuthorize(AppPermissions.Ess_MyClearances)]
public class MyClearanceAppService : MyTransactionAppServiceBase<EmployeeClearance>, IMyClearanceAppService
{
    private readonly IRepository<ClearanceRequestType> _clearanceTypeRepository;
    private readonly IClearanceChecklistService _checklistService;

    public MyClearanceAppService(
        IRepository<EmployeeClearance> repository,
        IRepository<WorkflowRequest> requestRepository,
        IRepository<ClearanceRequestType> clearanceTypeRepository,
        IEmployeeScopeProvider employeeScopeProvider,
        IRequestMasterResolver requestMasterResolver,
        IWorkflowApprovalService workflowApprovalService,
        IClearanceChecklistService checklistService)
        : base(repository, requestRepository, employeeScopeProvider, requestMasterResolver, workflowApprovalService)
    {
        _clearanceTypeRepository = clearanceTypeRepository;
        _checklistService = checklistService;
    }

    protected override string RequestType => ClearanceWorkflowTransactionHandler.ClearanceRequestType;

    protected override string RequestNumberPrefix => "CLR";

    [HttpPost]
    public async Task<PagedResultDto<MyClearanceDto>> GetMyClearances(GetMyRequestsInput input)
    {
        input.Normalize();

        var employeeId = await GetMyEmployeeIdAsync();

        using (CurrentUnitOfWork.DisableFilter(HCMv3DataFilters.ApprovedOnly))
        {
            // A clearance is a one-off at the end of employment, so there is no "current period" to narrow
            // to the way leave or exit permits have; history simply means the decided ones.
            var query = MyRowsQuery(employeeId)
                .WhereIf(!input.IncludeHistory,
                    c => c.WorkflowStatus == null
                         || c.WorkflowStatus == RequestStatus.Draft
                         || c.WorkflowStatus == RequestStatus.Open);

            var totalCount = await query.CountAsync();

            var items = await query.OrderBy(input.Sorting).PageBy(input).ToListAsync();

            var typeIds = items.Select(i => i.ClearanceRequestTypeId).Distinct().ToList();

            var types = await _clearanceTypeRepository.GetAll()
                .Where(t => typeIds.Contains(t.Id))
                .ToListAsync();

            return new PagedResultDto<MyClearanceDto>(
                totalCount,
                items.Select(i => ToDto(i, types.FirstOrDefault(t => t.Id == i.ClearanceRequestTypeId))).ToList());
        }
    }

    public async Task<List<ClearanceTypeLookupDto>> GetClearanceTypes()
    {
        // IncludeInWorkflow is the whole filter: a type without it routes nowhere, so offering it would
        // produce a request no approver ever sees.
        var types = await _clearanceTypeRepository.GetAll()
            .Where(t => t.IncludeInWorkflow)
            .OrderBy(t => t.NameEn)
            .ToListAsync();

        return types.Select(t => new ClearanceTypeLookupDto
        {
            Id = t.Id,
            Code = t.Code,
            NameEn = t.NameEn,
            NameAr = t.NameAr
        }).ToList();
    }

    public async Task<MyRequestSubmissionDto> RequestClearance(RequestClearanceInput input)
    {
        var type = await _clearanceTypeRepository.FirstOrDefaultAsync(t => t.Id == input.ClearanceRequestTypeId);

        if (type == null || !type.IncludeInWorkflow)
        {
            // Same rule as the dropdown, enforced on the write. A list that filters paired with a save that
            // does not is how a stale form ends up raising a clearance nobody can approve.
            throw new UserFriendlyException(L("ThisClearanceTypeIsNotAvailableForSelfService"));
        }

        if (input.EndDate < input.StartDate)
        {
            // The handler refuses this too; this is the early, friendlier failure before a row is created
            // and rolled back.
            throw new UserFriendlyException(L("EndDateCannotBeBeforeStartDate"));
        }

        return await CreateAndSubmitAsync(
            employeeId => new EmployeeClearance
            {
                TenantId = AbpSession.TenantId ?? 0,
                EmployeeId = employeeId,
                ClearanceRequestTypeId = input.ClearanceRequestTypeId,
                StartDate = input.StartDate.Date,
                EndDate = input.EndDate.Date,
                Comments = input.Comments
            },
            // The clearance type IS the sub-type, so a resignation clearance can route through a different
            // chain from an end-of-contract one.
            input.ClearanceRequestTypeId);
    }

    public Task CancelMyClearance(EntityDto input) => CancelAsync(input.Id);

    public async Task<ClearanceChecklistDto> GetClearanceChecklist(EntityDto input)
    {
        // The approver, not the subject: this is the one place in the service where "my employee id" means
        // the person signing rather than the person leaving.
        var approverId = await GetMyEmployeeIdAsync();

        var lines = await _checklistService.GetLinesForApproverAsync(input.Id, approverId);

        return new ClearanceChecklistDto
        {
            EmployeeClearanceId = input.Id,
            CanRecordAnswers = await _checklistService.IsAwaitingParallelApprovalAsync(input.Id),
            Lines = lines.Select(l => new ClearanceChecklistLineDto
            {
                ClearanceRequestFieldId = l.ClearanceRequestFieldId,
                Code = l.Code,
                NameEn = l.NameEn,
                NameAr = l.NameAr,
                Value = l.Value,
                AnsweredByEmployeeId = l.AnsweredByEmployeeId
            }).ToList()
        };
    }

    public async Task SaveClearanceChecklist(SaveClearanceChecklistInput input)
    {
        var approverId = await GetMyEmployeeIdAsync();

        var answers = (input.Answers ?? new List<ClearanceChecklistAnswerInput>())
            .Select(a => new ClearanceChecklistAnswer
            {
                ClearanceRequestFieldId = a.ClearanceRequestFieldId,
                Value = a.Value
            })
            .ToList();

        await _checklistService.SaveAnswersForApproverAsync(input.EmployeeClearanceId, approverId, answers);
    }

    private static MyClearanceDto ToDto(EmployeeClearance entity, ClearanceRequestType type)
    {
        return new MyClearanceDto
        {
            Id = entity.Id,
            ClearanceRequestTypeId = entity.ClearanceRequestTypeId,
            ClearanceRequestTypeNameEn = type?.NameEn,
            ClearanceRequestTypeNameAr = type?.NameAr,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate,
            Comments = entity.Comments,
            WorkflowStatus = entity.WorkflowStatus,
            WorkflowRequestId = entity.WorkflowRequestId,
            CanCancel = CanCancel(entity)
        };
    }
}
