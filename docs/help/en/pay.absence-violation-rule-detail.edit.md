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

**What it is.** The leave type or violation type the action would use, depending on the action.

**Example.** Unpaid Leave

**Rules.** Optional. Chosen from a list.

## NotificationConfig {#AbsenceViolationRuleDetail.NotificationConfigId}
<!-- alias: loc:NotificationConfig, dom:AbsenceViolationRuleDetail_NotificationConfigId -->

**What it is.** The notification configuration used to tell people when this step is reached.

**Example.** Absence escalation – notify HR and Line Manager

**Why it matters.** Its recipients get an in-app notification, and an email if the configuration has a template. Left empty, the step acts without telling anybody. A failed notification does not undo the step.

**Rules.** Optional. Chosen from Notification Config master data.

## HoldTillPunchFoundHint {#AbsenceViolationRuleDetail.HoldTillPunchFound}
<!-- alias: loc:HoldTillPunchFoundHint, dom:AbsenceViolationRuleDetail_HoldTillPunchFound -->

**What it is.** Whether this step waits until the employee punches again.

**Why it matters.** Ticked, the step's action -- a deduction leave or a violation record -- is applied only once the employee is back and has an approved punch, so someone still away is not penalised early.

**Rules.** Yes or no.
