---
helpId: pa.employee-fn-fpayment.edit
lang: en
title: Employee Full & Final Payments — Create / Edit
entity: HCMv3.PersonnelAdministration.EmployeeFnFPayment
surfaces:
  - angular/src/app/admin/personnel-administration/employee-detail/fnf-payments/create-or-edit-employee-fnf-payment-modal.component.html
audience: [admin]
permissions: [Pages.PersonnelAdministration.EmployeeFnFPayments.Create]
---

## Overview

The employee's full and final settlement at the end of service.

An end-of-service payroll run includes employees whose compensation date falls in its period, and needs a settlement whose compensation date equals the run's payment date. The amounts, days and counts on this form are recorded for reference: payroll calculates the settlement itself and does not read them at present.

When the employee still owes on a loan, the form shows **Settle the open loans in this payment**. Tick it to recover the whole remaining loan balance on the compensation date; without it, an employee with an open loan cannot be settled.

## CompensationDate {#EmployeeFnFPayment.CompensationDate}
<!-- alias: loc:CompensationDate, dom:EmployeeFnFPayment_CompensationDate -->

**What it is.** The date the settlement is paid.

**Example.** 15 Sep 2026

**Why it matters.** Decides which end-of-service run includes the employee. The employee information report shows the settlement as done once this date has passed.

**Rules.** Required. Date value.

## EmployeeActionId {#EmployeeFnFPayment.EmployeeActionId}
<!-- alias: loc:EmployeeActionId, dom:EmployeeFnFPayment_EmployeeActionId -->

**What it is.** The personnel action this settlement belongs to, if any.

**Why it matters.** Recorded only; payroll and reports do not read it at present.

**Rules.** Optional. Chosen from a list.

## EmployeeBasicPayDetailId {#EmployeeFnFPayment.EmployeeBasicPayDetailId}
<!-- alias: loc:EmployeeBasicPayDetailId, dom:EmployeeFnFPayment_EmployeeBasicPayDetailId -->

**What it is.** The basic pay line the settlement is based on, if any.

**Why it matters.** Recorded only; payroll and reports do not read it at present.

**Rules.** Optional. Chosen from a list.

## LeaveCompensateDays {#EmployeeFnFPayment.LeaveCompensateDays}
<!-- alias: loc:LeaveCompensateDays, dom:EmployeeFnFPayment_LeaveCompensateDays -->

**What it is.** Leave days compensated.

**Example.** 18

**Why it matters.** Recorded only; payroll and reports do not read it at present.

**Rules.** Optional. Number.

## GratuityCompensateDays {#EmployeeFnFPayment.GratuityCompensateDays}
<!-- alias: loc:GratuityCompensateDays, dom:EmployeeFnFPayment_GratuityCompensateDays -->

**What it is.** Indemnity days compensated.

**Example.** 255

**Why it matters.** Recorded only; payroll and reports do not read it at present.

**Rules.** Optional. Number.

## IndemnityCompensateAmount {#EmployeeFnFPayment.IndemnityCompensateAmount}
<!-- alias: loc:IndemnityCompensateAmount, dom:EmployeeFnFPayment_IndemnityCompensateAmount -->

**What it is.** The indemnity amount compensated.

**Example.** 4250.000

**Why it matters.** Recorded only; payroll and reports do not read it at present.

**Rules.** Optional. Number.

## IndemnityPayAdjustment {#EmployeeFnFPayment.IndemnityPayAdjustment}
<!-- alias: loc:IndemnityPayAdjustment, dom:EmployeeFnFPayment_IndemnityPayAdjustment -->

**What it is.** An adjustment to the indemnity pay.

**Example.** -50.000

**Why it matters.** Recorded only; payroll and reports do not read it at present.

**Rules.** Optional. Number.

## TicketCompensateCount {#EmployeeFnFPayment.TicketCompensateCount}
<!-- alias: loc:TicketCompensateCount, dom:EmployeeFnFPayment_TicketCompensateCount -->

**What it is.** Employee tickets compensated.

**Example.** 1

**Why it matters.** Recorded only; payroll and reports do not read it at present.

**Rules.** Optional. Number.

## Dependant1TicketCompensateCount {#EmployeeFnFPayment.Dependant1TicketCompensateCount}
<!-- alias: loc:Dependant1TicketCompensateCount, dom:EmployeeFnFPayment_Dependant1TicketCompensateCount -->

**What it is.** Tickets compensated for the first dependant.

**Example.** 1

**Why it matters.** Recorded only; payroll and reports do not read it at present.

**Rules.** Optional. Number.

## Dependant2TicketCompensateCount {#EmployeeFnFPayment.Dependant2TicketCompensateCount}
<!-- alias: loc:Dependant2TicketCompensateCount, dom:EmployeeFnFPayment_Dependant2TicketCompensateCount -->

**What it is.** Tickets compensated for the second dependant.

**Example.** 1

**Why it matters.** Recorded only; payroll and reports do not read it at present.

**Rules.** Optional. Number.

## Dependant3TicketCompensateCount {#EmployeeFnFPayment.Dependant3TicketCompensateCount}
<!-- alias: loc:Dependant3TicketCompensateCount, dom:EmployeeFnFPayment_Dependant3TicketCompensateCount -->

**What it is.** Tickets compensated for the third dependant.

**Example.** 0

**Why it matters.** Recorded only; payroll and reports do not read it at present.

**Rules.** Optional. Number.

## CompensateLoanAmount {#EmployeeFnFPayment.CompensateLoanAmount}
<!-- alias: loc:CompensateLoanAmount, dom:EmployeeFnFPayment_CompensateLoanAmount -->

**What it is.** The loan amount settled.

**Example.** 300.000

**Why it matters.** Recorded only; payroll and reports do not read it at present.

**Rules.** Optional. Number.

## CompensateBonusPay {#EmployeeFnFPayment.CompensateBonusPay}
<!-- alias: loc:CompensateBonusPay, dom:EmployeeFnFPayment_CompensateBonusPay -->

**What it is.** The bonus amount settled.

**Example.** 500.000

**Why it matters.** Recorded only; payroll and reports do not read it at present.

**Rules.** Optional. Number.
