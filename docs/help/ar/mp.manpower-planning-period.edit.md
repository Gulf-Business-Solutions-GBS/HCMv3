---
helpId: mp.manpower-planning-period.edit
lang: ar
title: فترات تخطيط القوى العاملة — إنشاء / تحرير
entity: HCMv3.ManpowerPlanningManagement.ManpowerPlanningPeriod
surfaces:
  - angular/src/app/admin/manpower-planning-management/manpower-planning-periods/create-or-edit-manpower-planning-period-modal.component.html
audience: [admin]
permissions: [Pages.ManpowerPlanningManagement.ManpowerPlanningPeriods.Create]
---

## Overview

فترة تخطيط، عادةً سنة الميزانية. لا يمكن أن يكون تاريخ الانتهاء قبل تاريخ البدء. وتبدأ الوظائف المنشأة من خطة موافق عليها في تاريخ بدء الفترة.

## Name {#ManpowerPlanningPeriod.Name}
<!-- alias: loc:Name, dom:ManpowerPlanningPeriod_Name -->

**ما هو.** اسم الفترة، ويظهر للمعتمد.

**مثال.** ميزانية 2027

**القواعد.** إلزامي. الحد الأقصى 200 حرف.

## Active {#ManpowerPlanningPeriod.Active}
<!-- alias: loc:Active, dom:ManpowerPlanningPeriod_Active -->

**ما هو.** ما إذا كانت الفترة مفتوحة للتخطيط.

**لماذا يهم.** لا يُقرأ حالياً.

**القواعد.** نعم أو لا.
