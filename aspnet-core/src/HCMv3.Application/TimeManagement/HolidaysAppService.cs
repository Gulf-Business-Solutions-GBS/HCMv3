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
/// Application service for the Holiday master (ported from legacy XAF Holiday business object). Listed
/// separately from <see cref="HolidayCalendarsAppService"/> (flat parent+child pattern), linked back via
/// <see cref="Holiday.HolidayCalendarId"/>. The dated occurrences are a separate child entity, exposed via
/// <see cref="HolidayPeriodsAppService"/>.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_TimeManagement_Holidays)]
public class HolidaysAppService : HCMv3AppServiceBase, IHolidaysAppService
{
    private readonly IRepository<Holiday> _holidayRepository;

    public HolidaysAppService(IRepository<Holiday> holidayRepository)
    {
        _holidayRepository = holidayRepository;
    }

    [HttpPost]
    public async Task<PagedResultDto<HolidayDto>> GetAll(GetAllHolidaysInput input)
    {
        input.Normalize();

        var query = _holidayRepository.GetAll()
            .Include(h => h.HolidayCalendar)
            .Include(h => h.OvertimeType)
            .WhereIf(!input.Filter.IsNullOrWhiteSpace(), h =>
                h.Code.Contains(input.Filter) ||
                h.NameEn.Contains(input.Filter) ||
                h.NameAr.Contains(input.Filter))
            .WhereIf(input.HolidayCalendarId.HasValue, h => h.HolidayCalendarId == input.HolidayCalendarId);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<HolidayDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<GetHolidayForEditOutput> GetHolidayForEdit(EntityDto input)
    {
        var entity = await _holidayRepository.GetAsync(input.Id);

        return new GetHolidayForEditOutput
        {
            Holiday = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditHolidayDto input)
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

    [AbpAuthorize(AppPermissions.Pages_TimeManagement_Holidays_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _holidayRepository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_TimeManagement_Holidays_Create)]
    protected virtual async Task CreateAsync(CreateOrEditHolidayDto input)
    {
        var entity = new Holiday
        {
            Code = input.Code,
            NameEn = input.NameEn,
            NameAr = input.NameAr,
            HolidayCalendarId = input.HolidayCalendarId,
            OvertimeTypeId = input.OvertimeTypeId,
            TenantId = AbpSession.TenantId ?? 0
        };

        await AssignCodeIfEmptyAsync(entity);
        await ValidateUniquenessAsync(entity);

        await _holidayRepository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_TimeManagement_Holidays_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditHolidayDto input)
    {
        var entity = await _holidayRepository.GetAsync(input.Id.Value);

        entity.Code = input.Code;
        entity.NameEn = input.NameEn;
        entity.NameAr = input.NameAr;
        entity.HolidayCalendarId = input.HolidayCalendarId;
        entity.OvertimeTypeId = input.OvertimeTypeId;

        await AssignCodeIfEmptyAsync(entity);
        await ValidateUniquenessAsync(entity);

        await _holidayRepository.UpdateAsync(entity);
    }

    /// <summary>
    /// Legacy <c>GetMaxCode</c> parity: when Code is blank, assign (max existing numeric code) + 1.
    /// </summary>
    private async Task AssignCodeIfEmptyAsync(Holiday entity)
    {
        if (!entity.Code.IsNullOrWhiteSpace())
        {
            return;
        }

        var numericCodes = await _holidayRepository.GetAll()
            .Where(h => h.Code != null && h.Code != "")
            .Select(h => h.Code)
            .ToListAsync();

        var maxCode = numericCodes
            .Select(c => int.TryParse(c, out var n) ? n : 0)
            .DefaultIfEmpty(0)
            .Max();

        entity.Code = (maxCode + 1).ToString();
    }

    /// <summary>
    /// Legacy <c>PerformValidation</c> parity: unique Code and unique NameEn/NameAr.
    /// </summary>
    private async Task ValidateUniquenessAsync(Holiday entity)
    {
        var duplicateCode = await _holidayRepository.GetAll()
            .AnyAsync(h => h.Id != entity.Id && h.Code == entity.Code);
        if (duplicateCode)
        {
            throw new UserFriendlyException(L("DuplicateCodeFound"));
        }

        var duplicateName = await _holidayRepository.GetAll()
            .AnyAsync(h => h.Id != entity.Id &&
                           (h.NameEn == entity.NameEn || h.NameAr == entity.NameAr));
        if (duplicateName)
        {
            throw new UserFriendlyException(L("NameAlreadyInUse"));
        }
    }

    private static HolidayDto ToDto(Holiday entity)
    {
        return new HolidayDto
        {
            Id = entity.Id,
            Code = entity.Code,
            NameEn = entity.NameEn,
            NameAr = entity.NameAr,
            HolidayCalendarId = entity.HolidayCalendarId,
            HolidayCalendarName = entity.HolidayCalendar?.NameEn,
            OvertimeTypeId = entity.OvertimeTypeId,
            OvertimeTypeName = entity.OvertimeType?.NameEn
        };
    }

    private static CreateOrEditHolidayDto ToCreateOrEditDto(Holiday entity)
    {
        return new CreateOrEditHolidayDto
        {
            Id = entity.Id,
            Code = entity.Code,
            NameEn = entity.NameEn,
            NameAr = entity.NameAr,
            HolidayCalendarId = entity.HolidayCalendarId,
            OvertimeTypeId = entity.OvertimeTypeId
        };
    }
}
