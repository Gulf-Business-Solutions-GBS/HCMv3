using HCMv3.GeneralMasterInfo;
using HCMv3.PayrollManagement;

namespace HCMv3.GoLiveSetup.ReferenceData;

/// <summary>
/// One wage type in the standard payroll catalogue. Every property mirrors <see cref="WageType"/> one-to-one,
/// so the pack can be regenerated from a legacy database by column name alone.
///
/// <para>The engines dispatch on <see cref="Code"/> -- Net Pay is <c>9000</c>, Basic Pay <c>1000</c>, the day
/// counters <c>4000</c>-<c>4031</c> -- which is why these are seeded as a catalogue rather than typed in by the
/// customer. A customer-named "Net Salary" would never be calculated.</para>
/// </summary>
public class ReferenceWageTypeEntry
{
    public string Code { get; set; }

    public string NameEn { get; set; }

    public string NameAr { get; set; }

    public string ShortNameEn { get; set; }

    public string ShortNameAr { get; set; }

    public BillingType BillingType { get; set; }

    public BillingMethod BillingMethod { get; set; }

    public ProrateMethod ProrateMethod { get; set; }

    public double FixedDays { get; set; }

    public double Rate { get; set; }

    public bool Status { get; set; }

    public Category Category { get; set; }

    public int PayrollOrder { get; set; }

    public bool IsSecured { get; set; }

    public bool IncludeInIndemnity { get; set; }

    public bool IncludeInPaidLeave { get; set; }

    public bool IncludeInBasicPay { get; set; }

    public bool IncludeInArrearPay { get; set; }

    public bool IncludeInCalculatedPay { get; set; }

    public bool IncludeInAdvancePay { get; set; }

    public bool IncludeInOvertimePay { get; set; }

    public bool IncludeInFIPosting { get; set; }

    public bool IncludeInCutoff { get; set; }

    public bool IncludeInDisciplinary { get; set; }

    public bool IncludeInVariablePay { get; set; }

    public bool IncludeInSocialSecurity { get; set; }

    public bool IncludeInPartialPay { get; set; }

    public bool IncludeInLateEarlyDed { get; set; }

    public bool IncludeInServiceBonus { get; set; }

    public bool IncludeInFund { get; set; }

    public bool DontImpactNetPay { get; set; }

    /// <summary>
    /// Code of the wage type this one reports the arrear of (e.g. <c>2</c> "Employee PIFSS Ded Arrear" points at
    /// <c>1</c>). Carried as a code because ids differ per tenant; the seeder resolves it once every row exists.
    /// Without it the arrear calculation returns early and a retro correction is silently never paid.
    /// </summary>
    public string ArrearSourceCode { get; set; }

    /// <summary>Builds a new tenant row. <see cref="ArrearSourceCode"/> is resolved separately.</summary>
    public WageType ToEntity(int tenantId)
    {
        return new WageType
        {
            Code = ReferenceText.Truncate(Code, SimpleLookupConsts.MaxCodeLength),
            NameEn = ReferenceText.Truncate(NameEn, SimpleLookupConsts.MaxNameLength),
            NameAr = ReferenceText.Truncate(NameAr, SimpleLookupConsts.MaxNameLength),
            ShortNameEn = ReferenceText.Truncate(ShortNameEn, SimpleLookupConsts.MaxNameLength),
            ShortNameAr = ReferenceText.Truncate(ShortNameAr, SimpleLookupConsts.MaxNameLength),
            BillingType = BillingType,
            BillingMethod = BillingMethod,
            ProrateMethod = ProrateMethod,
            FixedDays = FixedDays,
            Rate = Rate,
            Status = Status,
            Category = Category,
            PayrollOrder = PayrollOrder,
            IsSecured = IsSecured,
            IncludeInIndemnity = IncludeInIndemnity,
            IncludeInPaidLeave = IncludeInPaidLeave,
            IncludeInBasicPay = IncludeInBasicPay,
            IncludeInArrearPay = IncludeInArrearPay,
            IncludeInCalculatedPay = IncludeInCalculatedPay,
            IncludeInAdvancePay = IncludeInAdvancePay,
            IncludeInOvertimePay = IncludeInOvertimePay,
            IncludeInFIPosting = IncludeInFIPosting,
            IncludeInCutoff = IncludeInCutoff,
            IncludeInDisciplinary = IncludeInDisciplinary,
            IncludeInVariablePay = IncludeInVariablePay,
            IncludeInSocialSecurity = IncludeInSocialSecurity,
            IncludeInPartialPay = IncludeInPartialPay,
            IncludeInLateEarlyDed = IncludeInLateEarlyDed,
            IncludeInServiceBonus = IncludeInServiceBonus,
            IncludeInFund = IncludeInFund,
            DontImpactNetPay = DontImpactNetPay,
            TenantId = tenantId,
        };
    }
}

/// <summary>
/// One leave type in the standard payroll catalogue, mirroring <see cref="AbsenceType"/> one-to-one.
///
/// <para>The time engine finds leave by <see cref="Code"/>: <c>1</c> Annual, <c>2</c> Unpaid, <c>3</c> Sick paid,
/// <c>5</c> Sick unpaid, <c>15</c> Unapproved absence. An annual leave type with any other code accrues
/// nothing.</para>
///
/// <para>Three legacy columns are deliberately absent. <c>IsFullDay</c> and <c>NotificationConfig</c> have no
/// home in the port. <c>AbsenceViolationRule</c> points at a per-tenant rule set a new tenant does not have,
/// and every legacy row leaves it empty anyway.</para>
/// </summary>
/// <summary>One "wage type paid during leave type" link, by the two codes the engines look both up by.</summary>
public class ReferenceWageTypeLeaveEntry
{
    public string AbsenceTypeCode { get; set; }

    public string WageTypeCode { get; set; }
}

public class ReferenceAbsenceTypeEntry
{
    public string Code { get; set; }

    public string NameEn { get; set; }

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

    /// <summary>
    /// Code of the wage type this leave is paid through (Annual Leave -&gt; <c>1003</c> "Annual Leave Pay",
    /// Unpaid -&gt; <c>2007</c> "Absent Unpaid Ded"). Resolved by code after the wage types are seeded.
    /// </summary>
    public string WageTypeCode { get; set; }

    /// <summary>Builds a new tenant row. <see cref="WageTypeCode"/> is resolved separately.</summary>
    public AbsenceType ToEntity(int tenantId)
    {
        return new AbsenceType
        {
            Code = ReferenceText.Truncate(Code, SimpleLookupConsts.MaxCodeLength),
            NameEn = ReferenceText.Truncate(NameEn, SimpleLookupConsts.MaxNameLength),
            NameAr = ReferenceText.Truncate(NameAr, SimpleLookupConsts.MaxNameLength),
            IsActive = IsActive,
            IsPaid = IsPaid,
            IncludeWeekend = IncludeWeekend,
            IncludeRestDay = IncludeRestDay,
            IncludePublicHoliday = IncludePublicHoliday,
            LeaveBasisPeriod = LeaveBasisPeriod,
            LeaveBasisFixedDays = LeaveBasisFixedDays,
            HaveQuotaAccrual = HaveQuotaAccrual,
            AccrualPeriod = AccrualPeriod,
            CustomStartMonth = CustomStartMonth,
            MinActiveDaysInService = MinActiveDaysInService,
            CanApplyAgain = CanApplyAgain,
            CanApplyAfterDays = CanApplyAfterDays,
            CanApplyBeforeDays = CanApplyBeforeDays,
            ReInitialize = ReInitialize,
            ReInitializeAfter = ReInitializeAfter,
            ApplyLeaveInAdvance = ApplyLeaveInAdvance,
            ApplyLeaveInAdvanceDays = ApplyLeaveInAdvanceDays,
            ApplyMaxLeave = ApplyMaxLeave,
            MaxLimitInDays = MaxLimitInDays,
            CreateUnpaidAboveMaxLimitDays = CreateUnpaidAboveMaxLimitDays,
            ApplyMinLeave = ApplyMinLeave,
            MinLimitInDays = MinLimitInDays,
            ResetFrequency = ResetFrequency,
            TotalMinutes = TotalMinutes,
            MaxMinutes = MaxMinutes,
            MaxCount = MaxCount,
            PolicyRestrictType = PolicyRestrictType,
            IncludeInWorkflow = IncludeInWorkflow,
            IsReturnDateRequired = IsReturnDateRequired,
            LeaveGender = LeaveGender,
            LeaveReligion = LeaveReligion,
            ExcludePaidLeaveAccrual = ExcludePaidLeaveAccrual,
            QuotaType = QuotaType,
            AllowBackDatedDays = AllowBackDatedDays,
            IncludeInSalaryProfile = IncludeInSalaryProfile,
            TotalDaysAllowed = TotalDaysAllowed,
            LeaveEntitlement = LeaveEntitlement,
            AllowDuringProbation = AllowDuringProbation,
            AllowToReturnEarly = AllowToReturnEarly,
            AllowInTerminationNoticeOnly = AllowInTerminationNoticeOnly,
            IsAttachmentRequired = IsAttachmentRequired,
            AllowPartialLeave = AllowPartialLeave,
            SendNotification = SendNotification,
            DontAllowToStartOnSunday = DontAllowToStartOnSunday,
            DontAllowToStartOnMonday = DontAllowToStartOnMonday,
            DontAllowToStartOnTuesday = DontAllowToStartOnTuesday,
            DontAllowToStartOnWednesday = DontAllowToStartOnWednesday,
            DontAllowToStartOnThursday = DontAllowToStartOnThursday,
            DontAllowToStartOnFriday = DontAllowToStartOnFriday,
            DontAllowToStartOnSaturday = DontAllowToStartOnSaturday,
            EnableNegativeBalance = EnableNegativeBalance,
            NegativeBalanceLimit = NegativeBalanceLimit,
            TenantId = tenantId,
        };
    }
}

/// <summary>Shared trimming for pack text, so every seeded list cuts to its column length the same way.</summary>
internal static class ReferenceText
{
    public static string Truncate(string value, int maxLength)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        var trimmed = value.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed.Substring(0, maxLength);
    }
}
