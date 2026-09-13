---
helpId: pa.employee-abs-adjustment.edit
lang: ar
title: تعديلات إجازات الموظف — إنشاء / تحرير
entity: HCMv3.PersonnelAdministration.EmployeeAbsAdjustment
surfaces:
  - angular/src/app/admin/personnel-administration/employee-detail/abs-adjustments/create-or-edit-employee-abs-adjustment-modal.component.html
audience: [admin]
permissions: [Pages.PersonnelAdministration.EmployeeAbsAdjustments.Create]
---

## Overview

تغيير يدوي على إجازات الموظف -- أيام مصروفة نقداً أو معدَّلة أو مقرَّبة.

تصرف الرواتب صرف الإجازة السنوية نقداً وتعديلات الكسور الواقعة في فترة الدفع على أنها بدل إجازة. ويحتسب رصيد الإجازة حتى التاريخ الذي تستخدمه الرواتب كل تعديل عدا ما حُدد بأنه بلا أثر على الحصة.

لا يؤدي حفظ التعديل إلى تحديث سجل حصة إجازات الموظف، فلا يتغير الرصيد في الخدمة الذاتية.

## AbsenceTypeId {#EmployeeAbsAdjustment.AbsenceTypeId}
<!-- alias: loc:AbsenceTypeId, dom:EmployeeAbsAdjustment_AbsenceTypeId -->

**ما هو.** نوع الإجازة المعدَّل.

**مثال.** إجازة سنوية

**القواعد.** إلزامي. يُختار من قائمة أنواع الغياب.

:::admin
تُدار من إدارة الرواتب ← أنواع الغياب.
:::

## EffectiveDate {#EmployeeAbsAdjustment.EffectiveDate}
<!-- alias: loc:EffectiveDate, dom:EmployeeAbsAdjustment_EffectiveDate -->

**ما هو.** تاريخ سريان التعديل.

**مثال.** 30 يونيو 2026

**لماذا يهم.** تصرف الرواتب النقد الذي يقع تاريخ سريانه في فترة الدفع، ويحتسب رصيد الحصة حتى التاريخ التعديلاتِ حتى التاريخ المطلوب.

**القواعد.** إلزامي. تاريخ.

## AdjustmentType {#EmployeeAbsAdjustment.AdjustmentType}
<!-- alias: loc:AdjustmentType, dom:EmployeeAbsAdjustment_AdjustmentType -->

**ما هو.** نوع التعديل.

**مثال.** صرف نقدي

**لماذا يهم.** تصرف الرواتب تعديلات الصرف النقدي والكسور في الإجازة السنوية. أما «بلا أثر على الحصة» فيُسجَّل دون أن ينقص رصيد الحصة حتى التاريخ.

**القواعد.** إلزامي. إحدى القيم: الصرف النقدي، تعديل، كسري، بلا أثر على الحصة.

## AdjustmentPaymentType {#EmployeeAbsAdjustment.AdjustmentPaymentType}
<!-- alias: loc:AdjustmentPaymentType, dom:EmployeeAbsAdjustment_AdjustmentPaymentType -->

**ما هو.** متى يُصرف النقد.

**مثال.** مع الرواتب

**لماذا يهم.** تلتقط مسيرات الرواتب خارج الدورة تعديلات الدفع المقدم.

**القواعد.** إلزامي. إحدى القيم: الدفع مع الرواتب، الدفع مقدماً.

## QuotaAdjusted {#EmployeeAbsAdjustment.QuotaAdjusted}
<!-- alias: loc:QuotaAdjusted, dom:EmployeeAbsAdjustment_QuotaAdjusted -->

**ما هو.** عدد الأيام المعدَّلة.

**مثال.** 5

**لماذا يهم.** في الصرف النقدي، هي الأيام التي تصرفها الرواتب.

**القواعد.** اختياري. رقم.
