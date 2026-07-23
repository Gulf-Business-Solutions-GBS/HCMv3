using System;
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
/// Application service for the Daily Duty master (ported from legacy XAF DailyDuty business object — the
/// Roster subsystem's building block, as opposed to <see cref="DutiesAppService"/> for classic DutyPeriod).
///
/// DELIBERATE SIMPLIFICATION: the legacy <c>PerformValidation</c> break-range check used an hour-based
/// heuristic (adding a day when the break/end hour fell between 0 and 13) to guess whether a time crossed
/// midnight, and a separate convoluted <c>StartDate</c>/<c>EndTime</c> sentinel-date comparison to validate
/// the <c>IsNightShift</c> flag. Both are replaced here with the same explicit, flag-driven check used by
/// <see cref="DutiesAppService"/> (via the shared <see cref="TimeRangeValidationHelper"/>), which is
/// simpler and unambiguous. The IsNightShift/StartDate cross-midnight consistency check itself is not
/// ported (its exact legacy semantics were not testable/clear without a UI to reproduce against).
/// </summary>
[AbpAuthorize(AppPermissions.Pages_TimeManagement_DailyDuties)]
public class DailyDutiesAppService : HCMv3AppServiceBase, IDailyDutiesAppService
{
    private readonly IRepository<DailyDuty> _dailyDutyRepository;
    private readonly IRepository<BreakShift> _breakShiftRepository;

    public DailyDutiesAppService(IRepository<DailyDuty> dailyDutyRepository, IRepository<BreakShift> breakShiftRepository)
    {
        _dailyDutyRepository = dailyDutyRepository;
        _breakShiftRepository = breakShiftRepository;
    }

    [HttpPost]
    public async Task<PagedResultDto<DailyDutyDto>> GetAll(GetAllDailyDutiesInput input)
    {
        input.Normalize();

        var query = _dailyDutyRepository.GetAll()
            .Include(d => d.BreakShift)
            .WhereIf(!input.Filter.IsNullOrWhiteSpace(), d =>
                d.Code.Contains(input.Filter) ||
                d.NameEn.Contains(input.Filter) ||
                d.NameAr.Contains(input.Filter));

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<DailyDutyDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<GetDailyDutyForEditOutput> GetDailyDutyForEdit(EntityDto input)
    {
        var entity = await _dailyDutyRepository.GetAsync(input.Id);

        return new GetDailyDutyForEditOutput
        {
            DailyDuty = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditDailyDutyDto input)
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

    [AbpAuthorize(AppPermissions.Pages_TimeManagement_DailyDuties_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _dailyDutyRepository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_TimeManagement_DailyDuties_Create)]
    protected virtual async Task CreateAsync(CreateOrEditDailyDutyDto input)
    {
        var entity = new DailyDuty
        {
            Code = input.Code,
            NameEn = input.NameEn,
            NameAr = input.NameAr,
            StartDate = input.StartDate,
            EndDate = input.EndDate,
            StartTime = input.StartTime,
            EndTime = input.EndTime,
            IsNightShift = input.IsNightShift,
            BreakShiftId = input.BreakShiftId,
            AllowTransStart = input.AllowTransStart,
            AllowTransEnd = input.AllowTransEnd,
            TenantId = AbpSession.TenantId ?? 0
        };

        await AssignCodeIfEmptyAsync(entity);
        await ValidateAsync(entity);

        await _dailyDutyRepository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_TimeManagement_DailyDuties_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditDailyDutyDto input)
    {
        var entity = await _dailyDutyRepository.GetAsync(input.Id.Value);

        entity.Code = input.Code;
        entity.NameEn = input.NameEn;
        entity.NameAr = input.NameAr;
        entity.StartDate = input.StartDate;
        entity.EndDate = input.EndDate;
        entity.StartTime = input.StartTime;
        entity.EndTime = input.EndTime;
        entity.IsNightShift = input.IsNightShift;
        entity.BreakShiftId = input.BreakShiftId;
        entity.AllowTransStart = input.AllowTransStart;
        entity.AllowTransEnd = input.AllowTransEnd;

        await AssignCodeIfEmptyAsync(entity);
        await ValidateAsync(entity);

        await _dailyDutyRepository.UpdateAsync(entity);
    }

    /// <summary>
    /// Legacy <c>GetMaxCode</c> parity: when Code is blank, assign (max existing numeric code) + 1.
    /// </summary>
    private async Task AssignCodeIfEmptyAsync(DailyDuty entity)
    {
        if (!entity.Code.IsNullOrWhiteSpace())
        {
            return;
        }

        var numericCodes = await _dailyDutyRepository.GetAll()
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
    /// Legacy <c>PerformValidation</c> parity (simplified break-range check — see class remarks).
    /// </summary>
    private async Task ValidateAsync(DailyDuty entity)
    {
        var duplicateCode = await _dailyDutyRepository.GetAll()
            .AnyAsync(d => d.Id != entity.Id && d.Code == entity.Code);
        if (duplicateCode)
        {
            throw new UserFriendlyException(L("DuplicateCodeFound"));
        }

        var duplicateName = await _dailyDutyRepository.GetAll()
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

        var dutyStart = entity.StartTime;
        var dutyEnd = entity.EndTime + (entity.IsNightShift ? TimeSpan.FromDays(1) : TimeSpan.Zero);

        if (entity.BreakShiftId.HasValue)
        {
            var breakShift = await _breakShiftRepository.GetAsync(entity.BreakShiftId.Value);

            var breakStart = breakShift.StartTime + (breakShift.IsBreakStartNextDay ? TimeSpan.FromDays(1) : TimeSpan.Zero);
            var breakEnd = breakShift.EndTime + (breakShift.IsBreakEndNextDay ? TimeSpan.FromDays(1) : TimeSpan.Zero);

            if (TimeRangeValidationHelper.IsFirstGreaterThanSecond(dutyStart, breakStart))
            {
                throw new UserFriendlyException(L("StartTimeCannotBeAfterEndTime", dutyStart, breakStart));
            }

            if (TimeRangeValidationHelper.IsFirstGreaterThanSecond(breakStart, breakEnd))
            {
                throw new UserFriendlyException(L("StartTimeCannotBeAfterEndTime", breakStart, breakEnd));
            }

            if (TimeRangeValidationHelper.IsFirstGreaterThanSecond(breakEnd, dutyEnd))
            {
                throw new UserFriendlyException(L("StartTimeCannotBeAfterEndTime", breakEnd, dutyEnd));
            }
        }
    }

    private static DailyDutyDto ToDto(DailyDuty entity)
    {
        return new DailyDutyDto
        {
            Id = entity.Id,
            Code = entity.Code,
            NameEn = entity.NameEn,
            NameAr = entity.NameAr,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate,
            StartTime = entity.StartTime,
            EndTime = entity.EndTime,
            IsNightShift = entity.IsNightShift,
            BreakShiftId = entity.BreakShiftId,
            BreakShiftName = entity.BreakShift?.NameEn,
            AllowTransStart = entity.AllowTransStart,
            AllowTransEnd = entity.AllowTransEnd
        };
    }

    private static CreateOrEditDailyDutyDto ToCreateOrEditDto(DailyDuty entity)
    {
        return new CreateOrEditDailyDutyDto
        {
            Id = entity.Id,
            Code = entity.Code,
            NameEn = entity.NameEn,
            NameAr = entity.NameAr,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate,
            StartTime = entity.StartTime,
            EndTime = entity.EndTime,
            IsNightShift = entity.IsNightShift,
            BreakShiftId = entity.BreakShiftId,
            AllowTransStart = entity.AllowTransStart,
            AllowTransEnd = entity.AllowTransEnd
        };
    }
}
