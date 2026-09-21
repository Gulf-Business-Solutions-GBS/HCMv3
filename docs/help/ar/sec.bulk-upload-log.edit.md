---
helpId: sec.bulk-upload-log.edit
lang: ar
title: سجلات الرفع الجماعي — إنشاء / تحرير
entity: HCMv3.SystemSecurity.BulkUploadLog
surfaces:
  - angular/src/app/admin/system-security/bulk-upload-logs/create-or-edit-bulk-upload-log-modal.component.html
audience: [admin]
permissions: [Pages.SystemSecurity.BulkUploadLogs.Create]
---

## Overview

قيود يكتبها الرفع الجماعي، مثل استيراد صور الملفات الشخصية. وتعرض شاشة الاستيراد الجماعي تقدم آخر تشغيل منها. يكتبه النظام أثناء عمله، ولا يوجد عادةً سبب لإضافة قيود أو تعديلها يدوياً.

## Employee {#BulkUploadLog.EmployeeId}
<!-- alias: loc:Employee, dom:BulkUploadLog_EmployeeId -->

**ما هو.** الموظف الذي يخصه القيد.

**القواعد.** إلزامي. يُختار من قائمة الموظفين.

## LogType {#BulkUploadLog.LogType}
<!-- alias: loc:LogType, dom:BulkUploadLog_LogType -->

**ما هو.** ما الذي رُفع.

**مثال.** صورة الملف الشخصي

**القواعد.** إلزامي. إحدى القيم: الصورة الشخصية.

## RunTimeStamp {#BulkUploadLog.RunTimeStamp}
<!-- alias: loc:RunTimeStamp, dom:BulkUploadLog_RunTimeStamp -->

**ما هو.** متى كُتب القيد.

**القواعد.** اختياري. تاريخ.

## InPeriod {#BulkUploadLog.InPeriod}
<!-- alias: loc:InPeriod, dom:BulkUploadLog_InPeriod -->

**ما هو.** فترة التشغيل.

**القواعد.** اختياري. تاريخ.

## Status {#BulkUploadLog.Status}
<!-- alias: loc:Status, dom:BulkUploadLog_Status -->

**ما هو.** النتيجة: 0 نجاح.

**مثال.** `0`

**القواعد.** اختياري. رقم صحيح.

## NoOfEmployee {#BulkUploadLog.NoOfEmployee}
<!-- alias: loc:NoOfEmployee, dom:BulkUploadLog_NoOfEmployee -->

**ما هو.** عدد السجلات التي كان التشغيل يعالجها.

**القواعد.** اختياري. رقم صحيح.

## RecordNo {#BulkUploadLog.RecordNo}
<!-- alias: loc:RecordNo, dom:BulkUploadLog_RecordNo -->

**ما هو.** موقع السجل في التشغيل.

**القواعد.** اختياري. رقم صحيح.

## StopFlag {#BulkUploadLog.StopFlag}
<!-- alias: loc:StopFlag, dom:BulkUploadLog_StopFlag -->

**ما هو.** ما إذا طُلب إيقاف التشغيل.

**لماذا يهم.** يتوقف الاستيراد الجاري عندما يجد قيداً مؤشَّراً عليه.

**القواعد.** نعم أو لا.

## Message {#BulkUploadLog.Message}
<!-- alias: loc:Message, dom:BulkUploadLog_Message -->

**ما هو.** ما الذي حدث.

**القواعد.** اختياري.
