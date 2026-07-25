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
/// Application service for <see cref="RequestDetailParallel"/> (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.Workflow.WorkflowSetup.RequestDetailParallel</c>). Legacy
/// <c>PerformValidation</c> parity (WorkflowRole required) is ported here.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_Workflow_RequestDetailParallels)]
public class RequestDetailParallelsAppService : HCMv3AppServiceBase, IRequestDetailParallelsAppService
{
    private readonly IRepository<RequestDetailParallel> _repository;

    public RequestDetailParallelsAppService(IRepository<RequestDetailParallel> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<RequestDetailParallelDto>> GetAll(GetAllRequestDetailParallelsInput input)
    {
        var query = _repository.GetAll()
            .Include(p => p.JobTitle)
            .Include(p => p.WorkflowRole)
            .WhereIf(input.RequestDetailId.HasValue, p => p.RequestDetailId == input.RequestDetailId.Value);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting.IsNullOrWhiteSpace() ? "Id" : input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<RequestDetailParallelDto>(totalCount, items.Select(ToDto).ToList());
    }

    public async Task<GetRequestDetailParallelForEditOutput> GetRequestDetailParallelForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);
        return new GetRequestDetailParallelForEditOutput { RequestDetailParallel = ToCreateOrEditDto(entity) };
    }

    public async Task CreateOrEdit(CreateOrEditRequestDetailParallelDto input)
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

    [AbpAuthorize(AppPermissions.Pages_Workflow_RequestDetailParallels_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_Workflow_RequestDetailParallels_Create)]
    protected virtual async Task CreateAsync(CreateOrEditRequestDetailParallelDto input)
    {
        ValidateWorkflowRoleRequired(input.WorkflowRoleId);

        var entity = MapToEntity(input, new RequestDetailParallel());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_Workflow_RequestDetailParallels_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditRequestDetailParallelDto input)
    {
        ValidateWorkflowRoleRequired(input.WorkflowRoleId);

        var entity = await _repository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await _repository.UpdateAsync(entity);
    }

    private void ValidateWorkflowRoleRequired(int? workflowRoleId)
    {
        if (!workflowRoleId.HasValue)
        {
            throw new UserFriendlyException(L("WorkflowRoleCannotBeBlank"));
        }
    }

    private static RequestDetailParallel MapToEntity(CreateOrEditRequestDetailParallelDto input, RequestDetailParallel entity)
    {
        entity.RequestDetailId = input.RequestDetailId;
        entity.JobTitleId = input.JobTitleId;
        entity.WorkflowRoleId = input.WorkflowRoleId;
        return entity;
    }

    private static RequestDetailParallelDto ToDto(RequestDetailParallel entity)
    {
        return new RequestDetailParallelDto
        {
            Id = entity.Id,
            RequestDetailId = entity.RequestDetailId,
            JobTitleId = entity.JobTitleId,
            JobTitleNameEn = entity.JobTitle?.NameEn,
            WorkflowRoleId = entity.WorkflowRoleId,
            WorkflowRoleNameEn = entity.WorkflowRole?.NameEn
        };
    }

    private static CreateOrEditRequestDetailParallelDto ToCreateOrEditDto(RequestDetailParallel entity)
    {
        return new CreateOrEditRequestDetailParallelDto
        {
            Id = entity.Id,
            RequestDetailId = entity.RequestDetailId,
            JobTitleId = entity.JobTitleId,
            WorkflowRoleId = entity.WorkflowRoleId
        };
    }
}
