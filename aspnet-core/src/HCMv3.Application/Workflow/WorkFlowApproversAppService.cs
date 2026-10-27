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
/// Application service for <see cref="WorkFlowApprover"/> (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.Workflow.General.WorkFlowApprover</c>).
/// </summary>
[AbpAuthorize(AppPermissions.Pages_Workflow_WorkFlowApprovers)]
public class WorkFlowApproversAppService : HCMv3AppServiceBase, IWorkFlowApproversAppService
{
    private readonly IRepository<WorkFlowApprover> _repository;

    public WorkFlowApproversAppService(IRepository<WorkFlowApprover> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<WorkFlowApproverDto>> GetAll(GetAllWorkFlowApproversInput input)
    {
        var query = _repository.GetAll()
            .Include(a => a.Approver)
            .WhereIf(input.WorkflowRequestId.HasValue, a => a.WorkflowRequestId == input.WorkflowRequestId.Value);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting.IsNullOrWhiteSpace() ? "Id" : input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<WorkFlowApproverDto>(totalCount, items.Select(ToDto).ToList());
    }

    public async Task<GetWorkFlowApproverForEditOutput> GetWorkFlowApproverForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);
        return new GetWorkFlowApproverForEditOutput { WorkFlowApprover = ToCreateOrEditDto(entity) };
    }

    public async Task CreateOrEdit(CreateOrEditWorkFlowApproverDto input)
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

    [AbpAuthorize(AppPermissions.Pages_Workflow_WorkFlowApprovers_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_Workflow_WorkFlowApprovers_Create)]
    protected virtual async Task CreateAsync(CreateOrEditWorkFlowApproverDto input)
    {
        var entity = MapToEntity(input, new WorkFlowApprover());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_Workflow_WorkFlowApprovers_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditWorkFlowApproverDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await _repository.UpdateAsync(entity);
    }

    private static WorkFlowApprover MapToEntity(CreateOrEditWorkFlowApproverDto input, WorkFlowApprover entity)
    {
        entity.WorkflowRequestId = input.WorkflowRequestId;
        entity.ApproverId = input.ApproverId;
        entity.IsApproved = input.IsApproved;
        return entity;
    }

    private static WorkFlowApproverDto ToDto(WorkFlowApprover entity)
    {
        return new WorkFlowApproverDto
        {
            Id = entity.Id,
            WorkflowRequestId = entity.WorkflowRequestId,
            ApproverId = entity.ApproverId,
            ApproverNameEn = entity.Approver?.FirstNameEn,
            IsApproved = entity.IsApproved
        };
    }

    private static CreateOrEditWorkFlowApproverDto ToCreateOrEditDto(WorkFlowApprover entity)
    {
        return new CreateOrEditWorkFlowApproverDto
        {
            Id = entity.Id,
            WorkflowRequestId = entity.WorkflowRequestId,
            ApproverId = entity.ApproverId,
            IsApproved = entity.IsApproved
        };
    }
}
