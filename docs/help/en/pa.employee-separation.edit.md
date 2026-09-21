---
helpId: pa.employee-separation.edit
lang: en
title: Employee Separations — Create / Edit
entity: HCMv3.PersonnelAdministration.EmployeeSeparation
surfaces:
  - angular/src/app/admin/personnel-administration/employee-detail/separations/create-or-edit-employee-separation-modal.component.html
audience: [admin]
permissions: [Pages.PersonnelAdministration.EmployeeSeparations.Create]
---

## Overview

The employee's exit: when they resigned or were terminated, and their last day in service. A separation stays in effect until it is revoked.

Payroll stops calculating most pay after the end of the month of the last day in service, and the end-of-service payroll run needs an unrevoked separation. Saving a separation does not create a pay hold or remove later time records; those are separate steps.

## SubmissionDate {#EmployeeSeparation.SubmissionDate}
<!-- alias: loc:SubmissionDate, dom:EmployeeSeparation_SubmissionDate -->

**What it is.** The date the resignation or termination was submitted.

**Example.** 1 Aug 2026

**Why it matters.** The last day in service cannot be before it. The end-of-service run uses the most recently submitted separation.

**Rules.** Required. Date value.

## LastDayInService {#EmployeeSeparation.LastDayInService}
<!-- alias: loc:LastDayInService, dom:EmployeeSeparation_LastDayInService -->

**What it is.** The employee's final working day.

**Example.** 31 Aug 2026

**Why it matters.** Payroll does not calculate wagetypes for pay dates after the end of this month, fund deductions stop, and the indemnity calculation is worked out as at this date.

**Rules.** Required. Date value.

## NoticePeriodDays {#EmployeeSeparation.NoticePeriodDays}
<!-- alias: loc:NoticePeriodDays, dom:EmployeeSeparation_NoticePeriodDays -->

**What it is.** The length of the notice period in days.

**Example.** 30

**Why it matters.** Shown on the separation report; the dates are not recalculated from it.

**Rules.** Required. Whole number.

## PlannedLastDayInService {#EmployeeSeparation.PlannedLastDayInService}
<!-- alias: loc:PlannedLastDayInService, dom:EmployeeSeparation_PlannedLastDayInService -->

**What it is.** The last day originally planned, before any change.

**Example.** 31 Aug 2026

**Why it matters.** Shown on the separation report.

**Rules.** Required. Date value.

## SeparationType {#EmployeeSeparation.SeparationType}
<!-- alias: loc:SeparationType, dom:EmployeeSeparation_SeparationType -->

**What it is.** Whether the employee resigned or was terminated.

**Example.** Resignation

**Why it matters.** Shown on employee and cost-to-company reports.

**Rules.** Required. One of: Resignation, Termination.

## SeparationReason {#EmployeeSeparation.SeparationReasonId}
<!-- alias: loc:SeparationReason, dom:EmployeeSeparation_SeparationReasonId -->

**What it is.** Why the employee is leaving.

**Example.** Better opportunity

**Rules.** Required. Chosen from Separation Reason master data.

:::admin
Maintained under General Master Info → Separation Reasons.
:::

## RevokeDate {#EmployeeSeparation.RevokeDate}
<!-- alias: loc:RevokeDate, dom:EmployeeSeparation_RevokeDate -->

**What it is.** The date the separation was withdrawn, if it was.

**Example.** 10 Aug 2026

**Why it matters.** Once set, payroll and the indemnity calculation ignore this separation.

**Rules.** Optional. Date value.
