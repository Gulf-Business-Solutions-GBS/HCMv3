using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Linq.Extensions;
using Abp.UI;
using HCMv3.Authorization;
using HCMv3.PersonnelAdministration.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.PersonnelAdministration;

/// <summary>
/// Application service for <see cref="EmployeeCertification"/> (ported from legacy XAF
/// EmployeeCertification business object). Legacy has an overlap-validation block but it is commented
/// out (dead code) -- only blank-field / EndDate&gt;=StartDate validation is ported.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeCertifications)]
public class EmployeeCertificationsAppService : HCMv3AppServiceBase, IEmployeeCertificationsAppService
{
    private readonly IRepository<EmployeeCertification> _repository;

    public EmployeeCertificationsAppService(IRepository<EmployeeCertification> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<EmployeeCertificationDto>> GetAll(GetAllEmployeeCertificationsInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .Include(c => c.Employee)
            .Include(c => c.Institution)
            .WhereIf(input.EmployeeId.HasValue, c => c.EmployeeId == input.EmployeeId.Value)
            .WhereIf(input.CertificationType.HasValue, c => c.CertificationType == input.CertificationType.Value);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<EmployeeCertificationDto>(totalCount, items.Select(ToDto).ToList());
    }

    public async Task<GetEmployeeCertificationForEditOutput> GetEmployeeCertificationForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetEmployeeCertificationForEditOutput
        {
            EmployeeCertification = new CreateOrEditEmployeeCertificationDto
            {
                Id = entity.Id,
                EmployeeId = entity.EmployeeId,
                StartDate = entity.StartDate,
                EndDate = entity.EndDate,
                Course = entity.Course,
                InstitutionId = entity.InstitutionId,
                Duration = entity.Duration,
                Certificate = entity.Certificate,
                Grade = entity.Grade,
                CertificationType = entity.CertificationType
            }
        };
    }

    public async Task CreateOrEdit(CreateOrEditEmployeeCertificationDto input)
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

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeCertifications_Create)]
    protected virtual async Task CreateAsync(CreateOrEditEmployeeCertificationDto input)
    {
        var entity = MapToEntity(input, new EmployeeCertification());
        entity.TenantId = AbpSession.TenantId ?? 0;

        ValidateDates(entity);
        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeCertifications_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditEmployeeCertificationDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        MapToEntity(input, entity);

        ValidateDates(entity);
        await _repository.UpdateAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeCertifications_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    private void ValidateDates(EmployeeCertification entity)
    {
        if (entity.EndDate < entity.StartDate)
        {
            throw new UserFriendlyException(L("EndDateCannotBeEarlierThanStartDate"));
        }
    }

    private static EmployeeCertification MapToEntity(CreateOrEditEmployeeCertificationDto input, EmployeeCertification entity)
    {
        entity.EmployeeId = input.EmployeeId;
        entity.StartDate = input.StartDate.Date;
        entity.EndDate = input.EndDate.Date;
        entity.Course = input.Course;
        entity.InstitutionId = input.InstitutionId;
        entity.Duration = input.Duration;
        entity.Certificate = input.Certificate;
        entity.Grade = input.Grade;
        entity.CertificationType = input.CertificationType;
        return entity;
    }

    private static EmployeeCertificationDto ToDto(EmployeeCertification entity)
    {
        return new EmployeeCertificationDto
        {
            Id = entity.Id,
            EmployeeId = entity.EmployeeId,
            EmployeeCode = entity.Employee?.Code,
            EmployeeFullNameEn = entity.Employee?.FullNameEn,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate,
            Course = entity.Course,
            InstitutionId = entity.InstitutionId,
            InstitutionName = entity.Institution?.NameEn,
            Duration = entity.Duration,
            Certificate = entity.Certificate,
            Grade = entity.Grade,
            CertificationType = entity.CertificationType
        };
    }
}
