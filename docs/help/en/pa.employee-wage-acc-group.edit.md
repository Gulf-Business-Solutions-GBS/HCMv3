---
helpId: pa.employee-wage-acc-group.edit
lang: en
title: Employee Wage Account Groups — Create / Edit
entity: HCMv3.PayrollManagement.EmployeeWageAccGroup
surfaces:
  - angular/src/app/admin/personnel-administration/employee-detail/wage-acc-groups/create-or-edit-employee-wage-acc-group-modal.component.html
audience: [admin]
permissions: [Pages.PayrollManagement.EmployeeWageAccGroups.Create]
---

## Overview

The wage account group assigned to the employee from a given date. Saving a new one closes the previous one on the day before the new start date. The same group cannot be recorded twice with the same start date.

It is recorded for reference; the cost engine and journal postings do not read it at present.

## WageAccGroupId {#EmployeeWageAccGroup.WageAccGroupId}
<!-- alias: loc:WageAccGroupId, dom:EmployeeWageAccGroup_WageAccGroupId -->

**What it is.** The wage account group.

**Example.** Head Office Staff

**Rules.** Required. Chosen from Wage Acc Group master data.

:::admin
Maintained under Payroll Management → Wage Account Groups.
:::
