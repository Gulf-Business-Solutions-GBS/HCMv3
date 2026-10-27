---
helpId: pay.employee-group.edit
lang: en
title: Salary Profiles — Create / Edit
entity: HCMv3.PayrollManagement.EmployeeGroup
surfaces:
  - angular/src/app/admin/payroll-management/employee-groups/create-or-edit-employee-group-modal.component.html
audience: [admin]
permissions: [Pages.PayrollManagement.EmployeeGroups.Create]
---

## Overview

A broad grouping of employees, such as staff and labour, that grades sit under. Each employee's organisation assignment names their salary profile and grade.

The group carries the cap on end-of-service indemnity. The cap applies only when **Indemnity Max Number** is not 0.

## IndemnityMaxNumber {#EmployeeGroup.IndemnityMaxNumber}
<!-- alias: loc:IndemnityMaxNumber, dom:EmployeeGroup_IndemnityMaxNumber -->

**What it is.** How much pay the end-of-service indemnity can reach at most, counted in the unit below.

**Example.** `18` with unit Month -- at most eighteen months' pay, the Kuwait labour law cap.

**Why it matters.** The cap is the employee's monthly pay for wagetypes marked *Include In Indemnity*, times this many months, times the percentage below. Indemnity days beyond the cap are not accrued. 0 means no cap.

**Rules.** Optional. Number.

## IndemnityMaxUnit {#EmployeeGroup.IndemnityMaxUnit}
<!-- alias: loc:IndemnityMaxUnit, dom:EmployeeGroup_IndemnityMaxUnit -->

**What it is.** Whether the maximum is counted in months or in days.

**Example.** Month

**Why it matters.** Days are converted at 30 to a month: 540 days is the same cap as 18 months.

**Rules.** Required. One of: Day, Month.

## IndemnityMaxAccruePercentage {#EmployeeGroup.IndemnityMaxAccruePercentage}
<!-- alias: loc:IndemnityMaxAccruePercentage, dom:EmployeeGroup_IndemnityMaxAccruePercentage -->

**What it is.** The percentage of that maximum the employee can actually accrue.

**Example.** `100`

**Why it matters.** Multiplies the cap. Leave it at 0 and the cap becomes 0 -- so with a maximum number set, set this too.

**Rules.** Optional. Number.
