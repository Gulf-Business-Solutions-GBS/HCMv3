---
helpId: perf.paemployee-detail.edit
lang: ar
title: تفاصيل موظفي تقييم الأداء — إنشاء / تحرير
entity: HCMv3.PerformanceManagement.PAEmployeeDetail
surfaces:
  - angular/src/app/admin/performance-management/pa-employee-detail/pa-employee-details/create-or-edit-pa-employee-detail-modal.component.html
audience: [admin]
permissions: []
---

## Overview

مؤشر أداء واحد في التقييم، مع وزنه والمستهدف منه. ويقيّم المراجعون التقييم مؤشراً مؤشراً.

ويحتسب تقرير بطاقة أداء الموظف درجة كل مؤشر بوزنه مضروباً في متوسط التقدير، نسبةً إلى أعلى رمز تقدير، ثم يجمعها. ولا يظهر المؤشر إلا مرة واحدة في التقييم.

## KpiInfo {#PAEmployeeDetail.KpiInfoId}
<!-- alias: loc:KpiInfo, dom:PAEmployeeDetail_KpiInfoId -->

**ما هو.** مؤشر الأداء.

**مثال.** رضا العملاء

**القواعد.** اختياري. يُختار من قائمة مؤشرات الأداء.

## Weight {#PAEmployeeDetail.Weight}
<!-- alias: loc:Weight, dom:PAEmployeeDetail_Weight -->

**ما هو.** حصة المؤشر من التقييم، نسبةً مئوية.

**مثال.** `25`

**القواعد.** اختياري. رقم.

## Target {#PAEmployeeDetail.Target}
<!-- alias: loc:Target, dom:PAEmployeeDetail_Target -->

**ما هو.** المستهدف المحدد للمؤشر.

**القواعد.** اختياري.
