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
/// Application service for <see cref="YearlyTimeRegister"/> (ported from legacy XAF YearlyTimeRegister
/// business object, Wave 4). Enforces unique (Employee, Year). The legacy <c>OnSaving</c>
/// exception-swallowing bug is NOT replicated -- see the entity class remarks.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_TimeManagement_YearlyTimeRegisters)]
public class YearlyTimeRegistersAppService : HCMv3AppServiceBase, IYearlyTimeRegistersAppService
{
    private readonly IRepository<YearlyTimeRegister> _repository;

    public YearlyTimeRegistersAppService(IRepository<YearlyTimeRegister> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<YearlyTimeRegisterDto>> GetAll(GetAllYearlyTimeRegistersInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .Include(y => y.Employee)
            .WhereIf(input.EmployeeId.HasValue, y => y.EmployeeId == input.EmployeeId.Value);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<YearlyTimeRegisterDto>(totalCount, items.Select(ToDto).ToList());
    }

    public async Task<GetYearlyTimeRegisterForEditOutput> GetYearlyTimeRegisterForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetYearlyTimeRegisterForEditOutput
        {
            YearlyTimeRegister = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditYearlyTimeRegisterDto input)
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

    [AbpAuthorize(AppPermissions.Pages_TimeManagement_YearlyTimeRegisters_Create)]
    protected virtual async Task CreateAsync(CreateOrEditYearlyTimeRegisterDto input)
    {
        var entity = MapToEntity(input, new YearlyTimeRegister());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await ValidateAsync(entity);

        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_TimeManagement_YearlyTimeRegisters_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditYearlyTimeRegisterDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await ValidateAsync(entity);

        await _repository.UpdateAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_TimeManagement_YearlyTimeRegisters_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    private static YearlyTimeRegister MapToEntity(CreateOrEditYearlyTimeRegisterDto input, YearlyTimeRegister entity)
    {
        entity.EmployeeId = input.EmployeeId;
        entity.Year = input.Year.Date;
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
    /// Only basic required-field + unique (Employee, Year) validation is enforced (the legacy
    /// exception-swallowing <c>OnSaving</c> bug is intentionally NOT replicated).
    /// </summary>
    private async Task ValidateAsync(YearlyTimeRegister entity)
    {
        if (entity.EmployeeId <= 0)
        {
            throw new UserFriendlyException(L("EmployeeCannotBeBlank"));
        }

        var duplicate = await _repository.GetAll()
            .AnyAsync(y => y.Id != entity.Id
                           && y.EmployeeId == entity.EmployeeId
                           && y.Year == entity.Year);
        if (duplicate)
        {
            throw new UserFriendlyException(L("YearlyTimeRegisterAlreadyExists", entity.Year.ToString("yyyy")));
        }
    }

    private static YearlyTimeRegisterDto ToDto(YearlyTimeRegister entity)
    {
        return new YearlyTimeRegisterDto
        {
            Id = entity.Id,
            EmployeeId = entity.EmployeeId,
            EmployeeCode = entity.Employee?.Code,
            EmployeeFullNameEn = entity.Employee?.FullNameEn,
            Year = entity.Year,
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

    private static CreateOrEditYearlyTimeRegisterDto ToCreateOrEditDto(YearlyTimeRegister entity)
    {
        return new CreateOrEditYearlyTimeRegisterDto
        {
            Id = entity.Id,
            EmployeeId = entity.EmployeeId,
            Year = entity.Year,
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
