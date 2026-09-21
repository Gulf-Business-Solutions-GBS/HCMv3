---
helpId: sec.retro-reason-log.edit
lang: ar
title: سجلات أسباب الاحتساب بأثر رجعي — إنشاء / تحرير
entity: HCMv3.SystemSecurity.RetroReasonLog
surfaces:
  - angular/src/app/admin/system-security/retro-reason-logs/create-or-edit-retro-reason-log-modal.component.html
audience: [admin]
permissions: [Pages.SystemSecurity.RetroReasonLogs.Create]
---

## Overview

سجل لتعديل بتاريخ سابق جعل نظام الرواتب يعيد احتساب موظف من تاريخ أسبق: ما الذي تغيّر، ومتى، وتاريخ الأثر الرجعي. يكتبه النظام أثناء عمله، ولا يوجد عادةً سبب لإضافة قيود أو تعديلها يدوياً.

## Employee {#RetroReasonLog.EmployeeId}
<!-- alias: loc:Employee, dom:RetroReasonLog_EmployeeId -->

**ما هو.** الموظف.

**القواعد.** إلزامي. يُختار من قائمة الموظفين.

## ChangedOn {#RetroReasonLog.ChangedOn}
<!-- alias: loc:ChangedOn, dom:RetroReasonLog_ChangedOn -->

**ما هو.** متى أُجري التعديل.

**القواعد.** إلزامي. تاريخ.

## RetroDate {#RetroReasonLog.RetroDate}
<!-- alias: loc:RetroDate, dom:RetroReasonLog_RetroDate -->

**ما هو.** التاريخ الذي يعيد نظام الرواتب الاحتساب منه.

**مثال.** `2026-02-01`

**القواعد.** إلزامي. تاريخ.

## EntityObjectName {#RetroReasonLog.EntityObjectName}
<!-- alias: loc:EntityObjectName, dom:RetroReasonLog_EntityObjectName -->

**ما هو.** نوع السجل المعدَّل.

**مثال.** `HCMv3.PersonnelAdministration.EmployeeBasicPay`

**القواعد.** اختياري. الحد الأقصى 256 حرفاً.

## EntityObjectId {#RetroReasonLog.EntityObjectId}
<!-- alias: loc:EntityObjectId, dom:RetroReasonLog_EntityObjectId -->

**ما هو.** السجل المعدَّل.

**القواعد.** إلزامي. يُختار من قائمة.

## RetroReason {#RetroReasonLog.RetroReason}
<!-- alias: loc:RetroReason, dom:RetroReasonLog_RetroReason -->

**ما هو.** سبب ضبط تاريخ الأثر الرجعي.

**القواعد.** اختياري. الحد الأقصى 4000 حرف.
