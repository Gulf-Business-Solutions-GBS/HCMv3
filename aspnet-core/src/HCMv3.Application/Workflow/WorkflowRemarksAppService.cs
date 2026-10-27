using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Extensions;
using Abp.Linq.Extensions;
using Abp.UI;
using HCMv3.Authorization;
using HCMv3.Workflow.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.Workflow;

/// <summary>
/// Application service for <see cref="WorkflowRemark"/> (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.Workflow.WorkflowSetup.WorkflowRemark</c>). Legacy duplicate
/// CC-receiver-per-request check is ported here; legacy notification-sending on save is dropped
/// (see entity remarks).
/// </summary>
[AbpAuthorize(AppPermissions.Pages_Workflow_WorkflowRemarks)]
public class WorkflowRemarksAppService : HCMv3AppServiceBase, IWorkflowRemarksAppService
{
    private readonly IRepository<WorkflowRemark> _repository;

    public WorkflowRemarksAppService(IRepository<WorkflowRemark> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<WorkflowRemarkDto>> GetAll(GetAllWorkflowRemarksInput input)
    {
        var query = _repository.GetAll()
            .Include(r => r.CCReceiver)
            .WhereIf(input.WorkflowRequestId.HasValue, r => r.WorkflowRequestId == input.WorkflowRequestId.Value);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting.IsNullOrWhiteSpace() ? "Id" : input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<WorkflowRemarkDto>(totalCount, items.Select(ToDto).ToList());
    }

    public async Task<GetWorkflowRemarkForEditOutput> GetWorkflowRemarkForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);
        return new GetWorkflowRemarkForEditOutput { WorkflowRemark = ToCreateOrEditDto(entity) };
    }

    public async Task CreateOrEdit(CreateOrEditWorkflowRemarkDto input)
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

    [AbpAuthorize(AppPermissions.Pages_Workflow_WorkflowRemarks_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_Workflow_WorkflowRemarks_Create)]
    protected virtual async Task CreateAsync(CreateOrEditWorkflowRemarkDto input)
    {
        var entity = MapToEntity(input, new WorkflowRemark());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await ValidateNoDuplicateAsync(entity);

        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_Workflow_WorkflowRemarks_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditWorkflowRemarkDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await ValidateNoDuplicateAsync(entity);

        await _repository.UpdateAsync(entity);
    }

    private async Task ValidateNoDuplicateAsync(WorkflowRemark entity)
    {
        var duplicate = await _repository.GetAll()
            .AnyAsync(r => r.Id != entity.Id &&
                           r.WorkflowRequestId == entity.WorkflowRequestId &&
                           r.CCReceiverId == entity.CCReceiverId);
        if (duplicate)
        {
            throw new UserFriendlyException(L("DuplicateCodeFound"));
        }
    }

    private static WorkflowRemark MapToEntity(CreateOrEditWorkflowRemarkDto input, WorkflowRemark entity)
    {
        entity.WorkflowRequestId = input.WorkflowRequestId;
        entity.CCReceiverId = input.CCReceiverId;
        return entity;
    }

    private static WorkflowRemarkDto ToDto(WorkflowRemark entity)
    {
        return new WorkflowRemarkDto
        {
            Id = entity.Id,
            WorkflowRequestId = entity.WorkflowRequestId,
            CCReceiverId = entity.CCReceiverId,
            CCReceiverNameEn = entity.CCReceiver?.FirstNameEn
        };
    }

    private static CreateOrEditWorkflowRemarkDto ToCreateOrEditDto(WorkflowRemark entity)
    {
        return new CreateOrEditWorkflowRemarkDto
        {
            Id = entity.Id,
            WorkflowRequestId = entity.WorkflowRequestId,
            CCReceiverId = entity.CCReceiverId
        };
    }
}
