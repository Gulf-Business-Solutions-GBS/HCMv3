---
helpId: prj.project-log.edit
lang: ar
title: سجلات تشغيل المشاريع — إنشاء / تحرير
entity: HCMv3.ProjectManagement.ProjectLog
surfaces:
  - angular/src/app/admin/project-management/project-logs/create-or-edit-project-log-modal.component.html
audience: [admin]
permissions: [Pages.ProjectManagement.ProjectLogs.Create]
---

## Overview

سطر في سجل تشغيل محرك تكلفة المشاريع: سطر لكل موظف، إضافة إلى سطر للتشغيل الذي فشل قبل أن يبدأ. وتقرؤه شاشة المحرك لعرض التقدم. يكتبه محرك تكلفة المشاريع، ولا يوجد عادةً سبب لإضافة قيود أو تعديلها يدوياً.

## Employee {#ProjectLog.EmployeeId}
<!-- alias: loc:Employee, dom:ProjectLog_EmployeeId -->

**ما هو.** الموظف الذي عولج، إن وُجد.

**القواعد.** إلزامي. يُختار من قائمة الموظفين.

## RunTimeStamp {#ProjectLog.RunTimeStamp}
<!-- alias: loc:RunTimeStamp, dom:ProjectLog_RunTimeStamp -->

**ما هو.** متى كُتب السطر.

**القواعد.** اختياري. تاريخ.

## Message {#ProjectLog.Message}
<!-- alias: loc:Message, dom:ProjectLog_Message -->

**ما هو.** النتيجة، أو الخطأ.

**القواعد.** اختياري.

## Status {#ProjectLog.Status}
<!-- alias: loc:Status, dom:ProjectLog_Status -->

**ما هو.** 0 عند معالجة الموظف بنجاح، وأي قيمة أخرى تعني فشلاً.

**القواعد.** إلزامي. رقم صحيح.

## InPeriod {#ProjectLog.InPeriod}
<!-- alias: loc:InPeriod, dom:ProjectLog_InPeriod -->

**ما هو.** فترة الرواتب التي شُغّلت.

**القواعد.** اختياري. تاريخ.

## NoOfEmployee {#ProjectLog.NoOfEmployee}
<!-- alias: loc:NoOfEmployee, dom:ProjectLog_NoOfEmployee -->

**ما هو.** عدد الموظفين الذين يشملهم التشغيل.

**القواعد.** إلزامي. رقم صحيح.

## StopFlag {#ProjectLog.StopFlag}
<!-- alias: loc:StopFlag, dom:ProjectLog_StopFlag -->

**ما هو.** طلب إيقاف التشغيل.

**لماذا يهم.** ما دام أي سطر محدداً فيه، يتوقف المحرك قبل الموظف التالي.

**القواعد.** نعم أو لا.
