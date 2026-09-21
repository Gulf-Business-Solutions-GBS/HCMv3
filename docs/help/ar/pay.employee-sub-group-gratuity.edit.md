---
helpId: pay.employee-sub-group-gratuity.edit
lang: ar
title: مكافأة نهاية الخدمة للمجموعة الفرعية للموظفين — إنشاء / تحرير
entity: HCMv3.PayrollManagement.EmployeeSubGroupGratuity
surfaces:
  - angular/src/app/admin/payroll-management/employee-sub-group-detail/gratuities/create-or-edit-employee-sub-group-gratuity-modal.component.html
audience: [admin]
permissions: [Pages.PayrollManagement.EmployeeSubGroupGratuities.Create]
---

## Overview

سلّم مكافأة نهاية الخدمة للمجموعة الفرعية: كم يوماً من الراتب تكسبه كل سنة خدمة، في شرائح بحسب سنوات الخدمة.

وتُوزَّع مدة الخدمة على الشرائح بترتيب **تغيير السنة**. فقانون الكويت مثلاً شريحتان: 15 يوماً عن كل سنة حتى السنة الخامسة، و30 يوماً عن كل سنة بعدها.

## ChangeYear {#EmployeeSubGroupGratuity.ChangeYear}
<!-- alias: loc:ChangeYear, dom:EmployeeSubGroupGratuity_ChangeYear -->

**ما هو.** سنة الخدمة التي تنتهي عندها هذه الشريحة.

**مثال.** `5` للشريحة الأولى، و`99` للأخيرة.

**القواعد.** اختياري. رقم.

:::admin
مع ثلاث شرائح أو أكثر، تحقق من النتيجة على موظف طويل الخدمة: فالشريحة الوسطى تُحتسب بكامل تغيير السنة فيها سنواتٍ، وتُخفَّض الشريحة الأخيرة تعويضاً، ولا يكون ذلك دقيقاً إلا إذا كانت كل الشرائح بعد الأولى تكسب الأيام نفسها. وهذا مطابق للنظام السابق وقد أُبقي عليه عمداً.
:::

## GratuityDays {#EmployeeSubGroupGratuity.GratuityDays}
<!-- alias: loc:GratuityDays, dom:EmployeeSubGroupGratuity_GratuityDays -->

**ما هو.** أيام الراتب المكتسبة عن كل سنة خدمة في هذه الشريحة.

**مثال.** `15`

**لماذا يهم.** أيام المكافأة للشريحة هي هذه الأيام مقسومة على 365، مضروبة في الأيام التي قُضيت فيها.

**القواعد.** اختياري. رقم.

## BasisDays {#EmployeeSubGroupGratuity.BasisDays}
<!-- alias: loc:BasisDays, dom:EmployeeSubGroupGratuity_BasisDays -->

**ما هو.** عدد الأيام الذي يُقسم عليه راتب الشهر لتقييم يوم المكافأة الواحد في هذه الشريحة.

**مثال.** `26`

**لماذا يهم.** القيمة 0 تستخدم **الأيام الثابتة** لنوع الأجر `1010`.

**القواعد.** اختياري. رقم.

## ImpactFromDateOfJoining {#EmployeeSubGroupGratuity.ImpactFromDateOfJoining}
<!-- alias: loc:ImpactFromDateOfJoining, dom:EmployeeSubGroupGratuity_ImpactFromDateOfJoining -->

**ما هو.** ما إذا كانت هذه الشريحة تُقاس على مدة الخدمة كاملة من تاريخ الالتحاق.

**لماذا يهم.** يُستخدم عند اختيار الشريحة التي تُقيّم **أيام الأساس** فيها أجر المكافأة: إذا أُشِّر عليه تُقارن مدة خدمة الموظف كاملة بهذه الشريحة وما بعدها، لا المدة المتبقية بعد الشرائح السابقة فقط.

**القواعد.** نعم أو لا.
