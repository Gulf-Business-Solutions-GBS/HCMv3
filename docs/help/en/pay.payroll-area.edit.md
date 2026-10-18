---
helpId: pay.payroll-area.edit
lang: en
title: Payroll Areas — Create / Edit
entity: HCMv3.PayrollManagement.PayrollArea
surfaces:
  - angular/src/app/admin/payroll-management/payroll-areas/create-or-edit-payroll-area-modal.component.html
audience: [admin]
permissions: [Pages.PayrollManagement.PayrollAreas.Create]
---

## Overview

A payroll area is a set of employees paid together: on the same fiscal calendar, in the same currency, run by one payroll control record. Each employee's organisation assignment names the payroll area they belong to.

The area's control record says which month is being processed and whether that month is open, released for payroll or closed. Its FI settings feed the journal voucher that posts the payroll to finance.

## Name {#PayrollArea.Name}
<!-- alias: loc:Name, dom:PayrollArea_Name -->

**What it is.** The name of the payroll area. It has one name only, with no separate Arabic name.

**Example.** `Kuwait Monthly`

**Why it matters.** A payroll run, the costing file and the journal voucher all pick the area by this name, and it appears in the messages that refuse a change while the area is locked.

**Rules.** Required.

## PayrollPeriodId {#PayrollArea.PayrollPeriodId}
<!-- alias: loc:PayrollPeriodId, dom:PayrollArea_PayrollPeriodId -->

**What it is.** The fiscal year the area runs on, such as January–December or April–March.

**Example.** `Apr-Mar`

**Why it matters.** It decides which calendar month a payroll month means on the control record. With April–March, payroll month 1 of 2026 is April 2026 and payroll month 10 is January 2027.

**Rules.** Required. Chosen from Payroll Period master data.

:::admin
The control record recognises twelve period codes only, `Jan-Dec` through `Dec-Nov`. Any other code makes the control record refuse to save with "Incorrect Payroll Period".
:::

## CurrencyId {#PayrollArea.CurrencyId}
<!-- alias: loc:CurrencyId, dom:PayrollArea_CurrencyId -->

**What it is.** The currency the area is paid in.

**Example.** Kuwaiti Dinar

**Why it matters.** Printed as the currency of the net pay line on the journal voucher.

**Rules.** Required. Chosen from Currency master data.

## CutOffDay {#PayrollArea.CutOffDay}
<!-- alias: loc:CutOffDay, dom:PayrollArea_CutOffDay -->

**What it is.** The day of the month on which the pay month ends for time-based counts, when it is not the calendar month end. 0 means the calendar month.

**Example.** `20` -- the April run counts 21 March to 20 April.

**Why it matters.** For wagetypes marked *Include In Cutoff*, days such as unpaid absence are counted from the day after the previous month's cut-off day to this month's cut-off day, instead of the calendar month. When the payroll setting *Pay Leave As Per Cutoff* is on, it also decides whether a leave the employee has not yet returned from is dealt with in this month's regular run: only leave starting on or before the cut-off day is.

**Rules.** Required. Whole number.

:::admin
A cut-off day later than the month has days means the last day of that month -- 31 in February is 28 or 29.
:::

## CountryId {#PayrollArea.CountryId}
<!-- alias: loc:CountryId, dom:PayrollArea_CountryId -->

**What it is.** The country the payroll area belongs to.

**Example.** Kuwait

**Why it matters.** Payroll does not use it. It is shown as the country on the performance appraisal reviewer summary report.

**Rules.** Optional. Chosen from Country master data.

## FICompanyId {#PayrollArea.FICompanyId}
<!-- alias: loc:FICompanyId, dom:PayrollArea_FICompanyId -->

**What it is.** The finance company code the area's payroll is posted under, chosen from the cost elements.

**Example.** `KW01`

**Why it matters.** Written as the company on the net pay line of the journal voucher. Posting the journal voucher and producing the costing file are refused for the area while it is blank.

**Rules.** Optional. Chosen from Cost Element master data.

## FIBankId {#PayrollArea.FIBankId}
<!-- alias: loc:FIBankId, dom:PayrollArea_FIBankId -->

**What it is.** The bank account the area's total net pay is credited to on the journal voucher.

**Example.** `110200 -- NBK Salary Account`

**Why it matters.** The journal voucher credits the whole net pay of the run to this account, with the account's type beside it. Posting the journal voucher and producing the costing file are refused for the area while it is blank.

**Rules.** Optional. Chosen from Account Info master data.

## PAReleaseNotification {#PayrollArea.PAReleaseNotificationId}
<!-- alias: loc:PAReleaseNotification, dom:PayrollArea_PAReleaseNotificationId -->

**What it is.** The notification sent when this area's payroll control record is released for payroll.

**Example.** Payroll Open for Processing

**Why it matters.** Sent each time the control record is released for payroll. It is about the control record, not an employee, so it reaches the configuration's notification groups and other fixed recipients. Nothing is sent unless the notification configuration has a notification group.

**Rules.** Optional. Chosen from Notification Config master data.

## PAExitNotification {#PayrollArea.PAExitNotificationId}
<!-- alias: loc:PAExitNotification, dom:PayrollArea_PAExitNotificationId -->

**What it is.** The notification sent when this area's payroll control record exits payroll and the month is closed.

**Example.** Payroll Closed

**Why it matters.** Sent each time the control record exits payroll. Nothing is sent unless the notification configuration has a notification group.

**Rules.** Optional. Chosen from Notification Config master data.

## PACorrectionNotification {#PayrollArea.PACorrectionNotificationId}
<!-- alias: loc:PACorrectionNotification, dom:PayrollArea_PACorrectionNotificationId -->

**What it is.** The notification sent when this area's payroll control record is released for correction.

**Example.** Payroll Open for Correction

**Why it matters.** Sent each time the control record is released for correction. Nothing is sent unless the notification configuration has a notification group.

**Rules.** Optional. Chosen from Notification Config master data.

:::admin
For all three notifications: email goes when the configuration has Send Email ticked and an email template, and the in-app notification when Send Notification is ticked. A configuration with two notification groups sends everything twice, as the earlier system did. A failed send never undoes the change of status.
:::
