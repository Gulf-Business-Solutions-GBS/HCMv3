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
/// Application service for <see cref="EmployeeSeparation"/> -- DELIBERATELY MINIMAL "record-keeping only"
/// port of the legacy XAF EmployeeSeparation business object (Wave B). See the entity class remarks for
/// the full list of dropped legacy behaviors (OnHold auto-creation, TimeRegister deletion, payroll-retro,
/// advance-EoS indemnity accrual, ValidatePreviousEntry chain-blocking, interdependent date recompute).
/// Only required-field and LastDayInService &gt;= SubmissionDate validation is ported.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeSeparations)]
public class EmployeeSeparationsAppService : HCMv3AppServiceBase, IEmployeeSeparationsAppService
{
    private readonly IRepository<EmployeeSeparation> _repository;

    public EmployeeSeparationsAppService(IRepository<EmployeeSeparation> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<EmployeeSeparationDto>> GetAll(GetAllEmployeeSeparationsInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .Include(s => s.Employee)
            .Include(s => s.SeparationReason)
            .WhereIf(input.EmployeeId.HasValue, s => s.EmployeeId == input.EmployeeId.Value);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<EmployeeSeparationDto>(totalCount, items.Select(ToDto).ToList());
    }

    public async Task<GetEmployeeSeparationForEditOutput> GetEmployeeSeparationForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetEmployeeSeparationForEditOutput
        {
            EmployeeSeparation = new CreateOrEditEmployeeSeparationDto
            {
                Id = entity.Id,
                EmployeeId = entity.EmployeeId,
                SubmissionDate = entity.SubmissionDate,
                LastDayInService = entity.LastDayInService,
                NoticePeriodDays = entity.NoticePeriodDays,
                PlannedLastDayInService = entity.PlannedLastDayInService,
                SeparationType = entity.SeparationType,
                SeparationReasonId = entity.SeparationReasonId,
                RevokeDate = entity.RevokeDate
            }
        };
    }

    public async Task CreateOrEdit(CreateOrEditEmployeeSeparationDto input)
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

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeSeparations_Create)]
    protected virtual async Task CreateAsync(CreateOrEditEmployeeSeparationDto input)
    {
        var entity = MapToEntity(input, new EmployeeSeparation());
        entity.TenantId = AbpSession.TenantId ?? 0;

        Validate(entity);
        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeSeparations_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditEmployeeSeparationDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        MapToEntity(input, entity);

        Validate(entity);
        await _repository.UpdateAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeSeparations_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    /// <summary>
    /// Legacy <c>PerformValidations</c> parity (minimal): required fields already enforced by DTO Data
    /// Annotations; ports the LastDayInService &gt;= SubmissionDate check.
    /// </summary>
    private void Validate(EmployeeSeparation entity)
    {
        if (entity.LastDayInService.Date < entity.SubmissionDate.Date)
        {
            throw new UserFriendlyException(L("LastDayInServiceCannotBeEarlierThanSubmissionDate"));
        }
    }

    private static EmployeeSeparation MapToEntity(CreateOrEditEmployeeSeparationDto input, EmployeeSeparation entity)
    {
        entity.EmployeeId = input.EmployeeId;
        entity.SubmissionDate = input.SubmissionDate.Date;
        entity.LastDayInService = input.LastDayInService.Date;
        entity.NoticePeriodDays = input.NoticePeriodDays;
        entity.PlannedLastDayInService = input.PlannedLastDayInService == default
            ? input.LastDayInService.Date
            : input.PlannedLastDayInService.Date;
        entity.SeparationType = input.SeparationType;
        entity.SeparationReasonId = input.SeparationReasonId;
        entity.RevokeDate = input.RevokeDate?.Date;
        return entity;
    }

    private static EmployeeSeparationDto ToDto(EmployeeSeparation entity)
    {
        return new EmployeeSeparationDto
        {
            Id = entity.Id,
            EmployeeId = entity.EmployeeId,
            EmployeeCode = entity.Employee?.Code,
            EmployeeFullNameEn = entity.Employee?.FullNameEn,
            SubmissionDate = entity.SubmissionDate,
            LastDayInService = entity.LastDayInService,
            NoticePeriodDays = entity.NoticePeriodDays,
            PlannedLastDayInService = entity.PlannedLastDayInService,
            SeparationType = entity.SeparationType,
            SeparationReasonId = entity.SeparationReasonId,
            SeparationReasonName = entity.SeparationReason?.NameEn,
            RevokeDate = entity.RevokeDate
        };
    }
}
