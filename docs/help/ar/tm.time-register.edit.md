---
helpId: tm.time-register.edit
lang: ar
title: سجلات تسجيل الوقت — إنشاء / تحرير
entity: HCMv3.TimeManagement.TimeRegister
surfaces:
  - angular/src/app/admin/time-management/time-registers/create-or-edit-time-register-modal.component.html
audience: [admin]
permissions: [Pages.TimeManagement.TimeRegisters.Create]
---

## Overview

يوم موظف واحد كما احتسبه محرك الوقت: الدوام والسياسة المطبقان، والأوقات المخططة والفعلية، والبصمات المستخدمة، وما يُعدّ به اليوم -- عمل أو غياب أو إجازة أو عطلة أو راحة أسبوعية -- مع دقائق التأخير والانصراف المبكر والعمل الإضافي. ويأخذ نظام الرواتب أعداد الأيام من هذه الأسطر.

ويكتب محرك الوقت هذه الأسطر ويعيد كتابتها عند تشغيله، فلا تعدّل سطراً يدوياً إلا لتصحيح يوم، وتوقّع أن يعيد تشغيل لاحق احتسابه.

وإذا كان إعداد الوقت *EnableTimeRegisterApproval* مفعّلاً يستخدم نظام الرواتب الأرقام **المعتمدة**، ويرفض التشغيل للموظف الذي له أي يوم غير معتمد في الفترة.

## Employee {#TimeRegister.EmployeeId}
<!-- alias: loc:Employee, dom:TimeRegister_EmployeeId -->

**ما هو.** الموظف.

**القواعد.** إلزامي. يُختار من قائمة الموظفين.

## DayDate {#TimeRegister.DayDate}
<!-- alias: loc:DayDate, dom:TimeRegister_DayDate -->

**ما هو.** اليوم.

**مثال.** `2026-04-14`

**القواعد.** إلزامي. تاريخ.

## DutyPeriod {#TimeRegister.DutyPeriodId}
<!-- alias: loc:DutyPeriod, dom:TimeRegister_DutyPeriodId -->

**ما هو.** فترة الدوام المطبقة.

**القواعد.** اختياري. يُختار من قائمة فترات الدوام.

## Duty {#TimeRegister.DutyId}
<!-- alias: loc:Duty, dom:TimeRegister_DutyId -->

**ما هو.** الدوام المطبق.

**القواعد.** اختياري. يُختار من قائمة الدوامات.

## TimePolicy {#TimeRegister.TimePolicyId}
<!-- alias: loc:TimePolicy, dom:TimeRegister_TimePolicyId -->

**ما هو.** سياسة الوقت المطبقة.

**القواعد.** اختياري. يُختار من قائمة سياسات الوقت.

## ShiftStartTime {#TimeRegister.ShiftStartTime1}
<!-- alias: loc:ShiftStartTime, dom:TimeRegister_ShiftStartTime1 -->

**ما هو.** بداية المناوبة المخططة.

**مثال.** `07:30`

**القواعد.** اختياري. تاريخ.

## ShiftEndTime {#TimeRegister.ShiftEndTime1}
<!-- alias: loc:ShiftEndTime, dom:TimeRegister_ShiftEndTime1 -->

**ما هو.** نهاية المناوبة المخططة.

**مثال.** `14:30`

**القواعد.** اختياري. تاريخ.

## BreakStartTime {#TimeRegister.BreakStartTime}
<!-- alias: loc:BreakStartTime, dom:TimeRegister_BreakStartTime -->

**ما هو.** بداية الاستراحة المخططة.

**القواعد.** اختياري. تاريخ.

## BreakEndTime {#TimeRegister.BreakEndTime}
<!-- alias: loc:BreakEndTime, dom:TimeRegister_BreakEndTime -->

**ما هو.** نهاية الاستراحة المخططة.

**القواعد.** اختياري. تاريخ.

## ActualIn {#TimeRegister.ActualIn}
<!-- alias: loc:ActualIn, dom:TimeRegister_ActualIn -->

**ما هو.** بصمة الدخول المستخدمة.

**مثال.** `07:42`

**القواعد.** اختياري. تاريخ.

## ActualOut {#TimeRegister.ActualOut}
<!-- alias: loc:ActualOut, dom:TimeRegister_ActualOut -->

**ما هو.** بصمة الخروج المستخدمة.

**مثال.** `14:35`

**القواعد.** اختياري. تاريخ.

## BreakOut {#TimeRegister.BreakOut}
<!-- alias: loc:BreakOut, dom:TimeRegister_BreakOut -->

**ما هو.** بصمة الخروج إلى الاستراحة المستخدمة.

**القواعد.** اختياري. تاريخ.

## BreakIn {#TimeRegister.BreakIn}
<!-- alias: loc:BreakIn, dom:TimeRegister_BreakIn -->

**ما هو.** بصمة العودة من الاستراحة المستخدمة.

**القواعد.** اختياري. تاريخ.

## ActualStartTime {#TimeRegister.ActualStartTime1}
<!-- alias: loc:ActualStartTime, dom:TimeRegister_ActualStartTime1 -->

**ما هو.** بداية وقت العمل المحتسب -- الأكثر تأخراً من بداية المناوبة وبصمة الدخول.

**القواعد.** اختياري. تاريخ.

## ActualEndTime {#TimeRegister.ActualEndTime1}
<!-- alias: loc:ActualEndTime, dom:TimeRegister_ActualEndTime1 -->

**ما هو.** نهاية وقت العمل المحتسب.

**القواعد.** اختياري. تاريخ.

## ActualBreakStartTime {#TimeRegister.ActualBreakStartTime}
<!-- alias: loc:ActualBreakStartTime, dom:TimeRegister_ActualBreakStartTime -->

**ما هو.** بداية الاستراحة المحتسبة.

**القواعد.** اختياري. تاريخ.

## ActualBreakEndTime {#TimeRegister.ActualBreakEndTime}
<!-- alias: loc:ActualBreakEndTime, dom:TimeRegister_ActualBreakEndTime -->

**ما هو.** نهاية الاستراحة المحتسبة.

**القواعد.** اختياري. تاريخ.

## PlannedWorkingMins {#TimeRegister.PlannedWorkingMins}
<!-- alias: loc:PlannedWorkingMins, dom:TimeRegister_PlannedWorkingMins -->

**ما هو.** الدقائق التي كان مخططاً أن يعملها الموظف.

**مثال.** `420`

**القواعد.** اختياري. رقم.

## ActualWorkingMins {#TimeRegister.ActualWorkingMins}
<!-- alias: loc:ActualWorkingMins, dom:TimeRegister_ActualWorkingMins -->

**ما هو.** الدقائق المعمولة ضمن المناوبة.

**مثال.** `408`

**القواعد.** اختياري. رقم.

## TotalWorkingMins {#TimeRegister.TotalWorkingMins}
<!-- alias: loc:TotalWorkingMins, dom:TimeRegister_TotalWorkingMins -->

**ما هو.** كل الدقائق بين الدخول والخروج، ناقص الاستراحة، مضافاً إليها دقائق المواظبة.

**مثال.** `413`

**القواعد.** اختياري. رقم.

## ActualAbsenceMins {#TimeRegister.ActualAbsenceMins}
<!-- alias: loc:ActualAbsenceMins, dom:TimeRegister_ActualAbsenceMins -->

**ما هو.** الدقائق المخططة التي غاب عنها الموظف.

**مثال.** `12`

**القواعد.** اختياري. رقم.

## PlannedWorkingDay {#TimeRegister.PlannedWorkingDay}
<!-- alias: loc:PlannedWorkingDay, dom:TimeRegister_PlannedWorkingDay -->

**ما هو.** 1 إذا كان اليوم يوم عمل مخططاً.

**مثال.** `1`

**القواعد.** اختياري. رقم.

## ActualWorkingDay {#TimeRegister.ActualWorkingDay}
<!-- alias: loc:ActualWorkingDay, dom:TimeRegister_ActualWorkingDay -->

**ما هو.** الجزء المعمول فعلاً من اليوم.

**مثال.** `1`

**القواعد.** اختياري. رقم.

## AttendanceDay {#TimeRegister.AttendanceDay}
<!-- alias: loc:AttendanceDay, dom:TimeRegister_AttendanceDay -->

**ما هو.** 1 إذا سُجّل اليوم مواظبة (مهمة عمل، تدريب).

**القواعد.** اختياري. رقم.

## AttendanceType {#TimeRegister.AttendanceTypeId}
<!-- alias: loc:AttendanceType, dom:TimeRegister_AttendanceTypeId -->

**ما هو.** نوع المواظبة، في يوم المواظبة.

**القواعد.** اختياري. يُختار من قائمة.

## AttendanceMinutes {#TimeRegister.AttendanceMinutes}
<!-- alias: loc:AttendanceMinutes, dom:TimeRegister_AttendanceMinutes -->

**ما هو.** الدقائق المحتسبة للمواظبة.

**القواعد.** اختياري. رقم.

## AbsentDay {#TimeRegister.AbsentDay}
<!-- alias: loc:AbsentDay, dom:TimeRegister_AbsentDay -->

**ما هو.** 1 إذا كان الموظف غائباً.

**القواعد.** اختياري. رقم.

## ActiveDay {#TimeRegister.ActiveDay}
<!-- alias: loc:ActiveDay, dom:TimeRegister_ActiveDay -->

**ما هو.** 1 إذا كان الموظف على رأس العمل في ذلك اليوم.

**مثال.** `1`

**القواعد.** اختياري. رقم.

## InactiveDay {#TimeRegister.InactiveDay}
<!-- alias: loc:InactiveDay, dom:TimeRegister_InactiveDay -->

**ما هو.** 1 إذا لم يكن الموظف قد التحق بعد أو كان قد ترك العمل.

**القواعد.** اختياري. رقم.

## LeaveDay {#TimeRegister.LeaveDay}
<!-- alias: loc:LeaveDay, dom:TimeRegister_LeaveDay -->

**ما هو.** الجزء من اليوم الذي كان في إجازة.

**القواعد.** اختياري. رقم.

## LeaveType {#TimeRegister.LeaveTypeId}
<!-- alias: loc:LeaveType, dom:TimeRegister_LeaveTypeId -->

**ما هو.** نوع الإجازة، في يوم الإجازة.

**القواعد.** اختياري. يُختار من قائمة.

## LeaveMinutes {#TimeRegister.LeaveMinutes}
<!-- alias: loc:LeaveMinutes, dom:TimeRegister_LeaveMinutes -->

**ما هو.** دقائق الإجازة بالساعات.

**القواعد.** اختياري. رقم.

## AnnualLeaveDay {#TimeRegister.AnnualLeaveDay}
<!-- alias: loc:AnnualLeaveDay, dom:TimeRegister_AnnualLeaveDay -->

**ما هو.** 1 إذا كان اليوم إجازة سنوية.

**القواعد.** اختياري. رقم.

## UnpaidLeaveDay {#TimeRegister.UnpaidLeaveDay}
<!-- alias: loc:UnpaidLeaveDay, dom:TimeRegister_UnpaidLeaveDay -->

**ما هو.** 1 إذا كان اليوم إجازة غير مدفوعة.

**القواعد.** اختياري. رقم.

## SickLeavePaidDay {#TimeRegister.SickLeavePaidDay}
<!-- alias: loc:SickLeavePaidDay, dom:TimeRegister_SickLeavePaidDay -->

**ما هو.** الجزء المدفوع من يوم الإجازة المرضية.

**القواعد.** اختياري. رقم.

## SickLeaveUnpaidDay {#TimeRegister.SickLeaveUnpaidDay}
<!-- alias: loc:SickLeaveUnpaidDay, dom:TimeRegister_SickLeaveUnpaidDay -->

**ما هو.** الجزء غير المدفوع من يوم الإجازة المرضية.

**القواعد.** اختياري. رقم.

## PublicHoliday {#TimeRegister.PublicHoliday}
<!-- alias: loc:PublicHoliday, dom:TimeRegister_PublicHoliday -->

**ما هو.** 1 إذا كان اليوم عطلة رسمية في تقويم الموظف.

**القواعد.** اختياري. رقم.

## WeeklyOff {#TimeRegister.WeeklyOff}
<!-- alias: loc:WeeklyOff, dom:TimeRegister_WeeklyOff -->

**ما هو.** 1 إذا كان اليوم راحة أسبوعية.

**القواعد.** اختياري. رقم.

## RestDay {#TimeRegister.RestDay}
<!-- alias: loc:RestDay, dom:TimeRegister_RestDay -->

**ما هو.** 1 إذا كان اليوم يوم راحة.

**القواعد.** اختياري. رقم.

## AnnualLeaveAccrualPerDay {#TimeRegister.AnnualLeaveAccrualPerDay}
<!-- alias: loc:AnnualLeaveAccrualPerDay, dom:TimeRegister_AnnualLeaveAccrualPerDay -->

**ما هو.** الإجازة السنوية المكتسبة في هذا اليوم.

**مثال.** `0.0822`

**القواعد.** اختياري. رقم.

## TicketAccrualPerDay {#TimeRegister.TicketAccrualPerDay}
<!-- alias: loc:TicketAccrualPerDay, dom:TimeRegister_TicketAccrualPerDay -->

**ما هو.** استحقاق تذاكر الطيران المكتسب في هذا اليوم.

**مثال.** `0.0027`

**القواعد.** اختياري. رقم.

## AirTicketPaidDay {#TimeRegister.AirTicketPaidDay}
<!-- alias: loc:AirTicketPaidDay, dom:TimeRegister_AirTicketPaidDay -->

**ما هو.** 1 إذا صُرفت تذكرة طيران في هذا اليوم.

**القواعد.** اختياري. رقم.

## LeaveAdjDay {#TimeRegister.LeaveAdjDay}
<!-- alias: loc:LeaveAdjDay, dom:TimeRegister_LeaveAdjDay -->

**ما هو.** تعديل إجازة محتسب في هذا اليوم.

**القواعد.** اختياري. رقم.

## IncludeInLeavePay {#TimeRegister.IncludeInLeavePay}
<!-- alias: loc:IncludeInLeavePay, dom:TimeRegister_IncludeInLeavePay -->

**ما هو.** الجزء من اليوم المصروف عبر أجر الإجازة لا الراتب الأساسي.

**القواعد.** اختياري. رقم.

## OvertimeMinutes {#TimeRegister.OvertimeMinutes}
<!-- alias: loc:OvertimeMinutes, dom:TimeRegister_OvertimeMinutes -->

**ما هو.** دقائق العمل الإضافي المحتسبة، بعد الحد الأدنى في السياسة.

**مثال.** `45`

**القواعد.** اختياري. رقم.

## OvertimeType {#TimeRegister.OvertimeTypeId}
<!-- alias: loc:OvertimeType, dom:TimeRegister_OvertimeTypeId -->

**ما هو.** نوع العمل الإضافي الذي يُسجَّل تحته العمل الإضافي.

**القواعد.** اختياري. يُختار من قائمة أنواع العمل الإضافي.

## LateIn {#TimeRegister.LateIn}
<!-- alias: loc:LateIn, dom:TimeRegister_LateIn -->

**ما هو.** دقائق التأخير في بداية المناوبة.

**مثال.** `12`

**القواعد.** اختياري. رقم.

## DeductionLateIn {#TimeRegister.DeductionLateIn}
<!-- alias: loc:DeductionLateIn, dom:TimeRegister_DeductionLateIn -->

**ما هو.** دقائق التأخير المطلوب خصمها، بعد السماح.

**مثال.** `2`

**القواعد.** اختياري. رقم.

## EarlyOut {#TimeRegister.EarlyOut}
<!-- alias: loc:EarlyOut, dom:TimeRegister_EarlyOut -->

**ما هو.** دقائق الانصراف المبكر في نهاية المناوبة.

**القواعد.** اختياري. رقم.

## DeductionEarlyOut {#TimeRegister.DeductionEarlyOut}
<!-- alias: loc:DeductionEarlyOut, dom:TimeRegister_DeductionEarlyOut -->

**ما هو.** دقائق الانصراف المبكر المطلوب خصمها، بعد السماح.

**القواعد.** اختياري. رقم.

## BreakEarlyOut {#TimeRegister.BreakEarlyOut}
<!-- alias: loc:BreakEarlyOut, dom:TimeRegister_BreakEarlyOut -->

**ما هو.** دقائق الخروج المبكر إلى الاستراحة.

**القواعد.** اختياري. رقم.

## BreakLateIn {#TimeRegister.BreakLateIn}
<!-- alias: loc:BreakLateIn, dom:TimeRegister_BreakLateIn -->

**ما هو.** دقائق التأخر في العودة من الاستراحة.

**القواعد.** اختياري. رقم.

## BreakDeductionEarlyOut {#TimeRegister.BreakDeductionEarlyOut}
<!-- alias: loc:BreakDeductionEarlyOut, dom:TimeRegister_BreakDeductionEarlyOut -->

**ما هو.** دقائق الخروج المبكر إلى الاستراحة المطلوب خصمها.

**القواعد.** اختياري. رقم.

## BreakDeductionLateIn {#TimeRegister.BreakDeductionLateIn}
<!-- alias: loc:BreakDeductionLateIn, dom:TimeRegister_BreakDeductionLateIn -->

**ما هو.** دقائق التأخر في العودة من الاستراحة المطلوب خصمها.

**القواعد.** اختياري. رقم.

## DedLateInEarlyOutMins {#TimeRegister.DedLateInEarlyOutMins}
<!-- alias: loc:DedLateInEarlyOutMins, dom:TimeRegister_DedLateInEarlyOutMins -->

**ما هو.** كل دقائق التأخير والانصراف المبكر المطلوب خصمها لليوم.

**القواعد.** اختياري. رقم.

## DedLateInEarlyOutDay {#TimeRegister.DedLateInEarlyOutDay}
<!-- alias: loc:DedLateInEarlyOutDay, dom:TimeRegister_DedLateInEarlyOutDay -->

**ما هو.** تلك الدقائق جزءاً من يوم.

**القواعد.** اختياري. رقم.

## DedAfterMonthlyTol {#TimeRegister.DedAfterMonthlyTol}
<!-- alias: loc:DedAfterMonthlyTol, dom:TimeRegister_DedAfterMonthlyTol -->

**ما هو.** دقائق التأخير والانصراف المبكر للشهر التي تتجاوز السماح الشهري، محمولة على يوم واحد من الشهر.

**القواعد.** اختياري. رقم.

## Status {#TimeRegister.Status}
<!-- alias: loc:Status, dom:TimeRegister_Status -->

**ما هو.** ما إذا كان اليوم مفتوحاً أو معتمداً أو معاد فتحه.

**مثال.** مفتوح

**لماذا يهم.** عند تفعيل اعتماد سجل الوقت يحتاج نظام الرواتب إلى أن تكون كل أيام الفترة معتمدة.

**القواعد.** إلزامي. إحدى القيم: Opened، معتمد، Reopened.

## ApprovedActualWorkingDay {#TimeRegister.ApprovedActualWorkingDay}
<!-- alias: loc:ApprovedActualWorkingDay, dom:TimeRegister_ApprovedActualWorkingDay -->

**ما هو.** الرقم المعتمد لليوم المعمول، يستخدمه نظام الرواتب عند تفعيل الاعتماد.

**القواعد.** اختياري. رقم.

## ApprovedAbsentDay {#TimeRegister.ApprovedAbsentDay}
<!-- alias: loc:ApprovedAbsentDay, dom:TimeRegister_ApprovedAbsentDay -->

**ما هو.** الرقم المعتمد للغياب، يستخدمه نظام الرواتب عند تفعيل الاعتماد.

**القواعد.** اختياري. رقم.

## ApprovedDedLateInEarlyOutMins {#TimeRegister.ApprovedDedLateInEarlyOutMins}
<!-- alias: loc:ApprovedDedLateInEarlyOutMins, dom:TimeRegister_ApprovedDedLateInEarlyOutMins -->

**ما هو.** دقائق التأخير والانصراف المبكر المعتمدة للخصم.

**القواعد.** اختياري. رقم.

## Remarks {#TimeRegister.Remarks}
<!-- alias: loc:Remarks, dom:TimeRegister_Remarks -->

**ما هو.** ملاحظة على اليوم.

**القواعد.** اختياري. الحد الأقصى 1000 حرف.
