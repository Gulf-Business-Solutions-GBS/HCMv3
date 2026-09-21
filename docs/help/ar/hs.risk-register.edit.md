---
helpId: hs.risk-register.edit
lang: ar
title: سجلات المخاطر — إنشاء / تحرير
entity: HCMv3.HealthAndSafetyManagement.RiskRegister
surfaces:
  - angular/src/app/admin/health-and-safety-management/risk-registers/create-or-edit-risk-register-modal.component.html
audience: [admin]
permissions: [Pages.HealthAndSafetyManagement.RiskRegisters.Create]
---

## Overview

خطر في مكان العمل، ومدى احتماله وخطورته، وما يُتخذ بشأنه.

## ReportingDate {#RiskRegister.ReportingDate}
<!-- alias: loc:ReportingDate, dom:RiskRegister_ReportingDate -->

**ما هو.** متى أُبلغ عن الخطر.

**القواعد.** إلزامي. تاريخ.

## ReportingTime {#RiskRegister.ReportingTime}
<!-- alias: loc:ReportingTime, dom:RiskRegister_ReportingTime -->

**ما هو.** الوقت من اليوم، إن كان معروفاً.

**مثال.** 14:30

**القواعد.** اختياري. تاريخ.

## RiskType {#RiskRegister.RiskTypeId}
<!-- alias: loc:RiskType, dom:RiskRegister_RiskTypeId -->

**ما هو.** نوع الخطر.

**مثال.** كهربائي

**القواعد.** إلزامي. يُختار من قائمة أنواع المخاطر.

## ReassessmentDate {#RiskRegister.ReassessmentDate}
<!-- alias: loc:ReassessmentDate, dom:RiskRegister_ReassessmentDate -->

**ما هو.** متى يجب إعادة النظر في الخطر.

**لماذا يهم.** للتسجيل فقط: لا يُرسل تذكير عند حلوله.

**القواعد.** إلزامي. تاريخ.

## Impact {#RiskRegister.ImpactId}
<!-- alias: loc:Impact, dom:RiskRegister_ImpactId -->

**ما هو.** مدى سوء أثره.

**مثال.** مرتفع

**القواعد.** إلزامي. يُختار من قائمة آثار المخاطر.

## Possibility {#RiskRegister.PossibilityId}
<!-- alias: loc:Possibility, dom:RiskRegister_PossibilityId -->

**ما هو.** مدى احتماله.

**مثال.** غير محتمل

**القواعد.** إلزامي. يُختار من قائمة احتمالات المخاطر.

## Location {#RiskRegister.LocationId}
<!-- alias: loc:Location, dom:RiskRegister_LocationId -->

**ما هو.** موقع العمل.

**مثال.** المستودع الرئيسي

**القواعد.** إلزامي. يُختار من قائمة أماكن العمل.

## Status {#RiskRegister.StatusId}
<!-- alias: loc:Status, dom:RiskRegister_StatusId -->

**ما هو.** حالة الخطر.

**مثال.** مفتوح

**القواعد.** اختياري. يُختار من قائمة حالات المخاطر.

## ReportedBy {#RiskRegister.ReportedById}
<!-- alias: loc:ReportedBy, dom:RiskRegister_ReportedById -->

**ما هو.** من أبلغ عنه.

**القواعد.** إلزامي. يُختار من قائمة الموظفين.

## AssignedTo {#RiskRegister.AssignedToId}
<!-- alias: loc:AssignedTo, dom:RiskRegister_AssignedToId -->

**ما هو.** من يتولى معالجته.

**القواعد.** اختياري. يُختار من قائمة الموظفين.

## Action {#RiskRegister.ActionId}
<!-- alias: loc:Action, dom:RiskRegister_ActionId -->

**ما هو.** نوع الاستجابة.

**مثال.** تقليل

**القواعد.** اختياري. يُختار من قائمة إجراءات المخاطر.

## Description {#RiskRegister.Description}
<!-- alias: loc:Description, dom:RiskRegister_Description -->

**ما هو.** الخطر.

**القواعد.** اختياري.

## RiskMitigation {#RiskRegister.RiskMitigation}
<!-- alias: loc:RiskMitigation, dom:RiskRegister_RiskMitigation -->

**ما هو.** ما يُتخذ لتقليله.

**القواعد.** اختياري.
