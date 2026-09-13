---
helpId: pa.employee-duty-period.edit
lang: en
title: Employee Duty Periods — Create / Edit
entity: HCMv3.TimeManagement.EmployeeDutyPeriod
surfaces:
  - angular/src/app/admin/personnel-administration/employee-detail/duty-periods/create-or-edit-employee-duty-period-modal.component.html
audience: [admin]
permissions: [Pages.TimeManagement.EmployeeDutyPeriods.Create]
---

## Overview

The duty pattern the employee works from a given date. Saving a new one closes the previous one on the day before the new start date.

For each day, the employee's duty comes from a substitute duty for that exact date if there is one, otherwise from the duty period in force. It decides working days for leave, the time engine's expected shift, and payroll day counts.

## DutyPeriod {#EmployeeDutyPeriod.DutyPeriodId}
<!-- alias: loc:DutyPeriod, dom:EmployeeDutyPeriod_DutyPeriodId -->

**What it is.** The duty period -- the weekly pattern of duties -- assigned from this date.

**Example.** Sunday–Thursday 8:00–16:00

**Why it matters.** The duty for each day is taken from this period's pattern for that day of the week.

**Rules.** Required. Chosen from Duty Period master data.

:::admin
Rotating patterns need a rotation start point, which this form does not set; an assignment saved here always uses the day-of-week pattern. Duty periods are maintained under Time Management → Duty Periods.
:::
