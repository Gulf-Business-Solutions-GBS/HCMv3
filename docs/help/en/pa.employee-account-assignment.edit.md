---
helpId: pa.employee-account-assignment.edit
lang: en
title: Employee Account Assignments — Create / Edit
entity: HCMv3.PersonnelAdministration.EmployeeAccountAssignment
surfaces:
  - angular/src/app/admin/personnel-administration/employee-detail/account-assignments/create-or-edit-employee-account-assignment-modal.component.html
audience: [admin]
permissions: [Pages.PersonnelAdministration.EmployeeAccountAssignments.Create]
---

## Overview

A share of the employee's cost assigned for a period.

It is recorded for reference; the cost engine and journal postings do not read it at present. Use Employee Wage Accounts for account mapping that postings use.

## CostPercentage {#EmployeeAccountAssignment.CostPercentage}
<!-- alias: loc:CostPercentage, dom:EmployeeAccountAssignment_CostPercentage -->

**What it is.** The percentage of cost assigned.

**Example.** 50

**Rules.** Optional. Number.
