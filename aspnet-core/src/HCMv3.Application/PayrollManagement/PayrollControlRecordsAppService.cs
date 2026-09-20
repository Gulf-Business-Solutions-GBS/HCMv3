using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Timing;
using Abp.Domain.Repositories;
using Abp.Linq.Extensions;
using Abp.UI;
using HCMv3.Authorization;
using HCMv3.Authorization.Users;
using HCMv3.PayrollManagement.Dto;
using HCMv3.PersonnelAdministration;
using HCMv3.SystemSettings.Services;
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
    private readonly IRepository<User, long> _userRepository;
    private readonly IRepository<SalaryRegister> _salaryRegisterRepository;
    private readonly IRepository<EmployeeOrgAssignment> _orgAssignmentRepository;
    private readonly ISysSettingsAccessor _sysSettings;

    public PayrollControlRecordsAppService(
        IRepository<PayrollControlRecord> payrollControlRecordRepository,
        IRepository<PayrollArea> payrollAreaRepository,
        IRepository<User, long> userRepository,
        IRepository<SalaryRegister> salaryRegisterRepository,
        IRepository<EmployeeOrgAssignment> orgAssignmentRepository,
        ISysSettingsAccessor sysSettings)
    {
        _payrollControlRecordRepository = payrollControlRecordRepository;
        _payrollAreaRepository = payrollAreaRepository;
        _userRepository = userRepository;
        _salaryRegisterRepository = salaryRegisterRepository;
        _orgAssignmentRepository = orgAssignmentRepository;
        _sysSettings = sysSettings;
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

        var dtos = items.Select(ToDto).ToList();
        await FillStateChangedByAsync(dtos, items);

        return new PagedResultDto<PayrollControlRecordDto>(totalCount, dtos);
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

    /// <summary>
    /// Moves the period to another state, stamping who did it and when. Go-Live Setup uses the same path, which is
    /// why the change is recorded rather than done by quietly flipping the flags.
    /// </summary>
    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_PayrollControlRecords_SetState)]
    public async Task SetState(SetPayrollPeriodStateInput input)
    {
        var entity = await _payrollControlRecordRepository.GetAsync(input.Id);

        entity.ApplyState(input.State, AbpSession.UserId, Clock.Now);

        await _payrollControlRecordRepository.UpdateAsync(entity);
        await LockRegistersForStateAsync(entity, input.State);
    }

    /// <summary>
    /// Legacy's register lock cascade, kept to the same two transitions: closing the period ("exit payroll") locks
    /// that month's REGULAR payslips, and releasing it for a payroll run locks that month's OFF-CYCLE ones. A locked
    /// payslip can no longer be changed or deleted.
    ///
    /// <para>Only when the company has "Lock salary register on payroll exit" switched on, and only for employees in
    /// this payroll area on the month's last day -- both legacy conditions.</para>
    /// </summary>
    private async Task LockRegistersForStateAsync(PayrollControlRecord record, PayrollPeriodState state)
    {
        var runType = state switch
        {
            PayrollPeriodState.Closed => (PayrollRunType?)PayrollRunType.Regular,
            PayrollPeriodState.ReleasedForPayroll => PayrollRunType.OffCycle,
            _ => null,
        };

        if (runType == null)
        {
            return;
        }

        var settings = await _sysSettings.GetPayrollSettingsAsync();
        if (settings?.LockSalaryRegisterOnPayrollExit != true)
        {
            return;
        }

        var monthEnd = record.CalendarMonthEnd.Date;

        var employeeIds = await _orgAssignmentRepository.GetAll()
            .Where(a => a.PayrollAreaId == record.PayrollAreaId && a.StartDate <= monthEnd && a.EndDate >= monthEnd)
            .Select(a => a.EmployeeId)
            .Distinct()
            .ToListAsync();

        if (employeeIds.Count == 0)
        {
            return;
        }

        var registers = await _salaryRegisterRepository.GetAll()
            .Where(r => !r.Locked && r.PayrollRunType == runType.Value && r.InPeriod == monthEnd && employeeIds.Contains(r.EmployeeId))
            .ToListAsync();

        foreach (var register in registers)
        {
            register.Locked = true;
            await _salaryRegisterRepository.UpdateAsync(register);
        }
    }

    /// <summary>
    /// Names the people who last moved each period's state, in one query rather than one per row.
    /// </summary>
    private async Task FillStateChangedByAsync(List<PayrollControlRecordDto> dtos, List<PayrollControlRecord> entities)
    {
        var userIds = entities.Where(e => e.StateChangedUserId.HasValue).Select(e => e.StateChangedUserId.Value).Distinct().ToList();
        if (userIds.Count == 0)
        {
            return;
        }

        var names = await _userRepository.GetAll()
            .Where(u => userIds.Contains(u.Id))
            .Select(u => new { u.Id, u.Name, u.Surname, u.UserName })
            .ToListAsync();

        foreach (var dto in dtos)
        {
            var userId = entities.First(e => e.Id == dto.Id).StateChangedUserId;
            var user = names.FirstOrDefault(u => u.Id == userId);
            if (user != null)
            {
                dto.StateChangedUserName = string.IsNullOrWhiteSpace(user.Name) ? user.UserName : (user.Name + " " + user.Surname).Trim();
            }
        }
    }

    private static PayrollControlRecord MapToEntity(CreateOrEditPayrollControlRecordDto input, PayrollControlRecord entity)
    {
        entity.PayrollAreaId = input.PayrollAreaId;
        entity.PayrollMonth = input.PayrollMonth;
        entity.PayrollYear = input.PayrollYear;
        entity.PeriodLocked = input.PeriodLocked;
        entity.PeriodClosed = input.PeriodClosed;

        // The two flags are what the engine reads, so a save that sets them directly must not leave the named state
        // saying something else.
        entity.PeriodState = entity.StateFromFlags();

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
            PeriodClosed = entity.PeriodClosed,
            PeriodState = entity.StateFromFlags(),
            StateChangedTime = entity.StateChangedTime
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
