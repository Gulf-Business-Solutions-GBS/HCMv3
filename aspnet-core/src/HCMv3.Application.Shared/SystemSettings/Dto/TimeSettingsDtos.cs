using System;
using Abp.Application.Services.Dto;

namespace HCMv3.SystemSettings.Dto;

public class TimeSettingsListDto : EntityDto
{
    public DateTime EarliestEvaluationDate { get; set; }

    public int FutureEvaluationInDays { get; set; }

    public bool EnableRosterDuty { get; set; }

    public bool ExcludePaidLeaveAccrualOnAbsent { get; set; }

    public bool EnableTimeAttendanceIntegration { get; set; }

    public bool EnableMiddleTierTimeEvents { get; set; }

    public bool EnableAttendanceOnHoliday { get; set; }

    public bool EnableAttendanceOnWeekOff { get; set; }

    public bool EnableAttendanceOnRestDay { get; set; }

    public bool HourlyLeavelogicEnable { get; set; }

    public bool HourlyLeaveShouldSignIn { get; set; }

    public bool HourlyLeaveShouldSignOut { get; set; }

    public bool EnableTimeEngineAutorun { get; set; }

    public int LeaveDeductionStartMonth { get; set; }

    public bool EnableOvertimeInTimeEngine { get; set; }

    public string TimeEventImportPath { get; set; }

    public bool CountWeekoffOnPH { get; set; }

    public bool RestrictUnpaidleaveIfAnnualleaveAvailable { get; set; }

    public bool EnableIsTravelling { get; set; }

    public bool AllowPaidleaveBackdateEntry { get; set; }

    public bool SickLeaveAttachMandatory { get; set; }

    public bool EnableMySqlIntegration { get; set; }

    public bool EnablePayFullWithPayroll { get; set; }

    public bool EnableAutoReturnInConsecutiveLeave { get; set; }

    public bool CountRestDayOnPH { get; set; }

    public bool EnableTimeAttIntegrationAPI { get; set; }

    public ForfeitRule ForfeitRule { get; set; }

    public RosterPlannerFrequency RosterPlannerFrequency { get; set; }

    public bool AllowFutureDatedReturnFromLeave { get; set; }

    public bool EnableAutoHalfDayLeave { get; set; }

    public bool EnableSettleFractionalLeaveBalance { get; set; }

    public bool EnableTimeRegisterApproval { get; set; }

    public bool AutoApproveTimeLog { get; set; }

    public int TimeEventSyncBatchCount { get; set; }

    public int TimeEventSyncFreq { get; set; }

    public int MidTierSyncBatchCount { get; set; }

    public int MidTierSyncFreq { get; set; }

    public bool EnableBookingQuotaInLeaveRequest { get; set; }

    public bool AllowDuplicateInTimeEvent { get; set; }

    public bool UseActualTransInPlannedTimePolicy { get; set; }

    public int? AbsenceViolationRuleId { get; set; }

    public DateTime AbsenceViolationEffectiveDate { get; set; }

    public bool EnableGeoFence { get; set; }
}

public class TimeSettingsEditDto : EntityDto
{
    public DateTime EarliestEvaluationDate { get; set; }

    public int FutureEvaluationInDays { get; set; }

    public bool EnableRosterDuty { get; set; }

    public bool ExcludePaidLeaveAccrualOnAbsent { get; set; }

    public bool EnableTimeAttendanceIntegration { get; set; }

    public bool EnableMiddleTierTimeEvents { get; set; }

    public bool EnableAttendanceOnHoliday { get; set; }

    public bool EnableAttendanceOnWeekOff { get; set; }

    public bool EnableAttendanceOnRestDay { get; set; }

    public bool HourlyLeavelogicEnable { get; set; }

    public bool HourlyLeaveShouldSignIn { get; set; }

    public bool HourlyLeaveShouldSignOut { get; set; }

    public bool EnableTimeEngineAutorun { get; set; }

    public int LeaveDeductionStartMonth { get; set; }

    public bool EnableOvertimeInTimeEngine { get; set; }

    public string TimeEventImportPath { get; set; }

    public bool CountWeekoffOnPH { get; set; }

    public bool RestrictUnpaidleaveIfAnnualleaveAvailable { get; set; }

    public bool EnableIsTravelling { get; set; }

    public bool AllowPaidleaveBackdateEntry { get; set; }

    public bool SickLeaveAttachMandatory { get; set; }

    public bool EnableMySqlIntegration { get; set; }

    public bool EnablePayFullWithPayroll { get; set; }

    public bool EnableAutoReturnInConsecutiveLeave { get; set; }

    public bool CountRestDayOnPH { get; set; }

    public bool EnableTimeAttIntegrationAPI { get; set; }

    public ForfeitRule ForfeitRule { get; set; }

    public RosterPlannerFrequency RosterPlannerFrequency { get; set; }

    public bool AllowFutureDatedReturnFromLeave { get; set; }

    public bool EnableAutoHalfDayLeave { get; set; }

    public bool EnableSettleFractionalLeaveBalance { get; set; }

    public bool EnableTimeRegisterApproval { get; set; }

    public bool AutoApproveTimeLog { get; set; }

    public int TimeEventSyncBatchCount { get; set; }

    public int TimeEventSyncFreq { get; set; }

    public int MidTierSyncBatchCount { get; set; }

    public int MidTierSyncFreq { get; set; }

    public bool EnableBookingQuotaInLeaveRequest { get; set; }

    public bool AllowDuplicateInTimeEvent { get; set; }

    public bool UseActualTransInPlannedTimePolicy { get; set; }

    public int? AbsenceViolationRuleId { get; set; }

    public DateTime AbsenceViolationEffectiveDate { get; set; }

    public bool EnableGeoFence { get; set; }
}
