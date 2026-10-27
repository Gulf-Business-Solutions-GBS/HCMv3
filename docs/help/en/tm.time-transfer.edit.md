---
helpId: tm.time-transfer.edit
lang: en
title: Time Transfers — Create / Edit
entity: HCMv3.TimeManagement.TimeTransfer
surfaces:
  - angular/src/app/admin/time-management/time-transfers/create-or-edit-time-transfer-modal.component.html
audience: [admin]
permissions: [Pages.TimeManagement.TimeTransfers.Create]
---

## Overview

A manual adjustment to one of an employee's time balances on a date. What it adjusts depends on its transfer type, which payroll and the time engine recognise by code:

- `1` adds to the annual leave accrued on that date.
- `2` counts as sick leave already taken in the cycle.
- `5` counts as absence days taken off the service indemnity is counted on.
- `6` posts an opening amount to the employee's fund in that pay period.

## Employee {#TimeTransfer.EmployeeId}
<!-- alias: loc:Employee, dom:TimeTransfer_EmployeeId -->

**What it is.** The employee whose balance is adjusted.

**Rules.** Required. Chosen from the employee list.

## TransferDate {#TimeTransfer.TransferDate}
<!-- alias: loc:TransferDate, dom:TimeTransfer_TransferDate -->

**What it is.** The date the adjustment takes effect.

**Example.** `2026-01-01`

**Rules.** Required. Date value.

## TransferType {#TimeTransfer.TransferTypeId}
<!-- alias: loc:TransferType, dom:TimeTransfer_TransferTypeId -->

**What it is.** Which balance is adjusted.

**Example.** Annual Leave Opening Balance

**Rules.** Required. Chosen from Time Transfer Type master data.

## TransferCount {#TimeTransfer.TransferCount}
<!-- alias: loc:TransferCount, dom:TimeTransfer_TransferCount -->

**What it is.** The days -- or, for a fund transfer, the amount -- of the adjustment.

**Example.** `12.5`

**Why it matters.** Rounded to 3 decimal places when saved.

**Rules.** Optional. Number.
