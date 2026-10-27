---
helpId: sec.punch-notification-log.edit
lang: en
title: Punch Notification Logs — Create / Edit
entity: HCMv3.SystemSecurity.PunchNotificationLog
surfaces:
  - angular/src/app/admin/system-security/punch-notification-logs/create-or-edit-punch-notification-log-modal.component.html
audience: [admin]
permissions: [Pages.SystemSecurity.PunchNotificationLogs.Create]
---

## Overview

A punch-related notification sent for an employee's duty period. Written by the system as it runs; there is normally no reason to add or change entries by hand.

## EmployeeDutyPeriod {#PunchNotificationLog.EmployeeDutyPeriodId}
<!-- alias: loc:EmployeeDutyPeriod, dom:PunchNotificationLog_EmployeeDutyPeriodId -->

**What it is.** The employee's duty period.

**Rules.** Required. Chosen from a list.

## EmailNotificationLog {#PunchNotificationLog.EmailNotificationLogId}
<!-- alias: loc:EmailNotificationLog, dom:PunchNotificationLog_EmailNotificationLogId -->

**What it is.** The email that was sent.

**Rules.** Optional. Chosen from Email Notification Log master data.

## Type {#PunchNotificationLog.Type}
<!-- alias: loc:Type, dom:PunchNotificationLog_Type -->

**What it is.** The kind of punch notification.

**Rules.** Optional. Maximum 128 characters.

## Date {#PunchNotificationLog.Date}
<!-- alias: loc:Date, dom:PunchNotificationLog_Date -->

**What it is.** The date it was for.

**Rules.** Required. Date value.
