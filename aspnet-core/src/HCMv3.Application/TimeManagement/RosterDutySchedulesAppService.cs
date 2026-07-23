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
using HCMv3.TimeManagement.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.TimeManagement;

/// <summary>
/// Application service for the Roster Duty Schedule (ported from legacy XAF RosterDutySchedule business
/// object). Its <see cref="RosterDutySchedule.DutyPeriodId"/> FK targets the CLASSIC
/// <see cref="TimeManagement.DutyPeriod"/> entity, not <see cref="RosterDutyPeriod"/> — a legacy naming
/// inconsistency, ported faithfully (verified against the actual legacy property type). Per the wave
/// brief, <see cref="TimeManagement.RosterDutySchedule.HolidayCalendarId"/> is ported as optional even
/// though the legacy validation required it.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_TimeManagement_RosterDutySchedules)]
public class RosterDutySchedulesAppService : HCMv3AppServiceBase, IRosterDutySchedulesAppService
{
    private readonly IRepository<RosterDutySchedule> _rosterDutyScheduleRepository;

    public RosterDutySchedulesAppService(IRepository<RosterDutySchedule> rosterDutyScheduleRepository)
    {
        _rosterDutyScheduleRepository = rosterDutyScheduleRepository;
    }

    [HttpPost]
    public async Task<PagedResultDto<RosterDutyScheduleDto>> GetAll(GetAllRosterDutySchedulesInput input)
    {
        input.Normalize();

        var query = _rosterDutyScheduleRepository.GetAll()
            .Include(r => r.DutyPeriod)
            .Include(r => r.HolidayCalendar)
            .WhereIf(!input.Filter.IsNullOrWhiteSpace(), r =>
                r.Code.Contains(input.Filter) ||
                r.NameEn.Contains(input.Filter) ||
                r.NameAr.Contains(input.Filter));

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<RosterDutyScheduleDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<GetRosterDutyScheduleForEditOutput> GetRosterDutyScheduleForEdit(EntityDto input)
    {
        var entity = await _rosterDutyScheduleRepository.GetAsync(input.Id);

        return new GetRosterDutyScheduleForEditOutput
        {
            RosterDutySchedule = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditRosterDutyScheduleDto input)
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

    [AbpAuthorize(AppPermissions.Pages_TimeManagement_RosterDutySchedules_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _rosterDutyScheduleRepository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_TimeManagement_RosterDutySchedules_Create)]
    protected virtual async Task CreateAsync(CreateOrEditRosterDutyScheduleDto input)
    {
        var entity = MapToEntity(input, new RosterDutySchedule());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await AssignCodeIfEmptyAsync(entity);
        await ValidateAsync(entity);

        await _rosterDutyScheduleRepository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_TimeManagement_RosterDutySchedules_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditRosterDutyScheduleDto input)
    {
        var entity = await _rosterDutyScheduleRepository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await AssignCodeIfEmptyAsync(entity);
        await ValidateAsync(entity);

        await _rosterDutyScheduleRepository.UpdateAsync(entity);
    }

    private static RosterDutySchedule MapToEntity(CreateOrEditRosterDutyScheduleDto input, RosterDutySchedule entity)
    {
        entity.Code = input.Code;
        entity.NameEn = input.NameEn;
        entity.NameAr = input.NameAr;
        entity.StartDate = input.StartDate;
        entity.DutyPeriodId = input.DutyPeriodId;
        entity.StartPoint = input.StartPoint;
        entity.HolidayCalendarId = input.HolidayCalendarId;

        return entity;
    }

    /// <summary>
    /// Legacy <c>GetMaxCode</c> parity: when Code is blank, assign (max existing numeric code) + 1.
    /// </summary>
    private async Task AssignCodeIfEmptyAsync(RosterDutySchedule entity)
    {
        if (!entity.Code.IsNullOrWhiteSpace())
        {
            return;
        }

        var numericCodes = await _rosterDutyScheduleRepository.GetAll()
            .Where(r => r.Code != null && r.Code != "")
            .Select(r => r.Code)
            .ToListAsync();

        var maxCode = numericCodes
            .Select(c => int.TryParse(c, out var n) ? n : 0)
            .DefaultIfEmpty(0)
            .Max();

        entity.Code = (maxCode + 1).ToString();
    }

    /// <summary>
    /// Legacy <c>PerformValidation</c> parity (simplified — see class remarks): DutyPeriod required and
    /// StartPoint &gt; 0. HolidayCalendar presence is deliberately NOT enforced here (see class remarks).
    /// </summary>
    private async Task ValidateAsync(RosterDutySchedule entity)
    {
        if (entity.DutyPeriodId <= 0)
        {
            throw new UserFriendlyException(L("DutyPeriodCannotBeBlank"));
        }

        if (entity.StartPoint <= 0)
        {
            throw new UserFriendlyException(L("StartPointMustBeGreaterThanZero"));
        }

        await Task.CompletedTask;
    }

    private static RosterDutyScheduleDto ToDto(RosterDutySchedule entity)
    {
        return new RosterDutyScheduleDto
        {
            Id = entity.Id,
            Code = entity.Code,
            NameEn = entity.NameEn,
            NameAr = entity.NameAr,
            StartDate = entity.StartDate,
            DutyPeriodId = entity.DutyPeriodId,
            DutyPeriodName = entity.DutyPeriod?.NameEn,
            StartPoint = entity.StartPoint,
            HolidayCalendarId = entity.HolidayCalendarId,
            HolidayCalendarName = entity.HolidayCalendar?.NameEn
        };
    }

    private static CreateOrEditRosterDutyScheduleDto ToCreateOrEditDto(RosterDutySchedule entity)
    {
        return new CreateOrEditRosterDutyScheduleDto
        {
            Id = entity.Id,
            Code = entity.Code,
            NameEn = entity.NameEn,
            NameAr = entity.NameAr,
            StartDate = entity.StartDate,
            DutyPeriodId = entity.DutyPeriodId,
            StartPoint = entity.StartPoint,
            HolidayCalendarId = entity.HolidayCalendarId
        };
    }
}
