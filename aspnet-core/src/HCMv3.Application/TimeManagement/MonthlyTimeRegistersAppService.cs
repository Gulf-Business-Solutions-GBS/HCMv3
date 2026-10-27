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
/// Application service for <see cref="MonthlyTimeRegister"/> (ported from legacy XAF MonthlyTimeRegister
/// business object, Wave 4). Enforces unique (Employee, Month). The legacy <c>OnSaving</c>
/// exception-swallowing bug is NOT replicated -- see the entity class remarks.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_TimeManagement_MonthlyTimeRegisters)]
public class MonthlyTimeRegistersAppService : HCMv3AppServiceBase, IMonthlyTimeRegistersAppService
{
    private readonly IRepository<MonthlyTimeRegister> _repository;

    public MonthlyTimeRegistersAppService(IRepository<MonthlyTimeRegister> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<MonthlyTimeRegisterDto>> GetAll(GetAllMonthlyTimeRegistersInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .Include(m => m.Employee)
            .WhereIf(input.EmployeeId.HasValue, m => m.EmployeeId == input.EmployeeId.Value);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<MonthlyTimeRegisterDto>(totalCount, items.Select(ToDto).ToList());
    }

    public async Task<GetMonthlyTimeRegisterForEditOutput> GetMonthlyTimeRegisterForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetMonthlyTimeRegisterForEditOutput
        {
            MonthlyTimeRegister = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditMonthlyTimeRegisterDto input)
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

    [AbpAuthorize(AppPermissions.Pages_TimeManagement_MonthlyTimeRegisters_Create)]
    protected virtual async Task CreateAsync(CreateOrEditMonthlyTimeRegisterDto input)
    {
        var entity = MapToEntity(input, new MonthlyTimeRegister());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await ValidateAsync(entity);

        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_TimeManagement_MonthlyTimeRegisters_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditMonthlyTimeRegisterDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await ValidateAsync(entity);

        await _repository.UpdateAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_TimeManagement_MonthlyTimeRegisters_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    private static MonthlyTimeRegister MapToEntity(CreateOrEditMonthlyTimeRegisterDto input, MonthlyTimeRegister entity)
    {
        entity.EmployeeId = input.EmployeeId;
        entity.Month = input.Month.Date;
        entity.AnnualLeaveDays = input.AnnualLeaveDays;
        entity.UnpaidLeaveDays = input.UnpaidLeaveDays;
        entity.SickLeavePaidDays = input.SickLeavePaidDays;
        entity.SickLeaveUnpaidDays = input.SickLeaveUnpaidDays;
        entity.GratuityDays = input.GratuityDays;
        entity.PublicHolidays = input.PublicHolidays;
        entity.WeeklyOffs = input.WeeklyOffs;
        entity.RestDays = input.RestDays;
        entity.AnnualLeaveAccrualDays = input.AnnualLeaveAccrualDays;
        entity.AnnualLeaveOpeningBal = input.AnnualLeaveOpeningBal;
        entity.AnnualLeaveClosingBal = input.AnnualLeaveClosingBal;
        entity.TicketAccrualDays = input.TicketAccrualDays;
        entity.TicketAccrualOpeningBal = input.TicketAccrualOpeningBal;
        entity.TicketAccrualClosingBal = input.TicketAccrualClosingBal;
        entity.AirTicketPaidDays = input.AirTicketPaidDays;
        entity.GratuityPaidDays = input.GratuityPaidDays;
        entity.ActiveDays = input.ActiveDays;
        entity.AdvanceLeavePaids = input.AdvanceLeavePaids;
        entity.AdvanceTicketPaids = input.AdvanceTicketPaids;
        entity.PlannedWorkingMins = input.PlannedWorkingMins;
        entity.ActualWorkingMins = input.ActualWorkingMins;
        entity.ActualAbsenceMins = input.ActualAbsenceMins;
        entity.NormalDayOTMins = input.NormalDayOTMins;
        entity.RestDayOTMins = input.RestDayOTMins;
        entity.HolidayOTMins = input.HolidayOTMins;
        entity.WeekendOTMins = input.WeekendOTMins;
        entity.PlannedWorkingDays = input.PlannedWorkingDays;
        entity.ActualWorkingDays = input.ActualWorkingDays;
        entity.ExtraWorkingDays = input.ExtraWorkingDays;
        entity.LieuDays = input.LieuDays;
        entity.AdvLeaveRecoveryDays = input.AdvLeaveRecoveryDays;

        return entity;
    }

    /// <summary>
    /// Legacy source has no field-level validation for this entity beyond required Employee/Month; only
    /// basic required-field + unique (Employee, Month) validation is enforced here (the legacy
    /// exception-swallowing <c>OnSaving</c> bug is intentionally NOT replicated).
    /// </summary>
    private async Task ValidateAsync(MonthlyTimeRegister entity)
    {
        if (entity.EmployeeId <= 0)
        {
            throw new UserFriendlyException(L("EmployeeCannotBeBlank"));
        }

        var duplicate = await _repository.GetAll()
            .AnyAsync(m => m.Id != entity.Id
                           && m.EmployeeId == entity.EmployeeId
                           && m.Month == entity.Month);
        if (duplicate)
        {
            throw new UserFriendlyException(L("MonthlyTimeRegisterAlreadyExists", entity.Month.ToString("MM/yyyy")));
        }
    }

    private static MonthlyTimeRegisterDto ToDto(MonthlyTimeRegister entity)
    {
        return new MonthlyTimeRegisterDto
        {
            Id = entity.Id,
            EmployeeId = entity.EmployeeId,
            EmployeeCode = entity.Employee?.Code,
            EmployeeFullNameEn = entity.Employee?.FullNameEn,
            Month = entity.Month,
            AnnualLeaveDays = entity.AnnualLeaveDays,
            UnpaidLeaveDays = entity.UnpaidLeaveDays,
            SickLeavePaidDays = entity.SickLeavePaidDays,
            SickLeaveUnpaidDays = entity.SickLeaveUnpaidDays,
            GratuityDays = entity.GratuityDays,
            PublicHolidays = entity.PublicHolidays,
            WeeklyOffs = entity.WeeklyOffs,
            RestDays = entity.RestDays,
            AnnualLeaveAccrualDays = entity.AnnualLeaveAccrualDays,
            AnnualLeaveOpeningBal = entity.AnnualLeaveOpeningBal,
            AnnualLeaveClosingBal = entity.AnnualLeaveClosingBal,
            TicketAccrualDays = entity.TicketAccrualDays,
            TicketAccrualOpeningBal = entity.TicketAccrualOpeningBal,
            TicketAccrualClosingBal = entity.TicketAccrualClosingBal,
            AirTicketPaidDays = entity.AirTicketPaidDays,
            GratuityPaidDays = entity.GratuityPaidDays,
            ActiveDays = entity.ActiveDays,
            AdvanceLeavePaids = entity.AdvanceLeavePaids,
            AdvanceTicketPaids = entity.AdvanceTicketPaids,
            PlannedWorkingMins = entity.PlannedWorkingMins,
            ActualWorkingMins = entity.ActualWorkingMins,
            ActualAbsenceMins = entity.ActualAbsenceMins,
            NormalDayOTMins = entity.NormalDayOTMins,
            RestDayOTMins = entity.RestDayOTMins,
            HolidayOTMins = entity.HolidayOTMins,
            WeekendOTMins = entity.WeekendOTMins,
            PlannedWorkingDays = entity.PlannedWorkingDays,
            ActualWorkingDays = entity.ActualWorkingDays,
            ExtraWorkingDays = entity.ExtraWorkingDays,
            LieuDays = entity.LieuDays,
            AdvLeaveRecoveryDays = entity.AdvLeaveRecoveryDays
        };
    }

    private static CreateOrEditMonthlyTimeRegisterDto ToCreateOrEditDto(MonthlyTimeRegister entity)
    {
        return new CreateOrEditMonthlyTimeRegisterDto
        {
            Id = entity.Id,
            EmployeeId = entity.EmployeeId,
            Month = entity.Month,
            AnnualLeaveDays = entity.AnnualLeaveDays,
            UnpaidLeaveDays = entity.UnpaidLeaveDays,
            SickLeavePaidDays = entity.SickLeavePaidDays,
            SickLeaveUnpaidDays = entity.SickLeaveUnpaidDays,
            GratuityDays = entity.GratuityDays,
            PublicHolidays = entity.PublicHolidays,
            WeeklyOffs = entity.WeeklyOffs,
            RestDays = entity.RestDays,
            AnnualLeaveAccrualDays = entity.AnnualLeaveAccrualDays,
            AnnualLeaveOpeningBal = entity.AnnualLeaveOpeningBal,
            AnnualLeaveClosingBal = entity.AnnualLeaveClosingBal,
            TicketAccrualDays = entity.TicketAccrualDays,
            TicketAccrualOpeningBal = entity.TicketAccrualOpeningBal,
            TicketAccrualClosingBal = entity.TicketAccrualClosingBal,
            AirTicketPaidDays = entity.AirTicketPaidDays,
            GratuityPaidDays = entity.GratuityPaidDays,
            ActiveDays = entity.ActiveDays,
            AdvanceLeavePaids = entity.AdvanceLeavePaids,
            AdvanceTicketPaids = entity.AdvanceTicketPaids,
            PlannedWorkingMins = entity.PlannedWorkingMins,
            ActualWorkingMins = entity.ActualWorkingMins,
            ActualAbsenceMins = entity.ActualAbsenceMins,
            NormalDayOTMins = entity.NormalDayOTMins,
            RestDayOTMins = entity.RestDayOTMins,
            HolidayOTMins = entity.HolidayOTMins,
            WeekendOTMins = entity.WeekendOTMins,
            PlannedWorkingDays = entity.PlannedWorkingDays,
            ActualWorkingDays = entity.ActualWorkingDays,
            ExtraWorkingDays = entity.ExtraWorkingDays,
            LieuDays = entity.LieuDays,
            AdvLeaveRecoveryDays = entity.AdvLeaveRecoveryDays
        };
    }
}
