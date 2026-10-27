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
/// Application service for <see cref="WorkflowGroupFieldAccess"/> (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.Workflow.WorkflowSetup.WorkflowGroupFieldAccess</c>). Legacy
/// <c>PerformValidation</c> parity (step must exist on the assigned request master, duplicate
/// step+field check) is ported here.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_Workflow_WorkflowGroupFieldAccesses)]
public class WorkflowGroupFieldAccessesAppService : HCMv3AppServiceBase, IWorkflowGroupFieldAccessesAppService
{
    private readonly IRepository<WorkflowGroupFieldAccess> _repository;
    private readonly IRepository<WorkflowGroupDetail> _workflowGroupDetailRepository;
    private readonly IRepository<RequestDetail> _requestDetailRepository;

    public WorkflowGroupFieldAccessesAppService(
        IRepository<WorkflowGroupFieldAccess> repository,
        IRepository<WorkflowGroupDetail> workflowGroupDetailRepository,
        IRepository<RequestDetail> requestDetailRepository)
    {
        _repository = repository;
        _workflowGroupDetailRepository = workflowGroupDetailRepository;
        _requestDetailRepository = requestDetailRepository;
    }

    [HttpPost]
    public async Task<PagedResultDto<WorkflowGroupFieldAccessDto>> GetAll(GetAllWorkflowGroupFieldAccessesInput input)
    {
        var query = _repository.GetAll()
            .WhereIf(input.WorkflowGroupDetailId.HasValue, f => f.WorkflowGroupDetailId == input.WorkflowGroupDetailId.Value);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting.IsNullOrWhiteSpace() ? "Step" : input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<WorkflowGroupFieldAccessDto>(totalCount, items.Select(ToDto).ToList());
    }

    public async Task<GetWorkflowGroupFieldAccessForEditOutput> GetWorkflowGroupFieldAccessForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);
        return new GetWorkflowGroupFieldAccessForEditOutput { WorkflowGroupFieldAccess = ToCreateOrEditDto(entity) };
    }

    public async Task CreateOrEdit(CreateOrEditWorkflowGroupFieldAccessDto input)
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

    [AbpAuthorize(AppPermissions.Pages_Workflow_WorkflowGroupFieldAccesses_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_Workflow_WorkflowGroupFieldAccesses_Create)]
    protected virtual async Task CreateAsync(CreateOrEditWorkflowGroupFieldAccessDto input)
    {
        var entity = MapToEntity(input, new WorkflowGroupFieldAccess());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await ValidateAsync(entity);

        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_Workflow_WorkflowGroupFieldAccesses_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditWorkflowGroupFieldAccessDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await ValidateAsync(entity);

        await _repository.UpdateAsync(entity);
    }

    private async Task ValidateAsync(WorkflowGroupFieldAccess entity)
    {
        var groupDetail = await _workflowGroupDetailRepository.GetAsync(entity.WorkflowGroupDetailId);

        var stepExists = await _requestDetailRepository.GetAll()
            .AnyAsync(d => d.RequestMasterId == groupDetail.RequestMasterId && d.Step == entity.Step);
        if (!stepExists)
        {
            throw new UserFriendlyException(L("RequestMasterHasNoSuchStep"));
        }

        var duplicate = await _repository.GetAll()
            .AnyAsync(f => f.Id != entity.Id &&
                           f.WorkflowGroupDetailId == entity.WorkflowGroupDetailId &&
                           f.Step == entity.Step &&
                           f.Field == entity.Field);
        if (duplicate)
        {
            throw new UserFriendlyException(L("FieldAlreadyAssignedOnStep"));
        }
    }

    private static WorkflowGroupFieldAccess MapToEntity(CreateOrEditWorkflowGroupFieldAccessDto input, WorkflowGroupFieldAccess entity)
    {
        entity.WorkflowGroupDetailId = input.WorkflowGroupDetailId;
        entity.Step = input.Step;
        entity.Field = input.Field;
        entity.IsVisible = input.IsVisible;
        entity.IsEditable = input.IsEditable;
        entity.IsRequired = input.IsRequired;
        return entity;
    }

    private static WorkflowGroupFieldAccessDto ToDto(WorkflowGroupFieldAccess entity)
    {
        return new WorkflowGroupFieldAccessDto
        {
            Id = entity.Id,
            WorkflowGroupDetailId = entity.WorkflowGroupDetailId,
            Step = entity.Step,
            Field = entity.Field,
            IsVisible = entity.IsVisible,
            IsEditable = entity.IsEditable,
            IsRequired = entity.IsRequired
        };
    }

    private static CreateOrEditWorkflowGroupFieldAccessDto ToCreateOrEditDto(WorkflowGroupFieldAccess entity)
    {
        return new CreateOrEditWorkflowGroupFieldAccessDto
        {
            Id = entity.Id,
            WorkflowGroupDetailId = entity.WorkflowGroupDetailId,
            Step = entity.Step,
            Field = entity.Field,
            IsVisible = entity.IsVisible,
            IsEditable = entity.IsEditable,
            IsRequired = entity.IsRequired
        };
    }
}
