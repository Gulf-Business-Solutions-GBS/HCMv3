---
helpId: perf.kpi-by-org-unit.edit
lang: ar
title: مؤشرات الأداء حسب وحدة الشركة — إنشاء / تحرير
entity: HCMv3.PerformanceManagement.KpiByOrgUnit
surfaces:
  - angular/src/app/admin/performance-management/kpi-by-org-units/create-or-edit-kpi-by-org-unit-modal.component.html
audience: [admin]
permissions: [Pages.PerformanceManagement.KpiByOrgUnits.Create]
---

## Overview

مجموعة مؤشرات أداء لوحدة تنظيمية، اعتباراً من تاريخ معيّن. ولكل وحدة مجموعة واحدة لكل تاريخ بداية.

تُحفظ نموذجاً: ولا تأخذ التقييمات مؤشراتها منها تلقائياً حالياً.

## OrganizationUnit {#KpiByOrgUnit.OrganizationUnitId}
<!-- alias: loc:OrganizationUnit, dom:KpiByOrgUnit_OrganizationUnitId -->

**ما هو.** الوحدة التنظيمية.

**مثال.** إدارة المالية

**القواعد.** إلزامي. يُختار من قائمة الوحدات التنظيمية.
