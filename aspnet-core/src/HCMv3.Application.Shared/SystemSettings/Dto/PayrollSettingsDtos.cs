using System;
using Abp.Application.Services.Dto;
using HCMv3.PersonnelAdministration;

namespace HCMv3.SystemSettings.Dto;

public class PayrollSettingsListDto : EntityDto
{
    public string ClientId { get; set; }

    public double DefaultWorkingHours { get; set; }

    public bool PayslipSignature { get; set; }
}

public class PayrollSettingsEditDto : EntityDto
{
    public string ClientId { get; set; }

    public double DefaultWorkingHours { get; set; }

    public bool PayslipSignature { get; set; }

    public bool LeaveAdvPaymentdateBefore15 { get; set; }

    public bool LeaveAdvPaymentdateAfter15 { get; set; }

    public bool LeaveProvisionAsPerCurrentSalary { get; set; }

    public bool EnableNotReturnFromLeaveLogic { get; set; }

    public bool EnableBasicpayMoreThenFixedDays { get; set; }

    public bool EnableUnpaidDedSlabWise { get; set; }

    public bool EnableFixedDaysLogicChange { get; set; }

    public bool GenerateRegisterOnNotReturnFromLeave { get; set; }

    public int OffcycleBankFileCutoff { get; set; }

    public bool RoundNetPay { get; set; }

    public bool ApplyAnnualleaveNoImpactLogic { get; set; }

    public bool EnablePayrollEngineAutorun { get; set; }

    public DateTime Decimal6RoundingFrom { get; set; }

    public bool FullMonthLeaveAdjForFixedDays { get; set; }

    public bool PayLeaveAsPerCutoff { get; set; }

    public int DecimalInSalaryRegister { get; set; }

    public LeavePaymentType DefaultLeavePaymentType { get; set; }

    public bool AllowDeleteBackdatedSalaryRegister { get; set; }

    public bool AutoFillFnFCompensation { get; set; }

    public bool EditableAutoFillFnFCompensation { get; set; }

    public bool FixedToActualWhenLeaveOverlapOnMonthStart { get; set; }

    public bool FixedToActualWhenLeaveOverlapOnMonthEnd { get; set; }

    public bool FixedToActualWhenUpaidLeaveFound { get; set; }

    public bool FixedToActualWhenAbsentDayFound { get; set; }

    public OvertimeHoursBasis OvertimeHoursBasis { get; set; }

    public bool CarryAllowanceOnSalaryImport { get; set; }

    public SocialSecurityRateRule SocialSecurityRateRule { get; set; }

    public bool AllowHealthReimburseBalCarryFwd { get; set; }

    public bool AllowSimulationRun { get; set; }

    public bool RunOffCycleForFutureDate { get; set; }

    public bool DisableRetroCalculation { get; set; }

    public PickOffcyclePayment PickOffcyclePayment { get; set; }

    public bool RegularLeaveEncashOnBasicPayOnly { get; set; }

    public bool FnFLeaveEncashOnBasicPayOnly { get; set; }

    public bool ShowBillingTypeInPaymentScreen { get; set; }

    public bool AutoUpdateLoanRecoveryEndDate { get; set; }

    public bool ChangePartialPeriodFactoringInFixedDaysBilling { get; set; }

    public int PartialPeriodCutoffDayInFixedDaysBilling { get; set; }

    public bool EnableEffectiveDateInRecPayment { get; set; }

    public double BankFileMinAmount { get; set; }

    public bool IncludeSalaryIncProvDiffInSalReg { get; set; }

    public DateTime IncludeSalaryIncProvDiffInSalRegEffDt { get; set; }

    public bool LockSalaryRegisterOnPayrollExit { get; set; }
}
