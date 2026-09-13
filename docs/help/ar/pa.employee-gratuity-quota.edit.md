---
helpId: pa.employee-gratuity-quota.edit
lang: ar
title: حصص مكافأة نهاية الخدمة للموظف — إنشاء / تحرير
entity: HCMv3.PersonnelAdministration.EmployeeGratuityQuota
surfaces:
  - angular/src/app/admin/personnel-administration/employee-detail/gratuity-quotas/create-or-edit-employee-gratuity-quota-modal.component.html
audience: [admin]
permissions: [Pages.PersonnelAdministration.EmployeeGratuityQuotas.Create]
---

## Overview

استحقاق الموظف من مكافأة نهاية الخدمة خلال فترة استحقاق.

ينشئ محرك الوقت حصة بنفسه لأي يوم لا تغطيه حصة، عندما يكون للمجموعة الفرعية للموظف قاعدة مكافأة نهاية خدمة. ويستبدل احتسابُ المكافأة المتراكمَ والمستخدَم من نتائجه في كل مرة يُشغَّل فيها.

## EntitlementFrom {#EmployeeGratuityQuota.EntitlementFrom}
<!-- alias: loc:EntitlementFrom, dom:EmployeeGratuityQuota_EntitlementFrom -->

**ما هو.** أول يوم في فترة الاستحقاق.

**مثال.** 1 يناير 2026

**القواعد.** إلزامي. تاريخ.

## EntitlementTo {#EmployeeGratuityQuota.EntitlementTo}
<!-- alias: loc:EntitlementTo, dom:EmployeeGratuityQuota_EntitlementTo -->

**ما هو.** آخر يوم في فترة الاستحقاق.

**مثال.** 31 ديسمبر 2026

**لماذا يهم.** يستخدم تقرير مخصص مكافأة نهاية الخدمة الحصة التي تغطي تاريخه.

**القواعد.** إلزامي. تاريخ.

## DeductionFrom {#EmployeeGratuityQuota.DeductionFrom}
<!-- alias: loc:DeductionFrom, dom:EmployeeGratuityQuota_DeductionFrom -->

**ما هو.** أول يوم في فترة الخصم.

**مثال.** 1 يناير 2026

**لماذا يهم.** يظهر في تقرير مخصص مكافأة نهاية الخدمة.

**القواعد.** إلزامي. تاريخ.

## DeductionTo {#EmployeeGratuityQuota.DeductionTo}
<!-- alias: loc:DeductionTo, dom:EmployeeGratuityQuota_DeductionTo -->

**ما هو.** آخر يوم في فترة الخصم.

**مثال.** 31 ديسمبر 2026

**لماذا يهم.** يظهر في تقرير مخصص مكافأة نهاية الخدمة.

**القواعد.** إلزامي. تاريخ.

## QuotaAccrued {#EmployeeGratuityQuota.QuotaAccrued}
<!-- alias: loc:QuotaAccrued, dom:EmployeeGratuityQuota_QuotaAccrued -->

**ما هو.** أيام مكافأة نهاية الخدمة المتراكمة.

**مثال.** 21

**لماذا يهم.** يستبدله احتساب المكافأة.

**القواعد.** اختياري. رقم.

## QuotaUsed {#EmployeeGratuityQuota.QuotaUsed}
<!-- alias: loc:QuotaUsed, dom:EmployeeGratuityQuota_QuotaUsed -->

**ما هو.** أيام مكافأة نهاية الخدمة المصروفة.

**مثال.** 0

**لماذا يهم.** يستبدله احتساب المكافأة.

**القواعد.** اختياري. رقم.

## QuotaAdjusted {#EmployeeGratuityQuota.QuotaAdjusted}
<!-- alias: loc:QuotaAdjusted, dom:EmployeeGratuityQuota_QuotaAdjusted -->

**ما هو.** أيام مكافأة نهاية الخدمة المعدَّلة.

**مثال.** 0

**لماذا يهم.** يُسجَّل فقط، ولا يقرؤه احتساب المكافأة حالياً.

**القواعد.** اختياري. رقم.
