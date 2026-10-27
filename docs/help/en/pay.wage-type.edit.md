---
helpId: pay.wage-type.edit
lang: en
title: Wagetypes — Create / Edit
entity: HCMv3.PayrollManagement.WageType
surfaces:
  - angular/src/app/admin/payroll-management/wage-types/create-or-edit-wage-type-modal.component.html
audience: [admin]
permissions: [Pages.PayrollManagement.WageTypes.Create]
---

## Overview

A wagetype is one line of pay: basic salary, an allowance, a deduction, a leave payment, a provision. Every amount on a payslip, in costing and in the journal voucher belongs to a wagetype, and the settings here decide how payroll works it out and what else it counts towards.

Many wagetypes are recognised by the payroll engine **by their code** and calculated by dedicated logic -- basic pay, net pay, leave, overtime, indemnity and others. Those are marked *Technical* and cannot be deleted. The rest are *Custom*.

The many **Include In …** boxes each add this wagetype's monthly amount to the base of another calculation: the social security deduction adds up every wagetype marked *Include In Social Security*, and so on.

## Code {#WageType.Code}
<!-- alias: loc:Code, dom:WageType_Code -->

**What it is.** The wagetype's code. Unlike most lists it is never assigned automatically -- you must enter it.

**Example.** `1000` -- basic pay.

**Why it matters.** The payroll engine finds its own wagetypes by code: `1000` basic pay, `9000` net pay, `4001` unpaid leave, `4029` absent days and several dozen more each have their own calculation. A wagetype whose code the engine does not recognise is paid only through what points to it -- an employee's basic pay, a payment, or an absence, overtime or loan type.

:::admin
The code can still be edited after saving, even on a Technical wagetype. Changing the code of one the engine recognises stops its calculation -- the engine will no longer find it.
:::

**Rules.** Required. Must be unique in this list.

## ShortNameEn {#WageType.ShortNameEn}
<!-- alias: loc:ShortNameEn, dom:WageType_ShortNameEn -->

**What it is.** A short English name for the wagetype.

**Example.** `HRA`

**Why it matters.** Only the AI assistant uses it, to recognise the wagetype when you refer to it by its short name. Payslips and reports use the full name.

**Rules.** Optional.

## ShortNameAr {#WageType.ShortNameAr}
<!-- alias: loc:ShortNameAr, dom:WageType_ShortNameAr -->

**What it is.** A short Arabic name for the wagetype.

**Example.** `بدل سكن`

**Why it matters.** Only the AI assistant uses it, to recognise the wagetype when you refer to it by its short name.

**Rules.** Optional.

## BillingType {#WageType.BillingType}
<!-- alias: loc:BillingType, dom:WageType_BillingType -->

**What it is.** What kind of line this is: an earning, a deduction, a time type or a provision.

**Example.** Earning

**Why it matters.** Net pay is earnings minus deductions. Time types (counts of days and hours) and provisions (accruals such as leave and indemnity provision) are worked out by the engine and do not change net pay. Only earnings and deductions can be paid through a payment batch.

**Rules.** Required. One of: Earning, Deduction, Time Type, Provision.

:::admin
A new wagetype cannot be created as a time type or a provision -- those are reserved for the engine's own wagetypes.
:::

## BillingMethod {#WageType.BillingMethod}
<!-- alias: loc:BillingMethod, dom:WageType_BillingMethod -->

**What it is.** How the monthly amount is turned into a daily rate when a month is not paid in full.

**Example.** Calendar Days Per Month

**Why it matters.** *Calendar Days Per Month* divides by the days in that month; *Working Days Per Month* by the days in the month less weekly offs; *Fixed Days Per Month* by **Fixed Days** below; *Fixed Amount Per Month* pays the full amount regardless of days. The same divisor values leave accruals and leave provision.

**Rules.** Required. One of: Fixed Amount Per Month, Fixed Days Per Month, Calendar Days Per Month, Working Days Per Month.

## ProrateMethod {#WageType.ProrateMethod}
<!-- alias: loc:ProrateMethod, dom:WageType_ProrateMethod -->

**What it is.** Which days count as paid days when the month is not paid in full.

**Example.** Working Days

**Why it matters.** *Active Days* pays every day the employee was employed in the month, whatever the billing method. With *Working Days Per Month*, *Working Days* pays the days worked plus public holidays, and *Working Days (Rest)* pays the days worked plus rest days and the public holidays that do not fall on a weekly off. With *Calendar Days Per Month*, both also pay weekly offs and holidays the employee did not spend on leave. The daily rate comes from the billing method.

**Rules.** Required. One of: Working Days, Working Days (Rest), Active Days.

## Category {#WageType.Category}
<!-- alias: loc:Category, dom:WageType_Category -->

**What it is.** Whether the wagetype is one of the engine's own (Technical) or one your company added (Custom).

**Example.** Custom

**Why it matters.** A Technical wagetype cannot be deleted.

**Rules.** Required. One of: Custom, Technical.

## FixedDays {#WageType.FixedDays}
<!-- alias: loc:FixedDays, dom:WageType_FixedDays -->

**What it is.** The number of days a month is counted as, for the *Fixed Days Per Month* billing method.

**Example.** `26`

**Why it matters.** The daily rate is the monthly amount divided by this number. On the leave provision wagetypes it is the divisor for valuing leave days; on wagetype `1010` it is the day value used for indemnity when an indemnity slab gives no basis days.

**Rules.** Optional. Number.

## Rate {#WageType.Rate}
<!-- alias: loc:Rate, dom:WageType_Rate -->

**What it is.** A default rate for the wagetype.

**Why it matters.** Not read at present -- ticking it changes nothing in payroll.

**Rules.** Optional. Number.

## PayrollOrder {#WageType.PayrollOrder}
<!-- alias: loc:PayrollOrder, dom:WageType_PayrollOrder -->

**What it is.** The position of this wagetype in the order payroll calculates wagetypes, lowest first.

**Example.** `150`

**Why it matters.** Many wagetypes are worked out from lines already on the payslip -- deductions from allowances, net pay from everything -- so a wagetype must come after the ones it depends on. Journal voucher lines follow the same order. It cannot be 0.

**Rules.** Optional. Whole number.

## ArrearSourceWageTypeId {#WageType.ArrearSourceWageTypeId}
<!-- alias: loc:ArrearSourceWageTypeId, dom:WageType_ArrearSourceWageTypeId -->

**What it is.** For an arrear wagetype, the wagetype whose back pay it carries.

**Example.** Basic Salary (for "Basic Salary Arrears")

**Why it matters.** The arrear calculation takes the difference for this source wagetype, and costing posts the arrear to the source wagetype's accounts. An arrear wagetype with no source is left out of costing.

**Rules.** Optional. Chosen from Wage Type master data.

## Status {#WageType.Status}
<!-- alias: loc:Status, dom:WageType_Status -->

**What it is.** Whether the wagetype is active.

**Why it matters.** Payroll calculates only active wagetypes, and only active ones can be paid through a payment batch. Untick it to stop a wagetype without deleting its history.

**Rules.** Yes or no.

## IncludeInIndemnity {#WageType.IncludeInIndemnity}
<!-- alias: loc:IncludeInIndemnity, dom:WageType_includeInIndemnity -->

**What it is.** Whether this wagetype's monthly amount counts in the pay that end-of-service indemnity is based on.

**Why it matters.** Indemnity, and the indemnity cap, are worked out from the sum of the wagetypes marked here.

**Rules.** Yes or no.

:::admin
On the time types `4001` (unpaid leave) and `4029` (absent days) the box means something else: ticked, those days are taken off the service that indemnity is counted on.
:::

## IncludeInPaidLeave {#WageType.IncludeInPaidLeave}
<!-- alias: loc:IncludeInPaidLeave, dom:WageType_includeInPaidLeave -->

**What it is.** Whether the wagetype is paid during leave.

**Why it matters.** Not read at present. Which wagetypes make up leave pay is set per leave type in the **Wagetype Leaves** list.

**Rules.** Yes or no.

## IncludeInBasicPay {#WageType.IncludeInBasicPay}
<!-- alias: loc:IncludeInBasicPay, dom:WageType_includeInBasicPay -->

**What it is.** Whether the wagetype is part of the employee's basic pay.

**Why it matters.** Its monthly amount counts in the base for attendance pay. When an employee's basic pay line for this wagetype is worked out from another wagetype, it is paid in regular runs only with this ticked. A basic-pay wagetype cannot be paid through a payment batch.

**Rules.** Yes or no.

## IncludeInArrearPay {#WageType.IncludeInArrearPay}
<!-- alias: loc:IncludeInArrearPay, dom:WageType_includeInArrearPay -->

**What it is.** Whether this is an arrear wagetype -- one that carries back pay for another.

**Why it matters.** Ticked, payroll works it out through the arrear calculation from its **Arrear Source Wagetype**, instead of any other rule. It cannot be paid through a payment batch.

**Rules.** Yes or no.

## IncludeInCalculatedPay {#WageType.IncludeInCalculatedPay}
<!-- alias: loc:IncludeInCalculatedPay, dom:WageType_includeInCalculatedPay -->

**What it is.** Whether the wagetype can be paid as a one-off payment.

**Why it matters.** Payment batches offer only wagetypes with this ticked -- and only active earnings or deductions that are not basic pay, not arrears, not net pay and not already paid by an overtime, absence or loan type.

**Rules.** Yes or no.

## IncludeInAdvancePay {#WageType.IncludeInAdvancePay}
<!-- alias: loc:IncludeInAdvancePay, dom:WageType_includeInAdvancePay -->

**What it is.** Whether the wagetype is paid in advance with leave.

**Why it matters.** In an off-cycle run that pays leave in advance, only wagetypes with this ticked are paid ahead. An end-of-service run pays every wagetype, ticked or not.

**Rules.** Yes or no.

## IncludeInOvertimePay {#WageType.IncludeInOvertimePay}
<!-- alias: loc:IncludeInOvertimePay, dom:WageType_includeInOvertimePay -->

**What it is.** Whether this wagetype's monthly amount counts in the base for overtime pay.

**Why it matters.** When an overtime type is paid at the actual rate, the hourly rate is the sum of these wagetypes divided by the hours basis.

**Rules.** Yes or no.

## IncludeInFIPosting {#WageType.IncludeInFIPosting}
<!-- alias: loc:IncludeInFIPosting, dom:WageType_includeInFIPosting -->

**What it is.** Whether the wagetype is posted to finance.

**Why it matters.** The cost engine costs only wagetypes with this ticked, so only they reach the cost register and the journal voucher. Each one then needs a wage account for the employee, or costing stops for that employee.

**Rules.** Yes or no.

## IncludeInCutoff {#WageType.IncludeInCutoff}
<!-- alias: loc:IncludeInCutoff, dom:WageType_includeInCutoff -->

**What it is.** Whether the wagetype's day counts follow the payroll area's cut-off day.

**Why it matters.** Ticked, and with a cut-off day set on the payroll area, days are counted from the day after the previous month's cut-off to this month's cut-off day instead of the calendar month.

**Rules.** Yes or no.

## IncludeInDisciplinary {#WageType.IncludeInDisciplinary}
<!-- alias: loc:IncludeInDisciplinary, dom:WageType_includeInDisciplinary -->

**What it is.** Whether the wagetype counts in disciplinary deductions.

**Why it matters.** Not read at present -- ticking it changes nothing in payroll.

**Rules.** Yes or no.

## IncludeInVariablePay {#WageType.IncludeInVariablePay}
<!-- alias: loc:IncludeInVariablePay, dom:WageType_includeInVariablePay -->

**What it is.** Whether the wagetype counts as variable pay.

**Why it matters.** Not read at present -- ticking it changes nothing in payroll.

**Rules.** Yes or no.

## IncludeInSocialSecurity {#WageType.IncludeInSocialSecurity}
<!-- alias: loc:IncludeInSocialSecurity, dom:WageType_includeInSocialSecurity -->

**What it is.** Whether this wagetype's monthly amount counts in the base for the social security deduction.

**Why it matters.** When social security is based on wage salary, the deduction is worked out from the sum of these wagetypes.

**Rules.** Yes or no.

## IncludeInPartialPay {#WageType.IncludeInPartialPay}
<!-- alias: loc:IncludeInPartialPay, dom:WageType_includeInPartialPay -->

**What it is.** Whether this wagetype's monthly amount counts in the base for payments entered in days or hours.

**Why it matters.** A payment entered as a number of days or hours is turned into an amount from the sum of these wagetypes.

**Rules.** Yes or no.

## IncludeInLateEarlyDed {#WageType.IncludeInLateEarlyDed}
<!-- alias: loc:IncludeInLateEarlyDed, dom:WageType_includeInLateEarlyDed -->

**What it is.** Whether this wagetype's monthly amount counts in the base for late-arrival and early-departure deductions.

**Why it matters.** The deduction per day is worked out from the sum of these wagetypes.

**Rules.** Yes or no.

## IncludeInServiceBonus {#WageType.IncludeInServiceBonus}
<!-- alias: loc:IncludeInServiceBonus, dom:WageType_includeInServiceBonus -->

**What it is.** Whether this wagetype's monthly amount counts in the base for the service bonus.

**Why it matters.** The service bonus is a percentage of the sum of these wagetypes.

**Rules.** Yes or no.

## IncludeInFund {#WageType.IncludeInFund}
<!-- alias: loc:IncludeInFund, dom:WageType_includeInFund -->

**What it is.** Whether this wagetype's monthly amount counts in the base for the fund deduction.

**Why it matters.** The sum of these wagetypes decides which of the grade's fund bands applies.

**Rules.** Yes or no.

## DontImpactNetPay {#WageType.DontImpactNetPay}
<!-- alias: loc:DontImpactNetPay, dom:WageType_dontImpactNetPay -->

**What it is.** Whether the wagetype is left out of net pay.

**Why it matters.** Ticked, the line still appears on the payslip but is not added to or taken from net pay. Leave pay on such a wagetype does not create an advance payment deduction.

**Rules.** Yes or no.

## IsSecured {#WageType.IsSecured}
<!-- alias: loc:IsSecured, dom:WageType_isSecured -->

**What it is.** Whether access to the wagetype is limited to certain roles.

**Why it matters.** Not enforced at present: the roles can be listed under **Wagetype Security Roles**, but nothing restricts who sees or pays the wagetype.

**Rules.** Yes or no.
