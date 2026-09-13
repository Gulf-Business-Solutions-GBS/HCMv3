---
helpId: pa.employee-substitute-duty.edit
lang: en
title: Employee Substitute Duties — Create / Edit
entity: HCMv3.TimeManagement.EmployeeSubstituteDuty
surfaces:
  - angular/src/app/admin/personnel-administration/employee-detail/substitute-duties/create-or-edit-employee-substitute-duty-modal.component.html
audience: [admin]
permissions: [Pages.TimeManagement.EmployeeSubstituteDuties.Create]
---

## Overview

A different duty for the employee on one specific date -- for example covering a night shift. One substitute duty per employee per date.

A substitute duty takes priority over the employee's duty period for that date, wherever the employee's duty for the day is looked up: leave working days, accruals, time data and payroll day counts.

## SubstituteDate {#EmployeeSubstituteDuty.SubstituteDate}
<!-- alias: loc:SubstituteDate, dom:EmployeeSubstituteDuty_SubstituteDate -->

**What it is.** The date the substitute duty applies to.

**Example.** 14 Aug 2026

**Why it matters.** Only one substitute duty can exist for the employee on this date.

**Rules.** Required. Date value.

## Duty {#EmployeeSubstituteDuty.DutyId}
<!-- alias: loc:Duty, dom:EmployeeSubstituteDuty_DutyId -->

**What it is.** The duty worked on that date.

**Example.** Night Shift 22:00–06:00

**Rules.** Required. Chosen from Duty master data.

:::admin
Maintained under Time Management → Duties.
:::
