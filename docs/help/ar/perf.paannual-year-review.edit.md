---
helpId: perf.paannual-year-review.edit
lang: ar
title: المراجعات السنوية — إنشاء / تحرير
entity: HCMv3.PerformanceManagement.PAAnnualYearReview
surfaces:
  - angular/src/app/admin/performance-management/pa-form-detail/pa-annual-year-reviews/create-or-edit-pa-annual-year-review-modal.component.html
audience: [admin]
permissions: []
---

## Overview

سطر مؤشر أداء واحد من مراجعة نهاية السنة في نموذج تقييم الأداء: المؤشر ووزنه والمستهدف منه، وتقديرا الموظف المُقيَّم والمُقيِّم، وتعليقاتهما. جزء من نموذج التقييم القديم القائم على الفترات (نموذج تقييم الأداء).

يُحفظ ضمن سجل التقييم، ولا تُحتسب حالياً أي درجة من هذه الأسطر. ولا يظهر المؤشر إلا مرة واحدة في النموذج.

## KpiInfo {#PAAnnualYearReview.KpiInfoId}
<!-- alias: loc:KpiInfo, dom:PAAnnualYearReview_KpiInfoId -->

**ما هو.** مؤشر الأداء.

**مثال.** رضا العملاء

**القواعد.** اختياري. يُختار من قائمة مؤشرات الأداء.

## Weight {#PAAnnualYearReview.Weight}
<!-- alias: loc:Weight, dom:PAAnnualYearReview_Weight -->

**ما هو.** حصة المؤشر من التقييم، نسبةً مئوية.

**مثال.** `25`

**القواعد.** اختياري. رقم.

## Target {#PAAnnualYearReview.Target}
<!-- alias: loc:Target, dom:PAAnnualYearReview_Target -->

**ما هو.** المستهدف المحدد للمؤشر.

**مثال.** `90% تسليم في الموعد`

**القواعد.** اختياري.

## AppraiseeRating {#PAAnnualYearReview.AppraiseeRatingId}
<!-- alias: loc:AppraiseeRating, dom:PAAnnualYearReview_AppraiseeRatingId -->

**ما هو.** تقدير الموظف لنفسه.

**مثال.** يلبي التوقعات

**القواعد.** اختياري. يُختار من قائمة تقديرات تقييم الأداء.

## Rating {#PAAnnualYearReview.RatingId}
<!-- alias: loc:Rating, dom:PAAnnualYearReview_RatingId -->

**ما هو.** تقدير المُقيِّم.

**مثال.** يتجاوز التوقعات

**القواعد.** اختياري. يُختار من قائمة تقديرات تقييم الأداء.

## AppraiseeComments {#PAAnnualYearReview.AppraiseeComments}
<!-- alias: loc:AppraiseeComments, dom:PAAnnualYearReview_AppraiseeComments -->

**ما هو.** تعليقات الموظف.

**القواعد.** اختياري. الحد الأقصى 500 حرف.

## AppraiserComments {#PAAnnualYearReview.AppraiserComments}
<!-- alias: loc:AppraiserComments, dom:PAAnnualYearReview_AppraiserComments -->

**ما هو.** تعليقات المُقيِّم.

**القواعد.** اختياري. الحد الأقصى 500 حرف.
