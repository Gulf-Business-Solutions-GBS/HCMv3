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
using HCMv3.ProjectManagement.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.ProjectManagement;

/// <summary>
/// Application service for <see cref="Project"/> (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.ProjectManagement.Project</c>). Legacy <c>PerformValidation</c> parity
/// (auto Code, duplicate Code/Name checks) is ported here, same pattern as
/// <c>GeneralMasterInfo.SimpleLookupAppServiceBase</c> -- not reused directly because <see cref="Project"/>
/// has extra fields (<see cref="Project.ProjectTypeId"/>, dates, comments) beyond the pure Code/NameEn/NameAr
/// shape.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_ProjectManagement_Projects)]
public class ProjectsAppService : HCMv3AppServiceBase, IProjectsAppService
{
    private readonly IRepository<Project> _projectRepository;

    public ProjectsAppService(IRepository<Project> projectRepository)
    {
        _projectRepository = projectRepository;
    }

    [HttpPost]
    public async Task<PagedResultDto<ProjectDto>> GetAll(GetAllProjectsInput input)
    {
        input.Normalize();

        var query = _projectRepository.GetAll()
            .Include(p => p.ProjectType)
            .WhereIf(!input.Filter.IsNullOrWhiteSpace(), p =>
                p.Code.Contains(input.Filter) ||
                p.NameEn.Contains(input.Filter) ||
                p.NameAr.Contains(input.Filter));

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<ProjectDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<GetProjectForEditOutput> GetProjectForEdit(EntityDto input)
    {
        var entity = await _projectRepository.GetAsync(input.Id);

        return new GetProjectForEditOutput
        {
            Project = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditProjectDto input)
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

    [AbpAuthorize(AppPermissions.Pages_ProjectManagement_Projects_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _projectRepository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_ProjectManagement_Projects_Create)]
    protected virtual async Task CreateAsync(CreateOrEditProjectDto input)
    {
        var entity = MapToEntity(input, new Project());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await AssignCodeIfEmptyAsync(entity);
        await ValidateUniquenessAsync(entity);

        await _projectRepository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_ProjectManagement_Projects_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditProjectDto input)
    {
        var entity = await _projectRepository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await AssignCodeIfEmptyAsync(entity);
        await ValidateUniquenessAsync(entity);

        await _projectRepository.UpdateAsync(entity);
    }

    /// <summary>
    /// Legacy <c>GetMaxCode</c> parity: when Code is blank, assign (max existing numeric code) + 1.
    /// </summary>
    private async Task AssignCodeIfEmptyAsync(Project entity)
    {
        if (!entity.Code.IsNullOrWhiteSpace())
        {
            return;
        }

        var numericCodes = await _projectRepository.GetAll()
            .Where(p => p.Code != null && p.Code != "")
            .Select(p => p.Code)
            .ToListAsync();

        var maxCode = numericCodes
            .Select(c => int.TryParse(c, out var n) ? n : 0)
            .DefaultIfEmpty(0)
            .Max();

        entity.Code = (maxCode + 1).ToString();
    }

    /// <summary>
    /// Legacy <c>PerformValidation</c> parity: unique Code and unique NameEn/NameAr.
    /// </summary>
    private async Task ValidateUniquenessAsync(Project entity)
    {
        var duplicateCode = await _projectRepository.GetAll()
            .AnyAsync(p => p.Id != entity.Id && p.Code == entity.Code);
        if (duplicateCode)
        {
            throw new UserFriendlyException(L("DuplicateCodeFound"));
        }

        var duplicateName = await _projectRepository.GetAll()
            .AnyAsync(p => p.Id != entity.Id &&
                           (p.NameEn == entity.NameEn || p.NameAr == entity.NameAr));
        if (duplicateName)
        {
            throw new UserFriendlyException(L("NameAlreadyInUse"));
        }
    }

    private static Project MapToEntity(CreateOrEditProjectDto input, Project entity)
    {
        entity.Code = input.Code;
        entity.NameEn = input.NameEn;
        entity.NameAr = input.NameAr;
        entity.ProjectTypeId = input.ProjectTypeId;
        entity.StartDate = input.StartDate;
        entity.EndDate = input.EndDate;
        entity.Comments = input.Comments;

        return entity;
    }

    private static ProjectDto ToDto(Project entity)
    {
        return new ProjectDto
        {
            Id = entity.Id,
            Code = entity.Code,
            NameEn = entity.NameEn,
            NameAr = entity.NameAr,
            ProjectTypeId = entity.ProjectTypeId,
            ProjectTypeNameEn = entity.ProjectType?.NameEn,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate,
            Comments = entity.Comments
        };
    }

    private static CreateOrEditProjectDto ToCreateOrEditDto(Project entity)
    {
        return new CreateOrEditProjectDto
        {
            Id = entity.Id,
            Code = entity.Code,
            NameEn = entity.NameEn,
            NameAr = entity.NameAr,
            ProjectTypeId = entity.ProjectTypeId,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate,
            Comments = entity.Comments
        };
    }
}
