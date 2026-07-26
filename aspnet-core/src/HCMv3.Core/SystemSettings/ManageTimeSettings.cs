using System;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.SystemSettings;

/// <summary>
/// Tenant-wide time/attendance engine configuration (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.SysSettings.ManageTimeSettings</c>, 44 flat properties). Legacy
/// <c>OnDeleting</c> unconditionally throws -- enforced in
/// <c>ManageTimeSettingsAppService.DeleteTimeSettings</c>, not here. Legacy <c>PerformValidation</c> was empty
/// (no real business rule to replicate). Legacy <c>AbsenceViolationRule</c> navigation property
/// (<c>PayrollManagement.AbsenceViolationRule</c>) is ported here as a plain FK id
/// (<see cref="AbsenceViolationRuleId"/>), following this codebase's flat-FK convention.
/// </summary>
public class ManageTimeSettings : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual DateTime EarliestEvaluationDate { get; set; }

    public virtual int FutureEvaluationInDays { get; set; }

    public virtual bool EnableRosterDuty { get; set; }

    public virtual bool ExcludePaidLeaveAccrualOnAbsent { get; set; }

    public virtual bool EnableTimeAttendanceIntegration { get; set; }

    public virtual bool EnableMiddleTierTimeEvents { get; set; }

    public virtual bool EnableAttendanceOnHoliday { get; set; }

    public virtual bool EnableAttendanceOnWeekOff { get; set; }

    public virtual bool EnableAttendanceOnRestDay { get; set; }

    public virtual bool HourlyLeavelogicEnable { get; set; }

    public virtual bool HourlyLeaveShouldSignIn { get; set; }

    public virtual bool HourlyLeaveShouldSignOut { get; set; }

    public virtual bool EnableTimeEngineAutorun { get; set; }

    public virtual int LeaveDeductionStartMonth { get; set; }

    public virtual bool EnableOvertimeInTimeEngine { get; set; }

    public virtual string TimeEventImportPath { get; set; }

    public virtual bool CountWeekoffOnPH { get; set; }

    public virtual bool RestrictUnpaidleaveIfAnnualleaveAvailable { get; set; }

    public virtual bool EnableIsTravelling { get; set; }

    public virtual bool AllowPaidleaveBackdateEntry { get; set; }

    public virtual bool SickLeaveAttachMandatory { get; set; }

    public virtual bool EnableMySqlIntegration { get; set; }

    public virtual bool EnablePayFullWithPayroll { get; set; }

    public virtual bool EnableAutoReturnInConsecutiveLeave { get; set; }

    public virtual bool CountRestDayOnPH { get; set; }

    public virtual bool EnableTimeAttIntegrationAPI { get; set; }

    public virtual ForfeitRule ForfeitRule { get; set; }

    public virtual RosterPlannerFrequency RosterPlannerFrequency { get; set; }

    public virtual bool AllowFutureDatedReturnFromLeave { get; set; }

    public virtual bool EnableAutoHalfDayLeave { get; set; }

    public virtual bool EnableSettleFractionalLeaveBalance { get; set; }

    public virtual bool EnableTimeRegisterApproval { get; set; }

    public virtual bool AutoApproveTimeLog { get; set; }

    public virtual int TimeEventSyncBatchCount { get; set; }

    public virtual int TimeEventSyncFreq { get; set; }

    public virtual int MidTierSyncBatchCount { get; set; }

    public virtual int MidTierSyncFreq { get; set; }

    public virtual bool EnableBookingQuotaInLeaveRequest { get; set; }

    public virtual bool AllowDuplicateInTimeEvent { get; set; }

    public virtual bool UseActualTransInPlannedTimePolicy { get; set; }

    public virtual int? AbsenceViolationRuleId { get; set; }

    public virtual DateTime AbsenceViolationEffectiveDate { get; set; }

    public virtual bool EnableGeoFence { get; set; }
}
