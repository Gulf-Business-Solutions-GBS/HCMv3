using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Linq.Extensions;
using Abp.UI;
using HCMv3.Authorization;
using HCMv3.PayrollManagement.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.PayrollManagement;

/// <summary>
/// Application service for <see cref="PayrollCalendar"/> (ported from legacy XAF PayrollCalendar business
/// object).
/// </summary>
[AbpAuthorize(AppPermissions.Pages_PayrollManagement_PayrollCalendars)]
public class PayrollCalendarsAppService : HCMv3AppServiceBase, IPayrollCalendarsAppService
{
    private readonly IRepository<PayrollCalendar> _payrollCalendarRepository;

    public PayrollCalendarsAppService(IRepository<PayrollCalendar> payrollCalendarRepository)
    {
        _payrollCalendarRepository = payrollCalendarRepository;
    }

    [HttpPost]
    public async Task<PagedResultDto<PayrollCalendarDto>> GetAll(GetAllPayrollCalendarsInput input)
    {
        input.Normalize();

        var query = _payrollCalendarRepository.GetAll()
            .Include(c => c.PayrollArea)
            .WhereIf(input.PayrollAreaId.HasValue, c => c.PayrollAreaId == input.PayrollAreaId);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<PayrollCalendarDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<GetPayrollCalendarForEditOutput> GetPayrollCalendarForEdit(EntityDto input)
    {
        var entity = await _payrollCalendarRepository.GetAsync(input.Id);

        return new GetPayrollCalendarForEditOutput
        {
            PayrollCalendar = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditPayrollCalendarDto input)
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

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_PayrollCalendars_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _payrollCalendarRepository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_PayrollCalendars_Create)]
    protected virtual async Task CreateAsync(CreateOrEditPayrollCalendarDto input)
    {
        var entity = MapToEntity(input, new PayrollCalendar());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await ValidateAsync(entity);

        await _payrollCalendarRepository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_PayrollCalendars_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditPayrollCalendarDto input)
    {
        var entity = await _payrollCalendarRepository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await ValidateAsync(entity);

        await _payrollCalendarRepository.UpdateAsync(entity);
    }

    private static PayrollCalendar MapToEntity(CreateOrEditPayrollCalendarDto input, PayrollCalendar entity)
    {
        entity.PayrollAreaId = input.PayrollAreaId;
        entity.PayrollMonth = input.PayrollMonth;
        entity.PayrollYear = input.PayrollYear;
        entity.StartDate = input.StartDate.Date;
        entity.EndDate = input.EndDate.Date;

        return entity;
    }

    /// <summary>
    /// Legacy <c>PerformValidation</c> parity: required fields, EndDate &gt;= StartDate, no overlap with a
    /// sibling PayrollCalendar for the same PayrollArea.
    /// </summary>
    private async Task ValidateAsync(PayrollCalendar entity)
    {
        if (entity.EndDate.Date < entity.StartDate.Date)
        {
            throw new UserFriendlyException(L("EndDateCannotBeEarlierThanStartDate"));
        }

        var overlap = await _payrollCalendarRepository.GetAll()
            .AnyAsync(c => c.Id != entity.Id &&
                           c.PayrollAreaId == entity.PayrollAreaId &&
                           c.StartDate.Date <= entity.EndDate.Date &&
                           c.EndDate.Date >= entity.StartDate.Date);
        if (overlap)
        {
            throw new UserFriendlyException(L("DateRangeOverlapFound"));
        }
    }

    private static PayrollCalendarDto ToDto(PayrollCalendar entity)
    {
        return new PayrollCalendarDto
        {
            Id = entity.Id,
            PayrollAreaId = entity.PayrollAreaId,
            PayrollAreaName = entity.PayrollArea?.Name,
            PayrollMonth = entity.PayrollMonth,
            PayrollYear = entity.PayrollYear,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate
        };
    }

    private static CreateOrEditPayrollCalendarDto ToCreateOrEditDto(PayrollCalendar entity)
    {
        return new CreateOrEditPayrollCalendarDto
        {
            Id = entity.Id,
            PayrollAreaId = entity.PayrollAreaId,
            PayrollMonth = entity.PayrollMonth,
            PayrollYear = entity.PayrollYear,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate
        };
    }
}
