---
helpId: sec.punch-notification-log.edit
lang: ar
title: سجلات إشعارات البصمة — إنشاء / تحرير
entity: HCMv3.SystemSecurity.PunchNotificationLog
surfaces:
  - angular/src/app/admin/system-security/punch-notification-logs/create-or-edit-punch-notification-log-modal.component.html
audience: [admin]
permissions: [Pages.SystemSecurity.PunchNotificationLogs.Create]
---

## Overview

إشعار متعلق بالبصمة أُرسل لفترة دوام موظف. يكتبه النظام أثناء عمله، ولا يوجد عادةً سبب لإضافة قيود أو تعديلها يدوياً.

## EmployeeDutyPeriod {#PunchNotificationLog.EmployeeDutyPeriodId}
<!-- alias: loc:EmployeeDutyPeriod, dom:PunchNotificationLog_EmployeeDutyPeriodId -->

**ما هو.** فترة دوام الموظف.

**القواعد.** إلزامي. يُختار من قائمة.

## EmailNotificationLog {#PunchNotificationLog.EmailNotificationLogId}
<!-- alias: loc:EmailNotificationLog, dom:PunchNotificationLog_EmailNotificationLogId -->

**ما هو.** الرسالة التي أُرسلت.

**القواعد.** اختياري. يُختار من قائمة سجلات إشعارات البريد الإلكتروني.

## Type {#PunchNotificationLog.Type}
<!-- alias: loc:Type, dom:PunchNotificationLog_Type -->

**ما هو.** نوع إشعار البصمة.

**القواعد.** اختياري. الحد الأقصى 128 حرفاً.

## Date {#PunchNotificationLog.Date}
<!-- alias: loc:Date, dom:PunchNotificationLog_Date -->

**ما هو.** التاريخ الذي يخصه.

**القواعد.** إلزامي. تاريخ.
