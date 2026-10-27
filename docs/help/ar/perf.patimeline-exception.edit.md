---
helpId: perf.patimeline-exception.edit
lang: ar
title: استثناءات الجدول الزمني لتقييم الأداء — إنشاء / تحرير
entity: HCMv3.PerformanceManagement.PATimelineException
surfaces:
  - angular/src/app/admin/performance-management/pa-timeline-exceptions/create-or-edit-pa-timeline-exception-modal.component.html
audience: [admin]
permissions: [Pages.PerformanceManagement.PATimelineExceptions.Create]
---

## Overview

تمديد موعد مرحلة واحدة لموظف واحد.

لا يُقرأ حالياً: لا تمنع الجداول الزمنية التقديم المتأخر، فلا يغيّر التمديد شيئاً.

## PAPeriod {#PATimelineException.PAPeriodId}
<!-- alias: loc:PAPeriod, dom:PATimelineException_PAPeriodId -->

**ما هو.** فترة التقييم.

**القواعد.** إلزامي. يُختار من قائمة فترات تقييم الأداء.

## Employee {#PATimelineException.EmployeeId}
<!-- alias: loc:Employee, dom:PATimelineException_EmployeeId -->

**ما هو.** الموظف الممنوح التمديد.

**القواعد.** إلزامي. يُختار من قائمة الموظفين.

## ActivityType {#PATimelineException.ActivityType}
<!-- alias: loc:ActivityType, dom:PATimelineException_ActivityType -->

**ما هو.** المرحلة الممدَّدة.

**القواعد.** إلزامي. إحدى القيم: Plan، MidYearReview، AnnualReview، Quarter1Review، Quarter3Review.

## ExtensionDate {#PATimelineException.ExtensionDate}
<!-- alias: loc:ExtensionDate, dom:PATimelineException_ExtensionDate -->

**ما هو.** الموعد الجديد.

**مثال.** `2026-07-15`

**القواعد.** إلزامي. تاريخ.
