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
using HCMv3.SystemSettings.Dto;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.SystemSettings;

/// <summary>
/// Application service for <see cref="ManageTimeSettings"/> (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.SysSettings.ManageTimeSettings</c>, 44 flat properties). Legacy
/// <c>OnDeleting</c> unconditionally throws -- ported to <see cref="DeleteTimeSettings"/>.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_SystemSettings_TimeSettings)]
public class ManageTimeSettingsAppService : HCMv3AppServiceBase, IManageTimeSettingsAppService
{
    private readonly IRepository<ManageTimeSettings> _repository;

    public ManageTimeSettingsAppService(IRepository<ManageTimeSettings> repository)
    {
        _repository = repository;
    }

    public async Task<PagedResultDto<TimeSettingsListDto>> GetTimeSettingsList(PagedAndSortedResultRequestDto input)
    {
        var query = _repository.GetAll();

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting.IsNullOrWhiteSpace() ? "Id" : input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<TimeSettingsListDto>(
            totalCount,
            items.Select(ToListDto).ToList());
    }

    public async Task<TimeSettingsEditDto> GetTimeSettingsForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);
        return ToEditDto(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_SystemSettings_TimeSettings_Create, AppPermissions.Pages_SystemSettings_TimeSettings_Edit)]
    public async Task CreateOrEditTimeSettings(TimeSettingsEditDto input)
    {
        if (input.Id == 0)
        {
            await CreateAsync(input);
        }
        else
        {
            await UpdateAsync(input);
        }
    }

    [AbpAuthorize(AppPermissions.Pages_SystemSettings_TimeSettings_Delete)]
    public Task DeleteTimeSettings(EntityDto input)
    {
        throw new UserFriendlyException(L("CannotDeleteSystemSettingsRecord"));
    }

    private async Task CreateAsync(TimeSettingsEditDto input)
    {
        var entity = MapToEntity(input, new ManageTimeSettings());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await _repository.InsertAsync(entity);
    }

    private async Task UpdateAsync(TimeSettingsEditDto input)
    {
        var entity = await _repository.GetAsync(input.Id);
        MapToEntity(input, entity);

        await _repository.UpdateAsync(entity);
    }

    private static ManageTimeSettings MapToEntity(TimeSettingsEditDto input, ManageTimeSettings entity)
    {
        entity.EarliestEvaluationDate = input.EarliestEvaluationDate;
        entity.FutureEvaluationInDays = input.FutureEvaluationInDays;
        entity.EnableRosterDuty = input.EnableRosterDuty;
        entity.ExcludePaidLeaveAccrualOnAbsent = input.ExcludePaidLeaveAccrualOnAbsent;
        entity.EnableTimeAttendanceIntegration = input.EnableTimeAttendanceIntegration;
        entity.EnableMiddleTierTimeEvents = input.EnableMiddleTierTimeEvents;
        entity.EnableAttendanceOnHoliday = input.EnableAttendanceOnHoliday;
        entity.EnableAttendanceOnWeekOff = input.EnableAttendanceOnWeekOff;
        entity.EnableAttendanceOnRestDay = input.EnableAttendanceOnRestDay;
        entity.HourlyLeavelogicEnable = input.HourlyLeavelogicEnable;
        entity.HourlyLeaveShouldSignIn = input.HourlyLeaveShouldSignIn;
        entity.HourlyLeaveShouldSignOut = input.HourlyLeaveShouldSignOut;
        entity.EnableTimeEngineAutorun = input.EnableTimeEngineAutorun;
        entity.LeaveDeductionStartMonth = input.LeaveDeductionStartMonth;
        entity.EnableOvertimeInTimeEngine = input.EnableOvertimeInTimeEngine;
        entity.TimeEventImportPath = input.TimeEventImportPath;
        entity.CountWeekoffOnPH = input.CountWeekoffOnPH;
        entity.RestrictUnpaidleaveIfAnnualleaveAvailable = input.RestrictUnpaidleaveIfAnnualleaveAvailable;
        entity.EnableIsTravelling = input.EnableIsTravelling;
        entity.AllowPaidleaveBackdateEntry = input.AllowPaidleaveBackdateEntry;
        entity.SickLeaveAttachMandatory = input.SickLeaveAttachMandatory;
        entity.EnableMySqlIntegration = input.EnableMySqlIntegration;
        entity.EnablePayFullWithPayroll = input.EnablePayFullWithPayroll;
        entity.EnableAutoReturnInConsecutiveLeave = input.EnableAutoReturnInConsecutiveLeave;
        entity.CountRestDayOnPH = input.CountRestDayOnPH;
        entity.EnableTimeAttIntegrationAPI = input.EnableTimeAttIntegrationAPI;
        entity.ForfeitRule = input.ForfeitRule;
        entity.RosterPlannerFrequency = input.RosterPlannerFrequency;
        entity.AllowFutureDatedReturnFromLeave = input.AllowFutureDatedReturnFromLeave;
        entity.EnableAutoHalfDayLeave = input.EnableAutoHalfDayLeave;
        entity.EnableSettleFractionalLeaveBalance = input.EnableSettleFractionalLeaveBalance;
        entity.EnableTimeRegisterApproval = input.EnableTimeRegisterApproval;
        entity.AutoApproveTimeLog = input.AutoApproveTimeLog;
        entity.TimeEventSyncBatchCount = input.TimeEventSyncBatchCount;
        entity.TimeEventSyncFreq = input.TimeEventSyncFreq;
        entity.MidTierSyncBatchCount = input.MidTierSyncBatchCount;
        entity.MidTierSyncFreq = input.MidTierSyncFreq;
        entity.EnableBookingQuotaInLeaveRequest = input.EnableBookingQuotaInLeaveRequest;
        entity.AllowDuplicateInTimeEvent = input.AllowDuplicateInTimeEvent;
        entity.UseActualTransInPlannedTimePolicy = input.UseActualTransInPlannedTimePolicy;
        entity.AbsenceViolationRuleId = input.AbsenceViolationRuleId;
        entity.AbsenceViolationEffectiveDate = input.AbsenceViolationEffectiveDate;
        entity.EnableGeoFence = input.EnableGeoFence;

        return entity;
    }

    private static TimeSettingsListDto ToListDto(ManageTimeSettings entity)
    {
        return new TimeSettingsListDto
        {
            Id = entity.Id,
            EarliestEvaluationDate = entity.EarliestEvaluationDate,
            FutureEvaluationInDays = entity.FutureEvaluationInDays,
            EnableRosterDuty = entity.EnableRosterDuty,
            ExcludePaidLeaveAccrualOnAbsent = entity.ExcludePaidLeaveAccrualOnAbsent,
            EnableTimeAttendanceIntegration = entity.EnableTimeAttendanceIntegration,
            EnableMiddleTierTimeEvents = entity.EnableMiddleTierTimeEvents,
            EnableAttendanceOnHoliday = entity.EnableAttendanceOnHoliday,
            EnableAttendanceOnWeekOff = entity.EnableAttendanceOnWeekOff,
            EnableAttendanceOnRestDay = entity.EnableAttendanceOnRestDay,
            HourlyLeavelogicEnable = entity.HourlyLeavelogicEnable,
            HourlyLeaveShouldSignIn = entity.HourlyLeaveShouldSignIn,
            HourlyLeaveShouldSignOut = entity.HourlyLeaveShouldSignOut,
            EnableTimeEngineAutorun = entity.EnableTimeEngineAutorun,
            LeaveDeductionStartMonth = entity.LeaveDeductionStartMonth,
            EnableOvertimeInTimeEngine = entity.EnableOvertimeInTimeEngine,
            TimeEventImportPath = entity.TimeEventImportPath,
            CountWeekoffOnPH = entity.CountWeekoffOnPH,
            RestrictUnpaidleaveIfAnnualleaveAvailable = entity.RestrictUnpaidleaveIfAnnualleaveAvailable,
            EnableIsTravelling = entity.EnableIsTravelling,
            AllowPaidleaveBackdateEntry = entity.AllowPaidleaveBackdateEntry,
            SickLeaveAttachMandatory = entity.SickLeaveAttachMandatory,
            EnableMySqlIntegration = entity.EnableMySqlIntegration,
            EnablePayFullWithPayroll = entity.EnablePayFullWithPayroll,
            EnableAutoReturnInConsecutiveLeave = entity.EnableAutoReturnInConsecutiveLeave,
            CountRestDayOnPH = entity.CountRestDayOnPH,
            EnableTimeAttIntegrationAPI = entity.EnableTimeAttIntegrationAPI,
            ForfeitRule = entity.ForfeitRule,
            RosterPlannerFrequency = entity.RosterPlannerFrequency,
            AllowFutureDatedReturnFromLeave = entity.AllowFutureDatedReturnFromLeave,
            EnableAutoHalfDayLeave = entity.EnableAutoHalfDayLeave,
            EnableSettleFractionalLeaveBalance = entity.EnableSettleFractionalLeaveBalance,
            EnableTimeRegisterApproval = entity.EnableTimeRegisterApproval,
            AutoApproveTimeLog = entity.AutoApproveTimeLog,
            TimeEventSyncBatchCount = entity.TimeEventSyncBatchCount,
            TimeEventSyncFreq = entity.TimeEventSyncFreq,
            MidTierSyncBatchCount = entity.MidTierSyncBatchCount,
            MidTierSyncFreq = entity.MidTierSyncFreq,
            EnableBookingQuotaInLeaveRequest = entity.EnableBookingQuotaInLeaveRequest,
            AllowDuplicateInTimeEvent = entity.AllowDuplicateInTimeEvent,
            UseActualTransInPlannedTimePolicy = entity.UseActualTransInPlannedTimePolicy,
            AbsenceViolationRuleId = entity.AbsenceViolationRuleId,
            AbsenceViolationEffectiveDate = entity.AbsenceViolationEffectiveDate,
            EnableGeoFence = entity.EnableGeoFence
        };
    }

    private static TimeSettingsEditDto ToEditDto(ManageTimeSettings entity)
    {
        return new TimeSettingsEditDto
        {
            Id = entity.Id,
            EarliestEvaluationDate = entity.EarliestEvaluationDate,
            FutureEvaluationInDays = entity.FutureEvaluationInDays,
            EnableRosterDuty = entity.EnableRosterDuty,
            ExcludePaidLeaveAccrualOnAbsent = entity.ExcludePaidLeaveAccrualOnAbsent,
            EnableTimeAttendanceIntegration = entity.EnableTimeAttendanceIntegration,
            EnableMiddleTierTimeEvents = entity.EnableMiddleTierTimeEvents,
            EnableAttendanceOnHoliday = entity.EnableAttendanceOnHoliday,
            EnableAttendanceOnWeekOff = entity.EnableAttendanceOnWeekOff,
            EnableAttendanceOnRestDay = entity.EnableAttendanceOnRestDay,
            HourlyLeavelogicEnable = entity.HourlyLeavelogicEnable,
            HourlyLeaveShouldSignIn = entity.HourlyLeaveShouldSignIn,
            HourlyLeaveShouldSignOut = entity.HourlyLeaveShouldSignOut,
            EnableTimeEngineAutorun = entity.EnableTimeEngineAutorun,
            LeaveDeductionStartMonth = entity.LeaveDeductionStartMonth,
            EnableOvertimeInTimeEngine = entity.EnableOvertimeInTimeEngine,
            TimeEventImportPath = entity.TimeEventImportPath,
            CountWeekoffOnPH = entity.CountWeekoffOnPH,
            RestrictUnpaidleaveIfAnnualleaveAvailable = entity.RestrictUnpaidleaveIfAnnualleaveAvailable,
            EnableIsTravelling = entity.EnableIsTravelling,
            AllowPaidleaveBackdateEntry = entity.AllowPaidleaveBackdateEntry,
            SickLeaveAttachMandatory = entity.SickLeaveAttachMandatory,
            EnableMySqlIntegration = entity.EnableMySqlIntegration,
            EnablePayFullWithPayroll = entity.EnablePayFullWithPayroll,
            EnableAutoReturnInConsecutiveLeave = entity.EnableAutoReturnInConsecutiveLeave,
            CountRestDayOnPH = entity.CountRestDayOnPH,
            EnableTimeAttIntegrationAPI = entity.EnableTimeAttIntegrationAPI,
            ForfeitRule = entity.ForfeitRule,
            RosterPlannerFrequency = entity.RosterPlannerFrequency,
            AllowFutureDatedReturnFromLeave = entity.AllowFutureDatedReturnFromLeave,
            EnableAutoHalfDayLeave = entity.EnableAutoHalfDayLeave,
            EnableSettleFractionalLeaveBalance = entity.EnableSettleFractionalLeaveBalance,
            EnableTimeRegisterApproval = entity.EnableTimeRegisterApproval,
            AutoApproveTimeLog = entity.AutoApproveTimeLog,
            TimeEventSyncBatchCount = entity.TimeEventSyncBatchCount,
            TimeEventSyncFreq = entity.TimeEventSyncFreq,
            MidTierSyncBatchCount = entity.MidTierSyncBatchCount,
            MidTierSyncFreq = entity.MidTierSyncFreq,
            EnableBookingQuotaInLeaveRequest = entity.EnableBookingQuotaInLeaveRequest,
            AllowDuplicateInTimeEvent = entity.AllowDuplicateInTimeEvent,
            UseActualTransInPlannedTimePolicy = entity.UseActualTransInPlannedTimePolicy,
            AbsenceViolationRuleId = entity.AbsenceViolationRuleId,
            AbsenceViolationEffectiveDate = entity.AbsenceViolationEffectiveDate,
            EnableGeoFence = entity.EnableGeoFence
        };
    }
}
