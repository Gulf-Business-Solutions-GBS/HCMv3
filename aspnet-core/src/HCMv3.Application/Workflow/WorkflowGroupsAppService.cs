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
/// Application service for <see cref="WorkflowGroup"/> (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.Workflow.WorkflowSetup.WorkflowGroup</c>). Legacy
/// <c>PerformValidation</c> parity (auto Code, duplicate Code/Name checks, single-default enforcement)
/// is ported here.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_Workflow_WorkflowGroups)]
public class WorkflowGroupsAppService : HCMv3AppServiceBase, IWorkflowGroupsAppService
{
    private readonly IRepository<WorkflowGroup> _repository;

    public WorkflowGroupsAppService(IRepository<WorkflowGroup> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<WorkflowGroupDto>> GetAll(GetAllWorkflowGroupsInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .WhereIf(!input.Filter.IsNullOrWhiteSpace(), g =>
                g.Code.Contains(input.Filter) ||
                g.NameEn.Contains(input.Filter) ||
                g.NameAr.Contains(input.Filter));

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<WorkflowGroupDto>(totalCount, items.Select(ToDto).ToList());
    }

    public async Task<GetWorkflowGroupForEditOutput> GetWorkflowGroupForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);
        return new GetWorkflowGroupForEditOutput { WorkflowGroup = ToCreateOrEditDto(entity) };
    }

    public async Task CreateOrEdit(CreateOrEditWorkflowGroupDto input)
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

    [AbpAuthorize(AppPermissions.Pages_Workflow_WorkflowGroups_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_Workflow_WorkflowGroups_Create)]
    protected virtual async Task CreateAsync(CreateOrEditWorkflowGroupDto input)
    {
        var entity = MapToEntity(input, new WorkflowGroup());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await AssignCodeIfEmptyAsync(entity);
        await ValidateUniquenessAsync(entity);

        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_Workflow_WorkflowGroups_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditWorkflowGroupDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await AssignCodeIfEmptyAsync(entity);
        await ValidateUniquenessAsync(entity);

        await _repository.UpdateAsync(entity);
    }

    private async Task AssignCodeIfEmptyAsync(WorkflowGroup entity)
    {
        if (!entity.Code.IsNullOrWhiteSpace())
        {
            return;
        }

        var numericCodes = await _repository.GetAll()
            .Where(g => g.Code != null && g.Code != "")
            .Select(g => g.Code)
            .ToListAsync();

        var maxCode = numericCodes
            .Select(c => int.TryParse(c, out var n) ? n : 0)
            .DefaultIfEmpty(0)
            .Max();

        entity.Code = (maxCode + 1).ToString();
    }

    private async Task ValidateUniquenessAsync(WorkflowGroup entity)
    {
        var duplicateCode = await _repository.GetAll()
            .AnyAsync(g => g.Id != entity.Id && g.Code == entity.Code);
        if (duplicateCode)
        {
            throw new UserFriendlyException(L("DuplicateCodeFound"));
        }

        var duplicateName = await _repository.GetAll()
            .AnyAsync(g => g.Id != entity.Id && (g.NameEn == entity.NameEn || g.NameAr == entity.NameAr));
        if (duplicateName)
        {
            throw new UserFriendlyException(L("NameAlreadyInUse"));
        }

        if (entity.IsDefault)
        {
            var anotherDefaultExists = await _repository.GetAll()
                .AnyAsync(g => g.Id != entity.Id && g.IsDefault);
            if (anotherDefaultExists)
            {
                throw new UserFriendlyException(L("RequestMasterAlreadySetAsDefault"));
            }
        }
    }

    private static WorkflowGroup MapToEntity(CreateOrEditWorkflowGroupDto input, WorkflowGroup entity)
    {
        entity.Code = input.Code;
        entity.NameEn = input.NameEn;
        entity.NameAr = input.NameAr;
        entity.IsDefault = input.IsDefault;
        return entity;
    }

    private static WorkflowGroupDto ToDto(WorkflowGroup entity)
    {
        return new WorkflowGroupDto
        {
            Id = entity.Id,
            Code = entity.Code,
            NameEn = entity.NameEn,
            NameAr = entity.NameAr,
            IsDefault = entity.IsDefault
        };
    }

    private static CreateOrEditWorkflowGroupDto ToCreateOrEditDto(WorkflowGroup entity)
    {
        return new CreateOrEditWorkflowGroupDto
        {
            Id = entity.Id,
            Code = entity.Code,
            NameEn = entity.NameEn,
            NameAr = entity.NameAr,
            IsDefault = entity.IsDefault
        };
    }
}
