---
helpId: pa.employee-overtime.edit
lang: ar
title: العمل الإضافي للموظف — إنشاء / تحرير
entity: HCMv3.PersonnelAdministration.EmployeeOvertime
surfaces:
  - angular/src/app/admin/personnel-administration/employee-detail/overtimes/create-or-edit-employee-overtime-modal.component.html
audience: [admin]
permissions: [Pages.PersonnelAdministration.EmployeeOvertimes.Create]
---

## Overview

العمل الإضافي للموظف في تاريخ واحد: الساعات المطلوبة، والساعات التي قاسها محرك الوقت، والساعات المعتمدة. سجل عمل إضافي واحد لكل موظف في كل تاريخ.

تصرف الرواتب الساعات المعتمدة مضروبة في معدل نوع العمل الإضافي. وينشئ محرك الوقت هذه السجلات بنفسه عندما يجد عملاً إضافياً، ويملأ الساعات المعتمدة من الساعات المقاسة إن لم تُعتمد ساعات.

وبعد حفظ السجل يعرض النموذج أيضاً **مبلغ العمل الإضافي المتوقع** (للقراءة فقط ولا يُحفظ، ويظهر كذلك عموداً في قائمة العمل الإضافي وفي تقريري العمل الإضافي ومعاملات الرواتب الشهرية). وهو الساعات المعتمدة مضروبة في معدل نوع العمل الإضافي، ثم في أجر الساعة. وأجر الساعة هو المعدل الثابت في تفاصيل نوع العمل الإضافي التي تغطي التاريخ؛ وفيما عدا ذلك، أو إذا لم تغطِّ أي تفاصيل التاريخ، فهو الراتب الأساسي للموظف مقسوماً على أيام الشهر -- الأيام الثابتة أو الأيام التقويمية أو أيام العمل، حسب طريقة احتساب نوع الأجر المرتبط بنوع العمل الإضافي -- ثم على أساس ساعات العمل الإضافي في إعدادات الرواتب (ساعات العمل الافتراضية، أو ساعات دوام الموظف في ذلك اليوم). وإذا لم يكن لنوع العمل الإضافي نوع أجر كان المبلغ صفراً. وهو مبلغ تقديري: إذ تحتسب الرواتب أجر الساعة بنفسها من كشف الراتب، فقد يختلف المبلغ المصروف.

## EffectiveDate {#EmployeeOvertime.EffectiveDate}
<!-- alias: loc:EffectiveDate, dom:EmployeeOvertime_EffectiveDate -->

**ما هو.** التاريخ الذي أُدي فيه العمل الإضافي.

**مثال.** 12 سبتمبر 2026

**لماذا يهم.** تصرف الرواتب العمل الإضافي الواقع ضمن فترة الدفع.

**القواعد.** إلزامي. تاريخ.

## OvertimeTypeId {#EmployeeOvertime.OvertimeTypeId}
<!-- alias: loc:OvertimeTypeId, dom:EmployeeOvertime_OvertimeTypeId -->

**ما هو.** نوع العمل الإضافي، ويحدد المعدل.

**مثال.** عمل إضافي نهاية الأسبوع

**القواعد.** إلزامي. يُختار من قائمة أنواع العمل الإضافي.

:::admin
تُدار من البيانات الأساسية العامة ← أنواع العمل الإضافي.
:::

## OvertimeHours {#EmployeeOvertime.OvertimeHours}
<!-- alias: loc:OvertimeHours, dom:EmployeeOvertime_OvertimeHours -->

**ما هو.** الساعات المطلوبة.

**مثال.** 4

**لماذا يهم.** تظهر في طلب الاعتماد، ولا تصرفها الرواتب مباشرة.

**القواعد.** اختياري. رقم.

## ActualOvertimeHours {#EmployeeOvertime.ActualOvertimeHours}
<!-- alias: loc:ActualOvertimeHours, dom:EmployeeOvertime_ActualOvertimeHours -->

**ما هو.** الساعات التي قاسها محرك الوقت.

**مثال.** 3.5

**لماذا يهم.** يستبدلها محرك الوقت في كل مرة يعالج فيها التاريخ.

**القواعد.** اختياري. رقم.

## ApprovedOvertimeHours {#EmployeeOvertime.ApprovedOvertimeHours}
<!-- alias: loc:ApprovedOvertimeHours, dom:EmployeeOvertime_ApprovedOvertimeHours -->

**ما هو.** الساعات المعتمدة للصرف.

**مثال.** 3.5

**لماذا يهم.** تصرف الرواتب هذه الساعات مضروبة في معدل العمل الإضافي. وإذا تُركت صفراً يملؤها محرك الوقت من الساعات المقاسة.

**القواعد.** اختياري. رقم.
