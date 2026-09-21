---
helpId: pay.absence-payment.edit
lang: en
title: Absence Payments — Create / Edit
entity: HCMv3.PayrollManagement.AbsencePayment
surfaces:
  - angular/src/app/admin/payroll-management/absence-payments/create-or-edit-absence-payment-modal.component.html
audience: [admin]
permissions: [Pages.PayrollManagement.AbsencePayments.Create]
---

## Overview

Which wagetype pays a type of absence. Not read at present: leave is paid under the wagetype set on the absence type itself, and nothing reads this list.

## AbsenceTypes {#AbsencePayment.AbsenceTypeId}
<!-- alias: loc:AbsenceTypes, dom:AbsencePayment_AbsenceTypeId -->

**What it is.** The absence type.

**Example.** Annual Leave

**Rules.** Required. Chosen from Absence Type master data.

## WageTypes {#AbsencePayment.WageTypeId}
<!-- alias: loc:WageTypes, dom:AbsencePayment_WageTypeId -->

**What it is.** The wagetype it would be paid under.

**Example.** Annual Leave Pay

**Rules.** Required. Chosen from Wagetype master data.
