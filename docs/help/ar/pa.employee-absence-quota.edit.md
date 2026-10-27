---
helpId: pa.employee-absence-quota.edit
lang: ar
title: حصص إجازات الموظف — إنشاء / تحرير
entity: HCMv3.PersonnelAdministration.EmployeeAbsenceQuota
surfaces:
  - angular/src/app/admin/personnel-administration/employee-detail/absence-quotas/create-or-edit-employee-absence-quota-modal.component.html
audience: [admin]
permissions: [Pages.PersonnelAdministration.EmployeeAbsenceQuotas.Create]
---

## Overview

استحقاق الموظف من نوع إجازة واحد خلال فترة استحقاق: المتراكم منه، والمستخدَم والمعدَّل والمُسقط.

يُبنى رصيد الإجازات في الخدمة الذاتية من الحصة التي تغطي فترة استحقاقها التاريخ. وفي الإجازة السنوية، يضيف محرك الوقت تراكم كل يوم إلى الحصة المتراكمة أثناء تشغيله.

عند فتح حصة قائمة، يعرض **رصيد الحصة** المتبقي منها كما في القائمة: المتراكم مطروحاً منه المستخدَم والمعدَّل والمُسقط. ويُحتسب ولا يمكن إدخاله يدوياً.

## AbsenceTypeId {#EmployeeAbsenceQuota.AbsenceTypeId}
<!-- alias: loc:AbsenceTypeId, dom:EmployeeAbsenceQuota_AbsenceTypeId -->

**ما هو.** نوع الإجازة الذي تخصه هذه الحصة.

**مثال.** إجازة سنوية

**القواعد.** إلزامي. يُختار من قائمة أنواع الإجازات.

:::admin
تُدار من إدارة الرواتب ← أنواع الإجازات.
:::

## NotificationSent {#EmployeeAbsenceQuota.NotificationSent}
<!-- alias: loc:NotificationSent, dom:EmployeeAbsenceQuota_NotificationSent -->

**ما هو.** هل أُرسل إشعار بلوغ الحد الأقصى للإجازة السنوية.

**لماذا يهم.** يلغيه محرك الوقت عندما يعود الرصيد إلى ما دون حد الإشعار، ليمكن إرسال الإشعار مرة أخرى.

**القواعد.** نعم أو لا.

## EntitlementFrom {#EmployeeAbsenceQuota.EntitlementFrom}
<!-- alias: loc:EntitlementFrom, dom:EmployeeAbsenceQuota_EntitlementFrom -->

**ما هو.** أول يوم في فترة الاستحقاق.

**مثال.** 1 يناير 2026

**لماذا يهم.** الحصة التي تغطي فترة استحقاقها تاريخاً ما هي المستخدمة لحساب الرصيد في ذلك التاريخ.

**القواعد.** إلزامي. تاريخ.

:::admin
احرص على أن تكون فترات استحقاق الإجازة السنوية متصلة. ينشئ محرك الوقت أول حصة إجازة سنوية بنفسه عندما لا يكون للموظف أي حصة، لكن إذا كانت للموظف حصة ولم تغطِّ أيٌّ منها يوماً يعالجه، يتوقف برسالة «AbsenceQuota exists for Employee».
:::

## EntitlementTo {#EmployeeAbsenceQuota.EntitlementTo}
<!-- alias: loc:EntitlementTo, dom:EmployeeAbsenceQuota_EntitlementTo -->

**ما هو.** آخر يوم في فترة الاستحقاق.

**مثال.** 31 ديسمبر 2026

**القواعد.** إلزامي. تاريخ.

## DeductionFrom {#EmployeeAbsenceQuota.DeductionFrom}
<!-- alias: loc:DeductionFrom, dom:EmployeeAbsenceQuota_DeductionFrom -->

**ما هو.** أول يوم يمكن فيه أخذ إجازة من هذه الحصة.

**مثال.** 1 يناير 2026

**لماذا يهم.** تختار احتسابات إسقاط الإجازات الحصة التي تغطي فترة الخصم فيها الشهر.

**القواعد.** إلزامي. تاريخ.

## DeductionTo {#EmployeeAbsenceQuota.DeductionTo}
<!-- alias: loc:DeductionTo, dom:EmployeeAbsenceQuota_DeductionTo -->

**ما هو.** آخر يوم يمكن فيه أخذ إجازة من هذه الحصة.

**مثال.** 31 مارس 2027

**القواعد.** إلزامي. تاريخ.

## QuotaAccrued {#EmployeeAbsenceQuota.QuotaAccrued}
<!-- alias: loc:QuotaAccrued, dom:EmployeeAbsenceQuota_QuotaAccrued -->

**ما هو.** الأيام المكتسبة في هذه الفترة.

**مثال.** 30

**القواعد.** اختياري. رقم.

## QuotaUsed {#EmployeeAbsenceQuota.QuotaUsed}
<!-- alias: loc:QuotaUsed, dom:EmployeeAbsenceQuota_QuotaUsed -->

**ما هو.** الأيام المأخوذة من هذه الحصة.

**مثال.** 12

**لماذا يهم.** تضيف إليه الإجازات المعتمدة عبر الخدمة الذاتية، ويُطرح منه عند إلغاء إجازة معتمدة. أما حالات الغياب التي يُدخلها قسم الموارد البشرية في نموذج الغياب فلا تغيّره.

**القواعد.** اختياري. رقم.

## QuotaAdjusted {#EmployeeAbsenceQuota.QuotaAdjusted}
<!-- alias: loc:QuotaAdjusted, dom:EmployeeAbsenceQuota_QuotaAdjusted -->

**ما هو.** الأيام المعدَّلة على هذه الحصة -- كالأيام المصروفة نقداً.

**مثال.** 5

**لماذا يهم.** لا يؤدي حفظ تعديل إجازة إلى تحديث هذا الرقم؛ أدخله هنا إن كان يجب أن تعكس الحصة التعديل.

**القواعد.** اختياري. رقم.

## QuotaForfeited {#EmployeeAbsenceQuota.QuotaForfeited}
<!-- alias: loc:QuotaForfeited, dom:EmployeeAbsenceQuota_QuotaForfeited -->

**ما هو.** الأيام المُسقطة من هذه الحصة.

**مثال.** 3

**لماذا يهم.** تحدّثه احتسابات إسقاط الإجازات، ولا يحدّثه حفظ إسقاط يدوي.

**القواعد.** اختياري. رقم.
