---
helpId: tm.time-log.edit
lang: ar
title: سجلات الوقت — إنشاء / تحرير
entity: HCMv3.TimeManagement.TimeLog
surfaces:
  - angular/src/app/admin/time-management/time-logs/create-or-edit-time-log-modal.component.html
audience: [admin]
permissions: [Pages.TimeManagement.TimeLogs.Create]
---

## Overview

قيود يكتبها تشغيل محرك الوقت أثناء معالجة الموظفين. ويعدّها سجل التشغيلات ليبيّن النجاح والفشل، ويظهر القيد الفاشل لموظف أيضاً عندما يشغّل نظام الرواتب محرك الوقت تلقائياً.

## RunTimeStamp {#TimeLog.RunTimeStamp}
<!-- alias: loc:RunTimeStamp, dom:TimeLog_RunTimeStamp -->

**ما هو.** متى كُتب القيد.

**القواعد.** اختياري. تاريخ.

## DayDate {#TimeLog.DayDate}
<!-- alias: loc:DayDate, dom:TimeLog_DayDate -->

**ما هو.** اليوم الجاري معالجته.

**مثال.** `2026-04-14`

**القواعد.** اختياري. تاريخ.

## Employee {#TimeLog.EmployeeId}
<!-- alias: loc:Employee, dom:TimeLog_EmployeeId -->

**ما هو.** الموظف الذي يخصه القيد، إن وُجد.

**القواعد.** اختياري. يُختار من قائمة الموظفين.

## Status {#TimeLog.Status}
<!-- alias: loc:Status, dom:TimeLog_Status -->

**ما هو.** النتيجة: 0 نجاح، و1 فشل.

**مثال.** `0`

**القواعد.** اختياري. رقم صحيح.

## NoOfEmployee {#TimeLog.NoOfEmployee}
<!-- alias: loc:NoOfEmployee, dom:TimeLog_NoOfEmployee -->

**ما هو.** عدد الموظفين الذين كان التشغيل يعالجهم.

**لماذا يهم.** لا يُقرأ حالياً.

**القواعد.** اختياري. رقم صحيح.

## StopFlag {#TimeLog.StopFlag}
<!-- alias: loc:StopFlag, dom:TimeLog_StopFlag -->

**ما هو.** ما إذا طُلب إيقاف التشغيل.

**لماذا يهم.** يتوقف محرك الوقت عندما يجد قيداً مؤشَّراً عليه، ويُعرض التشغيل على أنه أُوقف.

**القواعد.** نعم أو لا.

## Message {#TimeLog.Message}
<!-- alias: loc:Message, dom:TimeLog_Message -->

**ما هو.** ما سجّله التشغيل.

**مثال.** Employee has VariableTime Policy found on 14/04/2026

**القواعد.** اختياري.
