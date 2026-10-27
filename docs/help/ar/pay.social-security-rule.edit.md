---
helpId: pay.social-security-rule.edit
lang: ar
title: قواعد التأمينات الاجتماعية — إنشاء / تحرير
entity: HCMv3.PayrollManagement.SocialSecurityRule
surfaces:
  - angular/src/app/admin/payroll-management/social-security-rules/create-or-edit-social-security-rule-modal.component.html
audience: [admin]
permissions: [Pages.PayrollManagement.SocialSecurityRules.Create]
---

## Overview

علاوة التأمينات الاجتماعية -- نوع الأجر `5` -- بحسب المؤهل والجنس والحالة الاجتماعية وعدد العائلين.

لا تنطبق التأمينات الاجتماعية على الموظف إلا إذا كان له سجل تأمينات اجتماعية مؤشَّر فيه على *اشتراك التأمينات الاجتماعية* للشهر، وكان هناك إعداد تأمينات اجتماعية لجنسيته سارياً في تاريخ الدفع. ثم يأخذ نظام الرواتب القاعدة السارية المطابقة تماماً لمؤهل الموظف وجنسه وحالته الاجتماعية، ذات أعلى **عائل** لا يتجاوز عدد عائلي الموظف، ويصرف مبلغها.

## SocialSecurityId {#SocialSecurityRule.SocialSecurityId}
<!-- alias: loc:SocialSecurityId, dom:SocialSecurityRule_SocialSecurityId -->

**ما هو.** إعداد التأمينات الاجتماعية الذي تتبعه هذه القاعدة.

**القواعد.** إلزامي. يُختار من قائمة التأمينات الاجتماعية.

## QualificationId {#SocialSecurityRule.QualificationId}
<!-- alias: loc:QualificationId, dom:SocialSecurityRule_QualificationId -->

**ما هو.** المؤهل الذي تخصه القاعدة.

**مثال.** جامعي

**القواعد.** إلزامي. يُختار من قائمة المؤهلات.

## Gender {#SocialSecurityRule.Gender}
<!-- alias: loc:Gender, dom:SocialSecurityRule_Gender -->

**ما هو.** الجنس الذي تخصه القاعدة.

**مثال.** ذكر

**القواعد.** إلزامي. إحدى القيم: ذكر، أنثى.

## MaritalStatusId {#SocialSecurityRule.MaritalStatusId}
<!-- alias: loc:MaritalStatusId, dom:SocialSecurityRule_MaritalStatusId -->

**ما هو.** الحالة الاجتماعية التي تخصها القاعدة.

**مثال.** متزوج

**لماذا يهم.** إذا تُركت فارغة لا تطابق القاعدة إلا الموظفين الذين لم تُسجَّل حالتهم الاجتماعية.

**القواعد.** اختياري. يُختار من قائمة الحالات الاجتماعية.

## Dependant {#SocialSecurityRule.Dependant}
<!-- alias: loc:Dependant, dom:SocialSecurityRule_Dependant -->

**ما هو.** عدد العائلين الذي تبدأ منه القاعدة.

**مثال.** `2`

**لماذا يهم.** يُعدّ كل عائلي الموظف، أياً كان نوعهم.

**القواعد.** اختياري. رقم صحيح.

## Amount {#SocialSecurityRule.Amount}
<!-- alias: loc:Amount, dom:SocialSecurityRule_Amount -->

**ما هو.** العلاوة المصروفة.

**مثال.** `120`

**القواعد.** اختياري. رقم.

## Disability {#SocialSecurityRule.Disability}
<!-- alias: loc:Disability, dom:SocialSecurityRule_Disability -->

**ما هو.** ما إذا كانت القاعدة للموظفين ذوي الإعاقة.

**لماذا يهم.** لا يُقرأ حالياً.

**القواعد.** نعم أو لا.
