---
helpId: perf.kpi-by-org-unit-detail.edit
lang: ar
title: تفاصيل مؤشرات الأداء حسب وحدة الشركة — إنشاء / تحرير
entity: HCMv3.PerformanceManagement.KpiByOrgUnitDetail
surfaces:
  - angular/src/app/admin/performance-management/kpi-by-org-unit-details/create-or-edit-kpi-by-org-unit-detail-modal.component.html
audience: [admin]
permissions: []
---

## Overview

مؤشر أداء واحد في مجموعة الوحدة التنظيمية. ولا يظهر المؤشر إلا مرة واحدة، ويجب ألا يتجاوز مجموع الأوزان في المجموعة 100.

## KpiByOrgUnit {#KpiByOrgUnitDetail.KpiByOrgUnitId}
<!-- alias: loc:KpiByOrgUnit, dom:KpiByOrgUnitDetail_KpiByOrgUnitId -->

**ما هو.** مجموعة مؤشرات الوحدة.

**القواعد.** إلزامي. يُختار من قائمة مؤشرات الأداء حسب وحدة الشركة.

## KpiInfo {#KpiByOrgUnitDetail.KpiInfoId}
<!-- alias: loc:KpiInfo, dom:KpiByOrgUnitDetail_KpiInfoId -->

**ما هو.** مؤشر الأداء.

**القواعد.** إلزامي. يُختار من قائمة مؤشرات الأداء.

## Weight {#KpiByOrgUnitDetail.Weight}
<!-- alias: loc:Weight, dom:KpiByOrgUnitDetail_Weight -->

**ما هو.** وزن المؤشر، نسبةً مئوية.

**مثال.** `20`

**القواعد.** إلزامي. رقم.

## Target {#KpiByOrgUnitDetail.Target}
<!-- alias: loc:Target, dom:KpiByOrgUnitDetail_Target -->

**ما هو.** المستهدف للمؤشر.

**القواعد.** اختياري.
