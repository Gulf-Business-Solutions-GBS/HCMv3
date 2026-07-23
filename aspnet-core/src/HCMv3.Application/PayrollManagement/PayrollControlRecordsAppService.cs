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
/// Application service for <see cref="PayrollControlRecord"/> (ported from legacy XAF
/// PayrollControlRecord business object). See <see cref="PayrollControlRecord"/> class remarks for the
/// PeriodLocked/PeriodClosed bool simplification and the dropped SalaryRegister lock cascade/retro-log
/// block.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_PayrollManagement_PayrollControlRecords)]
public class PayrollControlRecordsAppService : HCMv3AppServiceBase, IPayrollControlRecordsAppService
{
    private readonly IRepository<PayrollControlRecord> _payrollControlRecordRepository;
    private readonly IRepository<PayrollArea> _payrollAreaRepository;

    public PayrollControlRecordsAppService(
        IRepository<PayrollControlRecord> payrollControlRecordRepository,
        IRepository<PayrollArea> payrollAreaRepository)
    {
        _payrollControlRecordRepository = payrollControlRecordRepository;
        _payrollAreaRepository = payrollAreaRepository;
    }

    [HttpPost]
    public async Task<PagedResultDto<PayrollControlRecordDto>> GetAll(GetAllPayrollControlRecordsInput input)
    {
        input.Normalize();

        var query = _payrollControlRecordRepository.GetAll()
            .Include(r => r.PayrollArea)
            .WhereIf(input.PayrollAreaId.HasValue, r => r.PayrollAreaId == input.PayrollAreaId);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<PayrollControlRecordDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<GetPayrollControlRecordForEditOutput> GetPayrollControlRecordForEdit(EntityDto input)
    {
        var entity = await _payrollControlRecordRepository.GetAsync(input.Id);

        return new GetPayrollControlRecordForEditOutput
        {
            PayrollControlRecord = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditPayrollControlRecordDto input)
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

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_PayrollControlRecords_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _payrollControlRecordRepository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_PayrollControlRecords_Create)]
    protected virtual async Task CreateAsync(CreateOrEditPayrollControlRecordDto input)
    {
        var entity = MapToEntity(input, new PayrollControlRecord());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await ValidateAndComputeCalendarRangeAsync(entity);

        await _payrollControlRecordRepository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_PayrollControlRecords_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditPayrollControlRecordDto input)
    {
        var entity = await _payrollControlRecordRepository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await ValidateAndComputeCalendarRangeAsync(entity);

        await _payrollControlRecordRepository.UpdateAsync(entity);
    }

    private static PayrollControlRecord MapToEntity(CreateOrEditPayrollControlRecordDto input, PayrollControlRecord entity)
    {
        entity.PayrollAreaId = input.PayrollAreaId;
        entity.PayrollMonth = input.PayrollMonth;
        entity.PayrollYear = input.PayrollYear;
        entity.PeriodLocked = input.PeriodLocked;
        entity.PeriodClosed = input.PeriodClosed;

        return entity;
    }

    /// <summary>
    /// Legacy <c>OnSaving</c> parity: exactly one control record is allowed per PayrollArea ("Control record
    /// is unique for a PayrollArea"), and CalendarMonthStart/CalendarMonthEnd are (re)computed from the
    /// PayrollArea's PayrollPeriod fiscal-shift code via <see cref="PayrollPeriodShiftCalculator"/> on every
    /// save.
    /// </summary>
    private async Task ValidateAndComputeCalendarRangeAsync(PayrollControlRecord entity)
    {
        var existingForArea = await _payrollControlRecordRepository.GetAll()
            .AnyAsync(r => r.Id != entity.Id && r.PayrollAreaId == entity.PayrollAreaId);
        if (existingForArea)
        {
            throw new UserFriendlyException(L("PayrollControlRecordAlreadyExistsForPayrollArea"));
        }

        var payrollArea = await _payrollAreaRepository.GetAll()
            .Include(a => a.PayrollPeriod)
            .FirstOrDefaultAsync(a => a.Id == entity.PayrollAreaId);
        if (payrollArea?.PayrollPeriod == null)
        {
            throw new UserFriendlyException(L("IncorrectPayrollPeriod"));
        }

        try
        {
            var (start, end) = PayrollPeriodShiftCalculator.GetCalendarMonthRange(
                payrollArea.PayrollPeriod.Code, entity.PayrollMonth, entity.PayrollYear);

            entity.CalendarMonthStart = start;
            entity.CalendarMonthEnd = end;
        }
        catch (System.ArgumentException)
        {
            throw new UserFriendlyException(L("IncorrectPayrollPeriod"));
        }
    }

    private static PayrollControlRecordDto ToDto(PayrollControlRecord entity)
    {
        return new PayrollControlRecordDto
        {
            Id = entity.Id,
            PayrollAreaId = entity.PayrollAreaId,
            PayrollAreaName = entity.PayrollArea?.Name,
            PayrollMonth = entity.PayrollMonth,
            PayrollYear = entity.PayrollYear,
            CalendarMonthStart = entity.CalendarMonthStart,
            CalendarMonthEnd = entity.CalendarMonthEnd,
            PeriodLocked = entity.PeriodLocked,
            PeriodClosed = entity.PeriodClosed
        };
    }

    private static CreateOrEditPayrollControlRecordDto ToCreateOrEditDto(PayrollControlRecord entity)
    {
        return new CreateOrEditPayrollControlRecordDto
        {
            Id = entity.Id,
            PayrollAreaId = entity.PayrollAreaId,
            PayrollMonth = entity.PayrollMonth,
            PayrollYear = entity.PayrollYear,
            PeriodLocked = entity.PeriodLocked,
            PeriodClosed = entity.PeriodClosed
        };
    }
}
