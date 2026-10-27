---
helpId: sys.payroll-driver.edit
lang: en
title: Payroll drivers — Create / Edit
entity: HCMv3.SystemSettings.PayrollDriver
surfaces:
  - angular/src/app/admin/system-settings/payroll-drivers/create-or-edit-payroll-driver-modal.component.html
audience: [admin]
permissions: [Pages.SystemSettings.PayrollDrivers.Create]
---

## Overview

A per-country payroll setting. Not read at present: the leave-deduction behaviour it names is taken from **Time Settings**, and nothing reads this list.

## CountryId {#PayrollDriver.CountryId}
<!-- alias: loc:CountryId, dom:PayrollDriver_CountryId -->

**What it is.** The country the setting is for.

**Example.** Kuwait

**Rules.** Required. Chosen from a list.

## LeaveDeductionStartMonth {#PayrollDriver.LeaveDeductionStartMonth}
<!-- alias: loc:LeaveDeductionStartMonth, dom:PayrollDriver_LeaveDeductionStartMonth -->

**What it is.** Whether leave is deducted from the month it starts in.

**Why it matters.** Not read at present -- changing it has no effect.

**Rules.** Yes or no.
