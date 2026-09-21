---
helpId: pay.absence-violation-action-record.edit
lang: en
title: Absence Violation Action Records — Create / Edit
entity: HCMv3.PayrollManagement.AbsenceViolationActionRecord
surfaces:
  - angular/src/app/admin/payroll-management/absence-violation-action-records/create-or-edit-absence-violation-action-record-modal.component.html
audience: [admin]
permissions: [Pages.PayrollManagement.AbsenceViolationActionRecords.Create]
---

## Overview

A record of an absence or time-event violation being escalated, open until it is closed.

Not written at present: no process creates these records, so the list stays as it is entered by hand.

## ActionLogType {#AbsenceViolationActionRecord.ActionLogType}
<!-- alias: loc:ActionLogType, dom:AbsenceViolationActionRecord_ActionLogType -->

**What it is.** Whether the violation came from leave or from a time event.

**Example.** Leave

**Rules.** Required. One of: Leave, Time Event.

## LogStatus {#AbsenceViolationActionRecord.LogStatus}
<!-- alias: loc:LogStatus, dom:AbsenceViolationActionRecord_LogStatus -->

**What it is.** Whether the escalation is still open.

**Example.** Open

**Rules.** Required. One of: Open, Closed.

## EntityObjectId {#AbsenceViolationActionRecord.EntityObjectId}
<!-- alias: loc:EntityObjectId, dom:AbsenceViolationActionRecord_EntityObjectId -->

**What it is.** The leave or time event the record is about.

**Rules.** Optional. Chosen from a list.
