---
helpId: pay.absence-violation-rule-detail.edit
lang: en
title: Absence Violation Rule Details — Create / Edit
entity: HCMv3.PayrollManagement.AbsenceViolationRuleDetail
surfaces:
  - angular/src/app/admin/payroll-management/absence-violation-rule-details/create-or-edit-absence-violation-rule-detail-modal.component.html
audience: [admin]
permissions: [Pages.PayrollManagement.AbsenceViolationRuleDetails.Create]
---

## Overview

One step of an absence violation rule: after how many days, and what action -- a notification, a disciplinary violation or a leave.

Not run at present: no process escalates absences using these rules. They are saved and kept for reference, and nothing happens when an employee is absent.

## AbsenceViolationRules {#AbsenceViolationRuleDetail.AbsenceViolationRuleId}
<!-- alias: loc:AbsenceViolationRules, dom:AbsenceViolationRuleDetail_AbsenceViolationRuleId -->

**What it is.** The rule this step belongs to.

**Example.** Unauthorised Absence

**Rules.** Required. Chosen from Absence Violation Rule master data.

## Step {#AbsenceViolationRuleDetail.Step}
<!-- alias: loc:Step, dom:AbsenceViolationRuleDetail_Step -->

**What it is.** The step's position in the escalation.

**Example.** `2`

**Why it matters.** Must be greater than 0, and each step number can be used once per rule.

**Rules.** Optional. Whole number.

## DayInterval {#AbsenceViolationRuleDetail.DayInterval}
<!-- alias: loc:DayInterval, dom:AbsenceViolationRuleDetail_DayInterval -->

**What it is.** The days of absence after which this step would apply.

**Example.** `3`

**Rules.** Optional. Whole number.

## Action {#AbsenceViolationRuleDetail.Action}
<!-- alias: loc:Action, dom:AbsenceViolationRuleDetail_Action -->

**What it is.** What the step would do: send a notification, record a disciplinary violation, or record a leave.

**Example.** Violation

**Rules.** Required. One of: Notification, Violation, Leave.

## ActionType {#AbsenceViolationRuleDetail.ActionTypeId}
<!-- alias: loc:ActionType, dom:AbsenceViolationRuleDetail_ActionTypeId -->

**What it is.** The leave type or disciplinary type the action would use, depending on the action.

**Example.** Unpaid Leave

**Rules.** Optional. Chosen from a list.
