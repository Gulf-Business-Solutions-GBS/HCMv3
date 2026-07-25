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
/// Application service for <see cref="JobDescriptionCertificate"/>, the aggregated child of
/// <see cref="JobDescription"/> (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.JobProfileManagement.JDEducationCertificate</c>). Legacy
/// <c>PerformValidation</c> parity (no duplicate EducationCertificate for the same JobDescription) is
/// ported here.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_JobProfileManagement_JobDescriptionCertificates)]
public class JobDescriptionCertificatesAppService : HCMv3AppServiceBase, IJobDescriptionCertificatesAppService
{
    private readonly IRepository<JobDescriptionCertificate> _repository;

    public JobDescriptionCertificatesAppService(IRepository<JobDescriptionCertificate> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<JobDescriptionCertificateDto>> GetAll(GetAllJobDescriptionCertificatesInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .Include(x => x.EducationCertificate)
            .WhereIf(input.JobDescriptionId.HasValue, x => x.JobDescriptionId == input.JobDescriptionId);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<JobDescriptionCertificateDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<GetJobDescriptionCertificateForEditOutput> GetJobDescriptionCertificateForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetJobDescriptionCertificateForEditOutput
        {
            JobDescriptionCertificate = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditJobDescriptionCertificateDto input)
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

    [AbpAuthorize(AppPermissions.Pages_JobProfileManagement_JobDescriptionCertificates_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_JobProfileManagement_JobDescriptionCertificates_Create)]
    protected virtual async Task CreateAsync(CreateOrEditJobDescriptionCertificateDto input)
    {
        var entity = MapToEntity(input, new JobDescriptionCertificate());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await ValidateAsync(entity);

        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_JobProfileManagement_JobDescriptionCertificates_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditJobDescriptionCertificateDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await ValidateAsync(entity);

        await _repository.UpdateAsync(entity);
    }

    /// <summary>
    /// Legacy <c>PerformValidation</c> parity: no duplicate EducationCertificate for the same
    /// JobDescription.
    /// </summary>
    private async Task ValidateAsync(JobDescriptionCertificate entity)
    {
        var duplicate = await _repository.GetAll()
            .AnyAsync(x => x.Id != entity.Id &&
                           x.JobDescriptionId == entity.JobDescriptionId &&
                           x.EducationCertificateId == entity.EducationCertificateId);
        if (duplicate)
        {
            throw new UserFriendlyException(L("CertificateDetailsAlreadyExists"));
        }
    }

    private static JobDescriptionCertificate MapToEntity(CreateOrEditJobDescriptionCertificateDto input, JobDescriptionCertificate entity)
    {
        entity.JobDescriptionId = input.JobDescriptionId;
        entity.EducationCertificateId = input.EducationCertificateId;

        return entity;
    }

    private static JobDescriptionCertificateDto ToDto(JobDescriptionCertificate entity)
    {
        return new JobDescriptionCertificateDto
        {
            Id = entity.Id,
            JobDescriptionId = entity.JobDescriptionId,
            EducationCertificateId = entity.EducationCertificateId,
            EducationCertificateNameEn = entity.EducationCertificate?.NameEn
        };
    }

    private static CreateOrEditJobDescriptionCertificateDto ToCreateOrEditDto(JobDescriptionCertificate entity)
    {
        return new CreateOrEditJobDescriptionCertificateDto
        {
            Id = entity.Id,
            JobDescriptionId = entity.JobDescriptionId,
            EducationCertificateId = entity.EducationCertificateId
        };
    }
}
