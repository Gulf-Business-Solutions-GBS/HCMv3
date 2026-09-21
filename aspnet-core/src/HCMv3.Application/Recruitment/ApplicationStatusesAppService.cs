using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Linq.Extensions;
using Abp.UI;
using HCMv3.Authorization;
using HCMv3.PersonnelAdministration;
using HCMv3.PersonnelAdministration.Services;
using HCMv3.Recruitment.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.Recruitment;

/// <summary>
/// Application service for <see cref="ApplicationStatus"/> (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.Recruitment.ApplicationStatus</c>). Legacy <c>SaveToEmployee</c> (when
/// <see cref="Recruitment.AppStatus.Joined"/> is set, auto-create an <see cref="Employee"/> record from the
/// applicant's data if one matching by name+birthdate doesn't already exist) is ported to
/// <see cref="SaveToEmployeeIfJoinedAsync"/>. Legacy <c>IsSystemGenerated</c> flag on the created Employee
/// has no HCMv3 equivalent field and is dropped.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_Recruitment_ApplicationStatuses)]
public class ApplicationStatusesAppService : HCMv3AppServiceBase, IApplicationStatusesAppService
{
    private readonly IRepository<ApplicationStatus> _applicationStatusRepository;
    private readonly IRepository<ApplicantCV> _applicantCVRepository;
    private readonly IRepository<Employee> _employeeRepository;
    private readonly IEmployeeCodeGenerator _employeeCodeGenerator;

    public ApplicationStatusesAppService(
        IRepository<ApplicationStatus> applicationStatusRepository,
        IRepository<ApplicantCV> applicantCVRepository,
        IRepository<Employee> employeeRepository,
        IEmployeeCodeGenerator employeeCodeGenerator)
    {
        _applicationStatusRepository = applicationStatusRepository;
        _applicantCVRepository = applicantCVRepository;
        _employeeRepository = employeeRepository;
        _employeeCodeGenerator = employeeCodeGenerator;
    }

    [HttpPost]
    public async Task<PagedResultDto<ApplicationStatusDto>> GetAll(GetAllApplicationStatusesInput input)
    {
        input.Normalize();

        var query = _applicationStatusRepository.GetAll()
            .Include(a => a.Vacancy)
            .Include(a => a.ApplicantCV).ThenInclude(c => c.Applicant)
            .WhereIf(input.VacancyId.HasValue, a => a.VacancyId == input.VacancyId);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<ApplicationStatusDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<GetApplicationStatusForEditOutput> GetApplicationStatusForEdit(EntityDto input)
    {
        var entity = await _applicationStatusRepository.GetAsync(input.Id);

        return new GetApplicationStatusForEditOutput
        {
            ApplicationStatus = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditApplicationStatusDto input)
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

    [AbpAuthorize(AppPermissions.Pages_Recruitment_ApplicationStatuses_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _applicationStatusRepository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_Recruitment_ApplicationStatuses_Create)]
    protected virtual async Task CreateAsync(CreateOrEditApplicationStatusDto input)
    {
        var entity = MapToEntity(input, new ApplicationStatus());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await _applicationStatusRepository.InsertAsync(entity);

        await SaveToEmployeeIfJoinedAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_Recruitment_ApplicationStatuses_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditApplicationStatusDto input)
    {
        var entity = await _applicationStatusRepository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await _applicationStatusRepository.UpdateAsync(entity);

        await SaveToEmployeeIfJoinedAsync(entity);
    }

    /// <summary>
    /// Legacy <c>SaveToEmployee</c> parity: when the application status becomes
    /// <see cref="Recruitment.AppStatus.Joined"/>, create an <see cref="Employee"/> record from the
    /// applicant's data if one matching by name+birthdate doesn't already exist.
    /// </summary>
    private async Task SaveToEmployeeIfJoinedAsync(ApplicationStatus entity)
    {
        if (entity.AppStatus != AppStatus.Joined)
        {
            return;
        }

        if (entity.ApplicantCVId == null)
        {
            return;
        }

        var applicantCV = await _applicantCVRepository.GetAll()
            .Include(c => c.Applicant)
            .FirstOrDefaultAsync(c => c.Id == entity.ApplicantCVId);
        var applicant = applicantCV?.Applicant;
        if (applicant == null)
        {
            return;
        }

        if (applicant.JoiningDate == default)
        {
            throw new UserFriendlyException(L("Recruitment_JoiningDateCannotBeNull"));
        }

        if (applicant.ProbationEndDate == default)
        {
            throw new UserFriendlyException(L("Recruitment_ProbationEndDateCannotBeNull"));
        }

        var existing = await _employeeRepository.GetAll().FirstOrDefaultAsync(e =>
            e.FirstNameEn == applicant.FirstNameEn &&
            e.SecondNameEn == applicant.SecondNameEn &&
            e.ThirdNameEn == applicant.ThirdNameEn &&
            e.LastNameEn == applicant.LastNameEn &&
            e.BirthDate == applicant.BirthDate);

        if (existing != null)
        {
            return;
        }

        var employee = new Employee
        {
            TenantId = entity.TenantId,
            FirstNameEn = applicant.FirstNameEn,
            SecondNameEn = applicant.SecondNameEn,
            ThirdNameEn = applicant.ThirdNameEn,
            LastNameEn = applicant.LastNameEn,
            FirstNameAr = applicant.FirstNameAr,
            SecondNameAr = applicant.SecondNameAr,
            ThirdNameAr = applicant.ThirdNameAr,
            LastNameAr = applicant.LastNameAr,
            TitleId = applicant.TitleId,
            Status = EmployeeStatus.Active,
            BirthDate = applicant.BirthDate,
            JoiningDate = applicant.JoiningDate,
            ProbationEndDate = applicant.ProbationEndDate,
            NationalityId = applicant.NationalityId ?? 0,
            ReligionId = applicant.ReligionId,
            Gender = applicant.Gender,
            MaritalStatusId = applicant.MaritalStatusId,
            CountryId = applicant.CountryId,
            Telephone = applicant.Telephone,
            Mobile = applicant.Mobile,
            CurrentAddress = applicant.CurrentAddress,
            PermanentAddress = applicant.PermanentAddress,
            QualificationId = applicant.QualificationId,
            BloodGroup = applicant.BloodGroup,
            CivilID = applicant.CivilID,
            Email = applicant.Email
        };

        // Legacy set FullNameEn/Ar to the first name only here, but its emp.Save() then ran Employee.OnSaving,
        // which rebuilt both full names and generated the code. Inserting directly skips EmployeesAppService,
        // so both are applied here -- otherwise the employee has a first-name-only name and a blank code.
        employee.ApplyFullNames();
        _employeeCodeGenerator.AssignCodeIfEmpty(employee);

        await _employeeRepository.InsertAsync(employee);
    }

    private static ApplicationStatus MapToEntity(CreateOrEditApplicationStatusDto input, ApplicationStatus entity)
    {
        entity.VacancyId = input.VacancyId;
        entity.ApplicantCVId = input.ApplicantCVId;
        entity.AppStatus = input.AppStatus;
        entity.CoverLetter = input.CoverLetter;

        return entity;
    }

    private static ApplicationStatusDto ToDto(ApplicationStatus entity)
    {
        return new ApplicationStatusDto
        {
            Id = entity.Id,
            VacancyId = entity.VacancyId,
            VacancyTitle = entity.Vacancy?.VacancyTitle,
            ApplicantCVId = entity.ApplicantCVId,
            ApplicantFullNameEn = entity.ApplicantCV?.Applicant != null
                ? $"{entity.ApplicantCV.Applicant.FirstNameEn} {entity.ApplicantCV.Applicant.LastNameEn}"
                : null,
            AppStatus = entity.AppStatus,
            CoverLetter = entity.CoverLetter
        };
    }

    private static CreateOrEditApplicationStatusDto ToCreateOrEditDto(ApplicationStatus entity)
    {
        return new CreateOrEditApplicationStatusDto
        {
            Id = entity.Id,
            VacancyId = entity.VacancyId,
            ApplicantCVId = entity.ApplicantCVId,
            AppStatus = entity.AppStatus,
            CoverLetter = entity.CoverLetter
        };
    }
}
