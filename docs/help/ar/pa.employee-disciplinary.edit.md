---
helpId: pa.employee-disciplinary.edit
lang: ar
title: مخالفات الموظف — إنشاء / تحرير
entity: HCMv3.PersonnelAdministration.EmployeeDisciplinary
surfaces:
  - angular/src/app/admin/personnel-administration/employee-detail/disciplinaries/create-or-edit-employee-disciplinary-modal.component.html
audience: [admin]
permissions: [Pages.PersonnelAdministration.EmployeeDisciplinaries.Create]
---

## Overview

مخالفة تأديبية مسجلة على الموظف.

يمكن توجيه السجلات التأديبية للاعتماد. وعندها يكون الموظف المُبلِّغ ومكان العمل إلزاميين، ويُحدَّد نوع الخصم عند الاعتماد من شرائح نوع المخالفة بحسب عدد المخالفات السابقة من النوع نفسه الواقعة ضمن فترة التكرار الخاصة به. ولا يُقتطع أي خصم في الرواتب حالياً.

## OrganizationUnitId {#EmployeeDisciplinary.OrganizationUnitId}
<!-- alias: loc:OrganizationUnitId, dom:EmployeeDisciplinary_OrganizationUnitId -->

**ما هو.** القسم الذي وقعت فيه المخالفة.

**مثال.** عمليات المستودع

**لماذا يهم.** يظهر في تقرير المخالفات.

**القواعد.** اختياري. يُختار من قائمة الوحدات التنظيمية.

## WorkLocationId {#EmployeeDisciplinary.WorkLocationId}
<!-- alias: loc:WorkLocationId, dom:EmployeeDisciplinary_WorkLocationId -->

**ما هو.** مكان العمل الذي وقعت فيه المخالفة.

**مثال.** المستودع — الشويخ

**لماذا يهم.** إلزامي عند توجيه السجل للاعتماد.

**القواعد.** اختياري. يُختار من قائمة أماكن العمل.

## DisciplinaryTypeId {#EmployeeDisciplinary.DisciplinaryTypeId}
<!-- alias: loc:DisciplinaryTypeId, dom:EmployeeDisciplinary_DisciplinaryTypeId -->

**ما هو.** نوع المخالفة.

**مثال.** التأخر في الحضور

**لماذا يهم.** تحدد المخالفات السابقة من النوع نفسه ضمن فترة التكرار الخاصة به شريحةَ التصعيد المستخدمة عند الاعتماد.

**القواعد.** إلزامي. يُختار من قائمة أنواع المخالفات.

:::admin
تُدار من البيانات الأساسية العامة ← أنواع المخالفات.
:::

## DisciplinaryDate {#EmployeeDisciplinary.DisciplinaryDate}
<!-- alias: loc:DisciplinaryDate, dom:EmployeeDisciplinary_DisciplinaryDate -->

**ما هو.** تاريخ المخالفة.

**مثال.** 14 سبتمبر 2026

**لماذا يهم.** تُحتسب فترة التكرار رجوعاً من هذا التاريخ.

**القواعد.** إلزامي. تاريخ.

## DisciplinaryTime {#EmployeeDisciplinary.DisciplinaryTime}
<!-- alias: loc:DisciplinaryTime, dom:EmployeeDisciplinary_DisciplinaryTime -->

**ما هو.** وقت المخالفة.

**مثال.** 08:45

**لماذا يهم.** يُسجَّل للرجوع إليه، ولا يقرؤه أي احتساب أو تقرير حالياً.

**القواعد.** اختياري. تاريخ.

## ReportedById {#EmployeeDisciplinary.ReportedById}
<!-- alias: loc:ReportedById, dom:EmployeeDisciplinary_ReportedById -->

**ما هو.** الموظف الذي أبلغ عن المخالفة.

**مثال.** خالد الرشيدي (1044)

**لماذا يهم.** إلزامي عند توجيه السجل للاعتماد.

**القواعد.** اختياري. يُختار من قائمة الموظفين.

## Severity {#EmployeeDisciplinary.Severity}
<!-- alias: loc:Severity, dom:EmployeeDisciplinary_Severity -->

**ما هو.** درجة خطورة المخالفة.

**مثال.** منخفضة

**القواعد.** إلزامي. إحدى القيم: منخفض، مرتفع.

## DisciplinaryDeductionTypeId {#EmployeeDisciplinary.DisciplinaryDeductionTypeId}
<!-- alias: loc:DisciplinaryDeductionTypeId, dom:EmployeeDisciplinary_DisciplinaryDeductionTypeId -->

**ما هو.** الجزاء المطبَّق.

**مثال.** خصم أجر يوم

**لماذا يهم.** يُحدَّد من شرائح نوع المخالفة عند الاعتماد إذا وُجِّه السجل للاعتماد. ولا يُقتطع في الرواتب حالياً.

**القواعد.** اختياري. يُختار من قائمة أنواع الخصم بسبب إجراء تأديبي.

## WlSendToEmp {#EmployeeDisciplinary.WLSendToEmp}
<!-- alias: loc:WlSendToEmp, dom:EmployeeDisciplinary_WlSendToEmp -->

**ما هو.** هل أُرسل لفت النظر إلى الموظف.

**لماذا يهم.** يُسجَّل للرجوع إليه، ولا يقرؤه أي احتساب أو تقرير حالياً.

**القواعد.** نعم أو لا.

## WlRecvFromEmp {#EmployeeDisciplinary.WLRecvFromEmp}
<!-- alias: loc:WlRecvFromEmp, dom:EmployeeDisciplinary_WlRecvFromEmp -->

**ما هو.** هل استُلم لفت النظر موقَّعاً من الموظف.

**لماذا يهم.** يُسجَّل للرجوع إليه، ولا يقرؤه أي احتساب أو تقرير حالياً.

**القواعد.** نعم أو لا.
