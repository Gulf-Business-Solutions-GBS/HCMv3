using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Linq.Extensions;
using Abp.UI;
using HCMv3.Authorization;
using HCMv3.TimeManagement.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.TimeManagement;

/// <summary>
/// Application service for the Holiday Period (dated occurrence of a Holiday) — ported from legacy XAF
/// HolidayPeriod business object.
///
/// DELIBERATE SIMPLIFICATION: only the basic shape (HolidayId FK + HolidayDate) and its uniqueness
/// validation are ported. The legacy <c>OnSaving</c>/<c>OnDeleting</c> cascade (walking
/// <c>EmployeeAbsence</c>, adjusting <c>EmployeeAbsenceQuota</c>, triggering <c>EmployeePayrollStatus</c>
/// retro-calculation and <c>Employee.ProbationEndDate</c> recalculation) is entirely out of scope for this
/// wave — those entities are not yet ported. Uniqueness here is scoped to the same Holiday (per the wave
/// brief); note the legacy check was actually scoped to the whole HolidayCalendar (i.e. across all Holidays
/// under the same calendar), which is broader than what's implemented here.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_TimeManagement_HolidayPeriods)]
public class HolidayPeriodsAppService : HCMv3AppServiceBase, IHolidayPeriodsAppService
{
    private readonly IRepository<HolidayPeriod> _holidayPeriodRepository;

    public HolidayPeriodsAppService(IRepository<HolidayPeriod> holidayPeriodRepository)
    {
        _holidayPeriodRepository = holidayPeriodRepository;
    }

    [HttpPost]
    public async Task<PagedResultDto<HolidayPeriodDto>> GetAll(GetAllHolidayPeriodsInput input)
    {
        input.Normalize();

        var query = _holidayPeriodRepository.GetAll()
            .Include(p => p.Holiday)
            .WhereIf(input.HolidayId.HasValue, p => p.HolidayId == input.HolidayId);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<HolidayPeriodDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<GetHolidayPeriodForEditOutput> GetHolidayPeriodForEdit(EntityDto input)
    {
        var entity = await _holidayPeriodRepository.GetAsync(input.Id);

        return new GetHolidayPeriodForEditOutput
        {
            HolidayPeriod = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditHolidayPeriodDto input)
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

    [AbpAuthorize(AppPermissions.Pages_TimeManagement_HolidayPeriods_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _holidayPeriodRepository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_TimeManagement_HolidayPeriods_Create)]
    protected virtual async Task CreateAsync(CreateOrEditHolidayPeriodDto input)
    {
        var entity = new HolidayPeriod
        {
            HolidayId = input.HolidayId,
            HolidayDate = input.HolidayDate,
            TenantId = AbpSession.TenantId ?? 0
        };

        await ValidateUniquenessAsync(entity);

        await _holidayPeriodRepository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_TimeManagement_HolidayPeriods_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditHolidayPeriodDto input)
    {
        var entity = await _holidayPeriodRepository.GetAsync(input.Id.Value);

        entity.HolidayId = input.HolidayId;
        entity.HolidayDate = input.HolidayDate;

        await ValidateUniquenessAsync(entity);

        await _holidayPeriodRepository.UpdateAsync(entity);
    }

    /// <summary>
    /// Legacy <c>PerformValidations</c> parity (simplified scope — see class remarks): no two
    /// HolidayPeriod rows for the same Holiday on the same date.
    /// </summary>
    private async Task ValidateUniquenessAsync(HolidayPeriod entity)
    {
        var duplicate = await _holidayPeriodRepository.GetAll()
            .AnyAsync(p => p.Id != entity.Id &&
                           p.HolidayId == entity.HolidayId &&
                           p.HolidayDate == entity.HolidayDate);

        if (duplicate)
        {
            throw new UserFriendlyException(L("HolidayPeriodAlreadyExists", entity.HolidayDate.ToString("yyyy-MM-dd")));
        }
    }

    private static HolidayPeriodDto ToDto(HolidayPeriod entity)
    {
        return new HolidayPeriodDto
        {
            Id = entity.Id,
            HolidayId = entity.HolidayId,
            HolidayName = entity.Holiday?.NameEn,
            HolidayDate = entity.HolidayDate
        };
    }

    private static CreateOrEditHolidayPeriodDto ToCreateOrEditDto(HolidayPeriod entity)
    {
        return new CreateOrEditHolidayPeriodDto
        {
            Id = entity.Id,
            HolidayId = entity.HolidayId,
            HolidayDate = entity.HolidayDate
        };
    }
}
