---
helpId: perf.pamid-year-review.edit
lang: ar
title: مراجعات منتصف السنة — إنشاء / تحرير
entity: HCMv3.PerformanceManagement.PAMidYearReview
surfaces:
  - angular/src/app/admin/performance-management/pa-form-detail/pa-mid-year-reviews/create-or-edit-pa-mid-year-review-modal.component.html
audience: [admin]
permissions: []
---

## Overview

سطر مؤشر أداء واحد من مراجعة منتصف السنة في نموذج تقييم الأداء: المؤشر ووزنه والمستهدف منه، وتقديرا الموظف المُقيَّم والمُقيِّم، وتعليقاتهما. جزء من نموذج التقييم القديم القائم على الفترات (نموذج تقييم الأداء).

يُحفظ ضمن سجل التقييم، ولا تُحتسب حالياً أي درجة من هذه الأسطر. ولا يظهر المؤشر إلا مرة واحدة في النموذج.

## KpiInfo {#PAMidYearReview.KpiInfoId}
<!-- alias: loc:KpiInfo, dom:PAMidYearReview_KpiInfoId -->

**ما هو.** مؤشر الأداء.

**مثال.** رضا العملاء

**القواعد.** اختياري. يُختار من قائمة مؤشرات الأداء.

## Weight {#PAMidYearReview.Weight}
<!-- alias: loc:Weight, dom:PAMidYearReview_Weight -->

**ما هو.** حصة المؤشر من التقييم، نسبةً مئوية.

**مثال.** `25`

**القواعد.** اختياري. رقم.

## Target {#PAMidYearReview.Target}
<!-- alias: loc:Target, dom:PAMidYearReview_Target -->

**ما هو.** المستهدف المحدد للمؤشر.

**مثال.** `90% تسليم في الموعد`

**القواعد.** اختياري.

## AppraiseeRating {#PAMidYearReview.AppraiseeRatingId}
<!-- alias: loc:AppraiseeRating, dom:PAMidYearReview_AppraiseeRatingId -->

**ما هو.** تقدير الموظف لنفسه.

**مثال.** يلبي التوقعات

**القواعد.** اختياري. يُختار من قائمة تقديرات تقييم الأداء.

## Rating {#PAMidYearReview.RatingId}
<!-- alias: loc:Rating, dom:PAMidYearReview_RatingId -->

**ما هو.** تقدير المُقيِّم.

**مثال.** يتجاوز التوقعات

**القواعد.** اختياري. يُختار من قائمة تقديرات تقييم الأداء.

## AppraiseeComments {#PAMidYearReview.AppraiseeComments}
<!-- alias: loc:AppraiseeComments, dom:PAMidYearReview_AppraiseeComments -->

**ما هو.** تعليقات الموظف.

**القواعد.** اختياري. الحد الأقصى 500 حرف.

## AppraiserComments {#PAMidYearReview.AppraiserComments}
<!-- alias: loc:AppraiserComments, dom:PAMidYearReview_AppraiserComments -->

**ما هو.** تعليقات المُقيِّم.

**القواعد.** اختياري. الحد الأقصى 500 حرف.
