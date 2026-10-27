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
using HCMv3.JobProfileManagement.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.JobProfileManagement;

/// <summary>
/// Application service for <see cref="JobDescription"/> (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.JobProfileManagement.JobDescription</c>). Legacy
/// <c>PerformValidation</c> parity (JobTitle required -- enforced via DTO <c>[Required]</c>;
/// ShortDescriptionEn/Ar and LongDescriptionEn/Ar required and each unique across all JobDescriptions)
/// is ported here.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_JobProfileManagement_JobDescriptions)]
public class JobDescriptionsAppService : HCMv3AppServiceBase, IJobDescriptionsAppService
{
    private readonly IRepository<JobDescription> _jobDescriptionRepository;

    public JobDescriptionsAppService(IRepository<JobDescription> jobDescriptionRepository)
    {
        _jobDescriptionRepository = jobDescriptionRepository;
    }

    [HttpPost]
    public async Task<PagedResultDto<JobDescriptionDto>> GetAll(GetAllJobDescriptionsInput input)
    {
        input.Normalize();

        var query = _jobDescriptionRepository.GetAll()
            .Include(d => d.JobTitle)
            .WhereIf(!input.Filter.IsNullOrWhiteSpace(), d =>
                d.ShortDescriptionEn.Contains(input.Filter) ||
                d.ShortDescriptionAr.Contains(input.Filter) ||
                d.JobTitle.NameEn.Contains(input.Filter));

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<JobDescriptionDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<GetJobDescriptionForEditOutput> GetJobDescriptionForEdit(EntityDto input)
    {
        var entity = await _jobDescriptionRepository.GetAsync(input.Id);

        return new GetJobDescriptionForEditOutput
        {
            JobDescription = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditJobDescriptionDto input)
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

    [AbpAuthorize(AppPermissions.Pages_JobProfileManagement_JobDescriptions_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _jobDescriptionRepository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_JobProfileManagement_JobDescriptions_Create)]
    protected virtual async Task CreateAsync(CreateOrEditJobDescriptionDto input)
    {
        var entity = MapToEntity(input, new JobDescription());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await ValidateUniquenessAsync(entity);

        await _jobDescriptionRepository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_JobProfileManagement_JobDescriptions_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditJobDescriptionDto input)
    {
        var entity = await _jobDescriptionRepository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await ValidateUniquenessAsync(entity);

        await _jobDescriptionRepository.UpdateAsync(entity);
    }

    /// <summary>
    /// Legacy <c>PerformValidation</c> parity: ShortDescriptionEn, ShortDescriptionAr, LongDescriptionEn
    /// and LongDescriptionAr must each be unique across all JobDescriptions.
    /// </summary>
    private async Task ValidateUniquenessAsync(JobDescription entity)
    {
        var duplicateShortEn = await _jobDescriptionRepository.GetAll()
            .AnyAsync(d => d.Id != entity.Id && d.ShortDescriptionEn == entity.ShortDescriptionEn);
        if (duplicateShortEn)
        {
            throw new UserFriendlyException(L("ShortDescriptionEnAlreadyInUse"));
        }

        var duplicateShortAr = await _jobDescriptionRepository.GetAll()
            .AnyAsync(d => d.Id != entity.Id && d.ShortDescriptionAr == entity.ShortDescriptionAr);
        if (duplicateShortAr)
        {
            throw new UserFriendlyException(L("ShortDescriptionArAlreadyInUse"));
        }

        var duplicateLongEn = await _jobDescriptionRepository.GetAll()
            .AnyAsync(d => d.Id != entity.Id && d.LongDescriptionEn == entity.LongDescriptionEn);
        if (duplicateLongEn)
        {
            throw new UserFriendlyException(L("LongDescriptionEnAlreadyInUse"));
        }

        var duplicateLongAr = await _jobDescriptionRepository.GetAll()
            .AnyAsync(d => d.Id != entity.Id && d.LongDescriptionAr == entity.LongDescriptionAr);
        if (duplicateLongAr)
        {
            throw new UserFriendlyException(L("LongDescriptionArAlreadyInUse"));
        }
    }

    private static JobDescription MapToEntity(CreateOrEditJobDescriptionDto input, JobDescription entity)
    {
        entity.JobTitleId = input.JobTitleId;
        entity.ShortDescriptionEn = input.ShortDescriptionEn;
        entity.ShortDescriptionAr = input.ShortDescriptionAr;
        entity.LongDescriptionEn = input.LongDescriptionEn;
        entity.LongDescriptionAr = input.LongDescriptionAr;
        entity.Version = input.Version;
        entity.VersionChangedOn = input.VersionChangedOn;

        return entity;
    }

    private static JobDescriptionDto ToDto(JobDescription entity)
    {
        return new JobDescriptionDto
        {
            Id = entity.Id,
            JobTitleId = entity.JobTitleId,
            JobTitleNameEn = entity.JobTitle?.NameEn,
            ShortDescriptionEn = entity.ShortDescriptionEn,
            ShortDescriptionAr = entity.ShortDescriptionAr,
            LongDescriptionEn = entity.LongDescriptionEn,
            LongDescriptionAr = entity.LongDescriptionAr,
            Version = entity.Version,
            VersionChangedOn = entity.VersionChangedOn
        };
    }

    private static CreateOrEditJobDescriptionDto ToCreateOrEditDto(JobDescription entity)
    {
        return new CreateOrEditJobDescriptionDto
        {
            Id = entity.Id,
            JobTitleId = entity.JobTitleId,
            ShortDescriptionEn = entity.ShortDescriptionEn,
            ShortDescriptionAr = entity.ShortDescriptionAr,
            LongDescriptionEn = entity.LongDescriptionEn,
            LongDescriptionAr = entity.LongDescriptionAr,
            Version = entity.Version,
            VersionChangedOn = entity.VersionChangedOn
        };
    }
}
