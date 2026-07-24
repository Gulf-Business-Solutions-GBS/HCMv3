using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Linq.Extensions;
using Abp.UI;
using HCMv3.Authorization;
using HCMv3.ProjectManagement.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.ProjectManagement;

/// <summary>
/// Application service for <see cref="ProjectWageType"/>, the aggregated child of <see cref="Project"/>
/// (ported from legacy XAF <c>HCM10.Module.BusinessObjects.ProjectManagement.ProjectWageType</c>). Legacy
/// <c>PerformValidation</c> parity: a given <see cref="ProjectWageType.WageTypeId"/> must not already be
/// assigned to the same <see cref="ProjectWageType.ProjectId"/>.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_ProjectManagement_ProjectWageTypes)]
public class ProjectWageTypesAppService : HCMv3AppServiceBase, IProjectWageTypesAppService
{
    private readonly IRepository<ProjectWageType> _projectWageTypeRepository;

    public ProjectWageTypesAppService(IRepository<ProjectWageType> projectWageTypeRepository)
    {
        _projectWageTypeRepository = projectWageTypeRepository;
    }

    [HttpPost]
    public async Task<PagedResultDto<ProjectWageTypeDto>> GetAll(GetAllProjectWageTypesInput input)
    {
        input.Normalize();

        var query = _projectWageTypeRepository.GetAll()
            .Include(pwt => pwt.Project)
            .Include(pwt => pwt.WageType)
            .WhereIf(input.ProjectId.HasValue, pwt => pwt.ProjectId == input.ProjectId);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<ProjectWageTypeDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<GetProjectWageTypeForEditOutput> GetProjectWageTypeForEdit(EntityDto input)
    {
        var entity = await _projectWageTypeRepository.GetAsync(input.Id);

        return new GetProjectWageTypeForEditOutput
        {
            ProjectWageType = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditProjectWageTypeDto input)
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

    [AbpAuthorize(AppPermissions.Pages_ProjectManagement_ProjectWageTypes_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _projectWageTypeRepository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_ProjectManagement_ProjectWageTypes_Create)]
    protected virtual async Task CreateAsync(CreateOrEditProjectWageTypeDto input)
    {
        var entity = MapToEntity(input, new ProjectWageType());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await ValidateUniquenessAsync(entity);

        await _projectWageTypeRepository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_ProjectManagement_ProjectWageTypes_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditProjectWageTypeDto input)
    {
        var entity = await _projectWageTypeRepository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await ValidateUniquenessAsync(entity);

        await _projectWageTypeRepository.UpdateAsync(entity);
    }

    /// <summary>
    /// Legacy <c>PerformValidation</c> parity: the same WageType must not already be assigned to the same
    /// Project.
    /// </summary>
    private async Task ValidateUniquenessAsync(ProjectWageType entity)
    {
        var duplicate = await _projectWageTypeRepository.GetAll()
            .AnyAsync(pwt => pwt.Id != entity.Id &&
                             pwt.ProjectId == entity.ProjectId &&
                             pwt.WageTypeId == entity.WageTypeId);
        if (duplicate)
        {
            throw new UserFriendlyException(L("WageTypeAlreadyExists"));
        }
    }

    private static ProjectWageType MapToEntity(CreateOrEditProjectWageTypeDto input, ProjectWageType entity)
    {
        entity.ProjectId = input.ProjectId;
        entity.WageTypeId = input.WageTypeId;

        return entity;
    }

    private static ProjectWageTypeDto ToDto(ProjectWageType entity)
    {
        return new ProjectWageTypeDto
        {
            Id = entity.Id,
            ProjectId = entity.ProjectId,
            ProjectNameEn = entity.Project?.NameEn,
            WageTypeId = entity.WageTypeId,
            WageTypeNameEn = entity.WageType?.NameEn
        };
    }

    private static CreateOrEditProjectWageTypeDto ToCreateOrEditDto(ProjectWageType entity)
    {
        return new CreateOrEditProjectWageTypeDto
        {
            Id = entity.Id,
            ProjectId = entity.ProjectId,
            WageTypeId = entity.WageTypeId
        };
    }
}
