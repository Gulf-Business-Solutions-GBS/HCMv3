---
helpId: pay.employee-sub-group.edit
lang: en
title: Employee Sub Groups — Create / Edit
entity: HCMv3.PayrollManagement.EmployeeSubGroup
surfaces:
  - angular/src/app/admin/payroll-management/employee-sub-groups/create-or-edit-employee-sub-group-modal.component.html
audience: [admin]
permissions: [Pages.PayrollManagement.EmployeeSubGroups.Create]
---

## Overview

A finer grouping of employees under an employee group, and the place most benefit rules hang from: leave, gratuity, air tickets, health reimbursement, bonus and other entitlements are all set per sub group on its own tabs. Each employee's organisation assignment names their sub group.

The fields on this form are the sub group's general settings for overtime, air tickets and health reimbursement.

## EmployeeGroup {#EmployeeSubGroup.EmployeeGroupId}
<!-- alias: loc:EmployeeGroup, dom:EmployeeSubGroup_EmployeeGroupId -->

**What it is.** The employee group this sub group belongs to.

**Example.** Staff

**Why it matters.** Groups the sub groups; the sub group list can be filtered by it.

**Rules.** Required. Chosen from a list.

## TicketClass {#EmployeeSubGroup.TicketClassId}
<!-- alias: loc:TicketClass, dom:EmployeeSubGroup_TicketClassId -->

**What it is.** The class of air travel the sub group is entitled to.

**Example.** Economy

**Why it matters.** When the air ticket pay type is *FareAmount*, the ticket is priced from the fare for this class on the employee's ticket route. Left blank, a fare-priced ticket is worth 0.

**Rules.** Optional. Chosen from a list.

## PayOvertime {#EmployeeSubGroup.PayOvertime}
<!-- alias: loc:PayOvertime, dom:EmployeeSubGroup_PayOvertime -->

**What it is.** Whether employees in this sub group are paid for overtime.

**Why it matters.** Ticked, the time engine turns overtime it finds on a day into an employee overtime record, which is then approved and paid. Unticked, no overtime record is created, so no overtime is paid however long the employee works.

**Rules.** Yes or no.

:::admin
With it ticked, a day with overtime needs an overtime type -- set on the duty, or on the holiday for a public holiday. Without one the time engine raises an error for that day asking for one.
:::

## AirTicketPayType {#EmployeeSubGroup.AirTicketPayType}
<!-- alias: loc:AirTicketPayType, dom:EmployeeSubGroup_AirTicketPayType -->

**What it is.** How the value of one air ticket is worked out for this sub group.

**Example.** Fix Amount

**Why it matters.** *FareAmount* prices the ticket from the fare table for the ticket class and the employee's route. *Fix Amount* uses the fixed amount on the sub group's ticket rule. *Actual Amount* divides what was actually paid by the number of tickets, falling back to the rule's maximum amount when nothing was paid. *ActualAmountWithinMaxlimit* does the same division but never goes above the rule's maximum amount. The average-ticket wagetype is priced this way.

**Rules.** Required. One of: FareAmount, Fix Amount, Actual Amount, ActualAmountWithinMaxlimit.

## AutoPayAirTicket {#EmployeeSubGroup.AutoPayAirTicket}
<!-- alias: loc:AutoPayAirTicket, dom:EmployeeSubGroup_AutoPayAirTicket -->

**What it is.** Whether air tickets are paid automatically when they fall due.

**Why it matters.** Ticked, the time engine adds an employee ticket for each ticket type on the first day of that ticket's accrual period -- the employee's own ticket and their dependants' -- without anyone entering it. Benefit exceptions for the employee or a dependant are respected; an exception of 0 means not entitled.

**Rules.** Yes or no.

:::admin
A ticket type with no ticket rule on the sub group is paid automatically only when the employee has an exception for it.
:::

## MaxDependantAirTicketCount {#EmployeeSubGroup.MaxDependantAirTicketCount}
<!-- alias: loc:MaxDependantAirTicketCount, dom:EmployeeSubGroup_MaxDependantAirTicketCount -->

**What it is.** The most dependant air tickets paid automatically at one time.

**Example.** `3`

**Why it matters.** When automatic air ticket pay reaches this many dependant tickets it stops adding more. 0 means no limit.

**Rules.** Optional. Whole number.

## MaxHealthReimburseAmount {#EmployeeSubGroup.MaxHealthReimburseAmount}
<!-- alias: loc:MaxHealthReimburseAmount, dom:EmployeeSubGroup_MaxHealthReimburseAmount -->

**What it is.** The most an employee in this sub group can be reimbursed for health expenses in one health insurance year.

**Example.** `500`

**Why it matters.** Caps the employee's health reimbursement quota, after any balance carried forward from the previous year is added. 0 means no cap.

**Rules.** Optional. Number.

## HealthInsurancePeriod {#EmployeeSubGroup.HealthInsurancePeriod}
<!-- alias: loc:HealthInsurancePeriod, dom:EmployeeSubGroup_HealthInsurancePeriod -->

**What it is.** The month the health insurance year starts.

**Example.** January

**Why it matters.** The reimbursement quota resets every year from the first of this month. *None* makes each employee's year run from their joining date instead.

**Rules.** Required. One of: None, January, February, March, April, May, June, July, August, September, October, November, December.

## MaxDependantHealthReimburseCount {#EmployeeSubGroup.MaxDependantHealthReimburseCount}
<!-- alias: loc:MaxDependantHealthReimburseCount, dom:EmployeeSubGroup_MaxDependantHealthReimburseCount -->

**What it is.** The most dependants whose health reimbursement entitlement counts towards the employee's amount.

**Example.** `4`

**Why it matters.** The eligible amount adds up the sub group's per-dependant entitlements for the employee's dependants, up to this many. 0 means no limit.

**Rules.** Optional. Whole number.
