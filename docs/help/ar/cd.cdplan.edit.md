---
helpId: cd.cdplan.edit
lang: ar
title: خطط تطوير الكفاءات — إنشاء / تحرير
entity: HCMv3.CompetencyDevelopment.CDPlan
surfaces:
  - angular/src/app/admin/competency-development/cd-form-detail/cd-plans/create-or-edit-cd-plan-modal.component.html
audience: [admin]
permissions: []
---

## Overview

كفاءة واحدة في خطة الموظف للفترة. لا تُدرج الكفاءة إلا مرة واحدة في هذا الجزء من النموذج. وتُسجَّل الأوزان كما أُدخلت، ولا تُجمع أو تُتحقق منها هنا.

## Competency {#CDPlan.CompetencyId}
<!-- alias: loc:Competency, dom:CDPlan_CompetencyId -->

**ما هو.** الكفاءة.

**مثال.** العمل الجماعي

**القواعد.** اختياري. يُختار من قائمة الكفاءات.

## Weight {#CDPlan.Weight}
<!-- alias: loc:Weight, dom:CDPlan_Weight -->

**ما هو.** مقدار أهمية الكفاءة، كنسبة مئوية.

**مثال.** 20

**القواعد.** اختياري. رقم.

## Target {#CDPlan.Target}
<!-- alias: loc:Target, dom:CDPlan_Target -->

**ما هو.** ما يُتوقع أن يحققه الموظف.

**القواعد.** اختياري. الحد الأقصى 512 حرفاً.

## EmployeeComments {#CDPlan.EmployeeComments}
<!-- alias: loc:EmployeeComments, dom:CDPlan_EmployeeComments -->

**ما هو.** تعليقات الموظف.

**القواعد.** اختياري. الحد الأقصى 500 حرف.

## ManagerComments {#CDPlan.ManagerComments}
<!-- alias: loc:ManagerComments, dom:CDPlan_ManagerComments -->

**ما هو.** تعليقات المدير.

**القواعد.** اختياري. الحد الأقصى 500 حرف.
