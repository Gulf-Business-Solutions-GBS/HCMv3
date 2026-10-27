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
/// Application service for <see cref="JobDescriptionDegree"/>, the aggregated child of
/// <see cref="JobDescription"/> (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.JobProfileManagement.JDDegree</c>). Legacy <c>PerformValidation</c>
/// parity (no duplicate EducationDegree for the same JobDescription) is ported here.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_JobProfileManagement_JobDescriptionDegrees)]
public class JobDescriptionDegreesAppService : HCMv3AppServiceBase, IJobDescriptionDegreesAppService
{
    private readonly IRepository<JobDescriptionDegree> _repository;

    public JobDescriptionDegreesAppService(IRepository<JobDescriptionDegree> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<JobDescriptionDegreeDto>> GetAll(GetAllJobDescriptionDegreesInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .Include(x => x.EducationDegree)
            .WhereIf(input.JobDescriptionId.HasValue, x => x.JobDescriptionId == input.JobDescriptionId);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<JobDescriptionDegreeDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<GetJobDescriptionDegreeForEditOutput> GetJobDescriptionDegreeForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetJobDescriptionDegreeForEditOutput
        {
            JobDescriptionDegree = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditJobDescriptionDegreeDto input)
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

    [AbpAuthorize(AppPermissions.Pages_JobProfileManagement_JobDescriptionDegrees_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_JobProfileManagement_JobDescriptionDegrees_Create)]
    protected virtual async Task CreateAsync(CreateOrEditJobDescriptionDegreeDto input)
    {
        var entity = MapToEntity(input, new JobDescriptionDegree());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await ValidateAsync(entity);

        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_JobProfileManagement_JobDescriptionDegrees_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditJobDescriptionDegreeDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await ValidateAsync(entity);

        await _repository.UpdateAsync(entity);
    }

    /// <summary>
    /// Legacy <c>PerformValidation</c> parity: no duplicate EducationDegree for the same JobDescription.
    /// </summary>
    private async Task ValidateAsync(JobDescriptionDegree entity)
    {
        var duplicate = await _repository.GetAll()
            .AnyAsync(x => x.Id != entity.Id &&
                           x.JobDescriptionId == entity.JobDescriptionId &&
                           x.EducationDegreeId == entity.EducationDegreeId);
        if (duplicate)
        {
            throw new UserFriendlyException(L("DegreeDetailsAlreadyExists"));
        }
    }

    private static JobDescriptionDegree MapToEntity(CreateOrEditJobDescriptionDegreeDto input, JobDescriptionDegree entity)
    {
        entity.JobDescriptionId = input.JobDescriptionId;
        entity.EducationDegreeId = input.EducationDegreeId;

        return entity;
    }

    private static JobDescriptionDegreeDto ToDto(JobDescriptionDegree entity)
    {
        return new JobDescriptionDegreeDto
        {
            Id = entity.Id,
            JobDescriptionId = entity.JobDescriptionId,
            EducationDegreeId = entity.EducationDegreeId,
            EducationDegreeNameEn = entity.EducationDegree?.NameEn
        };
    }

    private static CreateOrEditJobDescriptionDegreeDto ToCreateOrEditDto(JobDescriptionDegree entity)
    {
        return new CreateOrEditJobDescriptionDegreeDto
        {
            Id = entity.Id,
            JobDescriptionId = entity.JobDescriptionId,
            EducationDegreeId = entity.EducationDegreeId
        };
    }
}
