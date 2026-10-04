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
/// Application service for <see cref="WorkflowGroupDetail"/> (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.Workflow.WorkflowSetup.WorkflowGroupDetail</c> -- see that entity's
/// remarks for the DELIBERATE SIMPLIFICATION of the legacy dynamic request-type resolution mechanism).
/// Legacy duplicate RequestType/RequestSubType/CustomSubType-per-RequestMaster check is ported here.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_Workflow_WorkflowGroupDetails)]
public class WorkflowGroupDetailsAppService : HCMv3AppServiceBase, IWorkflowGroupDetailsAppService
{
    private readonly IRepository<WorkflowGroupDetail> _repository;
    private readonly IRepository<RequestMaster> _requestMasterRepository;

    public WorkflowGroupDetailsAppService(
        IRepository<WorkflowGroupDetail> repository,
        IRepository<RequestMaster> requestMasterRepository)
    {
        _repository = repository;
        _requestMasterRepository = requestMasterRepository;
    }

    [HttpPost]
    public async Task<PagedResultDto<WorkflowGroupDetailDto>> GetAll(GetAllWorkflowGroupDetailsInput input)
    {
        var query = _repository.GetAll()
            .Include(d => d.RequestMaster)
            .WhereIf(input.WorkflowGroupId.HasValue, d => d.WorkflowGroupId == input.WorkflowGroupId.Value);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting.IsNullOrWhiteSpace() ? "RequestTypeName" : input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<WorkflowGroupDetailDto>(totalCount, items.Select(ToDto).ToList());
    }

    public async Task<GetWorkflowGroupDetailForEditOutput> GetWorkflowGroupDetailForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);
        return new GetWorkflowGroupDetailForEditOutput { WorkflowGroupDetail = ToCreateOrEditDto(entity) };
    }

    public async Task CreateOrEdit(CreateOrEditWorkflowGroupDetailDto input)
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

    [AbpAuthorize(AppPermissions.Pages_Workflow_WorkflowGroupDetails_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_Workflow_WorkflowGroupDetails_Create)]
    protected virtual async Task CreateAsync(CreateOrEditWorkflowGroupDetailDto input)
    {
        var entity = MapToEntity(input, new WorkflowGroupDetail());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await ValidateAsync(entity);

        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_Workflow_WorkflowGroupDetails_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditWorkflowGroupDetailDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await ValidateAsync(entity);

        await _repository.UpdateAsync(entity);
    }

    private async Task ValidateAsync(WorkflowGroupDetail entity)
    {
        var requestMaster = await _requestMasterRepository.GetAsync(entity.RequestMasterId);

        var duplicate = await _repository.GetAll()
            .AnyAsync(d => d.Id != entity.Id &&
                           d.WorkflowGroupId == entity.WorkflowGroupId &&
                           d.RequestTypeName == entity.RequestTypeName &&
                           d.RequestSubTypeId == entity.RequestSubTypeId &&
                           d.CustomSubTypeId == entity.CustomSubTypeId &&
                           d.RequestAction == entity.RequestAction &&
                           d.RequestMasterId == entity.RequestMasterId);

        if (duplicate)
        {
            throw new UserFriendlyException(L("RequestTypeAlreadyAssignedToRequestMaster"));
        }
    }

    private static WorkflowGroupDetail MapToEntity(CreateOrEditWorkflowGroupDetailDto input, WorkflowGroupDetail entity)
    {
        entity.WorkflowGroupId = input.WorkflowGroupId;
        entity.RequestMasterId = input.RequestMasterId;
        entity.RequestTypeName = input.RequestTypeName;
        entity.RequestSubTypeId = input.RequestSubTypeId;
        entity.CustomSubTypeId = input.CustomSubTypeId;
        entity.RequestAction = input.RequestAction;
        return entity;
    }

    private static WorkflowGroupDetailDto ToDto(WorkflowGroupDetail entity)
    {
        return new WorkflowGroupDetailDto
        {
            Id = entity.Id,
            WorkflowGroupId = entity.WorkflowGroupId,
            RequestMasterId = entity.RequestMasterId,
            RequestMasterNameEn = entity.RequestMaster?.NameEn,
            RequestTypeName = entity.RequestTypeName,
            RequestSubTypeId = entity.RequestSubTypeId,
            CustomSubTypeId = entity.CustomSubTypeId,
            RequestAction = entity.RequestAction
        };
    }

    private static CreateOrEditWorkflowGroupDetailDto ToCreateOrEditDto(WorkflowGroupDetail entity)
    {
        return new CreateOrEditWorkflowGroupDetailDto
        {
            Id = entity.Id,
            WorkflowGroupId = entity.WorkflowGroupId,
            RequestMasterId = entity.RequestMasterId,
            RequestTypeName = entity.RequestTypeName,
            RequestSubTypeId = entity.RequestSubTypeId,
            CustomSubTypeId = entity.CustomSubTypeId,
            RequestAction = entity.RequestAction
        };
    }
}
