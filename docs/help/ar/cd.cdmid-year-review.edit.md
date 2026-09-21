---
helpId: cd.cdmid-year-review.edit
lang: ar
title: مراجعات منتصف السنة لتطوير الكفاءات — إنشاء / تحرير
entity: HCMv3.CompetencyDevelopment.CDMidYearReview
surfaces:
  - angular/src/app/admin/competency-development/cd-form-detail/cd-mid-year-reviews/create-or-edit-cd-mid-year-review-modal.component.html
audience: [admin]
permissions: [Pages.CompetencyDevelopment.CDMidYearReviews.Create]
---

## Overview

كفاءة واحدة تُراجع في منتصف السنة. لا تُدرج الكفاءة إلا مرة واحدة في هذا الجزء من النموذج. وتُسجَّل الأوزان كما أُدخلت، ولا تُجمع أو تُتحقق منها هنا.

## Competency {#CDMidYearReview.CompetencyId}
<!-- alias: loc:Competency, dom:CDMidYearReview_CompetencyId -->

**ما هو.** الكفاءة.

**مثال.** العمل الجماعي

**القواعد.** اختياري. يُختار من قائمة الكفاءات.

## Weight {#CDMidYearReview.Weight}
<!-- alias: loc:Weight, dom:CDMidYearReview_Weight -->

**ما هو.** مقدار أهمية الكفاءة، كنسبة مئوية.

**مثال.** 20

**القواعد.** اختياري. رقم.

## Target {#CDMidYearReview.Target}
<!-- alias: loc:Target, dom:CDMidYearReview_Target -->

**ما هو.** ما يُتوقع أن يحققه الموظف.

**القواعد.** اختياري. الحد الأقصى 512 حرفاً.

## EmployeeComments {#CDMidYearReview.EmployeeComments}
<!-- alias: loc:EmployeeComments, dom:CDMidYearReview_EmployeeComments -->

**ما هو.** تعليقات الموظف.

**القواعد.** اختياري. الحد الأقصى 500 حرف.

## ManagerComments {#CDMidYearReview.ManagerComments}
<!-- alias: loc:ManagerComments, dom:CDMidYearReview_ManagerComments -->

**ما هو.** تعليقات المدير.

**القواعد.** اختياري. الحد الأقصى 500 حرف.
