using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Extensions;
using Abp.Linq.Extensions;
using HCMv3.Authorization;
using HCMv3.Workflow.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.Workflow;

/// <summary>
/// Application service for <see cref="WorkflowRequest"/> (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.Workflow.WorkflowRequest</c> -- see that entity's remarks for the
/// deliberate scope decision to port it as a standalone, non-abstract table rather than an EF Core
/// inheritance base for the ~50 out-of-scope concrete feature request subclasses).
/// </summary>
[AbpAuthorize(AppPermissions.Pages_Workflow_WorkflowRequests)]
public class WorkflowRequestsAppService : HCMv3AppServiceBase, IWorkflowRequestsAppService
{
    private readonly IRepository<WorkflowRequest> _repository;

    public WorkflowRequestsAppService(IRepository<WorkflowRequest> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<WorkflowRequestDto>> GetAll(GetAllWorkflowRequestsInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .Include(r => r.Employee)
            .Include(r => r.Approver)
            .WhereIf(input.EmployeeId.HasValue, r => r.EmployeeId == input.EmployeeId.Value)
            .WhereIf(!input.Filter.IsNullOrWhiteSpace(), r =>
                r.RequestNumber.Contains(input.Filter) ||
                r.RequestType.Contains(input.Filter));

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<WorkflowRequestDto>(totalCount, items.Select(ToDto).ToList());
    }

    public async Task<GetWorkflowRequestForEditOutput> GetWorkflowRequestForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);
        return new GetWorkflowRequestForEditOutput { WorkflowRequest = ToCreateOrEditDto(entity) };
    }

    public async Task CreateOrEdit(CreateOrEditWorkflowRequestDto input)
    {
        if (input.Id.HasValue)
        {
            await UpdateAsync(input);
        }
        else
        {
            await CreateAsync(input);
        }
    }

    [AbpAuthorize(AppPermissions.Pages_Workflow_WorkflowRequests_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_Workflow_WorkflowRequests_Create)]
    protected virtual async Task CreateAsync(CreateOrEditWorkflowRequestDto input)
    {
        var entity = MapToEntity(input, new WorkflowRequest());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_Workflow_WorkflowRequests_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditWorkflowRequestDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await _repository.UpdateAsync(entity);
    }

    private static WorkflowRequest MapToEntity(CreateOrEditWorkflowRequestDto input, WorkflowRequest entity)
    {
        entity.EmployeeId = input.EmployeeId;
        entity.WorkflowStep = input.WorkflowStep;
        entity.ApproverId = input.ApproverId;
        entity.RequestType = input.RequestType;
        entity.RequestNumber = input.RequestNumber;
        entity.RequestStatus = input.RequestStatus;
        entity.ApprovalProcessTime = input.ApprovalProcessTime;
        return entity;
    }

    private static WorkflowRequestDto ToDto(WorkflowRequest entity)
    {
        return new WorkflowRequestDto
        {
            Id = entity.Id,
            EmployeeId = entity.EmployeeId,
            EmployeeNameEn = entity.Employee?.FirstNameEn,
            WorkflowStep = entity.WorkflowStep,
            ApproverId = entity.ApproverId,
            ApproverNameEn = entity.Approver?.FirstNameEn,
            RequestType = entity.RequestType,
            RequestNumber = entity.RequestNumber,
            RequestStatus = entity.RequestStatus,
            ApprovalProcessTime = entity.ApprovalProcessTime
        };
    }

    private static CreateOrEditWorkflowRequestDto ToCreateOrEditDto(WorkflowRequest entity)
    {
        return new CreateOrEditWorkflowRequestDto
        {
            Id = entity.Id,
            EmployeeId = entity.EmployeeId,
            WorkflowStep = entity.WorkflowStep,
            ApproverId = entity.ApproverId,
            RequestType = entity.RequestType,
            RequestNumber = entity.RequestNumber,
            RequestStatus = entity.RequestStatus,
            ApprovalProcessTime = entity.ApprovalProcessTime
        };
    }
}
