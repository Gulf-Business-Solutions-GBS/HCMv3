using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Linq.Extensions;
using Abp.UI;
using HCMv3.Authorization;
using HCMv3.Workflow.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.Workflow;

/// <summary>
/// Application service for <see cref="EmployeeWorkflowRole"/> (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.PersonnelAdministration.EmployeeWorkflowRole</c>). Legacy
/// <c>PerformValidation</c> parity: Employee/WorkflowRole/StartDate/EndDate required, EndDate &gt;= StartDate.
/// (The legacy overlap-conflict check was already dead/commented-out code and is not ported.)
/// </summary>
[AbpAuthorize(AppPermissions.Pages_Workflow_EmployeeWorkflowRoles)]
public class EmployeeWorkflowRolesAppService : HCMv3AppServiceBase, IEmployeeWorkflowRolesAppService
{
    private readonly IRepository<EmployeeWorkflowRole> _repository;

    public EmployeeWorkflowRolesAppService(IRepository<EmployeeWorkflowRole> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<EmployeeWorkflowRoleDto>> GetAll(GetAllEmployeeWorkflowRolesInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .Include(r => r.Employee)
            .Include(r => r.WorkflowRole)
            .WhereIf(input.EmployeeId.HasValue, r => r.EmployeeId == input.EmployeeId);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<EmployeeWorkflowRoleDto>(totalCount, items.Select(ToDto).ToList());
    }

    public async Task<GetEmployeeWorkflowRoleForEditOutput> GetEmployeeWorkflowRoleForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetEmployeeWorkflowRoleForEditOutput
        {
            EmployeeWorkflowRole = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditEmployeeWorkflowRoleDto input)
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

    [AbpAuthorize(AppPermissions.Pages_Workflow_EmployeeWorkflowRoles_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_Workflow_EmployeeWorkflowRoles_Create)]
    protected virtual async Task CreateAsync(CreateOrEditEmployeeWorkflowRoleDto input)
    {
        var entity = MapToEntity(input, new EmployeeWorkflowRole());
        entity.TenantId = AbpSession.TenantId ?? 0;

        ValidateDates(entity);

        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_Workflow_EmployeeWorkflowRoles_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditEmployeeWorkflowRoleDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        ValidateDates(entity);

        await _repository.UpdateAsync(entity);
    }

    private void ValidateDates(EmployeeWorkflowRole entity)
    {
        if (entity.EndDate < entity.StartDate)
        {
            throw new UserFriendlyException(L("EndDateCannotBeEarlierThanStartDate"));
        }
    }

    private static EmployeeWorkflowRole MapToEntity(CreateOrEditEmployeeWorkflowRoleDto input, EmployeeWorkflowRole entity)
    {
        entity.EmployeeId = input.EmployeeId;
        entity.WorkflowRoleId = input.WorkflowRoleId;
        entity.StartDate = input.StartDate;
        entity.EndDate = input.EndDate;
        return entity;
    }

    private static EmployeeWorkflowRoleDto ToDto(EmployeeWorkflowRole entity)
    {
        return new EmployeeWorkflowRoleDto
        {
            Id = entity.Id,
            EmployeeId = entity.EmployeeId,
            EmployeeFullNameEn = entity.Employee?.FullNameEn,
            WorkflowRoleId = entity.WorkflowRoleId,
            WorkflowRoleNameEn = entity.WorkflowRole?.NameEn,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate
        };
    }

    private static CreateOrEditEmployeeWorkflowRoleDto ToCreateOrEditDto(EmployeeWorkflowRole entity)
    {
        return new CreateOrEditEmployeeWorkflowRoleDto
        {
            Id = entity.Id,
            EmployeeId = entity.EmployeeId,
            WorkflowRoleId = entity.WorkflowRoleId,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate
        };
    }
}
