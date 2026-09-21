---
helpId: perf.paquarter1-review.edit
lang: ar
title: مراجعات الربع الأول — إنشاء / تحرير
entity: HCMv3.PerformanceManagement.PAQuarter1Review
surfaces:
  - angular/src/app/admin/performance-management/pa-form-detail/pa-quarter1-reviews/create-or-edit-pa-quarter1-review-modal.component.html
audience: [admin]
permissions: []
---

## Overview

سطر مؤشر أداء واحد من مراجعة الربع الأول في نموذج تقييم الأداء: المؤشر ووزنه والمستهدف منه، وتقديرا الموظف المُقيَّم والمُقيِّم، وتعليقاتهما. جزء من نموذج التقييم القديم القائم على الفترات (نموذج تقييم الأداء).

يُحفظ ضمن سجل التقييم، ولا تُحتسب حالياً أي درجة من هذه الأسطر. ولا يظهر المؤشر إلا مرة واحدة في النموذج.

## KpiInfo {#PAQuarter1Review.KpiInfoId}
<!-- alias: loc:KpiInfo, dom:PAQuarter1Review_KpiInfoId -->

**ما هو.** مؤشر الأداء.

**مثال.** رضا العملاء

**القواعد.** اختياري. يُختار من قائمة مؤشرات الأداء.

## Weight {#PAQuarter1Review.Weight}
<!-- alias: loc:Weight, dom:PAQuarter1Review_Weight -->

**ما هو.** حصة المؤشر من التقييم، نسبةً مئوية.

**مثال.** `25`

**القواعد.** اختياري. رقم.

## Target {#PAQuarter1Review.Target}
<!-- alias: loc:Target, dom:PAQuarter1Review_Target -->

**ما هو.** المستهدف المحدد للمؤشر.

**مثال.** `90% تسليم في الموعد`

**القواعد.** اختياري.

## AppraiseeRating {#PAQuarter1Review.AppraiseeRatingId}
<!-- alias: loc:AppraiseeRating, dom:PAQuarter1Review_AppraiseeRatingId -->

**ما هو.** تقدير الموظف لنفسه.

**مثال.** يلبي التوقعات

**القواعد.** اختياري. يُختار من قائمة تقديرات تقييم الأداء.

## Rating {#PAQuarter1Review.RatingId}
<!-- alias: loc:Rating, dom:PAQuarter1Review_RatingId -->

**ما هو.** تقدير المُقيِّم.

**مثال.** يتجاوز التوقعات

**القواعد.** اختياري. يُختار من قائمة تقديرات تقييم الأداء.

## AppraiseeComments {#PAQuarter1Review.AppraiseeComments}
<!-- alias: loc:AppraiseeComments, dom:PAQuarter1Review_AppraiseeComments -->

**ما هو.** تعليقات الموظف.

**القواعد.** اختياري. الحد الأقصى 500 حرف.

## AppraiserComments {#PAQuarter1Review.AppraiserComments}
<!-- alias: loc:AppraiserComments, dom:PAQuarter1Review_AppraiserComments -->

**ما هو.** تعليقات المُقيِّم.

**القواعد.** اختياري. الحد الأقصى 500 حرف.
