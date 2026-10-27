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
using HCMv3.Recruitment.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.Recruitment;

/// <summary>
/// Application service for <see cref="ApplicantCV"/> (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.Recruitment.ApplicantCV</c>). Legacy <c>PerformValidation</c> parity
/// (duplicate non-empty CVName check) is ported here.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_Recruitment_ApplicantCVs)]
public class ApplicantCVsAppService : HCMv3AppServiceBase, IApplicantCVsAppService
{
    private readonly IRepository<ApplicantCV> _applicantCVRepository;

    public ApplicantCVsAppService(IRepository<ApplicantCV> applicantCVRepository)
    {
        _applicantCVRepository = applicantCVRepository;
    }

    [HttpPost]
    public async Task<PagedResultDto<ApplicantCVDto>> GetAll(GetAllApplicantCVsInput input)
    {
        input.Normalize();

        var query = _applicantCVRepository.GetAll()
            .Include(c => c.Applicant)
            .Include(c => c.JobTitle)
            .WhereIf(input.ApplicantId.HasValue, c => c.ApplicantId == input.ApplicantId);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<ApplicantCVDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<GetApplicantCVForEditOutput> GetApplicantCVForEdit(EntityDto input)
    {
        var entity = await _applicantCVRepository.GetAsync(input.Id);

        return new GetApplicantCVForEditOutput
        {
            ApplicantCV = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditApplicantCVDto input)
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

    [AbpAuthorize(AppPermissions.Pages_Recruitment_ApplicantCVs_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _applicantCVRepository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_Recruitment_ApplicantCVs_Create)]
    protected virtual async Task CreateAsync(CreateOrEditApplicantCVDto input)
    {
        var entity = MapToEntity(input, new ApplicantCV());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await ValidateAsync(entity);

        await _applicantCVRepository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_Recruitment_ApplicantCVs_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditApplicantCVDto input)
    {
        var entity = await _applicantCVRepository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await ValidateAsync(entity);

        await _applicantCVRepository.UpdateAsync(entity);
    }

    /// <summary>
    /// Legacy <c>PerformValidation</c> parity: duplicate non-empty CVName check.
    /// </summary>
    private async Task ValidateAsync(ApplicantCV entity)
    {
        if (entity.CVName.IsNullOrWhiteSpace())
        {
            return;
        }

        var duplicate = await _applicantCVRepository.GetAll()
            .AnyAsync(c => c.Id != entity.Id && c.CVName == entity.CVName);
        if (duplicate)
        {
            throw new UserFriendlyException(L("Recruitment_DuplicateCVNameFound"));
        }
    }

    private static ApplicantCV MapToEntity(CreateOrEditApplicantCVDto input, ApplicantCV entity)
    {
        entity.ApplicantId = input.ApplicantId;
        entity.JobTitleId = input.JobTitleId;
        entity.CVName = input.CVName;
        entity.UploadDate = input.UploadDate;

        return entity;
    }

    private static ApplicantCVDto ToDto(ApplicantCV entity)
    {
        var email = entity.Applicant?.Email;
        var cvName = entity.CVName;
        string emailCvName;
        if (email.IsNullOrWhiteSpace())
        {
            emailCvName = cvName ?? "";
        }
        else if (cvName.IsNullOrWhiteSpace())
        {
            emailCvName = email;
        }
        else
        {
            emailCvName = $"{email}-{cvName}";
        }

        return new ApplicantCVDto
        {
            Id = entity.Id,
            ApplicantId = entity.ApplicantId,
            ApplicantFullNameEn = entity.Applicant != null
                ? $"{entity.Applicant.FirstNameEn} {entity.Applicant.LastNameEn}"
                : null,
            JobTitleId = entity.JobTitleId,
            JobTitleNameEn = entity.JobTitle?.NameEn,
            CVName = entity.CVName,
            UploadDate = entity.UploadDate,
            EmailCVName = emailCvName
        };
    }

    private static CreateOrEditApplicantCVDto ToCreateOrEditDto(ApplicantCV entity)
    {
        return new CreateOrEditApplicantCVDto
        {
            Id = entity.Id,
            ApplicantId = entity.ApplicantId,
            JobTitleId = entity.JobTitleId,
            CVName = entity.CVName,
            UploadDate = entity.UploadDate
        };
    }
}
