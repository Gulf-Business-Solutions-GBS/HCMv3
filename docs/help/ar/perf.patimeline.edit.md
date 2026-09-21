---
helpId: perf.patimeline.edit
lang: ar
title: الجداول الزمنية لتقييم الأداء — إنشاء / تحرير
entity: HCMv3.PerformanceManagement.PATimeline
surfaces:
  - angular/src/app/admin/performance-management/pa-timelines/create-or-edit-pa-timeline-modal.component.html
audience: [admin]
permissions: [Pages.PerformanceManagement.PATimelines.Create]
---

## Overview

تواريخ مرحلة واحدة من تقييم الفترة -- التخطيط أو مراجعة منتصف السنة أو مراجعة نهاية السنة. ولا تظهر المرحلة إلا مرة واحدة في الفترة.

ويستخدم تقريرا تقييم الأداء وملخص التقييم هذه التواريخ لإيجاد الفترات الواقعة ضمن نطاق تاريخ التقرير. ولا تمنع التواريخ أحداً من التقديم خارجها.

## PAPeriod {#PATimeline.PAPeriodId}
<!-- alias: loc:PAPeriod, dom:PATimeline_PAPeriodId -->

**ما هو.** فترة التقييم.

**مثال.** السنة المالية 2026

**القواعد.** إلزامي. يُختار من قائمة فترات تقييم الأداء.

## ActivityType {#PATimeline.ActivityType}
<!-- alias: loc:ActivityType, dom:PATimeline_ActivityType -->

**ما هو.** المرحلة التي تخصها التواريخ.

**مثال.** مراجعة منتصف السنة

**القواعد.** إلزامي. إحدى القيم: Plan، MidYearReview، AnnualReview، Quarter1Review، Quarter3Review.
