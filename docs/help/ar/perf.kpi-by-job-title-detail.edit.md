---
helpId: perf.kpi-by-job-title-detail.edit
lang: ar
title: تفاصيل مؤشرات الأداء حسب المسمى الوظيفي — إنشاء / تحرير
entity: HCMv3.PerformanceManagement.KpiByJobTitleDetail
surfaces:
  - angular/src/app/admin/performance-management/kpi-by-job-title-details/create-or-edit-kpi-by-job-title-detail-modal.component.html
audience: [admin]
permissions: []
---

## Overview

مؤشر أداء واحد في مجموعة المسمى الوظيفي. ولا يظهر المؤشر إلا مرة واحدة، ويجب ألا يتجاوز مجموع الأوزان في المجموعة 100.

## KpiByJobTitle {#KpiByJobTitleDetail.KpiByJobTitleId}
<!-- alias: loc:KpiByJobTitle, dom:KpiByJobTitleDetail_KpiByJobTitleId -->

**ما هو.** مجموعة مؤشرات المسمى الوظيفي.

**القواعد.** إلزامي. يُختار من قائمة مؤشرات الأداء حسب المسمى الوظيفي.

## KpiInfo {#KpiByJobTitleDetail.KpiInfoId}
<!-- alias: loc:KpiInfo, dom:KpiByJobTitleDetail_KpiInfoId -->

**ما هو.** مؤشر الأداء.

**القواعد.** إلزامي. يُختار من قائمة مؤشرات الأداء.

## Weight {#KpiByJobTitleDetail.Weight}
<!-- alias: loc:Weight, dom:KpiByJobTitleDetail_Weight -->

**ما هو.** وزن المؤشر، نسبةً مئوية.

**مثال.** `20`

**القواعد.** إلزامي. رقم.

## Target {#KpiByJobTitleDetail.Target}
<!-- alias: loc:Target, dom:KpiByJobTitleDetail_Target -->

**ما هو.** المستهدف للمؤشر.

**القواعد.** اختياري.
