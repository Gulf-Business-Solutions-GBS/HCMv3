---
helpId: pay.indemnity-pay-rule.edit
lang: ar
title: قواعد صرف مكافأة نهاية الخدمة — إنشاء / تحرير
entity: HCMv3.PayrollManagement.IndemnityPayRule
surfaces:
  - angular/src/app/admin/payroll-management/indemnity-pay-rules/create-or-edit-indemnity-pay-rule-modal.component.html
audience: [admin]
permissions: [Pages.PayrollManagement.IndemnityPayRules.Create]
---

## Overview

حد لمكافأة نهاية الخدمة بحسب سبب انتهاء الخدمة والبلد.

لا تُستخدم حالياً: لا تمرّر أي عملية احتساب للرواتب أو التقارير قاعدة دفع مكافأة، فيأتي حد المكافأة دائماً من **مجموعة الموظفين** التي ينتمي إليها الموظف. وهذه القواعد تُحفظ للرجوع إليها.

## SeparationType {#IndemnityPayRule.SeparationType}
<!-- alias: loc:SeparationType, dom:IndemnityPayRule_SeparationType -->

**ما هو.** سبب انتهاء الخدمة الذي تخصه القاعدة.

**مثال.** استقالة

**القواعد.** إلزامي. إحدى القيم: استقالة، إنهاء خدمة.

## CountryId {#IndemnityPayRule.CountryId}
<!-- alias: loc:CountryId, dom:IndemnityPayRule_CountryId -->

**ما هو.** البلد الذي تخصه القاعدة.

**مثال.** الكويت

**القواعد.** اختياري. يُختار من قائمة الدول.

## MaxNumber {#IndemnityPayRule.MaxNumber}
<!-- alias: loc:MaxNumber, dom:IndemnityPayRule_MaxNumber -->

**ما هو.** أقصى ما يمكن أن تبلغه المكافأة من الراتب، بالوحدة أدناه.

**مثال.** `18`

**القواعد.** اختياري. رقم.

## MaxUnit {#IndemnityPayRule.MaxUnit}
<!-- alias: loc:MaxUnit, dom:IndemnityPayRule_MaxUnit -->

**ما هو.** ما إذا كان الحد الأقصى بالأشهر أو بالأيام.

**مثال.** شهر

**القواعد.** إلزامي. إحدى القيم: اليوم، الشهر.

## MaxPayPercentage {#IndemnityPayRule.MaxPayPercentage}
<!-- alias: loc:MaxPayPercentage, dom:IndemnityPayRule_MaxPayPercentage -->

**ما هو.** النسبة التي يمكن صرفها من ذلك الحد الأقصى.

**مثال.** `100`

**القواعد.** اختياري. رقم.
