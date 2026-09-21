---
helpId: cd.cdperiod.edit
lang: ar
title: فترات تطوير الكفاءات — إنشاء / تحرير
entity: HCMv3.CompetencyDevelopment.CDPeriod
surfaces:
  - angular/src/app/admin/competency-development/cd-periods/create-or-edit-cd-period-modal.component.html
audience: [admin]
permissions: [Pages.CompetencyDevelopment.CDPeriods.Create]
---

## Overview

دورة مراجعة، عادةً سنة، تنتمي إليها نماذج تطوير الكفاءات للموظفين.

## Name {#CDPeriod.Name}
<!-- alias: loc:Name, dom:CDPeriod_Name -->

**ما هو.** اسم الفترة، ويجب أن يكون فريداً.

**مثال.** 2026

**القواعد.** إلزامي. الحد الأقصى 256 حرفاً.

## Closed {#CDPeriod.Closed}
<!-- alias: loc:Closed, dom:CDPeriod_Closed -->

**ما هو.** ما إذا كانت الفترة منتهية.

**لماذا يهم.** يظهر في القائمة فقط: يمكن إضافة النماذج في فترة مغلقة وتعديلها.

**القواعد.** نعم أو لا.
