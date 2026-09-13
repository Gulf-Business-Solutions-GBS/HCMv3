---
helpId: pa.employee-gratuity.edit
lang: en
title: Employee Gratuities — Create / Edit
entity: HCMv3.PersonnelAdministration.EmployeeGratuity
surfaces:
  - angular/src/app/admin/personnel-administration/employee-detail/gratuities/create-or-edit-employee-gratuity-modal.component.html
audience: [admin]
permissions: [Pages.PersonnelAdministration.EmployeeGratuities.Create]
---

## Overview

An indemnity (gratuity) payment already made to the employee, in days.

Payroll counts the days paid in the month when working out the indemnity due. Ticking Reset makes the indemnity count start again from the paid date.

## GratuityPaidDate {#EmployeeGratuity.GratuityPaidDate}
<!-- alias: loc:GratuityPaidDate, dom:EmployeeGratuity_GratuityPaidDate -->

**What it is.** The date the indemnity was paid.

**Example.** 31 Dec 2025

**Why it matters.** Days paid are counted in the payroll month of this date.

**Rules.** Required. Date value.

## GratuityPaidDays {#EmployeeGratuity.GratuityPaidDays}
<!-- alias: loc:GratuityPaidDays, dom:EmployeeGratuity_GratuityPaidDays -->

**What it is.** The number of indemnity days paid.

**Example.** 120

**Rules.** Optional. Number.

## ResetGratuityCalc {#EmployeeGratuity.ResetGratuityCalc}
<!-- alias: loc:ResetGratuityCalc, dom:EmployeeGratuity_ResetGratuityCalc -->

**What it is.** Whether the indemnity calculation restarts from the paid date.

**Why it matters.** When ticked, service for indemnity is counted from this payment's date instead of from the start.

**Rules.** Yes or no.
