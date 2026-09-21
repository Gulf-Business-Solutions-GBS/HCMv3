---
helpId: pay.absence-type.edit
lang: en
title: Absence Types — Create / Edit
entity: HCMv3.PayrollManagement.AbsenceType
surfaces:
  - angular/src/app/admin/payroll-management/absence-types/create-or-edit-absence-type-modal.component.html
audience: [admin]
permissions: [Pages.PayrollManagement.AbsenceTypes.Create]
---

## Overview

A type of leave or absence -- annual, sick, unpaid, Hajj, maternity and so on -- and how it is counted, paid and accrued.

What each employee is entitled to is set per employee sub group, on the sub group's leave rules; this form sets how the type behaves.

**Many settings on this form are saved but not enforced.** The limits and restrictions on applying for leave -- maximum and minimum days, days it may not start on, attachments, probation, notice period, gender, religion, negative balance, hourly limits -- are not checked when leave is entered or approved. Each such field says so below. To enforce a leave policy, use a business rule on leave under **Configure Business Rules**; rules can read the leave type's code, whether it is paid, whether it is active and its minimum days in service.

## IsActive {#AbsenceType.IsActive}
<!-- alias: loc:IsActive, dom:AbsenceType_IsActive -->

**What it is.** Whether the leave type is in use.

**Why it matters.** Not enforced at present: an inactive type is still offered when leave is entered, including in self-service. Business rules on leave can read it, so a rule can refuse leave of an inactive type.

**Rules.** Yes or no.

## LeaveBasisPeriod {#AbsenceType.LeaveBasisPeriod}
<!-- alias: loc:LeaveBasisPeriod, dom:AbsenceType_LeaveBasisPeriod -->

**What it is.** Whether a leave day is valued on calendar days or on a fixed number of days.

**Why it matters.** Not checked at present -- the setting is saved, but nothing enforces it when leave is entered or approved.

**Rules.** Required. One of: Calendar Days, Fixed Days.

## LeaveBasisFixedDays {#AbsenceType.LeaveBasisFixedDays}
<!-- alias: loc:LeaveBasisFixedDays, dom:AbsenceType_LeaveBasisFixedDays -->

**What it is.** The fixed number of days, for the fixed-days basis.

**Why it matters.** Not checked at present -- the setting is saved, but nothing enforces it when leave is entered or approved.

**Rules.** Optional. Number.

## IsPaid {#AbsenceType.IsPaid}
<!-- alias: loc:IsPaid, dom:AbsenceType_IsPaid -->

**What it is.** Whether the leave is paid.

**Why it matters.** Only paid leave can be paid in advance: an advance-leave off-cycle run picks up only leave of paid types set to *Pay In Advance*. When the payroll setting *Fixed To Actual When Unpaid Leave Found* is on, a month containing leave of an unpaid type is paid on actual days instead of fixed days. Business rules on leave can read it.

**Rules.** Yes or no.

## WageType {#AbsenceType.WageTypeId}
<!-- alias: loc:WageType, dom:AbsenceType_WageTypeId -->

**What it is.** The wagetype this leave is paid under.

**Example.** Annual Leave Pay

**Why it matters.** Leave pay for this type is paid on this wagetype, and its billing method decides how an accrued leave day is valued. Which wagetypes make up the daily amount is set in **Wagetype Leaves**.

**Rules.** Optional. Chosen from a list.

## IncludeWeekend {#AbsenceType.IncludeWeekend}
<!-- alias: loc:IncludeWeekend, dom:AbsenceType_IncludeWeekend -->

**What it is.** Whether a weekly off inside the leave counts as a leave day.

**Why it matters.** Ticked, weekly offs within the leave are counted as leave and taken from the balance. Unticked, they are not.

**Rules.** Yes or no.

## IncludeRestDay {#AbsenceType.IncludeRestDay}
<!-- alias: loc:IncludeRestDay, dom:AbsenceType_IncludeRestDay -->

**What it is.** Whether a rest day inside the leave counts as a leave day.

**Why it matters.** Ticked, rest days within the leave are counted as leave and taken from the balance. Unticked, they are not.

**Rules.** Yes or no.

## IncludePublicHoliday {#AbsenceType.IncludePublicHoliday}
<!-- alias: loc:IncludePublicHoliday, dom:AbsenceType_IncludePublicHoliday -->

**What it is.** Whether a public holiday inside the leave counts as a leave day.

**Why it matters.** Ticked, public holidays within the leave are counted as leave and taken from the balance. Unticked, they are not.

**Rules.** Yes or no.

## IncludeInWorkflow {#AbsenceType.IncludeInWorkflow}
<!-- alias: loc:IncludeInWorkflow, dom:AbsenceType_IncludeInWorkflow -->

**What it is.** Whether leave of this type goes through workflow approval.

**Why it matters.** Not checked at present -- the setting is saved, but nothing enforces it when leave is entered or approved.

**Rules.** Yes or no.

## HaveQuotaAccrual {#AbsenceType.HaveQuotaAccrual}
<!-- alias: loc:HaveQuotaAccrual, dom:AbsenceType_HaveQuotaAccrual -->

**What it is.** Whether the employee builds up a balance of this leave over time.

**Why it matters.** Ticked, the balance is worked out from the employee's leave quota records, and the leave quota balance and quota provision reports show an accrued balance. Unticked, no accrued balance is kept.

**Rules.** Yes or no.

## AccrualPeriod {#AbsenceType.AccrualPeriod}
<!-- alias: loc:AccrualPeriod, dom:AbsenceType_AccrualPeriod -->

**What it is.** The cycle the leave balance runs over.

**Example.** Work Year

**Why it matters.** *Calendar Year* starts each cycle on 1 January, *Fiscal Year* on 1 April, *Work Year* on the employee's joining anniversary, and *Custom* on the first of the **Custom Start Month**. *Per Employment* makes one cycle from the joining date to the end of the employee's latest contract -- and fails for an employee with no contract. *Bi-Annual* and *No Accrual* count from the joining date like *Work Year*. Whichever is chosen, a cycle lasts **Re-Initialize After** years.

**Rules.** Required. One of: No Accrual, Calendar Year, Fiscal Year, Work Year, Bi-Annual, Per Employment, Custom.

## CustomStartMonth {#AbsenceType.CustomStartMonth}
<!-- alias: loc:CustomStartMonth, dom:AbsenceType_CustomStartMonth -->

**What it is.** The month the cycle starts, for the *Custom* accrual period.

**Example.** July

**Rules.** Required. One of: None, January, February, March, April, May, June, July, August, September, October, November, December.

## ExcludePaidLeaveAccrual {#AbsenceType.ExcludePaidLeaveAccrual}
<!-- alias: loc:ExcludePaidLeaveAccrual, dom:AbsenceType_ExcludePaidLeaveAccrual -->

**What it is.** Whether days on this leave stop annual leave from building up.

**Why it matters.** Ticked, the time engine accrues no annual leave for days the employee spends on this type of leave.

**Rules.** Yes or no.

## MinActiveDaysInService {#AbsenceType.MinActiveDaysInService}
<!-- alias: loc:MinActiveDaysInService, dom:AbsenceType_MinActiveDaysInService -->

**What it is.** The days of service an employee needs before taking this leave.

**Example.** `730` -- Hajj leave after two years.

**Why it matters.** Not checked by itself. Business rules on leave can read it, so a rule can refuse the leave for an employee with fewer days of service.

**Rules.** Optional. Number.

## CanApplyAgain {#AbsenceType.CanApplyAgain}
<!-- alias: loc:CanApplyAgain, dom:AbsenceType_CanApplyAgain -->

**What it is.** Whether the leave can be taken more than once.

**Why it matters.** Not checked at present -- the setting is saved, but nothing enforces it when leave is entered or approved.

**Rules.** Yes or no.

## CanApplyAfterDays {#AbsenceType.CanApplyAfterDays}
<!-- alias: loc:CanApplyAfterDays, dom:AbsenceType_CanApplyAfterDays -->

**What it is.** The days that must pass before the leave can be taken again.

**Why it matters.** Not checked at present -- the setting is saved, but nothing enforces it when leave is entered or approved.

**Rules.** Optional. Number.

## CanApplyBeforeDays {#AbsenceType.CanApplyBeforeDays}
<!-- alias: loc:CanApplyBeforeDays, dom:AbsenceType_CanApplyBeforeDays -->

**What it is.** How many days ahead the leave must be applied for.

**Why it matters.** Not checked at present -- the setting is saved, but nothing enforces it when leave is entered or approved.

**Rules.** Optional. Whole number.

## ReInitialize {#AbsenceType.ReInitialize}
<!-- alias: loc:ReInitialize, dom:AbsenceType_ReInitialize -->

**What it is.** Whether the balance starts again at the end of each cycle.

**Why it matters.** Ticked, the cycle restarts every **Re-Initialize After** years from its start. Unticked, the first cycle never ends.

**Rules.** Yes or no.

## ReInitializeAfter {#AbsenceType.ReInitializeAfter}
<!-- alias: loc:ReInitializeAfter, dom:AbsenceType_ReInitializeAfter -->

**What it is.** How many years each cycle lasts before the balance starts again.

**Example.** `1`

**Why it matters.** Used when **Re-Initialize** is ticked. It must be greater than 0 when that box is ticked.

**Rules.** Optional. Whole number.

## ApplyLeaveInAdvance {#AbsenceType.ApplyLeaveInAdvance}
<!-- alias: loc:ApplyLeaveInAdvance, dom:AbsenceType_ApplyLeaveInAdvance -->

**What it is.** Whether the leave may be taken before it has been earned.

**Why it matters.** Not checked at present -- the setting is saved, but nothing enforces it when leave is entered or approved.

**Rules.** Yes or no.

## ApplyLeaveInAdvanceDays {#AbsenceType.ApplyLeaveInAdvanceDays}
<!-- alias: loc:ApplyLeaveInAdvanceDays, dom:AbsenceType_ApplyLeaveInAdvanceDays -->

**What it is.** How many unearned days may be taken in advance.

**Why it matters.** Not checked at present -- the setting is saved, but nothing enforces it when leave is entered or approved.

**Rules.** Optional. Whole number.

## ApplyMaxLeave {#AbsenceType.ApplyMaxLeave}
<!-- alias: loc:ApplyMaxLeave, dom:AbsenceType_ApplyMaxLeave -->

**What it is.** Whether a maximum length applies to one leave.

**Why it matters.** Not checked at present -- the setting is saved, but nothing enforces it when leave is entered or approved.

**Rules.** Yes or no.

## MaxLimitInDays {#AbsenceType.MaxLimitInDays}
<!-- alias: loc:MaxLimitInDays, dom:AbsenceType_MaxLimitInDays -->

**What it is.** The most days one leave may last.

**Example.** `30`

**Why it matters.** Not checked at present -- the setting is saved, but nothing enforces it when leave is entered or approved.

**Rules.** Optional. Whole number.

## CreateUnpaidAboveMaxLimitDays {#AbsenceType.CreateUnpaidAboveMaxLimitDays}
<!-- alias: loc:CreateUnpaidAboveMaxLimitDays, dom:AbsenceType_CreateUnpaidAboveMaxLimitDays -->

**What it is.** Whether days beyond the maximum are turned into unpaid leave.

**Why it matters.** Not checked at present -- the setting is saved, but nothing enforces it when leave is entered or approved.

**Rules.** Yes or no.

## ApplyMinLeave {#AbsenceType.ApplyMinLeave}
<!-- alias: loc:ApplyMinLeave, dom:AbsenceType_ApplyMinLeave -->

**What it is.** Whether a minimum length applies to one leave.

**Why it matters.** Not checked at present -- the setting is saved, but nothing enforces it when leave is entered or approved.

**Rules.** Yes or no.

## MinLimitInDays {#AbsenceType.MinLimitInDays}
<!-- alias: loc:MinLimitInDays, dom:AbsenceType_MinLimitInDays -->

**What it is.** The fewest days one leave may last.

**Example.** `3`

**Why it matters.** Not checked at present -- the setting is saved, but nothing enforces it when leave is entered or approved.

**Rules.** Optional. Whole number.

## ResetFrequency {#AbsenceType.ResetFrequency}
<!-- alias: loc:ResetFrequency, dom:AbsenceType_ResetFrequency -->

**What it is.** For leave taken in hours: the period the hourly limits apply over.

**Why it matters.** Not checked at present -- the setting is saved, but nothing enforces it when leave is entered or approved.

**Rules.** Required. One of: Daily, Weekly, Monthly, Quarterly, Annually.

## TotalMinutes {#AbsenceType.TotalMinutes}
<!-- alias: loc:TotalMinutes, dom:AbsenceType_TotalMinutes -->

**What it is.** For leave taken in hours: the total minutes allowed in one period.

**Why it matters.** Not checked at present -- the setting is saved, but nothing enforces it when leave is entered or approved.

**Rules.** Optional. Whole number.

## MaxMinutes {#AbsenceType.MaxMinutes}
<!-- alias: loc:MaxMinutes, dom:AbsenceType_MaxMinutes -->

**What it is.** For leave taken in hours: the most minutes in one request.

**Why it matters.** Not checked at present -- the setting is saved, but nothing enforces it when leave is entered or approved.

**Rules.** Optional. Whole number.

## MaxCount {#AbsenceType.MaxCount}
<!-- alias: loc:MaxCount, dom:AbsenceType_MaxCount -->

**What it is.** For leave taken in hours: the most requests in one period.

**Why it matters.** Not checked at present -- the setting is saved, but nothing enforces it when leave is entered or approved.

**Rules.** Optional. Whole number.

## RestrictType {#AbsenceType.PolicyRestrictType}
<!-- alias: loc:RestrictType, dom:AbsenceType_PolicyRestrictType -->

**What it is.** Whether breaking the hourly limits warns or refuses.

**Why it matters.** Not checked at present -- the setting is saved, but nothing enforces it when leave is entered or approved.

**Rules.** Required. One of: Warning, Error!.

## AbsenceViolationRules {#AbsenceType.AbsenceViolationRuleId}
<!-- alias: loc:AbsenceViolationRules, dom:AbsenceType_AbsenceViolationRuleId -->

**What it is.** An absence violation rule linked to this leave type.

**Why it matters.** Not checked at present -- the setting is saved, but nothing enforces it when leave is entered or approved.

**Rules.** Optional. Chosen from Absence Violation Rule master data.

## LeaveGender {#AbsenceType.LeaveGender}
<!-- alias: loc:LeaveGender, dom:AbsenceType_LeaveGender -->

**What it is.** Which gender the leave is for.

**Example.** Female -- maternity leave.

**Why it matters.** Not checked at present -- the setting is saved, but nothing enforces it when leave is entered or approved.

**Rules.** Required. One of: All, Male, Female.

## LeaveReligion {#AbsenceType.LeaveReligion}
<!-- alias: loc:LeaveReligion, dom:AbsenceType_LeaveReligion -->

**What it is.** Which religion the leave is for.

**Example.** Muslim -- Hajj leave.

**Why it matters.** Not checked at present -- the setting is saved, but nothing enforces it when leave is entered or approved.

**Rules.** Required. One of: All, Muslim, Non Muslim.

## QuotaType {#AbsenceType.QuotaType}
<!-- alias: loc:QuotaType, dom:AbsenceType_QuotaType -->

**What it is.** Whether this leave type shares the annual leave or sick leave balance rules.

**Example.** Annual Leave

**Why it matters.** *Annual Leave* balances it like annual leave: accrued day by day through the year, capped by the sub group's maximum. *Sick Leave* balances it like sick leave: a yearly entitlement per cycle, less what has been taken. *None* gives it neither.

**Rules.** Required. One of: None, Annual Leave, Sick Leave.

## LeaveEntitlement {#AbsenceType.LeaveEntitlement}
<!-- alias: loc:LeaveEntitlement, dom:AbsenceType_LeaveEntitlement -->

**What it is.** Whether an employee who joins part-way through a cycle gets the full entitlement or a share of it.

**Example.** Prorated

**Why it matters.** Used for leave balanced like sick leave, and only with the *Calendar Year* accrual period: *Prorated* reduces the first year's entitlement in proportion to the part of the year left after joining.

**Rules.** Required. One of: Full, Prorated.

## AllowBackDatedDays {#AbsenceType.AllowBackDatedDays}
<!-- alias: loc:AllowBackDatedDays, dom:AbsenceType_AllowBackDatedDays -->

**What it is.** How many days back leave may be entered.

**Why it matters.** Not checked at present -- the setting is saved, but nothing enforces it when leave is entered or approved.

**Rules.** Optional. Whole number.

## TotalDaysAllowed {#AbsenceType.TotalDaysAllowed}
<!-- alias: loc:TotalDaysAllowed, dom:AbsenceType_TotalDaysAllowed -->

**What it is.** The yearly entitlement, for employees whose sub group sets none.

**Example.** `30`

**Why it matters.** When the employee's sub group has a leave rule for this type, the sub group's total days are used instead. The leave quota balance report shows the figure in use.

**Rules.** Optional. Number.

## NegativeBalanceLimit {#AbsenceType.NegativeBalanceLimit}
<!-- alias: loc:NegativeBalanceLimit, dom:AbsenceType_NegativeBalanceLimit -->

**What it is.** How far the balance may go below zero.

**Why it matters.** Not checked at present -- the setting is saved, but nothing enforces it when leave is entered or approved.

**Rules.** Optional. Whole number.

## EnableNegativeBalance {#AbsenceType.EnableNegativeBalance}
<!-- alias: loc:EnableNegativeBalance, dom:AbsenceType_EnableNegativeBalance -->

**What it is.** Whether the balance may go below zero.

**Why it matters.** Not checked at present -- the setting is saved, but nothing enforces it when leave is entered or approved.

**Rules.** Yes or no.

## IncludeInSalaryProfile {#AbsenceType.IncludeInSalaryProfile}
<!-- alias: loc:IncludeInSalaryProfile, dom:AbsenceType_IncludeInSalaryProfile -->

**What it is.** Whether the leave appears on the salary profile.

**Why it matters.** Not checked at present -- the setting is saved, but nothing enforces it when leave is entered or approved.

**Rules.** Yes or no.

## AllowDuringProbation {#AbsenceType.AllowDuringProbation}
<!-- alias: loc:AllowDuringProbation, dom:AbsenceType_AllowDuringProbation -->

**What it is.** Whether the leave may be taken during probation.

**Why it matters.** Not checked at present -- the setting is saved, but nothing enforces it when leave is entered or approved.

**Rules.** Yes or no.

## AllowToReturnEarly {#AbsenceType.AllowToReturnEarly}
<!-- alias: loc:AllowToReturnEarly, dom:AbsenceType_AllowToReturnEarly -->

**What it is.** Whether the leave can be cut short.

**Why it matters.** Unticked, a request to shorten the leave or return early is refused: "… must be taken in full, so it cannot be shortened or returned from early."

**Rules.** Yes or no.

## AllowInTerminationNoticeOnly {#AbsenceType.AllowInTerminationNoticeOnly}
<!-- alias: loc:AllowInTerminationNoticeOnly, dom:AbsenceType_AllowInTerminationNoticeOnly -->

**What it is.** Whether the leave may be taken only during a termination notice period.

**Why it matters.** Not checked at present -- the setting is saved, but nothing enforces it when leave is entered or approved.

**Rules.** Yes or no.

## IsReturnDateRequired {#AbsenceType.IsReturnDateRequired}
<!-- alias: loc:IsReturnDateRequired, dom:AbsenceType_IsReturnDateRequired -->

**What it is.** Whether the employee's return from this leave must be recorded.

**Why it matters.** An employee whose leave of this type has ended with no return recorded is treated as not returned. A leave-resumption reminder goes out, when that notification is set up. When the payroll setting *Enable Not Return From Leave Logic* is on, the employee's basic pay is not paid for that run, and the time engine counts their rest days -- and weekly offs, where basic pay is on calendar days -- as absent until the return is recorded.

**Rules.** Yes or no.

## IsAttachmentRequired {#AbsenceType.IsAttachmentRequired}
<!-- alias: loc:IsAttachmentRequired, dom:AbsenceType_IsAttachmentRequired -->

**What it is.** Whether a document must be attached to the leave.

**Why it matters.** Not checked at present -- the setting is saved, but nothing enforces it when leave is entered or approved.

**Rules.** Yes or no.

## AllowPartialLeave {#AbsenceType.AllowPartialLeave}
<!-- alias: loc:AllowPartialLeave, dom:AbsenceType_AllowPartialLeave -->

**What it is.** Whether part of a day may be taken.

**Why it matters.** Not checked at present -- the setting is saved, but nothing enforces it when leave is entered or approved.

**Rules.** Yes or no.

## SendNotification {#AbsenceType.SendNotification}
<!-- alias: loc:SendNotification, dom:AbsenceType_SendNotification -->

**What it is.** Whether a notification is sent for this leave.

**Why it matters.** Not checked at present -- the setting is saved, but nothing enforces it when leave is entered or approved.

**Rules.** Yes or no.

## Sunday {#AbsenceType.DontAllowToStartOnSunday}
<!-- alias: loc:Sunday, dom:AbsenceType_DontAllowToStartOnSunday -->

**What it is.** Whether leave of this type may not start on a Sunday.

**Why it matters.** Not checked at present -- the setting is saved, but nothing enforces it when leave is entered or approved.

**Rules.** Yes or no.

## Monday {#AbsenceType.DontAllowToStartOnMonday}
<!-- alias: loc:Monday, dom:AbsenceType_DontAllowToStartOnMonday -->

**What it is.** Whether leave of this type may not start on a Monday.

**Why it matters.** Not checked at present -- the setting is saved, but nothing enforces it when leave is entered or approved.

**Rules.** Yes or no.

## Tuesday {#AbsenceType.DontAllowToStartOnTuesday}
<!-- alias: loc:Tuesday, dom:AbsenceType_DontAllowToStartOnTuesday -->

**What it is.** Whether leave of this type may not start on a Tuesday.

**Why it matters.** Not checked at present -- the setting is saved, but nothing enforces it when leave is entered or approved.

**Rules.** Yes or no.

## Wednesday {#AbsenceType.DontAllowToStartOnWednesday}
<!-- alias: loc:Wednesday, dom:AbsenceType_DontAllowToStartOnWednesday -->

**What it is.** Whether leave of this type may not start on a Wednesday.

**Why it matters.** Not checked at present -- the setting is saved, but nothing enforces it when leave is entered or approved.

**Rules.** Yes or no.

## Thursday {#AbsenceType.DontAllowToStartOnThursday}
<!-- alias: loc:Thursday, dom:AbsenceType_DontAllowToStartOnThursday -->

**What it is.** Whether leave of this type may not start on a Thursday.

**Why it matters.** Not checked at present -- the setting is saved, but nothing enforces it when leave is entered or approved.

**Rules.** Yes or no.

## Friday {#AbsenceType.DontAllowToStartOnFriday}
<!-- alias: loc:Friday, dom:AbsenceType_DontAllowToStartOnFriday -->

**What it is.** Whether leave of this type may not start on a Friday.

**Why it matters.** Not checked at present -- the setting is saved, but nothing enforces it when leave is entered or approved.

**Rules.** Yes or no.

## Saturday {#AbsenceType.DontAllowToStartOnSaturday}
<!-- alias: loc:Saturday, dom:AbsenceType_DontAllowToStartOnSaturday -->

**What it is.** Whether leave of this type may not start on a Saturday.

**Why it matters.** Not checked at present -- the setting is saved, but nothing enforces it when leave is entered or approved.

**Rules.** Yes or no.

## Code {#AbsenceType.Code}
<!-- alias: loc:Code, dom:AbsenceType_Code -->

**What it is.** The leave type's code.

**Example.** `1` -- annual leave.

**Why it matters.** Payroll and the time engine recognise some leave types by code: `1` annual leave, `2` unpaid leave, `3` sick leave and `15` unapproved absence each have their own rules for balances, indemnity and pay. Use those codes only for those types.
