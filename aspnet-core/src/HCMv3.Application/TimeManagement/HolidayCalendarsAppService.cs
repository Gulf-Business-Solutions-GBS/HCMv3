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
/// Application service for the Holiday Calendar master (ported from legacy XAF HolidayCalendar business
/// object). The aggregated <c>Holiday</c> collection is exposed separately via
/// <see cref="HolidaysAppService"/> (flat parent+child pattern), not as a nested list here.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_TimeManagement_HolidayCalendars)]
public class HolidayCalendarsAppService : HCMv3AppServiceBase, IHolidayCalendarsAppService
{
    private readonly IRepository<HolidayCalendar> _holidayCalendarRepository;

    public HolidayCalendarsAppService(IRepository<HolidayCalendar> holidayCalendarRepository)
    {
        _holidayCalendarRepository = holidayCalendarRepository;
    }

    [HttpPost]
    public async Task<PagedResultDto<HolidayCalendarDto>> GetAll(GetAllHolidayCalendarsInput input)
    {
        input.Normalize();

        var query = _holidayCalendarRepository.GetAll()
            .WhereIf(!input.Filter.IsNullOrWhiteSpace(), h =>
                h.Code.Contains(input.Filter) ||
                h.NameEn.Contains(input.Filter) ||
                h.NameAr.Contains(input.Filter));

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<HolidayCalendarDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<GetHolidayCalendarForEditOutput> GetHolidayCalendarForEdit(EntityDto input)
    {
        var entity = await _holidayCalendarRepository.GetAsync(input.Id);

        return new GetHolidayCalendarForEditOutput
        {
            HolidayCalendar = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditHolidayCalendarDto input)
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

    [AbpAuthorize(AppPermissions.Pages_TimeManagement_HolidayCalendars_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _holidayCalendarRepository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_TimeManagement_HolidayCalendars_Create)]
    protected virtual async Task CreateAsync(CreateOrEditHolidayCalendarDto input)
    {
        var entity = new HolidayCalendar
        {
            Code = input.Code,
            NameEn = input.NameEn,
            NameAr = input.NameAr,
            TenantId = AbpSession.TenantId ?? 0
        };

        await AssignCodeIfEmptyAsync(entity);
        await ValidateUniquenessAsync(entity);

        await _holidayCalendarRepository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_TimeManagement_HolidayCalendars_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditHolidayCalendarDto input)
    {
        var entity = await _holidayCalendarRepository.GetAsync(input.Id.Value);

        entity.Code = input.Code;
        entity.NameEn = input.NameEn;
        entity.NameAr = input.NameAr;

        await AssignCodeIfEmptyAsync(entity);
        await ValidateUniquenessAsync(entity);

        await _holidayCalendarRepository.UpdateAsync(entity);
    }

    /// <summary>
    /// Legacy <c>GetMaxCode</c> parity: when Code is blank, assign (max existing numeric code) + 1.
    /// </summary>
    private async Task AssignCodeIfEmptyAsync(HolidayCalendar entity)
    {
        if (!entity.Code.IsNullOrWhiteSpace())
        {
            return;
        }

        var numericCodes = await _holidayCalendarRepository.GetAll()
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
    private async Task ValidateUniquenessAsync(HolidayCalendar entity)
    {
        var duplicateCode = await _holidayCalendarRepository.GetAll()
            .AnyAsync(h => h.Id != entity.Id && h.Code == entity.Code);
        if (duplicateCode)
        {
            throw new UserFriendlyException(L("DuplicateCodeFound"));
        }

        var duplicateName = await _holidayCalendarRepository.GetAll()
            .AnyAsync(h => h.Id != entity.Id &&
                           (h.NameEn == entity.NameEn || h.NameAr == entity.NameAr));
        if (duplicateName)
        {
            throw new UserFriendlyException(L("NameAlreadyInUse"));
        }
    }

    private static HolidayCalendarDto ToDto(HolidayCalendar entity)
    {
        return new HolidayCalendarDto
        {
            Id = entity.Id,
            Code = entity.Code,
            NameEn = entity.NameEn,
            NameAr = entity.NameAr
        };
    }

    private static CreateOrEditHolidayCalendarDto ToCreateOrEditDto(HolidayCalendar entity)
    {
        return new CreateOrEditHolidayCalendarDto
        {
            Id = entity.Id,
            Code = entity.Code,
            NameEn = entity.NameEn,
            NameAr = entity.NameAr
        };
    }
}
