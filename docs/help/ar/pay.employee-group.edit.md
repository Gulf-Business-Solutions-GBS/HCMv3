---
helpId: pay.employee-group.edit
lang: ar
title: ملفات الرواتب — إنشاء / تحرير
entity: HCMv3.PayrollManagement.EmployeeGroup
surfaces:
  - angular/src/app/admin/payroll-management/employee-groups/create-or-edit-employee-group-modal.component.html
audience: [admin]
permissions: [Pages.PayrollManagement.EmployeeGroups.Create]
---

## Overview

تصنيف عام للموظفين، مثل الموظفين الإداريين والعمال، تندرج تحته الدرجات. ويحدد التعيين التنظيمي لكل موظف مجموعته ومجموعته الفرعية.

وتحمل المجموعة الحد الأقصى لمكافأة نهاية الخدمة، ولا يُطبَّق هذا الحد إلا إذا لم يكن **الحد الأقصى لمكافأة نهاية الخدمة** صفراً.

## IndemnityMaxNumber {#EmployeeGroup.IndemnityMaxNumber}
<!-- alias: loc:IndemnityMaxNumber, dom:EmployeeGroup_IndemnityMaxNumber -->

**ما هو.** أقصى ما يمكن أن تبلغه مكافأة نهاية الخدمة من الراتب، محسوباً بالوحدة أدناه.

**مثال.** `18` مع الوحدة شهر -- أي راتب ثمانية عشر شهراً على الأكثر، وهو حد قانون العمل الكويتي.

**لماذا يهم.** الحد هو الراتب الشهري للموظف من أنواع الأجور المؤشَّر عليها *إدراج في مكافأة نهاية الخدمة*، مضروباً في هذا العدد من الأشهر، ثم في النسبة أدناه. ولا تُستحق أيام المكافأة التي تتجاوز الحد. والقيمة 0 تعني عدم وجود حد.

**القواعد.** اختياري. رقم.

## IndemnityMaxUnit {#EmployeeGroup.IndemnityMaxUnit}
<!-- alias: loc:IndemnityMaxUnit, dom:EmployeeGroup_IndemnityMaxUnit -->

**ما هو.** ما إذا كان الحد الأقصى محسوباً بالأشهر أو بالأيام.

**مثال.** شهر

**لماذا يهم.** تُحوَّل الأيام إلى أشهر بقسمتها على 30: فـ540 يوماً هي الحد نفسه لـ18 شهراً.

**القواعد.** إلزامي. إحدى القيم: اليوم، الشهر.

## IndemnityMaxAccruePercentage {#EmployeeGroup.IndemnityMaxAccruePercentage}
<!-- alias: loc:IndemnityMaxAccruePercentage, dom:EmployeeGroup_IndemnityMaxAccruePercentage -->

**ما هو.** النسبة التي يمكن للموظف أن يستحقها فعلاً من ذلك الحد الأقصى.

**مثال.** `100`

**لماذا يهم.** يُضرب فيها الحد. وإذا تُركت صفراً صار الحد صفراً -- فإذا حُدد الحد الأقصى فيجب تحديد هذه النسبة أيضاً.

**القواعد.** اختياري. رقم.
