---
helpId: pa.employee-separation.edit
lang: ar
title: حالات فصل الموظف — إنشاء / تحرير
entity: HCMv3.PersonnelAdministration.EmployeeSeparation
surfaces:
  - angular/src/app/admin/personnel-administration/employee-detail/separations/create-or-edit-employee-separation-modal.component.html
audience: [admin]
permissions: [Pages.PersonnelAdministration.EmployeeSeparations.Create]
---

## Overview

خروج الموظف: متى استقال أو أُنهيت خدمته، وآخر يوم له في الخدمة. ويبقى الفصل سارياً حتى يُلغى.

تتوقف الرواتب عن احتساب معظم الأجر بعد نهاية شهر آخر يوم في الخدمة، وتحتاج مسيرة نهاية الخدمة إلى فصل غير ملغى. ولا يؤدي حفظ الفصل إلى إنشاء إيقاف للأجر أو حذف سجلات الوقت اللاحقة؛ فتلك خطوات منفصلة.

## SubmissionDate {#EmployeeSeparation.SubmissionDate}
<!-- alias: loc:SubmissionDate, dom:EmployeeSeparation_SubmissionDate -->

**ما هو.** تاريخ تقديم الاستقالة أو إنهاء الخدمة.

**مثال.** 1 أغسطس 2026

**لماذا يهم.** لا يجوز أن يسبقه آخر يوم في الخدمة. وتستخدم مسيرة نهاية الخدمة أحدث فصل مقدَّم.

**القواعد.** إلزامي. تاريخ.

## LastDayInService {#EmployeeSeparation.LastDayInService}
<!-- alias: loc:LastDayInService, dom:EmployeeSeparation_LastDayInService -->

**ما هو.** آخر يوم عمل للموظف.

**مثال.** 31 أغسطس 2026

**لماذا يهم.** لا تحتسب الرواتب أنواع الأجور لتواريخ دفع بعد نهاية هذا الشهر، وتتوقف اقتطاعات الصندوق، وتُحتسب مكافأة نهاية الخدمة كما في هذا التاريخ.

**القواعد.** إلزامي. تاريخ.

## NoticePeriodDays {#EmployeeSeparation.NoticePeriodDays}
<!-- alias: loc:NoticePeriodDays, dom:EmployeeSeparation_NoticePeriodDays -->

**ما هو.** مدة فترة الإشعار بالأيام.

**مثال.** 30

**لماذا يهم.** يظهر في تقرير الفصل، ولا تُعاد حساب التواريخ منه.

**القواعد.** إلزامي. رقم صحيح.

## PlannedLastDayInService {#EmployeeSeparation.PlannedLastDayInService}
<!-- alias: loc:PlannedLastDayInService, dom:EmployeeSeparation_PlannedLastDayInService -->

**ما هو.** آخر يوم كان مخططاً في الأصل، قبل أي تغيير.

**مثال.** 31 أغسطس 2026

**لماذا يهم.** يظهر في تقرير الفصل.

**القواعد.** إلزامي. تاريخ.

## SeparationType {#EmployeeSeparation.SeparationType}
<!-- alias: loc:SeparationType, dom:EmployeeSeparation_SeparationType -->

**ما هو.** هل استقال الموظف أم أُنهيت خدمته.

**مثال.** استقالة

**لماذا يهم.** يظهر في تقارير الموظفين والتكلفة الإجمالية للموظف.

**القواعد.** إلزامي. إحدى القيم: استقالة، إنهاء خدمة.

## SeparationReason {#EmployeeSeparation.SeparationReasonId}
<!-- alias: loc:SeparationReason, dom:EmployeeSeparation_SeparationReasonId -->

**ما هو.** سبب مغادرة الموظف.

**مثال.** فرصة أفضل

**القواعد.** إلزامي. يُختار من قائمة أسباب الفصل.

:::admin
تُدار من البيانات الأساسية العامة ← أسباب الفصل.
:::

## RevokeDate {#EmployeeSeparation.RevokeDate}
<!-- alias: loc:RevokeDate, dom:EmployeeSeparation_RevokeDate -->

**ما هو.** تاريخ سحب الفصل، إن سُحب.

**مثال.** 10 أغسطس 2026

**لماذا يهم.** بعد تحديده، تتجاهل الرواتب واحتساب مكافأة نهاية الخدمة هذا الفصل.

**القواعد.** اختياري. تاريخ.
