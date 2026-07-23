using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Linq.Extensions;
using Abp.UI;
using HCMv3.Authorization;
using HCMv3.TimeManagement.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.TimeManagement;

/// <summary>
/// Application service for <see cref="TimeRegister"/> (ported from legacy XAF TimeRegister business object,
/// Wave 4). No calculation logic to replicate (none exists in the legacy source for this entity -- the
/// day-by-day calculation engine lives elsewhere, out of scope). Only required-field + unique
/// (Employee, DayDate) validation is ported. See the entity class remarks for the dropped WfRequest FK and
/// EmployeeAbsenceQuota reversal on delete.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_TimeManagement_TimeRegisters)]
public class TimeRegistersAppService : HCMv3AppServiceBase, ITimeRegistersAppService
{
    private readonly IRepository<TimeRegister> _repository;

    public TimeRegistersAppService(IRepository<TimeRegister> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<TimeRegisterDto>> GetAll(GetAllTimeRegistersInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .Include(t => t.Employee)
            .Include(t => t.DutyPeriod)
            .Include(t => t.Duty)
            .Include(t => t.TimePolicy)
            .Include(t => t.OvertimeType)
            .WhereIf(input.EmployeeId.HasValue, t => t.EmployeeId == input.EmployeeId.Value)
            .WhereIf(input.DayDateFrom.HasValue, t => t.DayDate >= input.DayDateFrom.Value)
            .WhereIf(input.DayDateTo.HasValue, t => t.DayDate <= input.DayDateTo.Value);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<TimeRegisterDto>(totalCount, items.Select(ToDto).ToList());
    }

    public async Task<GetTimeRegisterForEditOutput> GetTimeRegisterForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetTimeRegisterForEditOutput
        {
            TimeRegister = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditTimeRegisterDto input)
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

    [AbpAuthorize(AppPermissions.Pages_TimeManagement_TimeRegisters_Create)]
    protected virtual async Task CreateAsync(CreateOrEditTimeRegisterDto input)
    {
        var entity = MapToEntity(input, new TimeRegister());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await ValidateAsync(entity);

        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_TimeManagement_TimeRegisters_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditTimeRegisterDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await ValidateAsync(entity);

        await _repository.UpdateAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_TimeManagement_TimeRegisters_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    private static TimeRegister MapToEntity(CreateOrEditTimeRegisterDto input, TimeRegister entity)
    {
        entity.EmployeeId = input.EmployeeId;
        entity.DayDate = input.DayDate.Date;
        entity.DutyPeriodId = input.DutyPeriodId;
        entity.DutyId = input.DutyId;
        entity.TimePolicyId = input.TimePolicyId;
        entity.ShiftStartTime1 = input.ShiftStartTime1;
        entity.BreakStartTime = input.BreakStartTime;
        entity.BreakEndTime = input.BreakEndTime;
        entity.ShiftEndTime1 = input.ShiftEndTime1;
        entity.ActualIn = input.ActualIn;
        entity.BreakOut = input.BreakOut;
        entity.BreakIn = input.BreakIn;
        entity.ActualOut = input.ActualOut;
        entity.ActualStartTime1 = input.ActualStartTime1;
        entity.ActualBreakStartTime = input.ActualBreakStartTime;
        entity.ActualBreakEndTime = input.ActualBreakEndTime;
        entity.ActualEndTime1 = input.ActualEndTime1;
        entity.PlannedWorkingMins = input.PlannedWorkingMins;
        entity.ActualWorkingMins = input.ActualWorkingMins;
        entity.TotalWorkingMins = input.TotalWorkingMins;
        entity.ActualAbsenceMins = input.ActualAbsenceMins;
        entity.LeaveDay = input.LeaveDay;
        entity.LeaveTypeId = input.LeaveTypeId;
        entity.LeaveMinutes = input.LeaveMinutes;
        entity.AnnualLeaveDay = input.AnnualLeaveDay;
        entity.UnpaidLeaveDay = input.UnpaidLeaveDay;
        entity.SickLeavePaidDay = input.SickLeavePaidDay;
        entity.SickLeaveUnpaidDay = input.SickLeaveUnpaidDay;
        entity.PublicHoliday = input.PublicHoliday;
        entity.WeeklyOff = input.WeeklyOff;
        entity.RestDay = input.RestDay;
        entity.AnnualLeaveAccrualPerDay = input.AnnualLeaveAccrualPerDay;
        entity.TicketAccrualPerDay = input.TicketAccrualPerDay;
        entity.AirTicketPaidDay = input.AirTicketPaidDay;
        entity.ActiveDay = input.ActiveDay;
        entity.InactiveDay = input.InactiveDay;
        entity.OvertimeMinutes = input.OvertimeMinutes;
        entity.OvertimeTypeId = input.OvertimeTypeId;
        entity.PlannedWorkingDay = input.PlannedWorkingDay;
        entity.ActualWorkingDay = input.ActualWorkingDay;
        entity.AttendanceDay = input.AttendanceDay;
        entity.AttendanceTypeId = input.AttendanceTypeId;
        entity.AttendanceMinutes = input.AttendanceMinutes;
        entity.AbsentDay = input.AbsentDay;
        entity.LateIn = input.LateIn;
        entity.DeductionLateIn = input.DeductionLateIn;
        entity.EarlyOut = input.EarlyOut;
        entity.DeductionEarlyOut = input.DeductionEarlyOut;
        entity.BreakEarlyOut = input.BreakEarlyOut;
        entity.BreakLateIn = input.BreakLateIn;
        entity.BreakDeductionEarlyOut = input.BreakDeductionEarlyOut;
        entity.BreakDeductionLateIn = input.BreakDeductionLateIn;
        entity.DedLateInEarlyOutMins = input.DedLateInEarlyOutMins;
        entity.DedLateInEarlyOutDay = input.DedLateInEarlyOutDay;
        entity.DedAfterMonthlyTol = input.DedAfterMonthlyTol;
        entity.IncludeInLeavePay = input.IncludeInLeavePay;
        entity.LeaveAdjDay = input.LeaveAdjDay;
        entity.ApprovedActualWorkingDay = input.ApprovedActualWorkingDay;
        entity.ApprovedAbsentDay = input.ApprovedAbsentDay;
        entity.ApprovedDedLateInEarlyOutMins = input.ApprovedDedLateInEarlyOutMins;
        entity.Status = input.Status;
        entity.Remarks = input.Remarks;

        return entity;
    }

    /// <summary>
    /// Legacy source has no calculation logic for this entity, so only basic required-field + unique
    /// (Employee, DayDate) validation is enforced here.
    /// </summary>
    private async Task ValidateAsync(TimeRegister entity)
    {
        if (entity.EmployeeId <= 0)
        {
            throw new UserFriendlyException(L("EmployeeCannotBeBlank"));
        }

        var duplicate = await _repository.GetAll()
            .AnyAsync(t => t.Id != entity.Id
                           && t.EmployeeId == entity.EmployeeId
                           && t.DayDate == entity.DayDate);
        if (duplicate)
        {
            throw new UserFriendlyException(L("TimeRegisterAlreadyExists", entity.DayDate.ToString("dd/MM/yyyy")));
        }
    }

    private static TimeRegisterDto ToDto(TimeRegister entity)
    {
        return new TimeRegisterDto
        {
            Id = entity.Id,
            EmployeeId = entity.EmployeeId,
            EmployeeCode = entity.Employee?.Code,
            EmployeeFullNameEn = entity.Employee?.FullNameEn,
            DayDate = entity.DayDate,
            DutyPeriodId = entity.DutyPeriodId,
            DutyPeriodName = entity.DutyPeriod?.NameEn,
            DutyId = entity.DutyId,
            DutyName = entity.Duty?.NameEn,
            TimePolicyId = entity.TimePolicyId,
            TimePolicyName = entity.TimePolicy?.NameEn,
            ShiftStartTime1 = entity.ShiftStartTime1,
            BreakStartTime = entity.BreakStartTime,
            BreakEndTime = entity.BreakEndTime,
            ShiftEndTime1 = entity.ShiftEndTime1,
            ActualIn = entity.ActualIn,
            BreakOut = entity.BreakOut,
            BreakIn = entity.BreakIn,
            ActualOut = entity.ActualOut,
            ActualStartTime1 = entity.ActualStartTime1,
            ActualBreakStartTime = entity.ActualBreakStartTime,
            ActualBreakEndTime = entity.ActualBreakEndTime,
            ActualEndTime1 = entity.ActualEndTime1,
            PlannedWorkingMins = entity.PlannedWorkingMins,
            ActualWorkingMins = entity.ActualWorkingMins,
            TotalWorkingMins = entity.TotalWorkingMins,
            ActualAbsenceMins = entity.ActualAbsenceMins,
            LeaveDay = entity.LeaveDay,
            LeaveTypeId = entity.LeaveTypeId,
            LeaveMinutes = entity.LeaveMinutes,
            AnnualLeaveDay = entity.AnnualLeaveDay,
            UnpaidLeaveDay = entity.UnpaidLeaveDay,
            SickLeavePaidDay = entity.SickLeavePaidDay,
            SickLeaveUnpaidDay = entity.SickLeaveUnpaidDay,
            PublicHoliday = entity.PublicHoliday,
            WeeklyOff = entity.WeeklyOff,
            RestDay = entity.RestDay,
            AnnualLeaveAccrualPerDay = entity.AnnualLeaveAccrualPerDay,
            TicketAccrualPerDay = entity.TicketAccrualPerDay,
            AirTicketPaidDay = entity.AirTicketPaidDay,
            ActiveDay = entity.ActiveDay,
            InactiveDay = entity.InactiveDay,
            OvertimeMinutes = entity.OvertimeMinutes,
            OvertimeTypeId = entity.OvertimeTypeId,
            OvertimeTypeName = entity.OvertimeType?.NameEn,
            PlannedWorkingDay = entity.PlannedWorkingDay,
            ActualWorkingDay = entity.ActualWorkingDay,
            AttendanceDay = entity.AttendanceDay,
            AttendanceTypeId = entity.AttendanceTypeId,
            AttendanceMinutes = entity.AttendanceMinutes,
            AbsentDay = entity.AbsentDay,
            LateIn = entity.LateIn,
            DeductionLateIn = entity.DeductionLateIn,
            EarlyOut = entity.EarlyOut,
            DeductionEarlyOut = entity.DeductionEarlyOut,
            BreakEarlyOut = entity.BreakEarlyOut,
            BreakLateIn = entity.BreakLateIn,
            BreakDeductionEarlyOut = entity.BreakDeductionEarlyOut,
            BreakDeductionLateIn = entity.BreakDeductionLateIn,
            DedLateInEarlyOutMins = entity.DedLateInEarlyOutMins,
            DedLateInEarlyOutDay = entity.DedLateInEarlyOutDay,
            DedAfterMonthlyTol = entity.DedAfterMonthlyTol,
            IncludeInLeavePay = entity.IncludeInLeavePay,
            LeaveAdjDay = entity.LeaveAdjDay,
            ApprovedActualWorkingDay = entity.ApprovedActualWorkingDay,
            ApprovedAbsentDay = entity.ApprovedAbsentDay,
            ApprovedDedLateInEarlyOutMins = entity.ApprovedDedLateInEarlyOutMins,
            Status = entity.Status,
            Remarks = entity.Remarks
        };
    }

    private static CreateOrEditTimeRegisterDto ToCreateOrEditDto(TimeRegister entity)
    {
        return new CreateOrEditTimeRegisterDto
        {
            Id = entity.Id,
            EmployeeId = entity.EmployeeId,
            DayDate = entity.DayDate,
            DutyPeriodId = entity.DutyPeriodId,
            DutyId = entity.DutyId,
            TimePolicyId = entity.TimePolicyId,
            ShiftStartTime1 = entity.ShiftStartTime1,
            BreakStartTime = entity.BreakStartTime,
            BreakEndTime = entity.BreakEndTime,
            ShiftEndTime1 = entity.ShiftEndTime1,
            ActualIn = entity.ActualIn,
            BreakOut = entity.BreakOut,
            BreakIn = entity.BreakIn,
            ActualOut = entity.ActualOut,
            ActualStartTime1 = entity.ActualStartTime1,
            ActualBreakStartTime = entity.ActualBreakStartTime,
            ActualBreakEndTime = entity.ActualBreakEndTime,
            ActualEndTime1 = entity.ActualEndTime1,
            PlannedWorkingMins = entity.PlannedWorkingMins,
            ActualWorkingMins = entity.ActualWorkingMins,
            TotalWorkingMins = entity.TotalWorkingMins,
            ActualAbsenceMins = entity.ActualAbsenceMins,
            LeaveDay = entity.LeaveDay,
            LeaveTypeId = entity.LeaveTypeId,
            LeaveMinutes = entity.LeaveMinutes,
            AnnualLeaveDay = entity.AnnualLeaveDay,
            UnpaidLeaveDay = entity.UnpaidLeaveDay,
            SickLeavePaidDay = entity.SickLeavePaidDay,
            SickLeaveUnpaidDay = entity.SickLeaveUnpaidDay,
            PublicHoliday = entity.PublicHoliday,
            WeeklyOff = entity.WeeklyOff,
            RestDay = entity.RestDay,
            AnnualLeaveAccrualPerDay = entity.AnnualLeaveAccrualPerDay,
            TicketAccrualPerDay = entity.TicketAccrualPerDay,
            AirTicketPaidDay = entity.AirTicketPaidDay,
            ActiveDay = entity.ActiveDay,
            InactiveDay = entity.InactiveDay,
            OvertimeMinutes = entity.OvertimeMinutes,
            OvertimeTypeId = entity.OvertimeTypeId,
            PlannedWorkingDay = entity.PlannedWorkingDay,
            ActualWorkingDay = entity.ActualWorkingDay,
            AttendanceDay = entity.AttendanceDay,
            AttendanceTypeId = entity.AttendanceTypeId,
            AttendanceMinutes = entity.AttendanceMinutes,
            AbsentDay = entity.AbsentDay,
            LateIn = entity.LateIn,
            DeductionLateIn = entity.DeductionLateIn,
            EarlyOut = entity.EarlyOut,
            DeductionEarlyOut = entity.DeductionEarlyOut,
            BreakEarlyOut = entity.BreakEarlyOut,
            BreakLateIn = entity.BreakLateIn,
            BreakDeductionEarlyOut = entity.BreakDeductionEarlyOut,
            BreakDeductionLateIn = entity.BreakDeductionLateIn,
            DedLateInEarlyOutMins = entity.DedLateInEarlyOutMins,
            DedLateInEarlyOutDay = entity.DedLateInEarlyOutDay,
            DedAfterMonthlyTol = entity.DedAfterMonthlyTol,
            IncludeInLeavePay = entity.IncludeInLeavePay,
            LeaveAdjDay = entity.LeaveAdjDay,
            ApprovedActualWorkingDay = entity.ApprovedActualWorkingDay,
            ApprovedAbsentDay = entity.ApprovedAbsentDay,
            ApprovedDedLateInEarlyOutMins = entity.ApprovedDedLateInEarlyOutMins,
            Status = entity.Status,
            Remarks = entity.Remarks
        };
    }
}
