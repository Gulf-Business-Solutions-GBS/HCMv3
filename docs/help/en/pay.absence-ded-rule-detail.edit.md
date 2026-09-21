---
helpId: pay.absence-ded-rule-detail.edit
lang: en
title: Absence Deduction Rule Details — Create / Edit
entity: HCMv3.PayrollManagement.AbsenceDedRuleDetail
surfaces:
  - angular/src/app/admin/payroll-management/absence-ded-rule-details/create-or-edit-absence-ded-rule-detail-modal.component.html
audience: [admin]
permissions: [Pages.PayrollManagement.AbsenceDedRuleDetails.Create]
---

## Overview

One step of a sick leave deduction scale. Payroll counts the employee's sick days through the cycle; each day takes the first step whose **Days Count** reaches that day's number, and is paid at 100% less that step's deduction. Sick days beyond the last step are not paid at all, so make the last step reach as far as sick leave can run.

## AbsenceDeductionRules {#AbsenceDedRuleDetail.AbsenceDeductionRuleId}
<!-- alias: loc:AbsenceDeductionRules, dom:AbsenceDedRuleDetail_AbsenceDeductionRuleId -->

**What it is.** The scale this step belongs to.

**Example.** Kuwait Sick Leave Scale

**Rules.** Required. Chosen from Absence Deduction Rule master data.

## DaysCount {#AbsenceDedRuleDetail.DaysCount}
<!-- alias: loc:DaysCount, dom:AbsenceDedRuleDetail_DaysCount -->

**What it is.** The sick day, counted through the cycle, up to which this step applies.

**Example.** `25` -- days 16 to 25, after a first step of `15`.

**Why it matters.** Steps are read in order of this number.

**Rules.** Optional. Number.

## DeductionUnit {#AbsenceDedRuleDetail.DeductionUnit}
<!-- alias: loc:DeductionUnit, dom:AbsenceDedRuleDetail_DeductionUnit -->

**What it is.** Whether the deduction is a percentage or a fixed amount.

**Why it matters.** Not read at present: the deduction number is always treated as a percentage.

**Rules.** Required. One of: Fixed, Percentage.

## DeductionNumber {#AbsenceDedRuleDetail.DeductionNumber}
<!-- alias: loc:DeductionNumber, dom:AbsenceDedRuleDetail_DeductionNumber -->

**What it is.** The percentage of a day's pay taken off for sick days in this step.

**Example.** `25` -- each day paid at 75%.

**Rules.** Optional. Number.
