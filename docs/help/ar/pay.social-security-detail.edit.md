---
helpId: pay.social-security-detail.edit
lang: ar
title: تفاصيل التأمينات الاجتماعية — إنشاء / تحرير
entity: HCMv3.PayrollManagement.SocialSecurityDetail
surfaces:
  - angular/src/app/admin/payroll-management/social-security-details/create-or-edit-social-security-detail-modal.component.html
audience: [admin]
permissions: [Pages.PayrollManagement.SocialSecurityDetails.Create]
---

## Overview

شريحة اشتراك واحدة: نطاق الأجر الذي تغطيه والنسب المدفوعة عليه.

ويختار أجر الموظف للتأمينات الاجتماعية (مع العلاوات) الشريحة التي يقع ضمن نطاقها. والأجر الذي يتجاوز كل الشرائح يستخدم أعلى شريحة، بعد تخفيضه إلى الحد الأقصى لتلك الشريحة.

## SocialSecurityId {#SocialSecurityDetail.SocialSecurityId}
<!-- alias: loc:SocialSecurityId, dom:SocialSecurityDetail_SocialSecurityId -->

**ما هو.** إعداد التأمينات الاجتماعية الذي تتبعه هذه الشريحة.

**القواعد.** إلزامي. يُختار من قائمة التأمينات الاجتماعية.

## MinEarnings {#SocialSecurityDetail.MinEarnings}
<!-- alias: loc:MinEarnings, dom:SocialSecurityDetail_MinEarnings -->

**ما هو.** أدنى أجر في هذه الشريحة.

**مثال.** `1501`

**القواعد.** اختياري. رقم.

## MaxEarnings {#SocialSecurityDetail.MaxEarnings}
<!-- alias: loc:MaxEarnings, dom:SocialSecurityDetail_MaxEarnings -->

**ما هو.** أعلى أجر في هذه الشريحة، ولا يجوز أن يقل عن الحد الأدنى.

**مثال.** `2750`

**القواعد.** اختياري. رقم.

## EmployeeContribution {#SocialSecurityDetail.EmployeeContribution}
<!-- alias: loc:EmployeeContribution, dom:SocialSecurityDetail_EmployeeContribution -->

**ما هو.** نصيب الموظف، نسبةً مئوية من أجره.

**مثال.** `2.5`

**القواعد.** اختياري. رقم.

## EmployerContribution {#SocialSecurityDetail.EmployerContribution}
<!-- alias: loc:EmployerContribution, dom:SocialSecurityDetail_EmployerContribution -->

**ما هو.** نصيب صاحب العمل، نسبةً مئوية من أجر الموظف.

**مثال.** `11.5`

**القواعد.** اختياري. رقم.

:::admin
لا يُحتسب نصيب صاحب العمل إلا إذا لم يكن اشتراك الموظف في الشريحة صفراً. ففي الشريحة التي يدفع فيها صاحب العمل وحده، أعطِ اشتراك الموظف قيمة وإلا كان نصيب صاحب العمل صفراً -- وهذا سلوك النظام السابق وقد أُبقي عليه عمداً.
:::

## MinEarningsEmployeeContribution {#SocialSecurityDetail.MinEarningsEmployeeContribution}
<!-- alias: loc:MinEarningsEmployeeContribution, dom:SocialSecurityDetail_MinEarningsEmployeeContribution -->

**ما هو.** نسبة إضافية على الموظف تُحتسب على الأجر الذي يقل عن هذه الشريحة.

**مثال.** `2.5`

**لماذا يهم.** تُضاف إلى نصيب الموظف بمقدار هذه النسبة من الحد الأدنى للشريحة ناقص 1.

**القواعد.** اختياري. رقم.
