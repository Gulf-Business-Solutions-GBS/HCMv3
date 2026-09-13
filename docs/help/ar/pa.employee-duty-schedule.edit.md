---
helpId: pa.employee-duty-schedule.edit
lang: ar
title: جداول دوام الموظف — إنشاء / تحرير
entity: HCMv3.TimeManagement.EmployeeDutySchedule
surfaces:
  - angular/src/app/admin/personnel-administration/employee-detail/duty-schedules/create-or-edit-employee-duty-schedule-modal.component.html
audience: [admin]
permissions: [Pages.TimeManagement.EmployeeDutySchedules.Create]
---

## Overview

يخصص الموظف لجدول دوام مناوب خلال فترة. ولا يجوز أن تتداخل جداول الموظف نفسه.

يُسجَّل للرجوع إليه؛ إذ يأخذ محرك الوقت والرواتب دوام الموظف من فترات الدوام والدوام البديل، ولا يقرآن هذا التخصيص حالياً.

## RosterDutySchedule {#EmployeeDutySchedule.RosterDutyScheduleId}
<!-- alias: loc:RosterDutySchedule, dom:EmployeeDutySchedule_RosterDutyScheduleId -->

**ما هو.** جدول الدوام المناوب.

**مثال.** مناوبة الأمن الليلية

**القواعد.** إلزامي. يُختار من قائمة جداول ورديات العمل.
