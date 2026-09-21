---
helpId: perf.kpi-by-job-title.edit
lang: ar
title: مؤشرات الأداء حسب المسمى الوظيفي — إنشاء / تحرير
entity: HCMv3.PerformanceManagement.KpiByJobTitle
surfaces:
  - angular/src/app/admin/performance-management/kpi-by-job-titles/create-or-edit-kpi-by-job-title-modal.component.html
audience: [admin]
permissions: [Pages.PerformanceManagement.KpiByJobTitles.Create]
---

## Overview

مجموعة مؤشرات أداء لمسمى وظيفي، اعتباراً من تاريخ معيّن -- المؤشرات والأوزان التي تنطبق على هذا الدور. ولكل مسمى وظيفي مجموعة واحدة لكل تاريخ بداية.

تُحفظ نموذجاً: ولا تأخذ التقييمات مؤشراتها منها تلقائياً حالياً.

## JobTitle {#KpiByJobTitle.JobTitleId}
<!-- alias: loc:JobTitle, dom:KpiByJobTitle_JobTitleId -->

**ما هو.** المسمى الوظيفي.

**مثال.** محاسب

**القواعد.** إلزامي. يُختار من قائمة المسميات الوظيفية.
