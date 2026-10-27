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
/// Application service for <see cref="CVQualification"/>, an aggregated child of <see cref="ApplicantCV"/>
/// (ported from legacy XAF <c>HCM10.Module.BusinessObjects.Recruitment.CVQualification</c>). Legacy
/// <c>PerformValidation</c> was an empty no-op, so this is plain CRUD.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_Recruitment_CVQualifications)]
public class CVQualificationsAppService : HCMv3AppServiceBase, ICVQualificationsAppService
{
    private readonly IRepository<CVQualification> _cvQualificationRepository;

    public CVQualificationsAppService(IRepository<CVQualification> cvQualificationRepository)
    {
        _cvQualificationRepository = cvQualificationRepository;
    }

    [HttpPost]
    public async Task<PagedResultDto<CVQualificationDto>> GetAll(GetAllCVQualificationsInput input)
    {
        input.Normalize();

        var query = _cvQualificationRepository.GetAll()
            .Include(q => q.Qualification)
            .WhereIf(input.ApplicantCVId.HasValue, q => q.ApplicantCVId == input.ApplicantCVId);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<CVQualificationDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<GetCVQualificationForEditOutput> GetCVQualificationForEdit(EntityDto input)
    {
        var entity = await _cvQualificationRepository.GetAsync(input.Id);

        return new GetCVQualificationForEditOutput
        {
            CVQualification = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditCVQualificationDto input)
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

    [AbpAuthorize(AppPermissions.Pages_Recruitment_CVQualifications_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _cvQualificationRepository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_Recruitment_CVQualifications_Create)]
    protected virtual async Task CreateAsync(CreateOrEditCVQualificationDto input)
    {
        var entity = MapToEntity(input, new CVQualification());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await _cvQualificationRepository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_Recruitment_CVQualifications_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditCVQualificationDto input)
    {
        var entity = await _cvQualificationRepository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await _cvQualificationRepository.UpdateAsync(entity);
    }

    private static CVQualification MapToEntity(CreateOrEditCVQualificationDto input, CVQualification entity)
    {
        entity.ApplicantCVId = input.ApplicantCVId;
        entity.QualificationId = input.QualificationId;
        entity.University = input.University;
        entity.Percentage = input.Percentage;
        entity.PassedYear = input.PassedYear;

        return entity;
    }

    private static CVQualificationDto ToDto(CVQualification entity)
    {
        return new CVQualificationDto
        {
            Id = entity.Id,
            ApplicantCVId = entity.ApplicantCVId,
            QualificationId = entity.QualificationId,
            QualificationNameEn = entity.Qualification?.NameEn,
            University = entity.University,
            Percentage = entity.Percentage,
            PassedYear = entity.PassedYear
        };
    }

    private static CreateOrEditCVQualificationDto ToCreateOrEditDto(CVQualification entity)
    {
        return new CreateOrEditCVQualificationDto
        {
            Id = entity.Id,
            ApplicantCVId = entity.ApplicantCVId,
            QualificationId = entity.QualificationId,
            University = entity.University,
            Percentage = entity.Percentage,
            PassedYear = entity.PassedYear
        };
    }
}
