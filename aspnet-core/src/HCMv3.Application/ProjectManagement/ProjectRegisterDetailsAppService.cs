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
/// Application service for <see cref="ProjectRegisterDetail"/>, the aggregated child of
/// <see cref="ProjectRegister"/> (ported from legacy XAF ProjectRegisterDetail business object). Legacy
/// <c>PerformValidation</c> was an empty no-op, so this is plain CRUD.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_ProjectManagement_ProjectRegisterDetails)]
public class ProjectRegisterDetailsAppService : HCMv3AppServiceBase, IProjectRegisterDetailsAppService
{
    private readonly IRepository<ProjectRegisterDetail> _projectRegisterDetailRepository;

    public ProjectRegisterDetailsAppService(IRepository<ProjectRegisterDetail> projectRegisterDetailRepository)
    {
        _projectRegisterDetailRepository = projectRegisterDetailRepository;
    }

    [HttpPost]
    public async Task<PagedResultDto<ProjectRegisterDetailDto>> GetAll(GetAllProjectRegisterDetailsInput input)
    {
        input.Normalize();

        var query = _projectRegisterDetailRepository.GetAll()
            .Include(d => d.Project)
            .Include(d => d.WageType)
            .WhereIf(input.ProjectRegisterId.HasValue, d => d.ProjectRegisterId == input.ProjectRegisterId);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<ProjectRegisterDetailDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<GetProjectRegisterDetailForEditOutput> GetProjectRegisterDetailForEdit(EntityDto input)
    {
        var entity = await _projectRegisterDetailRepository.GetAsync(input.Id);

        return new GetProjectRegisterDetailForEditOutput
        {
            ProjectRegisterDetail = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditProjectRegisterDetailDto input)
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

    [AbpAuthorize(AppPermissions.Pages_ProjectManagement_ProjectRegisterDetails_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _projectRegisterDetailRepository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_ProjectManagement_ProjectRegisterDetails_Create)]
    protected virtual async Task CreateAsync(CreateOrEditProjectRegisterDetailDto input)
    {
        var entity = MapToEntity(input, new ProjectRegisterDetail());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await _projectRegisterDetailRepository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_ProjectManagement_ProjectRegisterDetails_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditProjectRegisterDetailDto input)
    {
        var entity = await _projectRegisterDetailRepository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await _projectRegisterDetailRepository.UpdateAsync(entity);
    }

    private static ProjectRegisterDetail MapToEntity(CreateOrEditProjectRegisterDetailDto input, ProjectRegisterDetail entity)
    {
        entity.ProjectRegisterId = input.ProjectRegisterId;
        entity.ProjectId = input.ProjectId;
        entity.WageTypeId = input.WageTypeId;
        entity.Number = input.Number;
        entity.Amount = input.Amount;

        return entity;
    }

    private static ProjectRegisterDetailDto ToDto(ProjectRegisterDetail entity)
    {
        return new ProjectRegisterDetailDto
        {
            Id = entity.Id,
            ProjectRegisterId = entity.ProjectRegisterId,
            ProjectId = entity.ProjectId,
            ProjectNameEn = entity.Project?.NameEn,
            WageTypeId = entity.WageTypeId,
            WageTypeNameEn = entity.WageType?.NameEn,
            Number = entity.Number,
            Amount = entity.Amount
        };
    }

    private static CreateOrEditProjectRegisterDetailDto ToCreateOrEditDto(ProjectRegisterDetail entity)
    {
        return new CreateOrEditProjectRegisterDetailDto
        {
            Id = entity.Id,
            ProjectRegisterId = entity.ProjectRegisterId,
            ProjectId = entity.ProjectId,
            WageTypeId = entity.WageTypeId,
            Number = entity.Number,
            Amount = entity.Amount
        };
    }
}
