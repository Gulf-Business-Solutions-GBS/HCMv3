---
helpId: perf.papeer-review.edit
lang: ar
title: مراجعات الزملاء — إنشاء / تحرير
entity: HCMv3.PerformanceManagement.PAPeerReview
surfaces:
  - angular/src/app/admin/performance-management/pa-form-detail/pa-peer-reviews/create-or-edit-pa-peer-review-modal.component.html
audience: [admin]
permissions: []
---

## Overview

تقدير زميل لمؤشر أداء واحد في نموذج تقييم الأداء. جزء من نموذج التقييم القديم القائم على الفترات (نموذج تقييم الأداء). يُحفظ مع النموذج، ولا يقرؤه أي تقرير حالياً.

## Peer {#PAPeerReview.PeerId}
<!-- alias: loc:Peer, dom:PAPeerReview_PeerId -->

**ما هو.** الزميل الذي يعطي التقدير.

**القواعد.** اختياري. يُختار من قائمة الموظفين.

## KpiInfo {#PAPeerReview.KpiInfoId}
<!-- alias: loc:KpiInfo, dom:PAPeerReview_KpiInfoId -->

**ما هو.** المؤشر المُقيَّم.

**القواعد.** اختياري. يُختار من قائمة مؤشرات الأداء.

## Rating {#PAPeerReview.RatingId}
<!-- alias: loc:Rating, dom:PAPeerReview_RatingId -->

**ما هو.** التقدير الممنوح.

**القواعد.** اختياري. يُختار من قائمة تقديرات تقييم الأداء.
