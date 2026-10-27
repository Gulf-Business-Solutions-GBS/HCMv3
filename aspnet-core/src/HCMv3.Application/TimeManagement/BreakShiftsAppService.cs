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
/// Application service for the Break Shift master (ported from legacy XAF BreakShift business object).
/// The legacy <c>PerformValidation</c>/<c>ValidateDutyBreakStartEnd</c> logic (auto Code, duplicate
/// Code/Name checks, date range, start/end time respecting the next-day flags) lives here, out of the
/// persistence lifecycle. Entity/DTO mapping is manual (not Mapperly), matching <c>CitiesAppService</c>.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_TimeManagement_BreakShifts)]
public class BreakShiftsAppService : HCMv3AppServiceBase, IBreakShiftsAppService
{
    private readonly IRepository<BreakShift> _breakShiftRepository;

    public BreakShiftsAppService(IRepository<BreakShift> breakShiftRepository)
    {
        _breakShiftRepository = breakShiftRepository;
    }

    [HttpPost]
    public async Task<PagedResultDto<BreakShiftDto>> GetAll(GetAllBreakShiftsInput input)
    {
        input.Normalize();

        var query = _breakShiftRepository.GetAll()
            .WhereIf(!input.Filter.IsNullOrWhiteSpace(), b =>
                b.Code.Contains(input.Filter) ||
                b.NameEn.Contains(input.Filter) ||
                b.NameAr.Contains(input.Filter));

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<BreakShiftDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<GetBreakShiftForEditOutput> GetBreakShiftForEdit(EntityDto input)
    {
        var entity = await _breakShiftRepository.GetAsync(input.Id);

        return new GetBreakShiftForEditOutput
        {
            BreakShift = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditBreakShiftDto input)
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

    [AbpAuthorize(AppPermissions.Pages_TimeManagement_BreakShifts_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _breakShiftRepository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_TimeManagement_BreakShifts_Create)]
    protected virtual async Task CreateAsync(CreateOrEditBreakShiftDto input)
    {
        var entity = new BreakShift
        {
            Code = input.Code,
            NameEn = input.NameEn,
            NameAr = input.NameAr,
            StartDate = input.StartDate,
            EndDate = input.EndDate,
            StartTime = input.StartTime,
            EndTime = input.EndTime,
            IsPaid = input.IsPaid,
            IsBreakStartNextDay = input.IsBreakStartNextDay,
            IsBreakEndNextDay = input.IsBreakEndNextDay,
            TenantId = AbpSession.TenantId ?? 0
        };

        await AssignCodeIfEmptyAsync(entity);
        await ValidateAsync(entity);

        await _breakShiftRepository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_TimeManagement_BreakShifts_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditBreakShiftDto input)
    {
        var entity = await _breakShiftRepository.GetAsync(input.Id.Value);

        entity.Code = input.Code;
        entity.NameEn = input.NameEn;
        entity.NameAr = input.NameAr;
        entity.StartDate = input.StartDate;
        entity.EndDate = input.EndDate;
        entity.StartTime = input.StartTime;
        entity.EndTime = input.EndTime;
        entity.IsPaid = input.IsPaid;
        entity.IsBreakStartNextDay = input.IsBreakStartNextDay;
        entity.IsBreakEndNextDay = input.IsBreakEndNextDay;

        await AssignCodeIfEmptyAsync(entity);
        await ValidateAsync(entity);

        await _breakShiftRepository.UpdateAsync(entity);
    }

    /// <summary>
    /// Legacy <c>GetMaxCode</c> parity: when Code is blank, assign (max existing numeric code) + 1.
    /// </summary>
    private async Task AssignCodeIfEmptyAsync(BreakShift entity)
    {
        if (!entity.Code.IsNullOrWhiteSpace())
        {
            return;
        }

        var numericCodes = await _breakShiftRepository.GetAll()
            .Where(b => b.Code != null && b.Code != "")
            .Select(b => b.Code)
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
    private async Task ValidateAsync(BreakShift entity)
    {
        var duplicateCode = await _breakShiftRepository.GetAll()
            .AnyAsync(b => b.Id != entity.Id && b.Code == entity.Code);
        if (duplicateCode)
        {
            throw new UserFriendlyException(L("DuplicateCodeFound"));
        }

        var duplicateName = await _breakShiftRepository.GetAll()
            .AnyAsync(b => b.Id != entity.Id &&
                           (b.NameEn == entity.NameEn || b.NameAr == entity.NameAr));
        if (duplicateName)
        {
            throw new UserFriendlyException(L("NameAlreadyInUse"));
        }

        if (entity.EndDate < entity.StartDate)
        {
            throw new UserFriendlyException(L("EndDateCannotBeEarlierThanStartDate"));
        }

        var breakStart = entity.StartTime + (entity.IsBreakStartNextDay ? TimeSpan.FromDays(1) : TimeSpan.Zero);
        var breakEnd = entity.EndTime + (entity.IsBreakEndNextDay ? TimeSpan.FromDays(1) : TimeSpan.Zero);

        if (TimeRangeValidationHelper.IsFirstGreaterThanSecond(breakStart, breakEnd))
        {
            throw new UserFriendlyException(L("StartTimeCannotBeAfterEndTime", breakStart, breakEnd));
        }
    }

    private static BreakShiftDto ToDto(BreakShift entity)
    {
        return new BreakShiftDto
        {
            Id = entity.Id,
            Code = entity.Code,
            NameEn = entity.NameEn,
            NameAr = entity.NameAr,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate,
            StartTime = entity.StartTime,
            EndTime = entity.EndTime,
            IsPaid = entity.IsPaid,
            IsBreakStartNextDay = entity.IsBreakStartNextDay,
            IsBreakEndNextDay = entity.IsBreakEndNextDay
        };
    }

    private static CreateOrEditBreakShiftDto ToCreateOrEditDto(BreakShift entity)
    {
        return new CreateOrEditBreakShiftDto
        {
            Id = entity.Id,
            Code = entity.Code,
            NameEn = entity.NameEn,
            NameAr = entity.NameAr,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate,
            StartTime = entity.StartTime,
            EndTime = entity.EndTime,
            IsPaid = entity.IsPaid,
            IsBreakStartNextDay = entity.IsBreakStartNextDay,
            IsBreakEndNextDay = entity.IsBreakEndNextDay
        };
    }
}
