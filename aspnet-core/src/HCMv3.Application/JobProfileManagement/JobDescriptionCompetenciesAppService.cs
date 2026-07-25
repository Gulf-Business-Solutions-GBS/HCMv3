using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Linq.Extensions;
using Abp.UI;
using HCMv3.Authorization;
using HCMv3.JobProfileManagement.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.JobProfileManagement;

/// <summary>
/// Application service for <see cref="JobDescriptionCompetency"/>, the aggregated child of
/// <see cref="JobDescription"/> (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.JobProfileManagement.JDCompetency</c>). Legacy
/// <c>PerformValidation</c> parity (no duplicate Competency for the same JobDescription) is ported
/// here.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_JobProfileManagement_JobDescriptionCompetencies)]
public class JobDescriptionCompetenciesAppService : HCMv3AppServiceBase, IJobDescriptionCompetenciesAppService
{
    private readonly IRepository<JobDescriptionCompetency> _repository;

    public JobDescriptionCompetenciesAppService(IRepository<JobDescriptionCompetency> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<JobDescriptionCompetencyDto>> GetAll(GetAllJobDescriptionCompetenciesInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .Include(x => x.Competency)
            .WhereIf(input.JobDescriptionId.HasValue, x => x.JobDescriptionId == input.JobDescriptionId);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<JobDescriptionCompetencyDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<GetJobDescriptionCompetencyForEditOutput> GetJobDescriptionCompetencyForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetJobDescriptionCompetencyForEditOutput
        {
            JobDescriptionCompetency = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditJobDescriptionCompetencyDto input)
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

    [AbpAuthorize(AppPermissions.Pages_JobProfileManagement_JobDescriptionCompetencies_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_JobProfileManagement_JobDescriptionCompetencies_Create)]
    protected virtual async Task CreateAsync(CreateOrEditJobDescriptionCompetencyDto input)
    {
        var entity = MapToEntity(input, new JobDescriptionCompetency());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await ValidateAsync(entity);

        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_JobProfileManagement_JobDescriptionCompetencies_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditJobDescriptionCompetencyDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await ValidateAsync(entity);

        await _repository.UpdateAsync(entity);
    }

    /// <summary>
    /// Legacy <c>PerformValidation</c> parity: no duplicate Competency for the same JobDescription.
    /// </summary>
    private async Task ValidateAsync(JobDescriptionCompetency entity)
    {
        var duplicate = await _repository.GetAll()
            .AnyAsync(x => x.Id != entity.Id &&
                           x.JobDescriptionId == entity.JobDescriptionId &&
                           x.CompetencyId == entity.CompetencyId);
        if (duplicate)
        {
            throw new UserFriendlyException(L("CompetencyDetailsAlreadyExists"));
        }
    }

    private static JobDescriptionCompetency MapToEntity(CreateOrEditJobDescriptionCompetencyDto input, JobDescriptionCompetency entity)
    {
        entity.JobDescriptionId = input.JobDescriptionId;
        entity.CompetencyId = input.CompetencyId;

        return entity;
    }

    private static JobDescriptionCompetencyDto ToDto(JobDescriptionCompetency entity)
    {
        return new JobDescriptionCompetencyDto
        {
            Id = entity.Id,
            JobDescriptionId = entity.JobDescriptionId,
            CompetencyId = entity.CompetencyId,
            CompetencyNameEn = entity.Competency?.NameEn
        };
    }

    private static CreateOrEditJobDescriptionCompetencyDto ToCreateOrEditDto(JobDescriptionCompetency entity)
    {
        return new CreateOrEditJobDescriptionCompetencyDto
        {
            Id = entity.Id,
            JobDescriptionId = entity.JobDescriptionId,
            CompetencyId = entity.CompetencyId
        };
    }
}
