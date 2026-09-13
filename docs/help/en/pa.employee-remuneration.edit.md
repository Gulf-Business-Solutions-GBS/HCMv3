---
helpId: pa.employee-remuneration.edit
lang: en
title: Employee Remunerations — Create / Edit
entity: HCMv3.PersonnelAdministration.EmployeeRemuneration
surfaces:
  - angular/src/app/admin/personnel-administration/employee-detail/remunerations/create-or-edit-employee-remuneration-modal.component.html
audience: [admin]
permissions: [Pages.PersonnelAdministration.EmployeeRemunerations.Create]
---

## Overview

Days or hours to be paid for the employee on a date, for employees paid by the day or hour.

When the employee's basic pay interval is Daily or Hourly, payroll adds up the days or hours recorded here in the month and pays them at the basic pay rate.

## WageTypeId {#EmployeeRemuneration.WageTypeId}
<!-- alias: loc:WageTypeId, dom:EmployeeRemuneration_WageTypeId -->

**What it is.** A wage type recorded with the entry.

**Example.** Basic Salary

**Why it matters.** The daily and hourly calculation counts every entry in the month, whatever its wage type.

**Rules.** Optional. Chosen from Wage Type master data.

## PaymentDate {#EmployeeRemuneration.PaymentDate}
<!-- alias: loc:PaymentDate, dom:EmployeeRemuneration_PaymentDate -->

**What it is.** The date the days or hours belong to.

**Example.** 15 Sep 2026

**Why it matters.** Entries dated in the payroll month are counted.

**Rules.** Required. Date value.

## PaymentNumber {#EmployeeRemuneration.PaymentNumber}
<!-- alias: loc:PaymentNumber, dom:EmployeeRemuneration_PaymentNumber -->

**What it is.** The number of days or hours.

**Example.** 22

**Why it matters.** Cannot be zero. Hours are converted to days using the employee's hours for the day.

**Rules.** Required. Number.

## PaymentUnit {#EmployeeRemuneration.PaymentUnit}
<!-- alias: loc:PaymentUnit, dom:EmployeeRemuneration_PaymentUnit -->

**What it is.** Whether the number is days or hours.

**Example.** Days

**Rules.** Required. One of: Days, Hours.
