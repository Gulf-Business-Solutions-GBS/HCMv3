---
helpId: perf.kpi-info.edit
lang: ar
title: مؤشرات الأداء — إنشاء / تحرير
entity: HCMv3.PerformanceManagement.KpiInfo
surfaces:
  - angular/src/app/admin/performance-management/kpi-infos/create-or-edit-kpi-info-modal.component.html
audience: [admin]
permissions: [Pages.PerformanceManagement.KpiInfos.Create]
---

## Overview

مؤشر أداء رئيسي تُقيِّمه التقييمات.

## KpiCategory {#KpiInfo.KpiCategoryId}
<!-- alias: loc:KpiCategory, dom:KpiInfo_KpiCategoryId -->

**ما هو.** الفئة التي يتبعها المؤشر.

**مثال.** العملاء

**القواعد.** إلزامي. يُختار من قائمة فئات مؤشرات الأداء.

## Active {#KpiInfo.Active}
<!-- alias: loc:Active, dom:KpiInfo_Active -->

**ما هو.** ما إذا كان المؤشر مستخدماً.

**لماذا يهم.** لا يُقرأ حالياً.

**القواعد.** نعم أو لا.

## Competency {#KpiInfo.CompetencyId}
<!-- alias: loc:Competency, dom:KpiInfo_CompetencyId -->

**ما هو.** الكفاءة التي يرتبط بها المؤشر، من قائمة الكفاءات.

**مثال.** التركيز على العملاء

**لماذا يهم.** تُسجَّل للرجوع إليها، ولا يستخدمها شيء آخر.

**القواعد.** اختياري. يُختار من قائمة الكفاءات.
