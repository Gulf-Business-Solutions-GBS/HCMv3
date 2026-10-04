using System.ComponentModel.DataAnnotations;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.PayrollManagement.Dto;

/// <summary>
/// Create/Update input DTO for <see cref="AbsenceType"/>. <c>Id == null</c> means create; otherwise update.
/// </summary>
public class CreateOrEditAbsenceTypeDto
{
    public int? Id { get; set; }

    /// <summary>
    /// Optional. When left blank the server auto-generates the next numeric code.
    /// </summary>
    [StringLength(SimpleLookupConsts.MaxCodeLength)]
    public string Code { get; set; }

    [Required]
    [StringLength(SimpleLookupConsts.MaxNameLength)]
    public string NameEn { get; set; }

    [Required]
    [StringLength(SimpleLookupConsts.MaxNameLength)]
    public string NameAr { get; set; }

    public bool IsActive { get; set; }

    public bool IsPaid { get; set; }

    public bool IncludeWeekend { get; set; }

    public bool IncludeRestDay { get; set; }

    public bool IncludePublicHoliday { get; set; }

    public LeaveBasisPeriod LeaveBasisPeriod { get; set; }

    public double LeaveBasisFixedDays { get; set; }

    public bool HaveQuotaAccrual { get; set; }

    public AccrualPeriod AccrualPeriod { get; set; }

    public CustomStartMonth CustomStartMonth { get; set; }

    public double MinActiveDaysInService { get; set; }

    public bool CanApplyAgain { get; set; }

    public double CanApplyAfterDays { get; set; }

    public int CanApplyBeforeDays { get; set; }

    public bool ReInitialize { get; set; }

    public int ReInitializeAfter { get; set; }

    public bool ApplyLeaveInAdvance { get; set; }

    public int ApplyLeaveInAdvanceDays { get; set; }

    public bool ApplyMaxLeave { get; set; }

    public int MaxLimitInDays { get; set; }

    public bool CreateUnpaidAboveMaxLimitDays { get; set; }

    public bool ApplyMinLeave { get; set; }

    public int MinLimitInDays { get; set; }

    public ResetFrequency ResetFrequency { get; set; }

    public int TotalMinutes { get; set; }

    public int MaxMinutes { get; set; }

    public int MaxCount { get; set; }

    public int? WageTypeId { get; set; }

    public RestrictType PolicyRestrictType { get; set; }

    public bool IncludeInWorkflow { get; set; }

    public bool IsReturnDateRequired { get; set; }

    public LeaveGender LeaveGender { get; set; }

    public LeaveReligion LeaveReligion { get; set; }

    public bool ExcludePaidLeaveAccrual { get; set; }

    public QuotaType QuotaType { get; set; }

    public int AllowBackDatedDays { get; set; }

    public bool IncludeInSalaryProfile { get; set; }

    public double TotalDaysAllowed { get; set; }

    public LeaveEntitlement LeaveEntitlement { get; set; }

    public bool AllowDuringProbation { get; set; }

    public bool AllowToReturnEarly { get; set; }

    public bool AllowInTerminationNoticeOnly { get; set; }

    public bool IsAttachmentRequired { get; set; }

    public bool AllowPartialLeave { get; set; }

    public LeaveDurationType LeaveDurationType { get; set; }

    [Range(0, 3650)]
    public int MaxFutureDays { get; set; }

    public bool RequirePolicyAcceptance { get; set; }

    public bool SendNotification { get; set; }

    public bool DontAllowToStartOnSunday { get; set; }

    public bool DontAllowToStartOnMonday { get; set; }

    public bool DontAllowToStartOnTuesday { get; set; }

    public bool DontAllowToStartOnWednesday { get; set; }

    public bool DontAllowToStartOnThursday { get; set; }

    public bool DontAllowToStartOnFriday { get; set; }

    public bool DontAllowToStartOnSaturday { get; set; }

    public bool EnableNegativeBalance { get; set; }

    public int NegativeBalanceLimit { get; set; }

    public int? AbsenceViolationRuleId { get; set; }
}
