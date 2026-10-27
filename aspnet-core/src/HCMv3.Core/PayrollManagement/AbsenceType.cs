using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.PayrollManagement;

/// <summary>
/// Leave Type master (ported from legacy XAF <c>HCM10.Module.BusinessObjects.PayrollManagement.AbsenceType</c>,
/// XAF display name "LeaveType"). Large flat configuration entity — ported faithfully field-by-field, with
/// the following DELIBERATE SIMPLIFICATIONS:
/// <list type="bullet">
/// <item><description><c>WageTypeId</c> is a plain nullable FK column with no navigation property: the
/// legacy <c>WageType</c> entity doesn't exist yet in this port (deferred to a later Payroll wave) and the
/// legacy <c>DataSourceProperty</c>-filtered picklist is an Angular-side concern, not ported here.</description></item>
/// <item><description>The legacy <c>NotificationConfig</c> FK is not ported — no such lookup exists in this
/// port.</description></item>
/// </list>
/// </summary>
public class AbsenceType : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    [StringLength(SimpleLookupConsts.MaxCodeLength)]
    public virtual string Code { get; set; }

    [Required]
    [StringLength(SimpleLookupConsts.MaxNameLength)]
    public virtual string NameEn { get; set; }

    [Required]
    [StringLength(SimpleLookupConsts.MaxNameLength)]
    public virtual string NameAr { get; set; }

    public virtual bool IsActive { get; set; }

    public virtual bool IsPaid { get; set; }

    public virtual bool IncludeWeekend { get; set; }

    public virtual bool IncludeRestDay { get; set; }

    public virtual bool IncludePublicHoliday { get; set; }

    public virtual LeaveBasisPeriod LeaveBasisPeriod { get; set; }

    /// <summary>
    /// Leave amount calculation basis when <see cref="LeaveBasisPeriod"/> is <see cref="PayrollManagement.LeaveBasisPeriod.FixedDays"/>.
    /// </summary>
    public virtual double LeaveBasisFixedDays { get; set; }

    public virtual bool HaveQuotaAccrual { get; set; }

    public virtual AccrualPeriod AccrualPeriod { get; set; }

    public virtual CustomStartMonth CustomStartMonth { get; set; }

    /// <summary>
    /// Minimum active days in service to take this type of leave (e.g. Hajj leave only after 2 years).
    /// </summary>
    public virtual double MinActiveDaysInService { get; set; }

    public virtual bool CanApplyAgain { get; set; }

    public virtual double CanApplyAfterDays { get; set; }

    public virtual int CanApplyBeforeDays { get; set; }

    /// <summary>
    /// Reinitialize after Accrual Period.
    /// </summary>
    public virtual bool ReInitialize { get; set; }

    /// <summary>
    /// Reinitialize after how many Accrual Periods.
    /// </summary>
    public virtual int ReInitializeAfter { get; set; }

    public virtual bool ApplyLeaveInAdvance { get; set; }

    public virtual int ApplyLeaveInAdvanceDays { get; set; }

    public virtual bool ApplyMaxLeave { get; set; }

    public virtual int MaxLimitInDays { get; set; }

    public virtual bool CreateUnpaidAboveMaxLimitDays { get; set; }

    public virtual bool ApplyMinLeave { get; set; }

    public virtual int MinLimitInDays { get; set; }

    /// <summary>
    /// Type of duration used to validate leave minutes (hour-based leave config).
    /// </summary>
    public virtual ResetFrequency ResetFrequency { get; set; }

    /// <summary>
    /// Total leave minutes allowed in one <see cref="ResetFrequency"/> duration.
    /// </summary>
    public virtual int TotalMinutes { get; set; }

    /// <summary>
    /// Maximum minutes allowed in one transaction.
    /// </summary>
    public virtual int MaxMinutes { get; set; }

    /// <summary>
    /// Maximum number of transactions in the selected <see cref="ResetFrequency"/> duration.
    /// </summary>
    public virtual int MaxCount { get; set; }

    /// <summary>
    /// FK to the (not-yet-ported) WageType entity. Deferred: no navigation property until a later Payroll
    /// wave introduces WageType. See class remarks.
    /// </summary>
    public virtual int? WageTypeId { get; set; }

    public virtual RestrictType PolicyRestrictType { get; set; }

    public virtual bool IncludeInWorkflow { get; set; }

    /// <summary>
    /// Whether a return date is required for payroll calculation.
    /// </summary>
    public virtual bool IsReturnDateRequired { get; set; }

    public virtual LeaveGender LeaveGender { get; set; }

    public virtual LeaveReligion LeaveReligion { get; set; }

    public virtual bool ExcludePaidLeaveAccrual { get; set; }

    /// <summary>
    /// Allows assigning the same leave quota to different leave types.
    /// </summary>
    public virtual QuotaType QuotaType { get; set; }

    public virtual int AllowBackDatedDays { get; set; }

    public virtual bool IncludeInSalaryProfile { get; set; }

    public virtual double TotalDaysAllowed { get; set; }

    public virtual LeaveEntitlement LeaveEntitlement { get; set; }

    public virtual bool AllowDuringProbation { get; set; }

    public virtual bool AllowToReturnEarly { get; set; }

    public virtual bool AllowInTerminationNoticeOnly { get; set; }

    public virtual bool IsAttachmentRequired { get; set; }

    public virtual bool AllowPartialLeave { get; set; }

    /// <summary>
    /// Legacy <c>LeaveDurationType</c>: <c>Partial</c> makes the start and end time mandatory, <c>FullDay</c>
    /// refuses them, <c>All</c> leaves it to <see cref="AllowPartialLeave"/>.
    /// </summary>
    public virtual LeaveDurationType LeaveDurationType { get; set; }

    /// <summary>
    /// How far ahead of today a leave of this type may START, in days. Zero means no limit. Not in legacy,
    /// which only had the opposite rules (<see cref="CanApplyBeforeDays"/>, <see cref="ApplyLeaveInAdvanceDays"/>:
    /// minimum notice).
    /// </summary>
    public virtual int MaxFutureDays { get; set; }

    /// <summary>
    /// The employee must tick "I accept the leave policy" before a leave of this type can be submitted. The
    /// policy document itself is an attachment on the leave type (entity type <c>AbsenceTypePolicy</c>), not a
    /// column, so the file lives in the tenant's file store like every other document.
    /// </summary>
    public virtual bool RequirePolicyAcceptance { get; set; }

    public virtual bool SendNotification { get; set; }

    public virtual bool DontAllowToStartOnSunday { get; set; }

    public virtual bool DontAllowToStartOnMonday { get; set; }

    public virtual bool DontAllowToStartOnTuesday { get; set; }

    public virtual bool DontAllowToStartOnWednesday { get; set; }

    public virtual bool DontAllowToStartOnThursday { get; set; }

    public virtual bool DontAllowToStartOnFriday { get; set; }

    public virtual bool DontAllowToStartOnSaturday { get; set; }

    public virtual bool EnableNegativeBalance { get; set; }

    public virtual int NegativeBalanceLimit { get; set; }

    /// <summary>
    /// Optional FK to enable deduction from salary based on the absence deduction/violation configuration.
    /// </summary>
    public virtual int? AbsenceViolationRuleId { get; set; }

    public virtual AbsenceViolationRule AbsenceViolationRule { get; set; }
}
