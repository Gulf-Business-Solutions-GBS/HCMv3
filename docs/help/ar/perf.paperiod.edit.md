---
helpId: perf.paperiod.edit
lang: ar
title: فترات تقييم الأداء — إنشاء / تحرير
entity: HCMv3.PerformanceManagement.PAPeriod
surfaces:
  - angular/src/app/admin/performance-management/pa-periods/create-or-edit-pa-period-modal.component.html
audience: [admin]
permissions: [Pages.PerformanceManagement.PAPeriods.Create]
---

## Overview

فترة تقييم لنموذج تقييم الأداء القديم -- مثل "السنة المالية 2026". ولكل موظف نموذج تقييم واحد لكل فترة. ويمكن تصفية تقريري تقييم الأداء وملخص التقييم بها.

## Name {#PAPeriod.Name}
<!-- alias: loc:Name, dom:PAPeriod_Name -->

**ما هو.** اسم الفترة، ويجب ألا يتكرر.

**مثال.** السنة المالية 2026

**القواعد.** إلزامي. الحد الأقصى 256 حرفاً.

## Closed {#PAPeriod.Closed}
<!-- alias: loc:Closed, dom:PAPeriod_Closed -->

**ما هو.** ما إذا كانت الفترة مقفلة.

**لماذا يهم.** لا يُقرأ حالياً.

**القواعد.** نعم أو لا.
