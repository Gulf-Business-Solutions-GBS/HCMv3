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
/// Application service for <see cref="JobDescriptionResponsibility"/>, the aggregated child of
/// <see cref="JobDescription"/> (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.JobProfileManagement.JDResponsibility</c>). Legacy
/// <c>PerformValidation</c> parity (no duplicate JobResponsibility for the same JobDescription) is
/// ported here.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_JobProfileManagement_JobDescriptionResponsibilities)]
public class JobDescriptionResponsibilitiesAppService : HCMv3AppServiceBase, IJobDescriptionResponsibilitiesAppService
{
    private readonly IRepository<JobDescriptionResponsibility> _repository;

    public JobDescriptionResponsibilitiesAppService(IRepository<JobDescriptionResponsibility> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<JobDescriptionResponsibilityDto>> GetAll(GetAllJobDescriptionResponsibilitiesInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .Include(x => x.JobResponsibility)
            .WhereIf(input.JobDescriptionId.HasValue, x => x.JobDescriptionId == input.JobDescriptionId);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<JobDescriptionResponsibilityDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<GetJobDescriptionResponsibilityForEditOutput> GetJobDescriptionResponsibilityForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetJobDescriptionResponsibilityForEditOutput
        {
            JobDescriptionResponsibility = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditJobDescriptionResponsibilityDto input)
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

    [AbpAuthorize(AppPermissions.Pages_JobProfileManagement_JobDescriptionResponsibilities_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_JobProfileManagement_JobDescriptionResponsibilities_Create)]
    protected virtual async Task CreateAsync(CreateOrEditJobDescriptionResponsibilityDto input)
    {
        var entity = MapToEntity(input, new JobDescriptionResponsibility());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await ValidateAsync(entity);

        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_JobProfileManagement_JobDescriptionResponsibilities_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditJobDescriptionResponsibilityDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await ValidateAsync(entity);

        await _repository.UpdateAsync(entity);
    }

    /// <summary>
    /// Legacy <c>PerformValidation</c> parity: no duplicate JobResponsibility for the same
    /// JobDescription.
    /// </summary>
    private async Task ValidateAsync(JobDescriptionResponsibility entity)
    {
        var duplicate = await _repository.GetAll()
            .AnyAsync(x => x.Id != entity.Id &&
                           x.JobDescriptionId == entity.JobDescriptionId &&
                           x.JobResponsibilityId == entity.JobResponsibilityId);
        if (duplicate)
        {
            throw new UserFriendlyException(L("ResponsibilityDetailsAlreadyExists"));
        }
    }

    private static JobDescriptionResponsibility MapToEntity(CreateOrEditJobDescriptionResponsibilityDto input, JobDescriptionResponsibility entity)
    {
        entity.JobDescriptionId = input.JobDescriptionId;
        entity.JobResponsibilityId = input.JobResponsibilityId;

        return entity;
    }

    private static JobDescriptionResponsibilityDto ToDto(JobDescriptionResponsibility entity)
    {
        return new JobDescriptionResponsibilityDto
        {
            Id = entity.Id,
            JobDescriptionId = entity.JobDescriptionId,
            JobResponsibilityId = entity.JobResponsibilityId,
            JobResponsibilityNameEn = entity.JobResponsibility?.NameEn
        };
    }

    private static CreateOrEditJobDescriptionResponsibilityDto ToCreateOrEditDto(JobDescriptionResponsibility entity)
    {
        return new CreateOrEditJobDescriptionResponsibilityDto
        {
            Id = entity.Id,
            JobDescriptionId = entity.JobDescriptionId,
            JobResponsibilityId = entity.JobResponsibilityId
        };
    }
}
