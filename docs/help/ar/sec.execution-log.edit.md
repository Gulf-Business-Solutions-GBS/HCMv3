---
helpId: sec.execution-log.edit
lang: ar
title: سجلات التنفيذ — إنشاء / تحرير
entity: HCMv3.SystemSecurity.ExecutionLog
surfaces:
  - angular/src/app/admin/system-security/execution-logs/create-or-edit-execution-log-modal.component.html
audience: [admin]
permissions: [Pages.SystemSecurity.ExecutionLogs.Create]
---

## Overview

تشغيل واحد لمحرك أو استيراد أو تقرير أو طباعة: من شغّله، ومتى، وبأي معايير، وكيف انتهى. ويسرد تقرير سجل التنفيذ هذه القيود. يكتبه النظام أثناء عمله، ولا يوجد عادةً سبب لإضافة قيود أو تعديلها يدوياً.

## UserName {#ExecutionLog.UserName}
<!-- alias: loc:UserName, dom:ExecutionLog_UserName -->

**ما هو.** من شغّله.

**القواعد.** اختياري. الحد الأقصى 256 حرفاً.

## ActionDateTime {#ExecutionLog.ActionDateTime}
<!-- alias: loc:ActionDateTime, dom:ExecutionLog_ActionDateTime -->

**ما هو.** متى جرى التشغيل.

**القواعد.** اختياري. تاريخ.

## Action {#ExecutionLog.Action}
<!-- alias: loc:Action, dom:ExecutionLog_Action -->

**ما هو.** ما الذي نُفِّذ.

**مثال.** تشغيل

**القواعد.** إلزامي. إحدى القيم: تشغيل، إنشاء، عرض، تصدير، طباعة.

## Module {#ExecutionLog.Module}
<!-- alias: loc:Module, dom:ExecutionLog_Module -->

**ما هو.** أي جزء من النظام.

**مثال.** محرك الرواتب

**القواعد.** إلزامي. إحدى القيم: محرك الوقت، محرك الرواتب، ملف البنك، تقرير جدولي، طباعة التقرير، محرك التكلفة، محرك المشاريع.

## ScreenName {#ExecutionLog.ScreenName}
<!-- alias: loc:ScreenName, dom:ExecutionLog_ScreenName -->

**ما هو.** الشاشة التي شُغِّل منها.

**القواعد.** اختياري. الحد الأقصى 256 حرفاً.

## ScreenCaption {#ExecutionLog.ScreenCaption}
<!-- alias: loc:ScreenCaption, dom:ExecutionLog_ScreenCaption -->

**ما هو.** عنوان الشاشة.

**القواعد.** اختياري. الحد الأقصى 256 حرفاً.

## ExecutionStatus {#ExecutionLog.ExecutionStatus}
<!-- alias: loc:ExecutionStatus, dom:ExecutionLog_ExecutionStatus -->

**ما هو.** كيف انتهى.

**مثال.** نجاح

**القواعد.** إلزامي. إحدى القيم: قيد التنفيذ، نجاح، فشل، جزئي، متوقف.

## TotalRecords {#ExecutionLog.TotalRecords}
<!-- alias: loc:TotalRecords, dom:ExecutionLog_TotalRecords -->

**ما هو.** السجلات المعالجة.

**مثال.** `240`

**القواعد.** اختياري. رقم صحيح.

## Successfull {#ExecutionLog.Successfull}
<!-- alias: loc:Successfull, dom:ExecutionLog_Successfull -->

**ما هو.** السجلات الناجحة.

**مثال.** `238`

**القواعد.** اختياري. رقم صحيح.

## Failed {#ExecutionLog.Failed}
<!-- alias: loc:Failed, dom:ExecutionLog_Failed -->

**ما هو.** السجلات الفاشلة.

**مثال.** `2`

**القواعد.** اختياري. رقم صحيح.

## Parameters {#ExecutionLog.Parameters}
<!-- alias: loc:Parameters, dom:ExecutionLog_Parameters -->

**ما هو.** المعايير التي شُغِّل بها.

**القواعد.** اختياري.
