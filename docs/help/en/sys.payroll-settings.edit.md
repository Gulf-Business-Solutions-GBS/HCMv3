---
helpId: sys.payroll-settings.edit
lang: en
title: Payroll settings
entity: HCMv3.SystemSettings.ManagePayrollSettings
surfaces:
  - angular/src/app/admin/system-settings/manage-payroll-settings/create-or-edit-manage-payroll-settings-modal.component.html
audience: [admin]
permissions: [Pages.SystemSettings.ManagePayrollSettings]
---

## Overview

Company-wide payroll behaviour: rounding, working hours, how fixed-days pay is prorated, leave pay and provision rules, not-returned-from-leave handling, off-cycle runs and bank files.

Most of these match switches in the legacy payroll and are applied exactly as it did. Several are not read at present and say so below.

There is one settings record per company; it cannot be deleted.

## ClientId {#ManagePayrollSettings.ClientId}
<!-- alias: loc:ClientId, dom:PayrollSettings_ClientId -->

**What it is.** The legacy customer code that selected customer-specific report variants.

**Why it matters.** Not read at present -- changing it has no effect.

**Rules.** Optional.

## DefaultWorkingHours {#ManagePayrollSettings.DefaultWorkingHours}
<!-- alias: loc:DefaultWorkingHours, dom:PayrollSettings_DefaultWorkingHours -->

**What it is.** The working hours in a normal day.

**Example.** `8`

**Why it matters.** Used to turn hours into days: the overtime hourly rate when **Overtime Hours Basis** is *Fixed Hours*, payments entered in hours, late and early deductions after monthly tolerance, and hourly leave on a duty with no set times.

**Rules.** Optional. Number.

## DecimalInSalaryRegister {#ManagePayrollSettings.DecimalInSalaryRegister}
<!-- alias: loc:DecimalInSalaryRegister, dom:PayrollSettings_DecimalInSalaryRegister -->

**What it is.** The decimal places payslip amounts are rounded to.

**Example.** `3`

**Why it matters.** 0 means 3.

**Rules.** Optional. Whole number.

## Decimal6RoundingFrom {#ManagePayrollSettings.Decimal6RoundingFrom}
<!-- alias: loc:Decimal6RoundingFrom, dom:PayrollSettings_Decimal6RoundingFrom -->

**What it is.** The month from which the current rounding applies.

**Example.** `2024-01-01`

**Why it matters.** Payslips for months before this date are rounded the old way, to the currency's decimal places. Left blank, the current rounding applies to every month.

**Rules.** Optional. Date value.

## PayslipSignature {#ManagePayrollSettings.PayslipSignature}
<!-- alias: loc:PayslipSignature, dom:PayrollSettings_PayslipSignature -->

**What it is.** Whether the payslip shows a signature panel.

**Why it matters.** Not read at present -- changing it has no effect.

**Rules.** Yes or no.

## RoundNetPay {#ManagePayrollSettings.RoundNetPay}
<!-- alias: loc:RoundNetPay, dom:PayrollSettings_RoundNetPay -->

**What it is.** Whether net pay is rounded to 2 decimal places.

**Why it matters.** Ticked, net pay is rounded to 2 decimals. Unticked, it is rounded to the currency's decimals (3 for the Kuwaiti dinar).

**Rules.** Yes or no.

## EnablePayrollEngineAutorun {#ManagePayrollSettings.EnablePayrollEngineAutorun}
<!-- alias: loc:EnablePayrollEngineAutorun, dom:PayrollSettings_EnablePayrollEngineAutorun -->

**What it is.** Whether the payroll engine runs automatically on a schedule.

**Why it matters.** Not read at present -- changing it has no effect.

**Rules.** Yes or no.

## AllowSimulationRun {#ManagePayrollSettings.AllowSimulationRun}
<!-- alias: loc:AllowSimulationRun, dom:PayrollSettings_AllowSimulationRun -->

**What it is.** Whether simulation payroll runs are allowed.

**Why it matters.** Not read at present -- changing it has no effect.

**Rules.** Yes or no.

## DisableRetroCalculation {#ManagePayrollSettings.DisableRetroCalculation}
<!-- alias: loc:DisableRetroCalculation, dom:PayrollSettings_DisableRetroCalculation -->

**What it is.** Whether changes dated in a processed payroll period are refused.

**Why it matters.** Ticked, adding, changing or deleting payroll-related records dated before the current period is refused -- before the period start while it is open or released, up to its end once it is closed. Unticked, such changes are allowed and payroll works out the difference retroactively.

**Rules.** Yes or no.

## LockSalaryRegisterOnPayrollExit {#ManagePayrollSettings.LockSalaryRegisterOnPayrollExit}
<!-- alias: loc:LockSalaryRegisterOnPayrollExit, dom:PayrollSettings_LockSalaryRegisterOnPayrollExit -->

**What it is.** Whether payslips are locked when a period moves on.

**Why it matters.** Ticked, closing a period locks that month's regular payslips and releasing it for payroll locks its off-cycle ones, so they can no longer be changed or deleted.

**Rules.** Yes or no.

## DefaultLeavePaymentType {#ManagePayrollSettings.DefaultLeavePaymentType}
<!-- alias: loc:DefaultLeavePaymentType, dom:PayrollSettings_DefaultLeavePaymentType -->

**What it is.** The leave payment type offered by default on new leave.

**Why it matters.** Not read at present -- changing it has no effect.

**Rules.** Required. One of: None, Pay With Payroll, Pay In Advance, Pay Full With Payroll.

## LeaveAdvPaymentdateBefore15 {#ManagePayrollSettings.LeaveAdvPaymentdateBefore15}
<!-- alias: loc:LeaveAdvPaymentdateBefore15, dom:PayrollSettings_LeaveAdvPaymentdateBefore15 -->

**What it is.** The advance payment date for leave starting before the 15th.

**Why it matters.** Not read at present -- changing it has no effect.

**Rules.** Yes or no.

## LeaveAdvPaymentdateAfter15 {#ManagePayrollSettings.LeaveAdvPaymentdateAfter15}
<!-- alias: loc:LeaveAdvPaymentdateAfter15, dom:PayrollSettings_LeaveAdvPaymentdateAfter15 -->

**What it is.** The advance payment date for leave starting after the 15th.

**Why it matters.** Not read at present -- changing it has no effect.

**Rules.** Yes or no.

## LeaveProvisionAsPerCurrentSalary {#ManagePayrollSettings.LeaveProvisionAsPerCurrentSalary}
<!-- alias: loc:LeaveProvisionAsPerCurrentSalary, dom:PayrollSettings_LeaveProvisionAsPerCurrentSalary -->

**What it is.** Whether the annual leave provision values the whole balance at the current salary.

**Why it matters.** Despite its name: ticked, the provision is worked out period by period over the employee's past basic pay records. Unticked, the whole leave balance is valued at the current month's pay.

**Rules.** Yes or no.

## EnableNotReturnFromLeaveLogic {#ManagePayrollSettings.EnableNotReturnFromLeaveLogic}
<!-- alias: loc:EnableNotReturnFromLeaveLogic, dom:PayrollSettings_EnableNotReturnFromLeaveLogic -->

**What it is.** Whether employees who have not returned from leave are treated specially.

**Why it matters.** Ticked, an employee whose return-date-required leave has ended with no return recorded gets no basic pay for the run, and the time engine counts their rest days as absent.

**Rules.** Yes or no.

## GenerateRegisterOnNotReturnFromLeave {#ManagePayrollSettings.GenerateRegisterOnNotReturnFromLeave}
<!-- alias: loc:GenerateRegisterOnNotReturnFromLeave, dom:PayrollSettings_GenerateRegisterOnNotReturnFromLeave -->

**What it is.** Whether a payslip is still produced for an employee who has not returned from leave.

**Why it matters.** Unticked, a regular run stops for such an employee with a message and produces no payslip. Off-cycle runs ignore this setting.

**Rules.** Yes or no.

## ApplyAnnualleaveNoImpactLogic {#ManagePayrollSettings.ApplyAnnualleaveNoImpactLogic}
<!-- alias: loc:ApplyAnnualleaveNoImpactLogic, dom:PayrollSettings_ApplyAnnualleaveNoImpactLogic -->

**What it is.** Whether a special "no impact" annual leave is recognised in fixed-days pay.

**Why it matters.** Ticked, when a month is counted on actual days, leave of a type named exactly "Annual Leave (No impact indemnity)" is handled differently from other leave. The type is recognised by its English name.

**Rules.** Yes or no.

## FullMonthLeaveAdjForFixedDays {#ManagePayrollSettings.FullMonthLeaveAdjForFixedDays}
<!-- alias: loc:FullMonthLeaveAdjForFixedDays, dom:PayrollSettings_FullMonthLeaveAdjForFixedDays -->

**What it is.** Whether a whole calendar month on annual leave counts as 26 leave days.

**Why it matters.** Ticked, in an annual leave longer than 27 days, each full calendar month it covers counts as 26 leave days (less public holidays and rest days) rather than being counted day by day.

**Rules.** Yes or no.

## PayLeaveAsPerCutoff {#ManagePayrollSettings.PayLeaveAsPerCutoff}
<!-- alias: loc:PayLeaveAsPerCutoff, dom:PayrollSettings_PayLeaveAsPerCutoff -->

**What it is.** Whether a leave not yet returned from is paid according to the payroll area's cut-off day.

**Why it matters.** Ticked, in a regular run only such leave that started on or before the area's cut-off day is dealt with in that month.

**Rules.** Yes or no.

## RegularLeaveEncashOnBasicPayOnly {#ManagePayrollSettings.RegularLeaveEncashOnBasicPayOnly}
<!-- alias: loc:RegularLeaveEncashOnBasicPayOnly, dom:PayrollSettings_RegularLeaveEncashOnBasicPayOnly -->

**What it is.** Whether leave encashment in regular and advance runs is valued on basic pay only.

**Why it matters.** Ticked, encashment uses wagetype `1000` alone instead of all the wagetypes that make up leave pay.

**Rules.** Yes or no.

## FnFLeaveEncashOnBasicPayOnly {#ManagePayrollSettings.FnFLeaveEncashOnBasicPayOnly}
<!-- alias: loc:FnFLeaveEncashOnBasicPayOnly, dom:PayrollSettings_FnFLeaveEncashOnBasicPayOnly -->

**What it is.** Whether leave encashment at end of service is valued on basic pay only.

**Why it matters.** Ticked, in the end-of-service run encashment uses wagetype `1000` alone.

**Rules.** Yes or no.

## EnableBasicpayMoreThenFixedDays {#ManagePayrollSettings.EnableBasicpayMoreThenFixedDays}
<!-- alias: loc:EnableBasicpayMoreThenFixedDays, dom:PayrollSettings_EnableBasicpayMoreThenFixedDays -->

**What it is.** Whether basic pay is still paid in a month with 26 or more leave days.

**Why it matters.** Unticked, a month with 26 or more leave days pays no basic pay working days -- the leave covers the month. Ticked, working days are still counted.

**Rules.** Yes or no.

## EnableUnpaidDedSlabWise {#ManagePayrollSettings.EnableUnpaidDedSlabWise}
<!-- alias: loc:EnableUnpaidDedSlabWise, dom:PayrollSettings_EnableUnpaidDedSlabWise -->

**What it is.** Whether unpaid days are taken off indemnity slab by slab.

**Why it matters.** Ticked, unpaid leave and absent days are deducted from the service in each indemnity slab they fall in. Unticked, they are deducted from the total service before it is split into slabs.

**Rules.** Yes or no.

## EnableFixedDaysLogicChange {#ManagePayrollSettings.EnableFixedDaysLogicChange}
<!-- alias: loc:EnableFixedDaysLogicChange, dom:PayrollSettings_EnableFixedDaysLogicChange -->

**What it is.** Whether a changed fixed-days logic is used.

**Why it matters.** Not read at present -- changing it has no effect.

**Rules.** Yes or no.

## ChangePartialPeriodFactoringInFixedDaysBilling {#ManagePayrollSettings.ChangePartialPeriodFactoringInFixedDaysBilling}
<!-- alias: loc:ChangePartialPeriodFactoringInFixedDaysBilling, dom:PayrollSettings_ChangePartialPeriodFactoringInFixedDaysBilling -->

**What it is.** Whether an employee who joins early in the month stays on fixed-days pay.

**Why it matters.** Ticked, with a **Partial Period Cutoff Day** set, an employee joining before that day is paid on fixed days less the working days before joining, instead of on actual days.

**Rules.** Yes or no.

## FixedToActualWhenLeaveOverlapOnMonthStart {#ManagePayrollSettings.FixedToActualWhenLeaveOverlapOnMonthStart}
<!-- alias: loc:FixedToActualWhenLeaveOverlapOnMonthStart, dom:PayrollSettings_FixedToActualWhenLeaveOverlapOnMonthStart -->

**What it is.** Whether a month is paid on actual working days when a leave runs across the start of the month.

**Why it matters.** For basic pay on *Fixed Days Per Month*, a month is normally paid as the fixed days less absences. Ticked, a month in which a leave runs across the start of the month is counted on actual working days instead.

**Rules.** Yes or no.

## FixedToActualWhenLeaveOverlapOnMonthEnd {#ManagePayrollSettings.FixedToActualWhenLeaveOverlapOnMonthEnd}
<!-- alias: loc:FixedToActualWhenLeaveOverlapOnMonthEnd, dom:PayrollSettings_FixedToActualWhenLeaveOverlapOnMonthEnd -->

**What it is.** Whether a month is paid on actual working days when a leave runs past the end of the month.

**Why it matters.** For basic pay on *Fixed Days Per Month*, a month is normally paid as the fixed days less absences. Ticked, a month in which a leave runs past the end of the month is counted on actual working days instead.

**Rules.** Yes or no.

## FixedToActualWhenUpaidLeaveFound {#ManagePayrollSettings.FixedToActualWhenUpaidLeaveFound}
<!-- alias: loc:FixedToActualWhenUpaidLeaveFound, dom:PayrollSettings_FixedToActualWhenUpaidLeaveFound -->

**What it is.** Whether a month is paid on actual working days when the employee has unpaid leave.

**Why it matters.** For basic pay on *Fixed Days Per Month*, a month is normally paid as the fixed days less absences. Ticked, a month in which the employee has unpaid leave is counted on actual working days instead.

**Rules.** Yes or no.

## FixedToActualWhenAbsentDayFound {#ManagePayrollSettings.FixedToActualWhenAbsentDayFound}
<!-- alias: loc:FixedToActualWhenAbsentDayFound, dom:PayrollSettings_FixedToActualWhenAbsentDayFound -->

**What it is.** Whether a month is paid on actual working days when the employee has absent days.

**Why it matters.** For basic pay on *Fixed Days Per Month*, a month is normally paid as the fixed days less absences. Ticked, a month in which the employee has absent days is counted on actual working days instead.

**Rules.** Yes or no.

## PartialPeriodCutoffDayInFixedDaysBilling {#ManagePayrollSettings.PartialPeriodCutoffDayInFixedDaysBilling}
<!-- alias: loc:PartialPeriodCutoffDayInFixedDaysBilling, dom:PayrollSettings_PartialPeriodCutoffDayInFixedDaysBilling -->

**What it is.** The day of the month before which a joiner stays on fixed-days pay.

**Example.** `10`

**Why it matters.** Used with **Change Partial Period Factoring**. A day beyond the month's length means its last day.

**Rules.** Optional. Whole number.

## OvertimeHoursBasis {#ManagePayrollSettings.OvertimeHoursBasis}
<!-- alias: loc:OvertimeHoursBasis, dom:PayrollSettings_OvertimeHoursBasis -->

**What it is.** The hours an hour of overtime is valued against.

**Example.** Fixed Hours

**Why it matters.** *Fixed Hours* divides the day's pay by **Default Working Hours**; *Duty Hours* uses the hours of the employee's duty that day.

**Rules.** Required. One of: Fixed Hours, Duty Hours.

## SocialSecurityRateRule {#ManagePayrollSettings.SocialSecurityRateRule}
<!-- alias: loc:SocialSecurityRateRule, dom:PayrollSettings_SocialSecurityRateRule -->

**What it is.** The pay social security is worked out on.

**Example.** Wage Salary

**Why it matters.** *Wage Salary* uses the wagetypes marked *Include In Social Security*; *Contract Salary* uses the contract salary on the employee's sponsor record.

**Rules.** Required. One of: Wage Salary, Contract Salary.

## CarryAllowanceOnSalaryImport {#ManagePayrollSettings.CarryAllowanceOnSalaryImport}
<!-- alias: loc:CarryAllowanceOnSalaryImport, dom:PayrollSettings_CarryAllowanceOnSalaryImport -->

**What it is.** Whether allowances are carried forward when salaries are imported.

**Why it matters.** Not read at present -- changing it has no effect.

**Rules.** Yes or no.

## AllowHealthReimburseBalCarryFwd {#ManagePayrollSettings.AllowHealthReimburseBalCarryFwd}
<!-- alias: loc:AllowHealthReimburseBalCarryFwd, dom:PayrollSettings_AllowHealthReimburseBalCarryFwd -->

**What it is.** Whether unused health reimbursement carries into the next insurance year.

**Why it matters.** Ticked, the unused balance is added to the next year's quota, still capped by the grade's maximum.

**Rules.** Yes or no.

## OffcycleBankFileCutoff {#ManagePayrollSettings.OffcycleBankFileCutoff}
<!-- alias: loc:OffcycleBankFileCutoff, dom:PayrollSettings_OffcycleBankFileCutoff -->

**What it is.** The day of the month used as the cut-off date for off-cycle bank files.

**Example.** `20`

**Why it matters.** 0 means the 1st; a day beyond the month's length means its last day.

**Rules.** Optional. Whole number.

## BankFileMinAmount {#ManagePayrollSettings.BankFileMinAmount}
<!-- alias: loc:BankFileMinAmount, dom:PayrollSettings_BankFileMinAmount -->

**What it is.** The smallest net pay included in a bank file.

**Example.** `1`

**Why it matters.** Payslips with a net pay below this are left out of the file. 0 includes everything.

**Rules.** Optional. Number.

## PickOffcyclePayment {#ManagePayrollSettings.PickOffcyclePayment}
<!-- alias: loc:PickOffcyclePayment, dom:PayrollSettings_PickOffcyclePayment -->

**What it is.** How far ahead an automatic off-cycle run looks for advance payments.

**Example.** Current Date

**Why it matters.** With time engine autorun, *Current Date* picks up advance leave and payments dated up to today; *Current Month* picks up everything up to the pay date.

**Rules.** Required. One of: Current Date, Current Month.

## RunOffCycleForFutureDate {#ManagePayrollSettings.RunOffCycleForFutureDate}
<!-- alias: loc:RunOffCycleForFutureDate, dom:PayrollSettings_RunOffCycleForFutureDate -->

**What it is.** Whether an off-cycle run can pick up items dated after today.

**Why it matters.** Unticked, an advance-leave off-cycle run only looks up to today even when the pay date is later.

**Rules.** Yes or no.

## AutoUpdateLoanRecoveryEndDate {#ManagePayrollSettings.AutoUpdateLoanRecoveryEndDate}
<!-- alias: loc:AutoUpdateLoanRecoveryEndDate, dom:PayrollSettings_AutoUpdateLoanRecoveryEndDate -->

**What it is.** Whether a loan's recovery end date moves when recovery is paused.

**Why it matters.** Ticked, adding or changing a loan exemption recalculates the loan's recovery end date.

**Rules.** Yes or no.

## EnableEffectiveDateInRecPayment {#ManagePayrollSettings.EnableEffectiveDateInRecPayment}
<!-- alias: loc:EnableEffectiveDateInRecPayment, dom:PayrollSettings_EnableEffectiveDateInRecPayment -->

**What it is.** Whether recurring payments use an effective date.

**Why it matters.** Not read at present -- changing it has no effect.

**Rules.** Yes or no.

## AllowDeleteBackdatedSalaryRegister {#ManagePayrollSettings.AllowDeleteBackdatedSalaryRegister}
<!-- alias: loc:AllowDeleteBackdatedSalaryRegister, dom:PayrollSettings_AllowDeleteBackdatedSalaryRegister -->

**What it is.** Whether back-dated payslips may be deleted.

**Why it matters.** Not read at present -- changing it has no effect.

**Rules.** Yes or no.

## AutoFillFnFCompensation {#ManagePayrollSettings.AutoFillFnFCompensation}
<!-- alias: loc:AutoFillFnFCompensation, dom:PayrollSettings_AutoFillFnFCompensation -->

**What it is.** Whether the Full and Final settlement fills in the days to compensate.

**Why it matters.** Ticked, the leave days to compensate are filled with the remaining leave balance, and the indemnity days with the suggested indemnity days.

**Rules.** Yes or no.

## EditableAutoFillFnFCompensation {#ManagePayrollSettings.EditableAutoFillFnFCompensation}
<!-- alias: loc:EditableAutoFillFnFCompensation, dom:PayrollSettings_EditableAutoFillFnFCompensation -->

**What it is.** Whether the filled-in compensation days can be edited.

**Why it matters.** Not read at present -- changing it has no effect.

**Rules.** Yes or no.

## ShowBillingTypeInPaymentScreen {#ManagePayrollSettings.ShowBillingTypeInPaymentScreen}
<!-- alias: loc:ShowBillingTypeInPaymentScreen, dom:PayrollSettings_ShowBillingTypeInPaymentScreen -->

**What it is.** Whether the payment screen shows the billing type.

**Why it matters.** Not read at present -- changing it has no effect.

**Rules.** Yes or no.

## IncludeSalaryIncProvDiffInSalReg {#ManagePayrollSettings.IncludeSalaryIncProvDiffInSalReg}
<!-- alias: loc:IncludeSalaryIncProvDiffInSalReg, dom:PayrollSettings_IncludeSalaryIncProvDiffInSalReg -->

**What it is.** Whether the provision difference from a salary increase is written to the payslip.

**Why it matters.** Ticked, from the date below, the annual leave and indemnity provisions record the day-by-day difference caused by a salary change.

**Rules.** Yes or no.

## IncludeSalaryIncProvDiffInSalRegEffDt {#ManagePayrollSettings.IncludeSalaryIncProvDiffInSalRegEffDt}
<!-- alias: loc:IncludeSalaryIncProvDiffInSalRegEffDt, dom:PayrollSettings_IncludeSalaryIncProvDiffInSalRegEffDt -->

**What it is.** The date from which the salary-increase provision difference is recorded.

**Example.** `2025-01-01`

**Rules.** Optional. Date value.
