---
helpId: pa.employee-action.edit
lang: en
title: Employee Actions — Create / Edit
entity: HCMv3.PersonnelAdministration.EmployeeAction
surfaces:
  - angular/src/app/admin/personnel-administration/employee-detail/actions/create-or-edit-employee-action-modal.component.html
audience: [admin]
permissions: [Pages.PersonnelAdministration.EmployeeActions.Create]
---

## Overview

A personnel action for the employee -- a promotion, transfer, re-grading or separation -- with its reason and date.

Personnel actions can be routed for approval; HR raises them, not the employee. The same kind of action cannot be recorded twice on the same day for the same employee.

## PersonalAction {#EmployeeAction.PersonalActionId}
<!-- alias: loc:PersonalAction, dom:EmployeeAction_PersonalActionId -->

**What it is.** The kind of action.

**Example.** Promotion

**Rules.** Required. Chosen from Personal Action master data.

:::admin
Maintained under General Master Info → Personal Actions. Their requirement flags are not enforced here at present.
:::

## ActionReason {#EmployeeAction.ActionReasonId}
<!-- alias: loc:ActionReason, dom:EmployeeAction_ActionReasonId -->

**What it is.** Why the action is being taken.

**Example.** Annual performance review

**Rules.** Required. Chosen from Personal Action Reason master data.

:::admin
Maintained under General Master Info → Personal Action Reasons.
:::

## RecommendedBy {#EmployeeAction.RecommendedBy}
<!-- alias: loc:RecommendedBy, dom:EmployeeAction_RecommendedBy -->

**What it is.** Who recommended the action, as free text.

**Example.** Department Head, Finance

**Rules.** Optional. Maximum 256 characters.
