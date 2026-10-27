---
helpId: perf.paquarter3-review.edit
lang: ar
title: مراجعات الربع الثالث — إنشاء / تحرير
entity: HCMv3.PerformanceManagement.PAQuarter3Review
surfaces:
  - angular/src/app/admin/performance-management/pa-form-detail/pa-quarter3-reviews/create-or-edit-pa-quarter3-review-modal.component.html
audience: [admin]
permissions: []
---

## Overview

سطر مؤشر أداء واحد من مراجعة الربع الثالث في نموذج تقييم الأداء: المؤشر ووزنه والمستهدف منه، وتقديرا الموظف المُقيَّم والمُقيِّم، وتعليقاتهما. جزء من نموذج التقييم القديم القائم على الفترات (نموذج تقييم الأداء).

يُحفظ ضمن سجل التقييم، ولا تُحتسب حالياً أي درجة من هذه الأسطر. ولا يظهر المؤشر إلا مرة واحدة في النموذج.

## KpiInfo {#PAQuarter3Review.KpiInfoId}
<!-- alias: loc:KpiInfo, dom:PAQuarter3Review_KpiInfoId -->

**ما هو.** مؤشر الأداء.

**مثال.** رضا العملاء

**القواعد.** اختياري. يُختار من قائمة مؤشرات الأداء.

## Weight {#PAQuarter3Review.Weight}
<!-- alias: loc:Weight, dom:PAQuarter3Review_Weight -->

**ما هو.** حصة المؤشر من التقييم، نسبةً مئوية.

**مثال.** `25`

**القواعد.** اختياري. رقم.

## Target {#PAQuarter3Review.Target}
<!-- alias: loc:Target, dom:PAQuarter3Review_Target -->

**ما هو.** المستهدف المحدد للمؤشر.

**مثال.** `90% تسليم في الموعد`

**القواعد.** اختياري.

## AppraiseeRating {#PAQuarter3Review.AppraiseeRatingId}
<!-- alias: loc:AppraiseeRating, dom:PAQuarter3Review_AppraiseeRatingId -->

**ما هو.** تقدير الموظف لنفسه.

**مثال.** يلبي التوقعات

**القواعد.** اختياري. يُختار من قائمة تقديرات تقييم الأداء.

## Rating {#PAQuarter3Review.RatingId}
<!-- alias: loc:Rating, dom:PAQuarter3Review_RatingId -->

**ما هو.** تقدير المُقيِّم.

**مثال.** يتجاوز التوقعات

**القواعد.** اختياري. يُختار من قائمة تقديرات تقييم الأداء.

## AppraiseeComments {#PAQuarter3Review.AppraiseeComments}
<!-- alias: loc:AppraiseeComments, dom:PAQuarter3Review_AppraiseeComments -->

**ما هو.** تعليقات الموظف.

**القواعد.** اختياري. الحد الأقصى 500 حرف.

## AppraiserComments {#PAQuarter3Review.AppraiserComments}
<!-- alias: loc:AppraiserComments, dom:PAQuarter3Review_AppraiserComments -->

**ما هو.** تعليقات المُقيِّم.

**القواعد.** اختياري. الحد الأقصى 500 حرف.
