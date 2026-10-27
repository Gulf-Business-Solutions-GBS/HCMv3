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

وتُعرض السجلات نفسها لجميع الموظفين من إدارة الوقت ← جدول الدوام، ويمكن البحث فيها برمز الموظف أو اسمه وبرمز الجدول أو اسمه. وعند إضافة سجل من هناك يُطلب اختيار الموظف، أما من سجل الموظف فيكون الموظف محدداً مسبقاً.

## Employee {#EmployeeDutySchedule.EmployeeId}
<!-- alias: loc:Employee, dom:EmployeeDutySchedule_EmployeeId -->

**ما هو.** الموظف الذي يُخصَّص له الجدول. ولا يظهر إلا عند إضافة سجل من شاشة جدول الدوام لجميع الموظفين.

**مثال.** 1024 - فاطمة الصباح

**القواعد.** إلزامي. يُختار من قائمة الموظفين.

## RosterDutySchedule {#EmployeeDutySchedule.RosterDutyScheduleId}
<!-- alias: loc:RosterDutySchedule, dom:EmployeeDutySchedule_RosterDutyScheduleId -->

**ما هو.** جدول الدوام المناوب.

**مثال.** مناوبة الأمن الليلية

**القواعد.** إلزامي. يُختار من قائمة جداول ورديات العمل.
