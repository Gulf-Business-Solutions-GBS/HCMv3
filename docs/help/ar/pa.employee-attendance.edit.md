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

**لماذا يهم.** يحدد طريقة احتساب مجموع أيام الحضور عند حفظ السجل.

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

## TotalAttendDays {#EmployeeAttendance.TotalAttendDays}
<!-- alias: loc:TotalAttendDays, dom:EmployeeAttendance_TotalAttendDays -->

**ما هو.** عدد الأيام التي يُحتسب بها الحضور، ويُحسب عند حفظ السجل. ويبقى فارغاً حتى يُحفظ السجل أول مرة.

**مثال.** 0.5

**لماذا يهم.** في وحدة الفترة مع وقت بداية ووقت نهاية، تُقسم الساعات بين الوقتين (وتحل محل الساعات المُدخلة) على ساعات الدوام المخططة للموظف في ذلك اليوم، أو على ساعات العمل الافتراضية في إعدادات الرواتب إن لم يكن للدوام ساعات، وتبقى النتيجة بين 0 و1. وفي وحدة الفترة دون أوقات تكون عدد الأيام التقويمية من تاريخ البداية إلى تاريخ النهاية. وفي وحدة الساعات تكون الساعات المُدخلة مقسومة بالطريقة نفسها، دون حد أعلى. أما وحدة اليوم فلا يُحتسب لها شيء. وإذا كان نوع المواظبة مؤشَّراً عليه بخيار له تراكم حصة، أُضيفت هذه الأيام إلى الحصة المتراكمة لنوع الإجازة المرتبط به، وتُسترد عند تعديل السجل أو حذفه. ويقرؤها كذلك تقرير مخصص الإجازات.

**القواعد.** اختياري. رقم.

:::admin
يتعذر الحفظ إذا لم يوجد دوام للموظف في تاريخ البداية. والحصة المستخدمة هي التي تغطي تاريخ التحاق الموظف، كما في النظام السابق، وإن لم توجد أُنشئت.
:::
