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
/// Application service for the materialized, per-calendar-date roster assignment (ported from legacy XAF
/// RosterDailyDuty business object).
///
/// This looks like calculated/generated data, normally populated by an external legacy
/// <c>RosterService</c> calculation engine not present in the legacy source available for this port. The
/// legacy <c>PerformValidation()</c> is an empty no-op stub, so this is ported as a plain CRUD entity with
/// only basic required-field validation — no attempt is made to replicate a calculation engine that isn't
/// in scope/available.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_TimeManagement_RosterDailyDuties)]
public class RosterDailyDutiesAppService : HCMv3AppServiceBase, IRosterDailyDutiesAppService
{
    private readonly IRepository<RosterDailyDuty> _rosterDailyDutyRepository;

    public RosterDailyDutiesAppService(IRepository<RosterDailyDuty> rosterDailyDutyRepository)
    {
        _rosterDailyDutyRepository = rosterDailyDutyRepository;
    }

    [HttpPost]
    public async Task<PagedResultDto<RosterDailyDutyDto>> GetAll(GetAllRosterDailyDutiesInput input)
    {
        input.Normalize();

        var query = _rosterDailyDutyRepository.GetAll()
            .Include(r => r.RosterDutySchedule)
            .Include(r => r.DailyDuty)
            .WhereIf(input.RosterDutyScheduleId.HasValue, r => r.RosterDutyScheduleId == input.RosterDutyScheduleId);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<RosterDailyDutyDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<GetRosterDailyDutyForEditOutput> GetRosterDailyDutyForEdit(EntityDto input)
    {
        var entity = await _rosterDailyDutyRepository.GetAsync(input.Id);

        return new GetRosterDailyDutyForEditOutput
        {
            RosterDailyDuty = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditRosterDailyDutyDto input)
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

    [AbpAuthorize(AppPermissions.Pages_TimeManagement_RosterDailyDuties_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _rosterDailyDutyRepository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_TimeManagement_RosterDailyDuties_Create)]
    protected virtual async Task CreateAsync(CreateOrEditRosterDailyDutyDto input)
    {
        var entity = MapToEntity(input, new RosterDailyDuty());
        entity.TenantId = AbpSession.TenantId ?? 0;

        Validate(entity);

        await _rosterDailyDutyRepository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_TimeManagement_RosterDailyDuties_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditRosterDailyDutyDto input)
    {
        var entity = await _rosterDailyDutyRepository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        Validate(entity);

        await _rosterDailyDutyRepository.UpdateAsync(entity);
    }

    private static RosterDailyDuty MapToEntity(CreateOrEditRosterDailyDutyDto input, RosterDailyDuty entity)
    {
        entity.CalendarDate = input.CalendarDate;
        entity.RosterDutyScheduleId = input.RosterDutyScheduleId;
        entity.DailyDutyId = input.DailyDutyId;

        return entity;
    }

    /// <summary>
    /// Legacy <c>PerformValidation</c> parity: none (empty no-op stub in the legacy source). Only basic
    /// required-field validation is added here.
    /// </summary>
    private void Validate(RosterDailyDuty entity)
    {
        if (entity.CalendarDate == default)
        {
            throw new UserFriendlyException(L("CalendarDateCannotBeBlank"));
        }

        if (entity.RosterDutyScheduleId <= 0)
        {
            throw new UserFriendlyException(L("RosterDutyScheduleCannotBeBlank"));
        }

        if (entity.DailyDutyId <= 0)
        {
            throw new UserFriendlyException(L("DailyDutyCannotBeBlank"));
        }
    }

    private static RosterDailyDutyDto ToDto(RosterDailyDuty entity)
    {
        return new RosterDailyDutyDto
        {
            Id = entity.Id,
            CalendarDate = entity.CalendarDate,
            RosterDutyScheduleId = entity.RosterDutyScheduleId,
            RosterDutyScheduleName = entity.RosterDutySchedule?.NameEn,
            DailyDutyId = entity.DailyDutyId,
            DailyDutyName = entity.DailyDuty?.NameEn
        };
    }

    private static CreateOrEditRosterDailyDutyDto ToCreateOrEditDto(RosterDailyDuty entity)
    {
        return new CreateOrEditRosterDailyDutyDto
        {
            Id = entity.Id,
            CalendarDate = entity.CalendarDate,
            RosterDutyScheduleId = entity.RosterDutyScheduleId,
            DailyDutyId = entity.DailyDutyId
        };
    }
}
