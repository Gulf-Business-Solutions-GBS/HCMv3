---
helpId: sec.retro-reason-log.edit
lang: en
title: Retro Reason Logs — Create / Edit
entity: HCMv3.SystemSecurity.RetroReasonLog
surfaces:
  - angular/src/app/admin/system-security/retro-reason-logs/create-or-edit-retro-reason-log-modal.component.html
audience: [admin]
permissions: [Pages.SystemSecurity.RetroReasonLogs.Create]
---

## Overview

A record of a back-dated change that made payroll recalculate an employee from an earlier date: what changed, when, and the retro date. Written by the system as it runs; there is normally no reason to add or change entries by hand.

## Employee {#RetroReasonLog.EmployeeId}
<!-- alias: loc:Employee, dom:RetroReasonLog_EmployeeId -->

**What it is.** The employee.

**Rules.** Required. Chosen from the employee list.

## ChangedOn {#RetroReasonLog.ChangedOn}
<!-- alias: loc:ChangedOn, dom:RetroReasonLog_ChangedOn -->

**What it is.** When the change was made.

**Rules.** Required. Date value.

## RetroDate {#RetroReasonLog.RetroDate}
<!-- alias: loc:RetroDate, dom:RetroReasonLog_RetroDate -->

**What it is.** The date payroll recalculates from.

**Example.** `2026-02-01`

**Rules.** Required. Date value.

## EntityObjectName {#RetroReasonLog.EntityObjectName}
<!-- alias: loc:EntityObjectName, dom:RetroReasonLog_EntityObjectName -->

**What it is.** The type of record changed.

**Example.** `HCMv3.PersonnelAdministration.EmployeeBasicPay`

**Rules.** Optional. Maximum 256 characters.

## EntityObjectId {#RetroReasonLog.EntityObjectId}
<!-- alias: loc:EntityObjectId, dom:RetroReasonLog_EntityObjectId -->

**What it is.** The record changed.

**Rules.** Required. Chosen from a list.

## RetroReason {#RetroReasonLog.RetroReason}
<!-- alias: loc:RetroReason, dom:RetroReasonLog_RetroReason -->

**What it is.** Why the retro date was set.

**Rules.** Optional. Maximum 4000 characters.
