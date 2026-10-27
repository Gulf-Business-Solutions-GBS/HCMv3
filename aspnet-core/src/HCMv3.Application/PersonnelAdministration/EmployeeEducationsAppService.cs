using System;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Linq.Extensions;
using HCMv3.Authorization;
using HCMv3.PersonnelAdministration.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.PersonnelAdministration;

/// <summary>
/// Application service for <see cref="EmployeeEducation"/> (ported from legacy XAF
/// EmployeeEducation business object). Per Wave A scope, the legacy EndDate auto-chaining and overlap
/// validation are intentionally NOT ported for this entity (multiple concurrent education records make
/// sense) -- plain CRUD with blank-field validation only. The <c>DocAttachment</c> child is skipped.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeEducations)]
public class EmployeeEducationsAppService : HCMv3AppServiceBase, IEmployeeEducationsAppService
{
    private static readonly DateTime InfiniteEndDate = new DateTime(9999, 12, 31);

    private readonly IRepository<EmployeeEducation> _repository;

    public EmployeeEducationsAppService(IRepository<EmployeeEducation> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<EmployeeEducationDto>> GetAll(GetAllEmployeeEducationsInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .Include(e => e.Employee)
            .Include(e => e.Education)
            .Include(e => e.EducationBranch)
            .Include(e => e.University)
            .Include(e => e.Institution)
            .Include(e => e.Country)
            .WhereIf(input.EmployeeId.HasValue, e => e.EmployeeId == input.EmployeeId.Value);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<EmployeeEducationDto>(totalCount, items.Select(ToDto).ToList());
    }

    public async Task<GetEmployeeEducationForEditOutput> GetEmployeeEducationForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetEmployeeEducationForEditOutput
        {
            EmployeeEducation = new CreateOrEditEmployeeEducationDto
            {
                Id = entity.Id,
                EmployeeId = entity.EmployeeId,
                StartDate = entity.StartDate,
                EndDate = entity.EndDate,
                EducationId = entity.EducationId,
                EducationBranchId = entity.EducationBranchId,
                UniversityId = entity.UniversityId,
                InstitutionId = entity.InstitutionId,
                CountryId = entity.CountryId,
                Location = entity.Location,
                DurationOfCourse = entity.DurationOfCourse,
                Grade = entity.Grade,
                YearOfPassing = entity.YearOfPassing
            }
        };
    }

    public async Task CreateOrEdit(CreateOrEditEmployeeEducationDto input)
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

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeEducations_Create)]
    protected virtual async Task CreateAsync(CreateOrEditEmployeeEducationDto input)
    {
        var entity = MapToEntity(input, new EmployeeEducation());
        entity.TenantId = AbpSession.TenantId ?? 0;
        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeEducations_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditEmployeeEducationDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        MapToEntity(input, entity);
        await _repository.UpdateAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeEducations_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    private static EmployeeEducation MapToEntity(CreateOrEditEmployeeEducationDto input, EmployeeEducation entity)
    {
        entity.EmployeeId = input.EmployeeId;
        entity.StartDate = input.StartDate.Date;
        entity.EndDate = input.EndDate?.Date ?? InfiniteEndDate;
        entity.EducationId = input.EducationId;
        entity.EducationBranchId = input.EducationBranchId;
        entity.UniversityId = input.UniversityId;
        entity.InstitutionId = input.InstitutionId;
        entity.CountryId = input.CountryId;
        entity.Location = input.Location;
        entity.DurationOfCourse = input.DurationOfCourse;
        entity.Grade = input.Grade;
        entity.YearOfPassing = input.YearOfPassing;
        return entity;
    }

    private static EmployeeEducationDto ToDto(EmployeeEducation entity)
    {
        return new EmployeeEducationDto
        {
            Id = entity.Id,
            EmployeeId = entity.EmployeeId,
            EmployeeCode = entity.Employee?.Code,
            EmployeeFullNameEn = entity.Employee?.FullNameEn,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate,
            EducationId = entity.EducationId,
            EducationName = entity.Education?.NameEn,
            EducationBranchId = entity.EducationBranchId,
            EducationBranchName = entity.EducationBranch?.NameEn,
            UniversityId = entity.UniversityId,
            UniversityName = entity.University?.NameEn,
            InstitutionId = entity.InstitutionId,
            InstitutionName = entity.Institution?.NameEn,
            CountryId = entity.CountryId,
            CountryName = entity.Country?.NameEn,
            Location = entity.Location,
            DurationOfCourse = entity.DurationOfCourse,
            Grade = entity.Grade,
            YearOfPassing = entity.YearOfPassing
        };
    }
}
