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
/// Application service for <see cref="JobDescriptionAcknowledgement"/> (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.JobProfileManagement.JDAcknowledgement</c>). Legacy
/// <c>PerformValidation</c> parity (Employee/JobTitle/JobDescription required -- enforced via DTO
/// <c>[Required]</c>; no duplicate Employee+JobTitle+JobDescription combination) is ported here.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_JobProfileManagement_JobDescriptionAcknowledgements)]
public class JobDescriptionAcknowledgementsAppService : HCMv3AppServiceBase, IJobDescriptionAcknowledgementsAppService
{
    private readonly IRepository<JobDescriptionAcknowledgement> _repository;

    public JobDescriptionAcknowledgementsAppService(IRepository<JobDescriptionAcknowledgement> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<JobDescriptionAcknowledgementDto>> GetAll(GetAllJobDescriptionAcknowledgementsInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .Include(x => x.Employee)
            .Include(x => x.JobTitle)
            .Include(x => x.JobDescription)
            .WhereIf(input.EmployeeId.HasValue, x => x.EmployeeId == input.EmployeeId)
            .WhereIf(input.JobDescriptionId.HasValue, x => x.JobDescriptionId == input.JobDescriptionId);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<JobDescriptionAcknowledgementDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<GetJobDescriptionAcknowledgementForEditOutput> GetJobDescriptionAcknowledgementForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetJobDescriptionAcknowledgementForEditOutput
        {
            JobDescriptionAcknowledgement = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditJobDescriptionAcknowledgementDto input)
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

    [AbpAuthorize(AppPermissions.Pages_JobProfileManagement_JobDescriptionAcknowledgements_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_JobProfileManagement_JobDescriptionAcknowledgements_Create)]
    protected virtual async Task CreateAsync(CreateOrEditJobDescriptionAcknowledgementDto input)
    {
        var entity = MapToEntity(input, new JobDescriptionAcknowledgement());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await ValidateAsync(entity);

        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_JobProfileManagement_JobDescriptionAcknowledgements_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditJobDescriptionAcknowledgementDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await ValidateAsync(entity);

        await _repository.UpdateAsync(entity);
    }

    /// <summary>
    /// Legacy <c>PerformValidation</c> parity: no duplicate Employee+JobTitle+JobDescription
    /// combination.
    /// </summary>
    private async Task ValidateAsync(JobDescriptionAcknowledgement entity)
    {
        var duplicate = await _repository.GetAll()
            .AnyAsync(x => x.Id != entity.Id &&
                           x.EmployeeId == entity.EmployeeId &&
                           x.JobTitleId == entity.JobTitleId &&
                           x.JobDescriptionId == entity.JobDescriptionId);
        if (duplicate)
        {
            throw new UserFriendlyException(L("JDAcknowledgementAlreadyExists"));
        }
    }

    private static JobDescriptionAcknowledgement MapToEntity(CreateOrEditJobDescriptionAcknowledgementDto input, JobDescriptionAcknowledgement entity)
    {
        entity.EmployeeId = input.EmployeeId;
        entity.JobTitleId = input.JobTitleId;
        entity.JobDescriptionId = input.JobDescriptionId;
        entity.Version = input.Version;
        entity.Acknowledged = input.Acknowledged;

        return entity;
    }

    private static JobDescriptionAcknowledgementDto ToDto(JobDescriptionAcknowledgement entity)
    {
        return new JobDescriptionAcknowledgementDto
        {
            Id = entity.Id,
            EmployeeId = entity.EmployeeId,
            EmployeeNameEn = entity.Employee?.FirstNameEn,
            JobTitleId = entity.JobTitleId,
            JobTitleNameEn = entity.JobTitle?.NameEn,
            JobDescriptionId = entity.JobDescriptionId,
            JobDescriptionShortDescriptionEn = entity.JobDescription?.ShortDescriptionEn,
            Version = entity.Version,
            Acknowledged = entity.Acknowledged
        };
    }

    private static CreateOrEditJobDescriptionAcknowledgementDto ToCreateOrEditDto(JobDescriptionAcknowledgement entity)
    {
        return new CreateOrEditJobDescriptionAcknowledgementDto
        {
            Id = entity.Id,
            EmployeeId = entity.EmployeeId,
            JobTitleId = entity.JobTitleId,
            JobDescriptionId = entity.JobDescriptionId,
            Version = entity.Version,
            Acknowledged = entity.Acknowledged
        };
    }
}
