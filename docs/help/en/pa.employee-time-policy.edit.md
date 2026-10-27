---
helpId: pa.employee-time-policy.edit
lang: en
title: Employee Time Policies — Create / Edit
entity: HCMv3.TimeManagement.EmployeeTimePolicy
surfaces:
  - angular/src/app/admin/personnel-administration/employee-detail/time-policies/create-or-edit-employee-time-policy-modal.component.html
audience: [admin]
permissions: [Pages.TimeManagement.EmployeeTimePolicies.Create]
---

## Overview

The time policy that governs the employee's attendance from a given date. Saving a new one closes the previous one on the day before the new start date.

The time engine applies the policy in force on each day, and stops with "Employee TimePolicy not found" for a day no policy covers. Payroll day counts read the policy's time management status and monthly tolerance.

## TimePolicy {#EmployeeTimePolicy.TimePolicyId}
<!-- alias: loc:TimePolicy, dom:EmployeeTimePolicy_TimePolicyId -->

**What it is.** The time policy assigned from this date.

**Example.** Office Staff — Biometric

**Rules.** Required. Chosen from Time Policy master data.

:::admin
Maintained under Time Management → Time Policies.
:::
