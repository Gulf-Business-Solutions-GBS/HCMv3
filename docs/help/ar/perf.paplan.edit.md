---
helpId: perf.paplan.edit
lang: ar
title: خطط تقييم الأداء — إنشاء / تحرير
entity: HCMv3.PerformanceManagement.PAPlan
surfaces:
  - angular/src/app/admin/performance-management/pa-form-detail/pa-plans/create-or-edit-pa-plan-modal.component.html
audience: [admin]
permissions: []
---

## Overview

سطر مؤشر أداء واحد من الخطة في نموذج تقييم الأداء: المؤشر ووزنه والمستهدف منه، وتقديرا الموظف المُقيَّم والمُقيِّم، وتعليقاتهما. جزء من نموذج التقييم القديم القائم على الفترات (نموذج تقييم الأداء).

يُحفظ ضمن سجل التقييم، ولا تُحتسب حالياً أي درجة من هذه الأسطر. ولا يظهر المؤشر إلا مرة واحدة في النموذج.

## KpiInfo {#PAPlan.KpiInfoId}
<!-- alias: loc:KpiInfo, dom:PAPlan_KpiInfoId -->

**ما هو.** مؤشر الأداء.

**مثال.** رضا العملاء

**القواعد.** اختياري. يُختار من قائمة مؤشرات الأداء.

## Weight {#PAPlan.Weight}
<!-- alias: loc:Weight, dom:PAPlan_Weight -->

**ما هو.** حصة المؤشر من التقييم، نسبةً مئوية.

**مثال.** `25`

**القواعد.** اختياري. رقم.

## Target {#PAPlan.Target}
<!-- alias: loc:Target, dom:PAPlan_Target -->

**ما هو.** المستهدف المحدد للمؤشر.

**مثال.** `90% تسليم في الموعد`

**القواعد.** اختياري.

## AppraiseeRating {#PAPlan.AppraiseeRatingId}
<!-- alias: loc:AppraiseeRating, dom:PAPlan_AppraiseeRatingId -->

**ما هو.** تقدير الموظف لنفسه.

**مثال.** يلبي التوقعات

**القواعد.** اختياري. يُختار من قائمة تقديرات تقييم الأداء.

## Rating {#PAPlan.RatingId}
<!-- alias: loc:Rating, dom:PAPlan_RatingId -->

**ما هو.** تقدير المُقيِّم.

**مثال.** يتجاوز التوقعات

**القواعد.** اختياري. يُختار من قائمة تقديرات تقييم الأداء.

## AppraiseeComments {#PAPlan.AppraiseeComments}
<!-- alias: loc:AppraiseeComments, dom:PAPlan_AppraiseeComments -->

**ما هو.** تعليقات الموظف.

**القواعد.** اختياري. الحد الأقصى 500 حرف.

## AppraiserComments {#PAPlan.AppraiserComments}
<!-- alias: loc:AppraiserComments, dom:PAPlan_AppraiserComments -->

**ما هو.** تعليقات المُقيِّم.

**القواعد.** اختياري. الحد الأقصى 500 حرف.
