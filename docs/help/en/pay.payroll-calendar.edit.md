---
helpId: pay.payroll-calendar.edit
lang: en
title: Payroll Calendars — Create / Edit
entity: HCMv3.PayrollManagement.PayrollCalendar
surfaces:
  - angular/src/app/admin/payroll-management/payroll-calendars/create-or-edit-payroll-calendar-modal.component.html
audience: [admin]
permissions: [Pages.PayrollManagement.PayrollCalendars.Create]
---

## Overview

One payroll month of a payroll area, with the dates it runs from and to.

A payroll run is refused when there is no calendar row for the month and year of its pay date ("Payroll Calender does not have record for Payroll month you are running."). The end-of-service report uses the row's end date as its report period.

## PayrollAreaId {#PayrollCalendar.PayrollAreaId}
<!-- alias: loc:PayrollAreaId, dom:PayrollCalendar_PayrollAreaId -->

**What it is.** The payroll area this calendar month belongs to.

**Example.** Kuwait Monthly

**Rules.** Required. Chosen from Payroll Area master data.

:::admin
The payroll run looks for a row by month and year only; it does not check that the row belongs to the area being run. A row for any area lets the run start. This matches the legacy system and is kept on purpose.
:::

## PayrollMonth {#PayrollCalendar.PayrollMonth}
<!-- alias: loc:PayrollMonth, dom:PayrollCalendar_PayrollMonth -->

**What it is.** The month number this row covers, 1 to 12.

**Example.** `4`

**Why it matters.** The payroll run matches it against the calendar month of the pay date.

**Rules.** Required. Whole number.

:::admin
This is the calendar month, not the fiscal month used on the payroll control record. For a January–December area the two are the same; for any other fiscal year they differ.
:::

## PayrollYear {#PayrollCalendar.PayrollYear}
<!-- alias: loc:PayrollYear, dom:PayrollCalendar_PayrollYear -->

**What it is.** The year this row covers.

**Example.** `2026`

**Why it matters.** The payroll run matches it against the calendar year of the pay date.

**Rules.** Required. Whole number.
