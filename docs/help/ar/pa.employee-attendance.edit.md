---
helpId: pa.employee-attendance.edit
lang: ar
title: حضور الموظف — إنشاء / تحرير
entity: HCMv3.PersonnelAdministration.EmployeeAttendance
surfaces:
  - angular/src/app/admin/personnel-administration/employee-detail/attendances/create-or-edit-employee-attendance-modal.component.html
audience: [admin]
permissions: [Pages.PersonnelAdministration.EmployeeAttendances.Create]
---

## Overview

وقت يقضيه الموظف بعيداً عن دوامه المعتاد مع احتسابه حضوراً -- كالتدريب أو الزيارة الميدانية أو المهمة الرسمية. ولا يجوز أن تتداخل سجلات حضور الموظف نفسه.

يقرأ محرك الوقت هذه السجلات عند بناء كل يوم. وتصرف الرواتب العادية الحضور من تلك الأيام، أما الرواتب خارج الدورة فتقرأ السجلات مباشرة.

## AttendanceTypeId {#EmployeeAttendance.AttendanceTypeId}
<!-- alias: loc:AttendanceTypeId, dom:EmployeeAttendance_AttendanceTypeId -->

**ما هو.** نوع الحضور.

**مثال.** مهمة رسمية

**لماذا يهم.** تصرف الرواتب الحضور حسب نوعه.

**القواعد.** إلزامي. يُختار من قائمة أنواع المواظبة.

:::admin
تُدار من إدارة الرواتب ← أنواع المواظبة.
:::

## AttendanceUnit {#EmployeeAttendance.AttendanceUnit}
<!-- alias: loc:AttendanceUnit, dom:EmployeeAttendance_AttendanceUnit -->

**ما هو.** هل يُسجَّل الحضور فترةً أم بالأيام أم بالساعات.

**مثال.** ساعات

**لماذا يهم.** يُسجَّل فقط، ولا يقرؤه أي احتساب حالياً.

**القواعد.** إلزامي. إحدى القيم: فترة، اليوم، ساعات.

## AttendanceHours {#EmployeeAttendance.AttendanceHours}
<!-- alias: loc:AttendanceHours, dom:EmployeeAttendance_AttendanceHours -->

**ما هو.** عدد الساعات، للحضور المسجل بالساعات.

**مثال.** 3

**لماذا يهم.** يظهر في تقرير الحضور وفي إشعار الاعتماد.

**القواعد.** اختياري. رقم.

## StartTime {#EmployeeAttendance.StartTime}
<!-- alias: loc:StartTime, dom:EmployeeAttendance_StartTime -->

**ما هو.** وقت بدء الحضور.

**مثال.** 09:00

**لماذا يهم.** يستخدم محرك الوقت وقتي البداية والنهاية لاحتساب دقائق الحضور في اليوم.

**القواعد.** اختياري. تاريخ.

## EndTime {#EmployeeAttendance.EndTime}
<!-- alias: loc:EndTime, dom:EmployeeAttendance_EndTime -->

**ما هو.** وقت انتهاء الحضور.

**مثال.** 12:00

**القواعد.** اختياري. تاريخ.
