---
helpId: pay.employee-sub-group-bonus.edit
lang: en
title: Employee Sub Group Bonuses — Create / Edit
entity: HCMv3.PayrollManagement.EmployeeSubGroupBonus
surfaces:
  - angular/src/app/admin/payroll-management/employee-sub-group-detail/bonuses/create-or-edit-employee-sub-group-bonus-modal.component.html
audience: [admin]
permissions: [Pages.PayrollManagement.EmployeeSubGroupBonuses.Create]
---

## Overview

The grade's service bonus: a percentage paid on a work anniversary. In the month of the employee's joining anniversary, payroll looks for the row whose **Change Year** equals the years the employee has completed, and pays that percentage of the monthly total of wagetypes marked *Include In Service Bonus*.

Only anniversaries with a row are paid -- add one row for each anniversary that earns a bonus.

## ChangeYear {#EmployeeSubGroupBonus.ChangeYear}
<!-- alias: loc:ChangeYear, dom:EmployeeSubGroupBonus_ChangeYear -->

**What it is.** The completed years of service this row pays on.

**Example.** `5` -- paid in the month of the fifth anniversary.

**Rules.** Optional. Number.

:::admin
The row's start and end dates are not checked when the bonus is paid; only the grade and the years matter.
:::

## Percentage {#EmployeeSubGroupBonus.Percentage}
<!-- alias: loc:Percentage, dom:EmployeeSubGroupBonus_Percentage -->

**What it is.** The percentage of the service-bonus pay paid as the bonus.

**Example.** `50`

**Rules.** Optional. Number.
