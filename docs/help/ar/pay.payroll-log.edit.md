---
helpId: pay.payroll-log.edit
lang: ar
title: سجلات تشغيل الرواتب — إنشاء / تحرير
entity: HCMv3.PayrollManagement.PayrollLog
surfaces:
  - angular/src/app/admin/payroll-management/payroll-logs/create-or-edit-payroll-log-modal.component.html
audience: [admin]
permissions: [Pages.PayrollManagement.PayrollLogs.Create]
---

## Overview

قيود يكتبها تشغيل الرواتب أثناء معالجة الموظفين. ويعدّها سجل التشغيلات في صفحة تشغيل الرواتب ليبيّن كم نجح وكم فشل.

## Message {#PayrollLog.Message}
<!-- alias: loc:Message, dom:PayrollLog_Message -->

**ما هو.** ما سجّله التشغيل.

**مثال.** Employee 1002: Payroll Control Record not locked or is not current.

**القواعد.** اختياري.

## RunTimeStamp {#PayrollLog.RunTimeStamp}
<!-- alias: loc:RunTimeStamp, dom:PayrollLog_RunTimeStamp -->

**ما هو.** متى كُتب القيد.

**القواعد.** إلزامي. تاريخ.

## EmployeeId {#PayrollLog.EmployeeId}
<!-- alias: loc:EmployeeId, dom:PayrollLog_EmployeeId -->

**ما هو.** الموظف الذي يخصه القيد، إن وُجد.

**القواعد.** اختياري. يُختار من قائمة الموظفين.

## Status {#PayrollLog.Status}
<!-- alias: loc:Status, dom:PayrollLog_Status -->

**ما هو.** النتيجة: 0 نجاح، وأي قيمة أخرى فشل.

**مثال.** `0`

**القواعد.** اختياري. رقم صحيح.

## InPeriod {#PayrollLog.InPeriod}
<!-- alias: loc:InPeriod, dom:PayrollLog_InPeriod -->

**ما هو.** شهر الدفع للتشغيل.

**مثال.** `2026-04-30`

**القواعد.** إلزامي. تاريخ.

## NoOfEmployee {#PayrollLog.NoOfEmployee}
<!-- alias: loc:NoOfEmployee, dom:PayrollLog_NoOfEmployee -->

**ما هو.** عدد الموظفين الذين كان التشغيل يعالجهم.

**مثال.** `240`

**القواعد.** اختياري. رقم صحيح.

## StopFlag {#PayrollLog.StopFlag}
<!-- alias: loc:StopFlag, dom:PayrollLog_StopFlag -->

**ما هو.** ما إذا طُلب إيقاف التشغيل.

**لماذا يهم.** التشغيل الذي فيه أي قيد مؤشَّر عليه يُعرض على أنه أُوقف.

**القواعد.** نعم أو لا.
