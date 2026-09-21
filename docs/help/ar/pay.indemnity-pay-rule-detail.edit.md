---
helpId: pay.indemnity-pay-rule-detail.edit
lang: ar
title: تفاصيل قواعد صرف مكافأة نهاية الخدمة — إنشاء / تحرير
entity: HCMv3.PayrollManagement.IndemnityPayRuleDetail
surfaces:
  - angular/src/app/admin/payroll-management/indemnity-pay-rule-details/create-or-edit-indemnity-pay-rule-detail-modal.component.html
audience: [admin]
permissions: [Pages.PayrollManagement.IndemnityPayRuleDetails.Create]
---

## Overview

خطوة واحدة من قاعدة دفع المكافأة، بحسب سنوات الخدمة -- مثل الاستقالة بعد ثلاث إلى خمس سنوات تُصرف بالنصف.

لا تُستخدم حالياً: لا تمرّر أي عملية احتساب للرواتب أو التقارير قاعدة دفع مكافأة، فيأتي حد المكافأة دائماً من **مجموعة الموظفين** التي ينتمي إليها الموظف. وهذه القواعد تُحفظ للرجوع إليها.

## IndemnityPayRuleId {#IndemnityPayRuleDetail.IndemnityPayRuleId}
<!-- alias: loc:IndemnityPayRuleId, dom:IndemnityPayRuleDetail_IndemnityPayRuleId -->

**ما هو.** القاعدة التي تتبعها هذه الخطوة.

**القواعد.** إلزامي. يُختار من قائمة قواعد صرف مكافأة نهاية الخدمة.

## MinYearOfService {#IndemnityPayRuleDetail.MinYearOfService}
<!-- alias: loc:MinYearOfService, dom:IndemnityPayRuleDetail_MinYearOfService -->

**ما هو.** أقل عدد من سنوات الخدمة تغطيه هذه الخطوة.

**مثال.** `3`

**القواعد.** اختياري. رقم.

## MaxYearOfService {#IndemnityPayRuleDetail.MaxYearOfService}
<!-- alias: loc:MaxYearOfService, dom:IndemnityPayRuleDetail_MaxYearOfService -->

**ما هو.** أكبر عدد من سنوات الخدمة تغطيه هذه الخطوة، ولا يجوز أن يقل عن الحد الأدنى.

**مثال.** `5`

**القواعد.** اختياري. رقم.

## Number {#IndemnityPayRuleDetail.Number}
<!-- alias: loc:Number, dom:IndemnityPayRuleDetail_Number -->

**ما هو.** كمية للخطوة، بالوحدة أدناه.

**مثال.** `15`

**القواعد.** اختياري. رقم.

## Unit {#IndemnityPayRuleDetail.Unit}
<!-- alias: loc:Unit, dom:IndemnityPayRuleDetail_Unit -->

**ما هو.** ما إذا كان العدد بالأيام أو بالأشهر.

**مثال.** يوم

**القواعد.** إلزامي. إحدى القيم: اليوم، الشهر.

## PayPercentage {#IndemnityPayRuleDetail.PayPercentage}
<!-- alias: loc:PayPercentage, dom:IndemnityPayRuleDetail_PayPercentage -->

**ما هو.** النسبة المصروفة من المكافأة في هذه الخطوة.

**مثال.** `50`

**القواعد.** اختياري. رقم.
