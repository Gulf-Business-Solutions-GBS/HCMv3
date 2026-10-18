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
/// Application service for the Duty master (ported from legacy XAF Duty business object — the classic
/// DutyPeriod building block, as opposed to <see cref="DailyDutiesAppService"/> for the Roster subsystem).
/// The legacy <c>PerformValidation</c>/<c>ValidateDutyBreakStartEnd</c> logic lives here, out of the
/// persistence lifecycle. Entity/DTO mapping is manual (not Mapperly), matching <c>CitiesAppService</c>.
/// The legacy <c>GetPlannedMinutes</c> static helper is ported as <see cref="DutyCalculations.GetPlannedMinutes"/>.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_TimeManagement_Duties)]
public class DutiesAppService : HCMv3AppServiceBase, IDutiesAppService
{
    private readonly IRepository<Duty> _dutyRepository;
    private readonly IRepository<BreakShift> _breakShiftRepository;

    public DutiesAppService(IRepository<Duty> dutyRepository, IRepository<BreakShift> breakShiftRepository)
    {
        _dutyRepository = dutyRepository;
        _breakShiftRepository = breakShiftRepository;
    }

    [HttpPost]
    public async Task<PagedResultDto<DutyDto>> GetAll(GetAllDutiesInput input)
    {
        input.Normalize();

        var query = _dutyRepository.GetAll()
            .Include(d => d.BreakShift)
            .Include(d => d.OvertimeType)
            .WhereIf(!input.Filter.IsNullOrWhiteSpace(), d =>
                d.Code.Contains(input.Filter) ||
                d.NameEn.Contains(input.Filter) ||
                d.NameAr.Contains(input.Filter));

        if (input.IsActive.HasValue)
        {
            var isActive = input.IsActive.Value;
            var includeIds = input.IncludeIds ?? new System.Collections.Generic.List<int>();
            query = query.Where(d => d.IsActive == isActive || includeIds.Contains(d.Id));
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<DutyDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<GetDutyForEditOutput> GetDutyForEdit(EntityDto input)
    {
        var entity = await _dutyRepository.GetAsync(input.Id);

        return new GetDutyForEditOutput
        {
            Duty = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditDutyDto input)
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

    [AbpAuthorize(AppPermissions.Pages_TimeManagement_Duties_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _dutyRepository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_TimeManagement_Duties_Create)]
    protected virtual async Task CreateAsync(CreateOrEditDutyDto input)
    {
        var entity = new Duty
        {
            Code = input.Code,
            NameEn = input.NameEn,
            NameAr = input.NameAr,
            StartDate = input.StartDate,
            EndDate = input.EndDate,
            StartTime = input.StartTime,
            EndTime = input.EndTime,
            BreakShiftId = input.BreakShiftId,
            OvertimeTypeId = input.OvertimeTypeId,
            AllowTransStart = input.AllowTransStart,
            AllowTransEnd = input.AllowTransEnd,
            IsNightShift = input.IsNightShift,
            IsWeeklyOff = input.IsWeeklyOff,
            IsRestDay = input.IsRestDay,
            IsHalfDay = input.IsHalfDay,
            FlexiStartTime = input.FlexiStartTime,
            FlexiEndTime = input.FlexiEndTime,
            IsActive = input.IsActive,
            TenantId = AbpSession.TenantId ?? 0
        };

        await AssignCodeIfEmptyAsync(entity);
        await ValidateAsync(entity);

        await _dutyRepository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_TimeManagement_Duties_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditDutyDto input)
    {
        var entity = await _dutyRepository.GetAsync(input.Id.Value);

        entity.Code = input.Code;
        entity.NameEn = input.NameEn;
        entity.NameAr = input.NameAr;
        entity.StartDate = input.StartDate;
        entity.EndDate = input.EndDate;
        entity.StartTime = input.StartTime;
        entity.EndTime = input.EndTime;
        entity.BreakShiftId = input.BreakShiftId;
        entity.OvertimeTypeId = input.OvertimeTypeId;
        entity.AllowTransStart = input.AllowTransStart;
        entity.AllowTransEnd = input.AllowTransEnd;
        entity.IsNightShift = input.IsNightShift;
        entity.IsWeeklyOff = input.IsWeeklyOff;
        entity.IsRestDay = input.IsRestDay;
        entity.IsHalfDay = input.IsHalfDay;
        entity.FlexiStartTime = input.FlexiStartTime;
        entity.FlexiEndTime = input.FlexiEndTime;
        entity.IsActive = input.IsActive;

        await AssignCodeIfEmptyAsync(entity);
        await ValidateAsync(entity);

        await _dutyRepository.UpdateAsync(entity);
    }

    /// <summary>
    /// Legacy <c>GetMaxCode</c> parity: when Code is blank, assign (max existing numeric code) + 1.
    /// </summary>
    private async Task AssignCodeIfEmptyAsync(Duty entity)
    {
        if (!entity.Code.IsNullOrWhiteSpace())
        {
            return;
        }

        var numericCodes = await _dutyRepository.GetAll()
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
    /// Legacy <c>PerformValidation</c>/<c>ValidateDutyBreakStartEnd</c> parity.
    /// </summary>
    private async Task ValidateAsync(Duty entity)
    {
        var duplicateCode = await _dutyRepository.GetAll()
            .AnyAsync(d => d.Id != entity.Id && d.Code == entity.Code);
        if (duplicateCode)
        {
            throw new UserFriendlyException(L("DuplicateCodeFound"));
        }

        var duplicateName = await _dutyRepository.GetAll()
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
        else if (!entity.IsWeeklyOff && !entity.IsRestDay &&
                 TimeRangeValidationHelper.IsFirstGreaterThanSecond(dutyStart, dutyEnd))
        {
            throw new UserFriendlyException(L("StartTimeCannotBeAfterEndTime", dutyStart, dutyEnd));
        }
    }

    private static DutyDto ToDto(Duty entity)
    {
        return new DutyDto
        {
            Id = entity.Id,
            Code = entity.Code,
            NameEn = entity.NameEn,
            NameAr = entity.NameAr,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate,
            StartTime = entity.StartTime,
            EndTime = entity.EndTime,
            BreakShiftId = entity.BreakShiftId,
            BreakShiftName = entity.BreakShift?.NameEn,
            OvertimeTypeId = entity.OvertimeTypeId,
            OvertimeTypeName = entity.OvertimeType?.NameEn,
            AllowTransStart = entity.AllowTransStart,
            AllowTransEnd = entity.AllowTransEnd,
            IsNightShift = entity.IsNightShift,
            IsWeeklyOff = entity.IsWeeklyOff,
            IsRestDay = entity.IsRestDay,
            IsHalfDay = entity.IsHalfDay,
            FlexiStartTime = entity.FlexiStartTime,
            FlexiEndTime = entity.FlexiEndTime,
            IsActive = entity.IsActive
        };
    }

    private static CreateOrEditDutyDto ToCreateOrEditDto(Duty entity)
    {
        return new CreateOrEditDutyDto
        {
            Id = entity.Id,
            Code = entity.Code,
            NameEn = entity.NameEn,
            NameAr = entity.NameAr,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate,
            StartTime = entity.StartTime,
            EndTime = entity.EndTime,
            BreakShiftId = entity.BreakShiftId,
            OvertimeTypeId = entity.OvertimeTypeId,
            AllowTransStart = entity.AllowTransStart,
            AllowTransEnd = entity.AllowTransEnd,
            IsNightShift = entity.IsNightShift,
            IsWeeklyOff = entity.IsWeeklyOff,
            IsRestDay = entity.IsRestDay,
            IsHalfDay = entity.IsHalfDay,
            FlexiStartTime = entity.FlexiStartTime,
            FlexiEndTime = entity.FlexiEndTime,
            IsActive = entity.IsActive
        };
    }
}
