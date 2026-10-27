---
helpId: trn.training-attendance.edit
lang: ar
title: حضور التدريب — إنشاء / تحرير
entity: HCMv3.TrainingManagement.TrainingAttendance
surfaces:
  - angular/src/app/admin/training-management/training-attendances/create-or-edit-training-attendance-modal.component.html
audience: [admin]
permissions: [Pages.TrainingManagement.TrainingAttendances.Create]
---

## Overview

ما إذا كان الموظف قد حضر مرة انعقاد تدريبية في يوم واحد. يجب أن يقع اليوم ضمن تواريخ الانعقاد، ولكل موظف سجل واحد لكل انعقاد في كل يوم. ولا يؤثر في الوقت أو الرواتب.

## TrainingScheduleId {#TrainingAttendance.TrainingScheduleId}
<!-- alias: loc:TrainingScheduleId, dom:TrainingAttendance_TrainingScheduleId -->

**ما هو.** مرة انعقاد الدورة.

**القواعد.** إلزامي. يُختار من قائمة جداول التدريب.

## EmployeeId {#TrainingAttendance.EmployeeId}
<!-- alias: loc:EmployeeId, dom:TrainingAttendance_EmployeeId -->

**ما هو.** الموظف.

**القواعد.** إلزامي. يُختار من قائمة الموظفين.

## DayDate {#TrainingAttendance.DayDate}
<!-- alias: loc:DayDate, dom:TrainingAttendance_DayDate -->

**ما هو.** اليوم.

**القواعد.** إلزامي. تاريخ.

## Attendance {#TrainingAttendance.Attendance}
<!-- alias: loc:Attendance, dom:TrainingAttendance_Attendance -->

**ما هو.** حاضر أو غائب.

**مثال.** حاضر

**القواعد.** إلزامي. إحدى القيم: حاضر، غائب.
