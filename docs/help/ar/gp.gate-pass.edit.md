---
helpId: gp.gate-pass.edit
lang: ar
title: تصاريح الدخول والخروج — إنشاء / تحرير
entity: HCMv3.GatePassManagement.GatePass
surfaces:
  - angular/src/app/admin/gate-pass-management/gate-passes/create-or-edit-gate-pass-modal.component.html
audience: [admin]
permissions: [Pages.GatePassManagement.GatePasses.Create]
---

## Overview

إذن بدخول أشخاص أو مركبات أو مواد إلى موقع أو خروجها منه. ويُدرج الأشخاص والمركبات والمواد كتفاصيل. ولا يجوز أن يكون تاريخ "صالح حتى" قبل تاريخ الإصدار.

## GatePassNumber {#GatePass.GatePassNumber}
<!-- alias: loc:GatePassNumber, dom:GatePass_GatePassNumber -->

**ما هو.** رقم التصريح. يُكتب يدوياً ولا يعطيه النظام.

**مثال.** 1024

**القواعد.** إلزامي. رقم صحيح.

## GatePassType {#GatePass.GatePassTypeId}
<!-- alias: loc:GatePassType, dom:GatePass_GatePassTypeId -->

**ما هو.** نوع التصريح.

**مثال.** زائر

**القواعد.** إلزامي. يُختار من قائمة أنواع تصاريح الدخول والخروج.

## GatePassMovement {#GatePass.GatePassMovementId}
<!-- alias: loc:GatePassMovement, dom:GatePass_GatePassMovementId -->

**ما هو.** اتجاه الحركة عبر البوابة.

**مثال.** خروج

**القواعد.** إلزامي. يُختار من قائمة.

## Location {#GatePass.LocationId}
<!-- alias: loc:Location, dom:GatePass_LocationId -->

**ما هو.** الموقع.

**القواعد.** اختياري. يُختار من قائمة أماكن العمل.

## RequestedByEmployee {#GatePass.RequestedByEmployeeId}
<!-- alias: loc:RequestedByEmployee, dom:GatePass_RequestedByEmployeeId -->

**ما هو.** من طلب التصريح.

**القواعد.** اختياري. يُختار من قائمة.

## GatePassStatus {#GatePass.GatePassStatusId}
<!-- alias: loc:GatePassStatus, dom:GatePass_GatePassStatusId -->

**ما هو.** حالة التصريح.

**مثال.** صادر

**القواعد.** اختياري. يُختار من قائمة.

## IssueDate {#GatePass.IssueDate}
<!-- alias: loc:IssueDate, dom:GatePass_IssueDate -->

**ما هو.** متى صدر التصريح.

**القواعد.** إلزامي. تاريخ.

## ValidUpto {#GatePass.ValidUpto}
<!-- alias: loc:ValidUpto, dom:GatePass_ValidUpto -->

**ما هو.** آخر يوم يمكن فيه استخدام التصريح.

**لماذا يهم.** للتسجيل فقط: لا يوجد ما ينبّه عند حلوله.

**القواعد.** إلزامي. تاريخ.

## StartTime {#GatePass.StartTime}
<!-- alias: loc:StartTime, dom:GatePass_StartTime -->

**ما هو.** وقت بدء التصريح.

**مثال.** 08:00

**القواعد.** إلزامي. تاريخ.

## EndTime {#GatePass.EndTime}
<!-- alias: loc:EndTime, dom:GatePass_EndTime -->

**ما هو.** وقت انتهاء التصريح.

**مثال.** 17:00

**القواعد.** إلزامي. تاريخ.
