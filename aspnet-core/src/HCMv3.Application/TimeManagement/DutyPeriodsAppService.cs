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
/// Application service for the weekly Duty Period template (ported from legacy XAF DutyPeriod business
/// object). Only Code/Name uniqueness and StartDate &lt; EndDate are validated — the legacy
/// EnableRosterDuty-driven conditional requirements ("all 7 days required" / "weeks must be contiguous")
/// are NOT ported. See <see cref="TimeManagement.DutyPeriod"/> remarks for details.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_TimeManagement_DutyPeriods)]
public class DutyPeriodsAppService : HCMv3AppServiceBase, IDutyPeriodsAppService
{
    private readonly IRepository<DutyPeriod> _dutyPeriodRepository;

    public DutyPeriodsAppService(IRepository<DutyPeriod> dutyPeriodRepository)
    {
        _dutyPeriodRepository = dutyPeriodRepository;
    }

    [HttpPost]
    public async Task<PagedResultDto<DutyPeriodDto>> GetAll(GetAllDutyPeriodsInput input)
    {
        input.Normalize();

        var query = _dutyPeriodRepository.GetAll()
            .Include(d => d.HolidayCalendar)
            .WhereIf(!input.Filter.IsNullOrWhiteSpace(), d =>
                d.Code.Contains(input.Filter) ||
                d.NameEn.Contains(input.Filter) ||
                d.NameAr.Contains(input.Filter));

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<DutyPeriodDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<GetDutyPeriodForEditOutput> GetDutyPeriodForEdit(EntityDto input)
    {
        var entity = await _dutyPeriodRepository.GetAsync(input.Id);

        return new GetDutyPeriodForEditOutput
        {
            DutyPeriod = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditDutyPeriodDto input)
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

    [AbpAuthorize(AppPermissions.Pages_TimeManagement_DutyPeriods_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _dutyPeriodRepository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_TimeManagement_DutyPeriods_Create)]
    protected virtual async Task CreateAsync(CreateOrEditDutyPeriodDto input)
    {
        var entity = MapToEntity(input, new DutyPeriod());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await AssignCodeIfEmptyAsync(entity);
        await ValidateAsync(entity);

        await _dutyPeriodRepository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_TimeManagement_DutyPeriods_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditDutyPeriodDto input)
    {
        var entity = await _dutyPeriodRepository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await AssignCodeIfEmptyAsync(entity);
        await ValidateAsync(entity);

        await _dutyPeriodRepository.UpdateAsync(entity);
    }

    private static DutyPeriod MapToEntity(CreateOrEditDutyPeriodDto input, DutyPeriod entity)
    {
        entity.Code = input.Code;
        entity.NameEn = input.NameEn;
        entity.NameAr = input.NameAr;
        entity.StartDate = input.StartDate;
        entity.EndDate = input.EndDate;
        entity.HolidayCalendarId = input.HolidayCalendarId;
        entity.SundayDutyId = input.SundayDutyId;
        entity.MondayDutyId = input.MondayDutyId;
        entity.TuesdayDutyId = input.TuesdayDutyId;
        entity.WednesdayDutyId = input.WednesdayDutyId;
        entity.ThursdayDutyId = input.ThursdayDutyId;
        entity.FridayDutyId = input.FridayDutyId;
        entity.SaturdayDutyId = input.SaturdayDutyId;

        return entity;
    }

    /// <summary>
    /// Legacy <c>GetMaxCode</c> parity: when Code is blank, assign (max existing numeric code) + 1.
    /// </summary>
    private async Task AssignCodeIfEmptyAsync(DutyPeriod entity)
    {
        if (!entity.Code.IsNullOrWhiteSpace())
        {
            return;
        }

        var numericCodes = await _dutyPeriodRepository.GetAll()
            .Where(d => d.Code != null && d.Code != "")
            .Select(d => d.Code)
            .ToListAsync();

        var maxCode = numericCodes
            .Select(c => int.TryParse(c, out var n) ? n : 0)
            .DefaultIfEmpty(0)
            .Max();

        entity.Code = (maxCode + 1).ToString();
    }

    /// <summary>
    /// Legacy <c>PerformValidation</c> parity (simplified — see class remarks): unique Code, unique
    /// NameEn/NameAr and StartDate &lt;= EndDate. The "all 7 days required" / roster-week-contiguity
    /// branches are NOT ported.
    /// </summary>
    private async Task ValidateAsync(DutyPeriod entity)
    {
        var duplicateCode = await _dutyPeriodRepository.GetAll()
            .AnyAsync(d => d.Id != entity.Id && d.Code == entity.Code);
        if (duplicateCode)
        {
            throw new UserFriendlyException(L("DuplicateCodeFound"));
        }

        var duplicateName = await _dutyPeriodRepository.GetAll()
            .AnyAsync(d => d.Id != entity.Id &&
                           (d.NameEn == entity.NameEn || d.NameAr == entity.NameAr));
        if (duplicateName)
        {
            throw new UserFriendlyException(L("NameAlreadyInUse"));
        }

        if (entity.EndDate < entity.StartDate)
        {
            throw new UserFriendlyException(L("EndDateCannotBeEarlierThanStartDate"));
        }
    }

    private static DutyPeriodDto ToDto(DutyPeriod entity)
    {
        return new DutyPeriodDto
        {
            Id = entity.Id,
            Code = entity.Code,
            NameEn = entity.NameEn,
            NameAr = entity.NameAr,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate,
            HolidayCalendarId = entity.HolidayCalendarId,
            HolidayCalendarName = entity.HolidayCalendar?.NameEn,
            SundayDutyId = entity.SundayDutyId,
            MondayDutyId = entity.MondayDutyId,
            TuesdayDutyId = entity.TuesdayDutyId,
            WednesdayDutyId = entity.WednesdayDutyId,
            ThursdayDutyId = entity.ThursdayDutyId,
            FridayDutyId = entity.FridayDutyId,
            SaturdayDutyId = entity.SaturdayDutyId
        };
    }

    private static CreateOrEditDutyPeriodDto ToCreateOrEditDto(DutyPeriod entity)
    {
        return new CreateOrEditDutyPeriodDto
        {
            Id = entity.Id,
            Code = entity.Code,
            NameEn = entity.NameEn,
            NameAr = entity.NameAr,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate,
            HolidayCalendarId = entity.HolidayCalendarId,
            SundayDutyId = entity.SundayDutyId,
            MondayDutyId = entity.MondayDutyId,
            TuesdayDutyId = entity.TuesdayDutyId,
            WednesdayDutyId = entity.WednesdayDutyId,
            ThursdayDutyId = entity.ThursdayDutyId,
            FridayDutyId = entity.FridayDutyId,
            SaturdayDutyId = entity.SaturdayDutyId
        };
    }
}
