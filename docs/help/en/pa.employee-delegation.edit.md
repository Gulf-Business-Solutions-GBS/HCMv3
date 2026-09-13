---
helpId: pa.employee-delegation.edit
lang: en
title: Employee Delegations — Create / Edit
entity: HCMv3.PersonnelAdministration.EmployeeDelegation
surfaces:
  - angular/src/app/admin/personnel-administration/employee-detail/delegations/create-or-edit-employee-delegation-modal.component.html
audience: [admin]
permissions: [Pages.PersonnelAdministration.EmployeeDelegations.Create]
---

## Overview

Hands the employee's duties to a colleague for a period -- typically while they are on leave.

While a delegation is in force, the colleague is copied on the notification emails the employee receives. Delegations can be routed for approval and take effect only once approved. An employee cannot delegate to themselves, and delegations for the same employee cannot overlap.

## DelegateTo {#EmployeeDelegation.DelegateToId}
<!-- alias: loc:DelegateTo, dom:EmployeeDelegation_DelegateToId -->

**What it is.** The colleague who covers for the employee.

**Example.** Ahmed Al-Kandari (1057)

**Why it matters.** Must be a different employee.

**Rules.** Required. Chosen from the employee list.
