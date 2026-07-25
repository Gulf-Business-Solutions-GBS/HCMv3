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
/// Application service for <see cref="WorkflowRole"/> (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.Workflow.WorkflowSetup.WorkflowRole</c>). Legacy
/// <c>PerformValidation</c> parity (auto Code, duplicate Code/Name checks) is ported here.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_Workflow_WorkflowRoles)]
public class WorkflowRolesAppService : HCMv3AppServiceBase, IWorkflowRolesAppService
{
    private readonly IRepository<WorkflowRole> _repository;

    public WorkflowRolesAppService(IRepository<WorkflowRole> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<WorkflowRoleDto>> GetAll(GetAllWorkflowRolesInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .Include(r => r.WorkflowRoleOrg)
            .WhereIf(!input.Filter.IsNullOrWhiteSpace(), r =>
                r.Code.Contains(input.Filter) ||
                r.NameEn.Contains(input.Filter) ||
                r.NameAr.Contains(input.Filter));

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<WorkflowRoleDto>(totalCount, items.Select(ToDto).ToList());
    }

    public async Task<GetWorkflowRoleForEditOutput> GetWorkflowRoleForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);
        return new GetWorkflowRoleForEditOutput { WorkflowRole = ToCreateOrEditDto(entity) };
    }

    public async Task CreateOrEdit(CreateOrEditWorkflowRoleDto input)
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

    [AbpAuthorize(AppPermissions.Pages_Workflow_WorkflowRoles_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_Workflow_WorkflowRoles_Create)]
    protected virtual async Task CreateAsync(CreateOrEditWorkflowRoleDto input)
    {
        var entity = MapToEntity(input, new WorkflowRole());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await AssignCodeIfEmptyAsync(entity);
        await ValidateUniquenessAsync(entity);

        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_Workflow_WorkflowRoles_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditWorkflowRoleDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await AssignCodeIfEmptyAsync(entity);
        await ValidateUniquenessAsync(entity);

        await _repository.UpdateAsync(entity);
    }

    private async Task AssignCodeIfEmptyAsync(WorkflowRole entity)
    {
        if (!entity.Code.IsNullOrWhiteSpace())
        {
            return;
        }

        var numericCodes = await _repository.GetAll()
            .Where(r => r.Code != null && r.Code != "")
            .Select(r => r.Code)
            .ToListAsync();

        var maxCode = numericCodes
            .Select(c => int.TryParse(c, out var n) ? n : 0)
            .DefaultIfEmpty(0)
            .Max();

        entity.Code = (maxCode + 1).ToString();
    }

    private async Task ValidateUniquenessAsync(WorkflowRole entity)
    {
        var duplicateCode = await _repository.GetAll()
            .AnyAsync(r => r.Id != entity.Id && r.Code == entity.Code);
        if (duplicateCode)
        {
            throw new UserFriendlyException(L("DuplicateCodeFound"));
        }

        var duplicateName = await _repository.GetAll()
            .AnyAsync(r => r.Id != entity.Id && (r.NameEn == entity.NameEn || r.NameAr == entity.NameAr));
        if (duplicateName)
        {
            throw new UserFriendlyException(L("NameAlreadyInUse"));
        }
    }

    private static WorkflowRole MapToEntity(CreateOrEditWorkflowRoleDto input, WorkflowRole entity)
    {
        entity.Code = input.Code;
        entity.NameEn = input.NameEn;
        entity.NameAr = input.NameAr;
        entity.WorkflowRoleGroup = input.WorkflowRoleGroup;
        entity.WorkflowRoleOrgId = input.WorkflowRoleOrgId;
        return entity;
    }

    private static WorkflowRoleDto ToDto(WorkflowRole entity)
    {
        return new WorkflowRoleDto
        {
            Id = entity.Id,
            Code = entity.Code,
            NameEn = entity.NameEn,
            NameAr = entity.NameAr,
            WorkflowRoleGroup = entity.WorkflowRoleGroup,
            WorkflowRoleOrgId = entity.WorkflowRoleOrgId,
            WorkflowRoleOrgNameEn = entity.WorkflowRoleOrg?.NameEn
        };
    }

    private static CreateOrEditWorkflowRoleDto ToCreateOrEditDto(WorkflowRole entity)
    {
        return new CreateOrEditWorkflowRoleDto
        {
            Id = entity.Id,
            Code = entity.Code,
            NameEn = entity.NameEn,
            NameAr = entity.NameAr,
            WorkflowRoleGroup = entity.WorkflowRoleGroup,
            WorkflowRoleOrgId = entity.WorkflowRoleOrgId
        };
    }
}
