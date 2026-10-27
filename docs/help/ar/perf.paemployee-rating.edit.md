---
helpId: perf.paemployee-rating.edit
lang: ar
title: تقديرات موظفي تقييم الأداء — إنشاء / تحرير
entity: HCMv3.PerformanceManagement.PAEmployeeRating
surfaces:
  - angular/src/app/admin/performance-management/pa-employee-detail/pa-employee-ratings/create-or-edit-pa-employee-rating-modal.component.html
audience: [admin]
permissions: []
---

## Overview

تقدير مراجع واحد لمؤشر أداء واحد في التقييم. ويعمل تقريرا بطاقة الأداء وتقييم المدير من هذه التقديرات -- وتستخدم بطاقة الأداء تقديرات المديرين فقط إذا وُجد أي منها في التقرير، وإلا فتقديرات كل المراجعين.

## PAEmployeeReviewer {#PAEmployeeRating.PAEmployeeReviewerId}
<!-- alias: loc:PAEmployeeReviewer, dom:PAEmployeeRating_PAEmployeeReviewerId -->

**ما هو.** المراجع الذي يعطي التقدير.

**القواعد.** اختياري. يُختار من قائمة مراجعو تقييم الأداء.

## PAEmployeeDetail {#PAEmployeeRating.PAEmployeeDetailId}
<!-- alias: loc:PAEmployeeDetail, dom:PAEmployeeRating_PAEmployeeDetailId -->

**ما هو.** مؤشر الأداء الجاري تقديره.

**القواعد.** اختياري. يُختار من قائمة تفاصيل موظفي تقييم الأداء.

## Marks {#PAEmployeeRating.Marks}
<!-- alias: loc:Marks, dom:PAEmployeeRating_Marks -->

**ما هو.** الدرجات الممنوحة، وتظهر في تقرير تقييم المدير.

**مثال.** `4`

**القواعد.** اختياري. رقم.

## Rating {#PAEmployeeRating.RatingId}
<!-- alias: loc:Rating, dom:PAEmployeeRating_RatingId -->

**ما هو.** التقدير الممنوح.

**مثال.** يتجاوز التوقعات

**القواعد.** اختياري. يُختار من قائمة تقديرات تقييم الأداء.
