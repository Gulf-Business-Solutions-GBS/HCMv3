---
helpId: tm.work-location-duty-period.edit
lang: ar
title: فترات دوام مكان العمل — إنشاء / تحرير
entity: HCMv3.TimeManagement.WorkLocationDutyPeriod
surfaces:
  - angular/src/app/admin/time-management/work-location-duty-periods/create-or-edit-work-location-duty-period-modal.component.html
audience: [admin]
permissions: [Pages.TimeManagement.WorkLocationDutyPeriods.Create]
---

## Overview

فترة الدوام المستخدمة في موقع عمل، اعتباراً من تاريخ معيّن.

لا يُقرأ حالياً: تأتي دوامات الموظفين من فترات دوامهم ومجموعات المناوبة، ولا شيء يقرأ هذه القائمة.

## WorkLocation {#WorkLocationDutyPeriod.WorkLocationId}
<!-- alias: loc:WorkLocation, dom:WorkLocationDutyPeriod_WorkLocationId -->

**ما هو.** موقع العمل.

**مثال.** مستودع الشويخ

**القواعد.** إلزامي. يُختار من قائمة أماكن العمل.

## DutyPeriod {#WorkLocationDutyPeriod.DutyPeriodId}
<!-- alias: loc:DutyPeriod, dom:WorkLocationDutyPeriod_DutyPeriodId -->

**ما هو.** فترة الدوام المستخدمة فيه.

**مثال.** مناوبات المستودع

**القواعد.** إلزامي. يُختار من قائمة فترات الدوام.
