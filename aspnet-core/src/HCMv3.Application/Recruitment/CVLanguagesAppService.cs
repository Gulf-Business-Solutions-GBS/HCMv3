using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Linq.Extensions;
using HCMv3.Authorization;
using HCMv3.Recruitment.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.Recruitment;

/// <summary>
/// Application service for <see cref="CVLanguage"/>, an aggregated child of <see cref="ApplicantCV"/>
/// (ported from legacy XAF <c>HCM10.Module.BusinessObjects.Recruitment.CVLanguage</c>). Legacy had no
/// <c>PerformValidation</c> override, so this is plain CRUD.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_Recruitment_CVLanguages)]
public class CVLanguagesAppService : HCMv3AppServiceBase, ICVLanguagesAppService
{
    private readonly IRepository<CVLanguage> _cvLanguageRepository;

    public CVLanguagesAppService(IRepository<CVLanguage> cvLanguageRepository)
    {
        _cvLanguageRepository = cvLanguageRepository;
    }

    [HttpPost]
    public async Task<PagedResultDto<CVLanguageDto>> GetAll(GetAllCVLanguagesInput input)
    {
        input.Normalize();

        var query = _cvLanguageRepository.GetAll()
            .Include(l => l.LanguageInfo)
            .WhereIf(input.ApplicantCVId.HasValue, l => l.ApplicantCVId == input.ApplicantCVId);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<CVLanguageDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<GetCVLanguageForEditOutput> GetCVLanguageForEdit(EntityDto input)
    {
        var entity = await _cvLanguageRepository.GetAsync(input.Id);

        return new GetCVLanguageForEditOutput
        {
            CVLanguage = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditCVLanguageDto input)
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

    [AbpAuthorize(AppPermissions.Pages_Recruitment_CVLanguages_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _cvLanguageRepository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_Recruitment_CVLanguages_Create)]
    protected virtual async Task CreateAsync(CreateOrEditCVLanguageDto input)
    {
        var entity = MapToEntity(input, new CVLanguage());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await _cvLanguageRepository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_Recruitment_CVLanguages_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditCVLanguageDto input)
    {
        var entity = await _cvLanguageRepository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await _cvLanguageRepository.UpdateAsync(entity);
    }

    private static CVLanguage MapToEntity(CreateOrEditCVLanguageDto input, CVLanguage entity)
    {
        entity.ApplicantCVId = input.ApplicantCVId;
        entity.LanguageInfoId = input.LanguageInfoId;
        entity.Proficiency = input.Proficiency;

        return entity;
    }

    private static CVLanguageDto ToDto(CVLanguage entity)
    {
        return new CVLanguageDto
        {
            Id = entity.Id,
            ApplicantCVId = entity.ApplicantCVId,
            LanguageInfoId = entity.LanguageInfoId,
            LanguageInfoNameEn = entity.LanguageInfo?.NameEn,
            Proficiency = entity.Proficiency
        };
    }

    private static CreateOrEditCVLanguageDto ToCreateOrEditDto(CVLanguage entity)
    {
        return new CreateOrEditCVLanguageDto
        {
            Id = entity.Id,
            ApplicantCVId = entity.ApplicantCVId,
            LanguageInfoId = entity.LanguageInfoId,
            Proficiency = entity.Proficiency
        };
    }
}
