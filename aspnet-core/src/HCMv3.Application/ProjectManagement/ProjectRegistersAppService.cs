using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Linq.Extensions;
using HCMv3.Authorization;
using HCMv3.ProjectManagement.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.ProjectManagement;

/// <summary>
/// Application service for the per-employee-per-period project register header (ported from legacy XAF
/// ProjectRegister business object). Legacy <c>PerformValidation</c> was an empty no-op, so this is plain
/// CRUD.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_ProjectManagement_ProjectRegisters)]
public class ProjectRegistersAppService : HCMv3AppServiceBase, IProjectRegistersAppService
{
    private readonly IRepository<ProjectRegister> _projectRegisterRepository;

    public ProjectRegistersAppService(IRepository<ProjectRegister> projectRegisterRepository)
    {
        _projectRegisterRepository = projectRegisterRepository;
    }

    [HttpPost]
    public async Task<PagedResultDto<ProjectRegisterDto>> GetAll(GetAllProjectRegistersInput input)
    {
        input.Normalize();

        var query = _projectRegisterRepository.GetAll()
            .Include(r => r.Employee)
            .WhereIf(input.EmployeeId.HasValue, r => r.EmployeeId == input.EmployeeId);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<ProjectRegisterDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<GetProjectRegisterForEditOutput> GetProjectRegisterForEdit(EntityDto input)
    {
        var entity = await _projectRegisterRepository.GetAsync(input.Id);

        return new GetProjectRegisterForEditOutput
        {
            ProjectRegister = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditProjectRegisterDto input)
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

    [AbpAuthorize(AppPermissions.Pages_ProjectManagement_ProjectRegisters_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _projectRegisterRepository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_ProjectManagement_ProjectRegisters_Create)]
    protected virtual async Task CreateAsync(CreateOrEditProjectRegisterDto input)
    {
        var entity = MapToEntity(input, new ProjectRegister());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await _projectRegisterRepository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_ProjectManagement_ProjectRegisters_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditProjectRegisterDto input)
    {
        var entity = await _projectRegisterRepository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await _projectRegisterRepository.UpdateAsync(entity);
    }

    private static ProjectRegister MapToEntity(CreateOrEditProjectRegisterDto input, ProjectRegister entity)
    {
        entity.EmployeeId = input.EmployeeId;
        entity.Period = input.Period;

        return entity;
    }

    private static ProjectRegisterDto ToDto(ProjectRegister entity)
    {
        return new ProjectRegisterDto
        {
            Id = entity.Id,
            EmployeeId = entity.EmployeeId,
            EmployeeCode = entity.Employee?.Code,
            Period = entity.Period
        };
    }

    private static CreateOrEditProjectRegisterDto ToCreateOrEditDto(ProjectRegister entity)
    {
        return new CreateOrEditProjectRegisterDto
        {
            Id = entity.Id,
            EmployeeId = entity.EmployeeId,
            Period = entity.Period
        };
    }
}
