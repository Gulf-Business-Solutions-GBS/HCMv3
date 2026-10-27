---
helpId: pay.absence-violation-action-log.edit
lang: en
title: Absence Violation Action Logs — Create / Edit
entity: HCMv3.PayrollManagement.AbsenceViolationActionLog
surfaces:
  - angular/src/app/admin/payroll-management/absence-violation-action-logs/create-or-edit-absence-violation-action-log-modal.component.html
audience: [admin]
permissions: [Pages.PayrollManagement.AbsenceViolationActionLogs.Create]
---

## Overview

One step taken on an escalation record: which step, which action, and when.

Not written at present: no process creates these records, so the list stays as it is entered by hand.

## AbsenceViolationActionRecords {#AbsenceViolationActionLog.AbsenceViolationActionRecordId}
<!-- alias: loc:AbsenceViolationActionRecords, dom:AbsenceViolationActionLog_AbsenceViolationActionRecordId -->

**What it is.** The escalation record this step belongs to.

**Rules.** Required. Chosen from Absence Violation Action Record master data.

## Step {#AbsenceViolationActionLog.Step}
<!-- alias: loc:Step, dom:AbsenceViolationActionLog_Step -->

**What it is.** The rule step that was taken.

**Example.** `2`

**Rules.** Optional. Whole number.

## Action {#AbsenceViolationActionLog.Action}
<!-- alias: loc:Action, dom:AbsenceViolationActionLog_Action -->

**What it is.** The action taken.

**Example.** Notification

**Rules.** Required. One of: Notification, Violation, Leave.

## ActionDate {#AbsenceViolationActionLog.ActionDate}
<!-- alias: loc:ActionDate, dom:AbsenceViolationActionLog_ActionDate -->

**What it is.** When the action was taken.

**Example.** `2026-04-14`

**Rules.** Optional. Date value.

## ObjectOId {#AbsenceViolationActionLog.ObjectOId}
<!-- alias: loc:ObjectOId, dom:AbsenceViolationActionLog_ObjectOId -->

**What it is.** The notification, violation or leave the action created.

**Rules.** Optional. Whole number.
