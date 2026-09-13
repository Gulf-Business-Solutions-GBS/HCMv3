---
helpId: pa.employee-date.edit
lang: en
title: Employee Dates — Create / Edit
entity: HCMv3.PersonnelAdministration.EmployeeDate
surfaces:
  - angular/src/app/admin/personnel-administration/employee-detail/dates/create-or-edit-employee-date-modal.component.html
audience: [admin]
permissions: [Pages.PersonnelAdministration.EmployeeDates.Create]
---

## Overview

A significant date for the employee -- for example a medical check-up or a contract review -- with an optional number of days.

It is recorded for reference; no calculation or report reads it at present.

## DateType {#EmployeeDate.DateTypeId}
<!-- alias: loc:DateType, dom:EmployeeDate_DateTypeId -->

**What it is.** The kind of date.

**Example.** Medical check-up

**Rules.** Optional. Chosen from Date Type master data.

:::admin
Maintained under General Master Info → Date Types.
:::

## DateData {#EmployeeDate.DateData}
<!-- alias: loc:DateData, dom:EmployeeDate_DateData -->

**What it is.** The date itself.

**Example.** 15 Jan 2027

**Rules.** Required. Date value.

## DayData {#EmployeeDate.DayData}
<!-- alias: loc:DayData, dom:EmployeeDate_DayData -->

**What it is.** A number of days recorded with the date.

**Example.** 0

**Rules.** Optional. Number.
