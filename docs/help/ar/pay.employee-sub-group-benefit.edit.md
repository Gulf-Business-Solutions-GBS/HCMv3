---
helpId: pay.employee-sub-group-benefit.edit
lang: ar
title: مزايا المجموعة الفرعية للموظفين — إنشاء / تحرير
entity: HCMv3.PayrollManagement.EmployeeSubGroupBenefit
surfaces:
  - angular/src/app/admin/payroll-management/employee-sub-group-detail/benefits/create-or-edit-employee-sub-group-benefit-modal.component.html
audience: [admin]
permissions: [Pages.PayrollManagement.EmployeeSubGroupBenefits.Create]
---

## Overview

نوع مزايا تستحقه الدرجة، مع حد أقصى للمبلغ.

لا تُقرأ حالياً: لا يستخدم نظام الرواتب هذه القائمة، وهي محفوظة للرجوع إليها فقط.

## BenefitType {#EmployeeSubGroupBenefit.BenefitTypeId}
<!-- alias: loc:BenefitType, dom:EmployeeSubGroupBenefit_BenefitTypeId -->

**ما هو.** نوع المزايا.

**مثال.** بدل تعليم

**القواعد.** اختياري. يُختار من قائمة أنواع المزايا.

## MaxAmount {#EmployeeSubGroupBenefit.MaxAmount}
<!-- alias: loc:MaxAmount, dom:EmployeeSubGroupBenefit_MaxAmount -->

**ما هو.** أقصى ما يمكن أن تصرفه المزايا.

**مثال.** `2000`

**القواعد.** اختياري. رقم.
