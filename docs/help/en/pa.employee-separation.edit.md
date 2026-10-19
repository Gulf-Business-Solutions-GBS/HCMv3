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

Payroll stops calculating most pay after the end of the month of the last day in service, and the end-of-service payroll run needs an unrevoked separation. Saving a separation does not remove later time records; that is a separate step.

**On Hold Information** can hold the employee's pay at the same time. Saving the separation with an **On Hold From Date** and at least one hold ticked creates the employee's on-hold record, for whole months: the from date is moved to the first of its month and the till date to the last day of its month. Saving again replaces the hold this separation created earlier, and deleting the separation removes it. Payroll reads that on-hold record, not the ticks on this form, so the hold also shows, and can be checked, under the employee's On Holds.

Once a later on-hold has been entered for the employee — one starting after this separation's from date — the separation can no longer be added, changed or deleted; the message names that hold's dates.

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

## RevokeComments {#EmployeeSeparation.RevokeComments}
<!-- alias: loc:RevokeComments, dom:EmployeeSeparation_RevokeComments -->

**What it is.** Why the separation was revoked, in a sentence or two.

**Example.** Employee withdrew the resignation after a counter-offer.

**Why it matters.** Recorded with the separation only; no report shows it at present.

**Rules.** Optional. Maximum 500 characters.

## OnHoldFromDate {#EmployeeSeparation.OnHoldFromDate}
<!-- alias: loc:OnHoldFromDate, dom:EmployeeSeparation_OnHoldFromDate -->

**What it is.** The month the pay hold starts. Any day in the month will do: on save it becomes the first of that month.

**Example.** 15 Aug 2026, saved as 1 Aug 2026

**Why it matters.** Nothing is held unless at least one hold below is ticked as well. The saved date also identifies the hold this separation created, which is how a later save replaces it and a delete removes it.

**Rules.** Optional. Date value.

## OnHoldTillDate {#EmployeeSeparation.OnHoldTillDate}
<!-- alias: loc:OnHoldTillDate, dom:EmployeeSeparation_OnHoldTillDate -->

**What it is.** The month the pay hold ends. On save it becomes the last day of that month.

**Example.** 10 Sep 2026, saved as 30 Sep 2026

**Why it matters.** Needed whenever a hold is ticked: without it the hold's end falls before its start and the save is refused. The period cannot overlap another hold of the same employee.

**Rules.** Optional. Date value.

## BankTransfer {#EmployeeSeparation.BankTransfer}
<!-- alias: loc:BankTransfer, dom:EmployeeSeparation_BankTransfer -->

**What it is.** Whether the employee's salary transfer is held for the hold period.

**Example.** Hold

**Why it matters.** Copied to the on-hold record, where the bank file reads it. It does not on its own create a hold: one of the ticks below is still needed.

**Rules.** Required. One of: N/A, Hold, Unhold.

## OnHoldAll {#EmployeeSeparation.OnHoldAll}
<!-- alias: loc:OnHoldAll, dom:EmployeeSeparation_onHoldAll -->

**What it is.** Holds everything for the hold period.

**Why it matters.** Copied to the on-hold record's **On Hold**, so payroll treats every individual hold as ticked.

**Rules.** Yes or no.

## OnHoldBasicPay {#EmployeeSeparation.OnHoldBasicPay}
<!-- alias: loc:OnHoldBasicPay, dom:EmployeeSeparation_onHoldBasicPay -->

**What it is.** Holds basic pay for the hold period.

**Why it matters.** Copied to the on-hold record; payroll pays no basic pay while it applies.

**Rules.** Yes or no.

## OnHoldLeavePay {#EmployeeSeparation.OnHoldLeavePay}
<!-- alias: loc:OnHoldLeavePay, dom:EmployeeSeparation_onHoldLeavePay -->

**What it is.** Holds leave pay for the hold period.

**Why it matters.** Copied to the on-hold record.

**Rules.** Yes or no.

## OnHoldIndemnityPay {#EmployeeSeparation.OnHoldIndemnityPay}
<!-- alias: loc:OnHoldIndemnityPay, dom:EmployeeSeparation_onHoldIndemnityPay -->

**What it is.** Holds indemnity pay for the hold period.

**Why it matters.** Copied to the on-hold record — the usual choice while a leaver's clearance is still open.

**Rules.** Yes or no.

## OnHoldAirTicketPay {#EmployeeSeparation.OnHoldAirTicketPay}
<!-- alias: loc:OnHoldAirTicketPay, dom:EmployeeSeparation_onHoldAirTicketPay -->

**What it is.** Holds air ticket pay for the hold period.

**Why it matters.** Copied to the on-hold record, where it is recorded only; payroll does not currently check it when paying tickets.

**Rules.** Yes or no.

## OnHoldLoanDeduction {#EmployeeSeparation.OnHoldLoanDeduction}
<!-- alias: loc:OnHoldLoanDeduction, dom:EmployeeSeparation_onHoldLoanDeduction -->

**What it is.** Holds loan instalment deductions for the hold period.

**Why it matters.** Copied to the on-hold record; payroll does not recover loans while it applies.

**Rules.** Yes or no.

## OnHoldAllEarnings {#EmployeeSeparation.OnHoldAllEarnings}
<!-- alias: loc:OnHoldAllEarnings, dom:EmployeeSeparation_onHoldAllEarnings -->

**What it is.** Holds all earnings for the hold period.

**Why it matters.** Copied to the on-hold record.

**Rules.** Yes or no.

## OnHoldAllDeductions {#EmployeeSeparation.OnHoldAllDeductions}
<!-- alias: loc:OnHoldAllDeductions, dom:EmployeeSeparation_onHoldAllDeductions -->

**What it is.** Holds all deductions for the hold period.

**Why it matters.** Copied to the on-hold record.

**Rules.** Yes or no.
