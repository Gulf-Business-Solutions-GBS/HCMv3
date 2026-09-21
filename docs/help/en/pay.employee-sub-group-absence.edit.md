---
helpId: pay.employee-sub-group-absence.edit
lang: en
title: Employee Sub Group Absences — Create / Edit
entity: HCMv3.PayrollManagement.EmployeeSubGroupAbsence
surfaces:
  - angular/src/app/admin/payroll-management/employee-sub-group-detail/absences/create-or-edit-employee-sub-group-absence-modal.component.html
audience: [admin]
permissions: [Pages.PayrollManagement.EmployeeSubGroupAbsences.Create]
---

## Overview

A sub group's entitlement to one type of leave, from a given date: how many days a year, the most that can be carried, and -- for sick leave -- the deduction rule.

Several rows for the same leave type can step the entitlement up with service. Each row applies while the employee's service is below its **Change Year**; the row with the lowest Change Year above the employee's service wins.

## AbsenceType {#EmployeeSubGroupAbsence.AbsenceTypeId}
<!-- alias: loc:AbsenceType, dom:EmployeeSubGroupAbsence_AbsenceTypeId -->

**What it is.** The type of leave this entitlement is for.

**Example.** Annual Leave

**Rules.** Optional. Chosen from Absence Type master data.

## AbsenceDeductionRule {#EmployeeSubGroupAbsence.AbsenceDeductionRuleId}
<!-- alias: loc:AbsenceDeductionRule, dom:EmployeeSubGroupAbsence_AbsenceDeductionRuleId -->

**What it is.** For sick leave, the steps by which pay is reduced as sick days add up.

**Example.** Kuwait Sick Leave Scale

**Why it matters.** Read only on the sub group's sick leave row -- the type with code 3, or quota type Sick Leave. Left blank, sick leave is paid in full.

**Rules.** Optional. Chosen from Absence Deduction Rule master data.

## ChangeYear {#EmployeeSubGroupAbsence.ChangeYear}
<!-- alias: loc:ChangeYear, dom:EmployeeSubGroupAbsence_ChangeYear -->

**What it is.** The years of service up to which this row applies.

**Example.** `5` -- 30 days a year for the first five years; a second row with `99` gives 45 days after that.

**Why it matters.** Rows are chosen by the employee's service on the day: the lowest Change Year still above it wins. If no row qualifies, any row for the leave type in force that day is used.

**Rules.** Optional. Number.

## TotalDays {#EmployeeSubGroupAbsence.TotalDays}
<!-- alias: loc:TotalDays, dom:EmployeeSubGroupAbsence_TotalDays -->

**What it is.** The days of this leave the employee is entitled to each year.

**Example.** `30`

**Why it matters.** Annual leave accrues day by day at this many days a year. Sick leave gives this many days per cycle. It replaces the absence type's own **Total Days Allowed** for employees in this sub group.

**Rules.** Optional. Whole number.

## MaxLimit {#EmployeeSubGroupAbsence.MaxLimit}
<!-- alias: loc:MaxLimit, dom:EmployeeSubGroupAbsence_MaxLimit -->

**What it is.** The most days of this leave a balance can hold.

**Example.** `60`

**Why it matters.** An annual leave balance above it is cut back to it as the time engine runs, when the time setting forfeit rule is *None*; balances worked out for payroll are capped at it too. 0 means no limit.

**Rules.** Optional. Whole number.

## MaxLimitReachNotificationInDays {#EmployeeSubGroupAbsence.MaxLimitReachNotificationInDays}
<!-- alias: loc:MaxLimitReachNotificationInDays, dom:EmployeeSubGroupAbsence_MaxLimitReachNotificationInDays -->

**What it is.** How far below the maximum the balance must fall before the "maximum reached" notification can be sent again.

**Example.** `5`

**Why it matters.** No effect at present: it only re-arms the annual leave maximum-reached notification, and nothing sends that notification.

**Rules.** Optional. Whole number.
