using System;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.PersonnelAdministration;

namespace HCMv3.SystemSettings;

/// <summary>
/// Tenant-wide payroll engine configuration (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.SysSettings.ManagePayrollSettings</c>). Legacy <c>OnDeleting</c>
/// unconditionally throws -- enforced in <c>ManagePayrollSettingsAppService.DeletePayrollSettings</c>, not
/// here. Legacy <c>PerformValidation</c> was empty (no real business rule to replicate).
/// </summary>
public class ManagePayrollSettings : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual string ClientId { get; set; }

    public virtual double DefaultWorkingHours { get; set; }

    public virtual bool PayslipSignature { get; set; }

    public virtual bool LeaveAdvPaymentdateBefore15 { get; set; }

    public virtual bool LeaveAdvPaymentdateAfter15 { get; set; }

    public virtual bool LeaveProvisionAsPerCurrentSalary { get; set; }

    public virtual bool EnableNotReturnFromLeaveLogic { get; set; }

    public virtual bool EnableBasicpayMoreThenFixedDays { get; set; }

    public virtual bool EnableUnpaidDedSlabWise { get; set; }

    public virtual bool EnableFixedDaysLogicChange { get; set; }

    public virtual bool GenerateRegisterOnNotReturnFromLeave { get; set; }

    public virtual int OffcycleBankFileCutoff { get; set; }

    public virtual bool RoundNetPay { get; set; }

    public virtual bool ApplyAnnualleaveNoImpactLogic { get; set; }

    public virtual bool EnablePayrollEngineAutorun { get; set; }

    public virtual DateTime Decimal6RoundingFrom { get; set; }

    public virtual bool FullMonthLeaveAdjForFixedDays { get; set; }

    public virtual bool PayLeaveAsPerCutoff { get; set; }

    public virtual int DecimalInSalaryRegister { get; set; }

    public virtual LeavePaymentType DefaultLeavePaymentType { get; set; }

    public virtual bool AllowDeleteBackdatedSalaryRegister { get; set; }

    public virtual bool AutoFillFnFCompensation { get; set; }

    public virtual bool EditableAutoFillFnFCompensation { get; set; }

    public virtual bool FixedToActualWhenLeaveOverlapOnMonthStart { get; set; }

    public virtual bool FixedToActualWhenLeaveOverlapOnMonthEnd { get; set; }

    public virtual bool FixedToActualWhenUpaidLeaveFound { get; set; }

    public virtual bool FixedToActualWhenAbsentDayFound { get; set; }

    public virtual OvertimeHoursBasis OvertimeHoursBasis { get; set; }

    public virtual bool CarryAllowanceOnSalaryImport { get; set; }

    public virtual SocialSecurityRateRule SocialSecurityRateRule { get; set; }

    public virtual bool AllowHealthReimburseBalCarryFwd { get; set; }

    public virtual bool AllowSimulationRun { get; set; }

    public virtual bool RunOffCycleForFutureDate { get; set; }

    public virtual bool DisableRetroCalculation { get; set; }

    public virtual PickOffcyclePayment PickOffcyclePayment { get; set; }

    public virtual bool RegularLeaveEncashOnBasicPayOnly { get; set; }

    public virtual bool FnFLeaveEncashOnBasicPayOnly { get; set; }

    public virtual bool ShowBillingTypeInPaymentScreen { get; set; }

    public virtual bool AutoUpdateLoanRecoveryEndDate { get; set; }

    public virtual bool ChangePartialPeriodFactoringInFixedDaysBilling { get; set; }

    public virtual int PartialPeriodCutoffDayInFixedDaysBilling { get; set; }

    public virtual bool EnableEffectiveDateInRecPayment { get; set; }

    public virtual double BankFileMinAmount { get; set; }

    public virtual bool IncludeSalaryIncProvDiffInSalReg { get; set; }

    public virtual DateTime IncludeSalaryIncProvDiffInSalRegEffDt { get; set; }

    public virtual bool LockSalaryRegisterOnPayrollExit { get; set; }
}
