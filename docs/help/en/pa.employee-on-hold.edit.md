---
helpId: pa.employee-on-hold.edit
lang: en
title: Employee On Holds — Create / Edit
entity: HCMv3.PersonnelAdministration.EmployeeOnHold
surfaces:
  - angular/src/app/admin/personnel-administration/employee-detail/on-holds/create-or-edit-employee-on-hold-modal.component.html
audience: [admin]
permissions: [Pages.PersonnelAdministration.EmployeeOnHolds.Create]
---

## Overview

Holds some or all of the employee's pay for a period. Hold periods for the same employee cannot overlap.

Payroll applies the hold in force on the pay date. Holding everything sets every individual hold at once.

## BankTransfer {#EmployeeOnHold.BankTransfer}
<!-- alias: loc:BankTransfer, dom:EmployeeOnHold_BankTransfer -->

**What it is.** Whether the employee's salary transfer is held.

**Example.** Hold

**Rules.** Required. One of: N/A, Hold, Unhold.

:::admin
Of the bank file formats, only ABK leaves out employees whose transfer is held. The other formats do not check it at present.
:::

## EmployeeActionId {#EmployeeOnHold.EmployeeActionId}
<!-- alias: loc:EmployeeActionId, dom:EmployeeOnHold_EmployeeActionId -->

**What it is.** The personnel action this hold belongs to, if any.

**Why it matters.** Recorded as a link only; payroll does not read it.

**Rules.** Optional. Chosen from a list.

## OnHold {#EmployeeOnHold.OnHold}
<!-- alias: loc:OnHold, dom:EmployeeOnHold_OnHold -->

**What it is.** Holds everything.

**Why it matters.** Payroll treats every hold below as ticked, and the staff-not-in-payroll report lists the employee.

**Rules.** Yes or no.

## OnHoldBasicPay {#EmployeeOnHold.OnHoldBasicPay}
<!-- alias: loc:OnHoldBasicPay, dom:EmployeeOnHold_OnHoldBasicPay -->

**What it is.** Holds basic pay.

**Why it matters.** Payroll pays no basic pay while it is ticked.

**Rules.** Yes or no.

## OnHoldLeavePay {#EmployeeOnHold.OnHoldLeavePay}
<!-- alias: loc:OnHoldLeavePay, dom:EmployeeOnHold_OnHoldLeavePay -->

**What it is.** Holds leave pay.

**Rules.** Yes or no.

## OnHoldIndemnityPay {#EmployeeOnHold.OnHoldIndemnityPay}
<!-- alias: loc:OnHoldIndemnityPay, dom:EmployeeOnHold_OnHoldIndemnityPay -->

**What it is.** Holds indemnity pay.

**Rules.** Yes or no.

## OnHoldAirTicketPay {#EmployeeOnHold.OnHoldAirTicketPay}
<!-- alias: loc:OnHoldAirTicketPay, dom:EmployeeOnHold_OnHoldAirTicketPay -->

**What it is.** Holds air ticket pay.

**Why it matters.** Recorded only; payroll does not currently check it when paying tickets.

**Rules.** Yes or no.

## OnHoldLoanDeduction {#EmployeeOnHold.OnHoldLoanDeduction}
<!-- alias: loc:OnHoldLoanDeduction, dom:EmployeeOnHold_OnHoldLoanDeduction -->

**What it is.** Holds loan instalment deductions.

**Why it matters.** Payroll does not recover loans while it is ticked.

**Rules.** Yes or no.

## OnHoldAllEarnings {#EmployeeOnHold.OnHoldAllEarnings}
<!-- alias: loc:OnHoldAllEarnings, dom:EmployeeOnHold_OnHoldAllEarnings -->

**What it is.** Holds all earnings.

**Rules.** Yes or no.

## OnHoldAllDeductions {#EmployeeOnHold.OnHoldAllDeductions}
<!-- alias: loc:OnHoldAllDeductions, dom:EmployeeOnHold_OnHoldAllDeductions -->

**What it is.** Holds all deductions.

**Rules.** Yes or no.
