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
/// Application service for <see cref="Vacancy"/> (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.Recruitment.Vacancy</c>). Legacy <c>PerformValidation</c> parity (auto
/// Code, duplicate Code check, required fields) is ported here.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_Recruitment_Vacancies)]
public class VacanciesAppService : HCMv3AppServiceBase, IVacanciesAppService
{
    private readonly IRepository<Vacancy> _vacancyRepository;

    public VacanciesAppService(IRepository<Vacancy> vacancyRepository)
    {
        _vacancyRepository = vacancyRepository;
    }

    [HttpPost]
    public async Task<PagedResultDto<VacancyDto>> GetAll(GetAllVacanciesInput input)
    {
        input.Normalize();

        var query = _vacancyRepository.GetAll()
            .Include(v => v.JobTitle)
            .Include(v => v.Location)
            .WhereIf(!input.Filter.IsNullOrWhiteSpace(), v =>
                v.Code.Contains(input.Filter) ||
                v.VacancyTitle.Contains(input.Filter));

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<VacancyDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<GetVacancyForEditOutput> GetVacancyForEdit(EntityDto input)
    {
        var entity = await _vacancyRepository.GetAsync(input.Id);

        return new GetVacancyForEditOutput
        {
            Vacancy = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditVacancyDto input)
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

    [AbpAuthorize(AppPermissions.Pages_Recruitment_Vacancies_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _vacancyRepository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_Recruitment_Vacancies_Create)]
    protected virtual async Task CreateAsync(CreateOrEditVacancyDto input)
    {
        var entity = MapToEntity(input, new Vacancy());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await AssignCodeIfEmptyAsync(entity);
        await ValidateUniquenessAsync(entity);

        await _vacancyRepository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_Recruitment_Vacancies_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditVacancyDto input)
    {
        var entity = await _vacancyRepository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await AssignCodeIfEmptyAsync(entity);
        await ValidateUniquenessAsync(entity);

        await _vacancyRepository.UpdateAsync(entity);
    }

    /// <summary>
    /// Legacy <c>GetMaxCode</c> parity: when Code is blank, assign (max existing numeric code) + 1.
    /// </summary>
    private async Task AssignCodeIfEmptyAsync(Vacancy entity)
    {
        if (!entity.Code.IsNullOrWhiteSpace())
        {
            return;
        }

        var numericCodes = await _vacancyRepository.GetAll()
            .Where(v => v.Code != null && v.Code != "")
            .Select(v => v.Code)
            .ToListAsync();

        var maxCode = numericCodes
            .Select(c => int.TryParse(c, out var n) ? n : 0)
            .DefaultIfEmpty(0)
            .Max();

        entity.Code = (maxCode + 1).ToString();
    }

    /// <summary>
    /// Legacy <c>PerformValidation</c> parity: unique Code check.
    /// </summary>
    private async Task ValidateUniquenessAsync(Vacancy entity)
    {
        var duplicateCode = await _vacancyRepository.GetAll()
            .AnyAsync(v => v.Id != entity.Id && v.Code == entity.Code);
        if (duplicateCode)
        {
            throw new UserFriendlyException(L("DuplicateCodeFound"));
        }
    }

    private static Vacancy MapToEntity(CreateOrEditVacancyDto input, Vacancy entity)
    {
        entity.Code = input.Code;
        entity.RequestionDate = input.RequestionDate;
        entity.JobTitleId = input.JobTitleId;
        entity.PublishOn = input.PublishOn;
        entity.ClosingDate = input.ClosingDate;
        entity.RecruiterId = input.RecruiterId;
        entity.TemplateOnbGroupId = input.TemplateOnbGroupId;
        entity.OrganizationUnitId = input.OrganizationUnitId;
        entity.IsBudgeted = input.IsBudgeted;
        entity.VacancyStatus = input.VacancyStatus;
        entity.SLADaysToFill = input.SLADaysToFill;
        entity.JobDescription = input.JobDescription;
        entity.VacancyTitle = input.VacancyTitle;
        entity.WorkLocationId = input.WorkLocationId;
        entity.TotalVacancy = input.TotalVacancy;
        entity.MonthlySalaryRange = input.MonthlySalaryRange;
        entity.EmploymentType = input.EmploymentType;
        entity.YearsOfExperience = input.YearsOfExperience;
        entity.QualificationId = input.QualificationId;
        entity.OnbGroupId = input.OnbGroupId;

        return entity;
    }

    private static VacancyDto ToDto(Vacancy entity)
    {
        return new VacancyDto
        {
            Id = entity.Id,
            Code = entity.Code,
            CodeWithTitle = $"{entity.Code}-{entity.VacancyTitle}",
            RequestionDate = entity.RequestionDate,
            JobTitleId = entity.JobTitleId,
            JobTitleNameEn = entity.JobTitle?.NameEn,
            PublishOn = entity.PublishOn,
            ClosingDate = entity.ClosingDate,
            RecruiterId = entity.RecruiterId,
            TemplateOnbGroupId = entity.TemplateOnbGroupId,
            OrganizationUnitId = entity.OrganizationUnitId,
            IsBudgeted = entity.IsBudgeted,
            VacancyStatus = entity.VacancyStatus,
            SLADaysToFill = entity.SLADaysToFill,
            JobDescription = entity.JobDescription,
            VacancyTitle = entity.VacancyTitle,
            WorkLocationId = entity.WorkLocationId,
            WorkLocationNameEn = entity.Location?.NameEn,
            TotalVacancy = entity.TotalVacancy,
            MonthlySalaryRange = entity.MonthlySalaryRange,
            EmploymentType = entity.EmploymentType,
            YearsOfExperience = entity.YearsOfExperience,
            QualificationId = entity.QualificationId,
            OnbGroupId = entity.OnbGroupId
        };
    }

    private static CreateOrEditVacancyDto ToCreateOrEditDto(Vacancy entity)
    {
        return new CreateOrEditVacancyDto
        {
            Id = entity.Id,
            Code = entity.Code,
            RequestionDate = entity.RequestionDate,
            JobTitleId = entity.JobTitleId ?? 0,
            PublishOn = entity.PublishOn,
            ClosingDate = entity.ClosingDate,
            RecruiterId = entity.RecruiterId,
            TemplateOnbGroupId = entity.TemplateOnbGroupId,
            OrganizationUnitId = entity.OrganizationUnitId ?? 0,
            IsBudgeted = entity.IsBudgeted,
            VacancyStatus = entity.VacancyStatus,
            SLADaysToFill = entity.SLADaysToFill,
            JobDescription = entity.JobDescription,
            VacancyTitle = entity.VacancyTitle,
            WorkLocationId = entity.WorkLocationId,
            TotalVacancy = entity.TotalVacancy,
            MonthlySalaryRange = entity.MonthlySalaryRange,
            EmploymentType = entity.EmploymentType,
            YearsOfExperience = entity.YearsOfExperience,
            QualificationId = entity.QualificationId,
            OnbGroupId = entity.OnbGroupId
        };
    }
}
