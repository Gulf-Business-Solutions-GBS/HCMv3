---
helpId: pa.employee-absence.edit
lang: en
title: Employee Absences — Create / Edit
entity: HCMv3.PersonnelAdministration.EmployeeAbsence
surfaces:
  - angular/src/app/admin/personnel-administration/employee-detail/absences/create-or-edit-employee-absence-modal.component.html
audience: [admin]
permissions: [Pages.PersonnelAdministration.EmployeeAbsences.Create]
---

## Overview

A period of leave or absence for the employee. Absences for the same employee cannot overlap.

Payroll pays leave according to the payment type and payment date, and the time engine marks the days as leave.

An absence entered on this form does not change the Used figure on the employee's leave quota, so the self-service leave balance does not go down. Leave requested and approved through self-service does update it.

## AbsenceTypeId {#EmployeeAbsence.AbsenceTypeId}
<!-- alias: loc:AbsenceTypeId, dom:EmployeeAbsence_AbsenceTypeId -->

**What it is.** The kind of leave.

**Example.** Annual Leave

**Why it matters.** Decides how the days are paid and which quota they belong to.

**Rules.** Required. Chosen from Absence Type master data.

:::admin
Maintained under Payroll Management → Absence Types.
:::

## HalfdayType {#EmployeeAbsence.HalfdayType}
<!-- alias: loc:HalfdayType, dom:EmployeeAbsence_HalfdayType -->

**What it is.** Whether the leave is a half day, and which half.

**Example.** Start Of Duty

**Why it matters.** Shown on the monthly attendance with tolerance reports.

**Rules.** Required. One of: None, Start Of Duty, End Of Duty.

## StartTime {#EmployeeAbsence.StartTime}
<!-- alias: loc:StartTime, dom:EmployeeAbsence_StartTime -->

**What it is.** The time the leave starts, for leave taken in hours.

**Example.** 08:00

**Rules.** Optional. Date value.

:::admin
The time and payroll engines treat an absence as hourly only when its absence hours are set, which this form does not do. An absence saved here counts as full days even if times are entered.
:::

## EndTime {#EmployeeAbsence.EndTime}
<!-- alias: loc:EndTime, dom:EmployeeAbsence_EndTime -->

**What it is.** The time the leave ends, for leave taken in hours.

**Example.** 12:00

**Rules.** Optional. Date value.

## LeavePaymentType {#EmployeeAbsence.LeavePaymentType}
<!-- alias: loc:LeavePaymentType, dom:EmployeeAbsence_LeavePaymentType -->

**What it is.** When the leave is paid.

**Example.** Pay In Advance

**Why it matters.** Pay In Advance is paid in an off-cycle payroll on the payment date. Pay Full With Payroll pays leave that runs into later months in the payroll whose pay date matches the payment date.

**Rules.** Required. One of: None, Pay With Payroll, Pay In Advance, Pay Full With Payroll.

## PaymentDate {#EmployeeAbsence.PaymentDate}
<!-- alias: loc:PaymentDate, dom:EmployeeAbsence_PaymentDate -->

**What it is.** The date the leave pay is due.

**Example.** 25 Jun 2026

**Why it matters.** Payroll matches it against the pay date of the run that should pay this leave. Advance-payment reminders can also be timed from it.

**Rules.** Optional. Date value.

## ReturnDate {#EmployeeAbsence.ReturnDate}
<!-- alias: loc:ReturnDate, dom:EmployeeAbsence_ReturnDate -->

**What it is.** The date the employee actually came back from leave.

**Example.** 16 Jul 2026

**Rules.** Optional. Date value.

## IsTravelling {#EmployeeAbsence.IsTravelling}
<!-- alias: loc:IsTravelling, dom:EmployeeAbsence_IsTravelling -->

**What it is.** Whether the employee is travelling during the leave.

**Why it matters.** Available to business rules.

**Rules.** Yes or no.

## AdvanceLeavePaid {#EmployeeAbsence.AdvanceLeavePaid}
<!-- alias: loc:AdvanceLeavePaid, dom:EmployeeAbsence_AdvanceLeavePaid -->

**What it is.** Whether the advance leave pay has already been paid.

**Why it matters.** Payroll ticks it when it pays the advance. Advance-payment reminders are sent only while it is not ticked.

**Rules.** Yes or no.

## IsReturned {#EmployeeAbsence.IsReturned}
<!-- alias: loc:IsReturned, dom:EmployeeAbsence_IsReturned -->

**What it is.** Whether the employee has returned from this leave.

**Rules.** Yes or no.

:::admin
For leave types that require a return date, an absence that has ended with neither a return date nor this box ticked is treated as not returned from leave: return reminders are sent, and payroll handles the employee as not returned when that payroll setting is on.
:::

## ExtAbsenceTypeId {#EmployeeAbsence.ExtAbsenceTypeId}
<!-- alias: loc:ExtAbsenceTypeId, dom:EmployeeAbsence_ExtAbsenceTypeId -->

**What it is.** The leave type an extension of this absence is converted to -- for example excess annual leave taken as unpaid.

**Example.** Unpaid Leave

**Why it matters.** Recorded only; no calculation reads the extension at present.

**Rules.** Optional. Chosen from Absence Type master data.

## ExtStartDate {#EmployeeAbsence.ExtStartDate}
<!-- alias: loc:ExtStartDate, dom:EmployeeAbsence_ExtStartDate -->

**What it is.** The first day of the extension.

**Example.** 11 Jul 2026

**Rules.** Optional. Date value.

## ExtEndDate {#EmployeeAbsence.ExtEndDate}
<!-- alias: loc:ExtEndDate, dom:EmployeeAbsence_ExtEndDate -->

**What it is.** The last day of the extension.

**Example.** 15 Jul 2026

**Rules.** Optional. Date value.
