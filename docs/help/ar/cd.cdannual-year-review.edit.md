---
helpId: cd.cdannual-year-review.edit
lang: ar
title: المراجعات السنوية لتطوير الكفاءات — إنشاء / تحرير
entity: HCMv3.CompetencyDevelopment.CDAnnualYearReview
surfaces:
  - angular/src/app/admin/competency-development/cd-form-detail/cd-annual-year-reviews/create-or-edit-cd-annual-year-review-modal.component.html
audience: [admin]
permissions: [Pages.CompetencyDevelopment.CDAnnualYearReviews.Create]
---

## Overview

كفاءة واحدة تُراجع وتُقيَّم في نهاية السنة. لا تُدرج الكفاءة إلا مرة واحدة في هذا الجزء من النموذج. وتُسجَّل الأوزان كما أُدخلت، ولا تُجمع أو تُتحقق منها هنا.

## Competency {#CDAnnualYearReview.CompetencyId}
<!-- alias: loc:Competency, dom:CDAnnualYearReview_CompetencyId -->

**ما هو.** الكفاءة.

**مثال.** العمل الجماعي

**القواعد.** اختياري. يُختار من قائمة الكفاءات.

## Weight {#CDAnnualYearReview.Weight}
<!-- alias: loc:Weight, dom:CDAnnualYearReview_Weight -->

**ما هو.** مقدار أهمية الكفاءة، كنسبة مئوية.

**مثال.** 20

**القواعد.** اختياري. رقم.

## Target {#CDAnnualYearReview.Target}
<!-- alias: loc:Target, dom:CDAnnualYearReview_Target -->

**ما هو.** ما يُتوقع أن يحققه الموظف.

**القواعد.** اختياري. الحد الأقصى 512 حرفاً.

## Rating {#CDAnnualYearReview.CDRatingId}
<!-- alias: loc:Rating, dom:CDAnnualYearReview_CDRatingId -->

**ما هو.** التقدير المعطى للسنة.

**مثال.** يلبي التوقعات

**القواعد.** اختياري. يُختار من قائمة تقديرات تطوير الكفاءات.

## EmployeeComments {#CDAnnualYearReview.EmployeeComments}
<!-- alias: loc:EmployeeComments, dom:CDAnnualYearReview_EmployeeComments -->

**ما هو.** تعليقات الموظف.

**القواعد.** اختياري. الحد الأقصى 500 حرف.

## ManagerComments {#CDAnnualYearReview.ManagerComments}
<!-- alias: loc:ManagerComments, dom:CDAnnualYearReview_ManagerComments -->

**ما هو.** تعليقات المدير.

**القواعد.** اختياري. الحد الأقصى 500 حرف.
