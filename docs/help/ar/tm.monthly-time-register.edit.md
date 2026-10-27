---
helpId: tm.monthly-time-register.edit
lang: ar
title: سجلات الوقت الشهرية — إنشاء / تحرير
entity: HCMv3.TimeManagement.MonthlyTimeRegister
surfaces:
  - angular/src/app/admin/time-management/monthly-time-registers/create-or-edit-monthly-time-register-modal.component.html
audience: [admin]
permissions: [Pages.TimeManagement.MonthlyTimeRegisters.Create]
---

## Overview

مجاميع شهر من سجل وقت الموظف.

لا تُكتب حالياً: لا توجد عملية تملأ هذه السجلات ولا شيء يقرؤها. ويعمل نظام الرواتب والتقارير من سجل الوقت اليومي.

## Employee {#MonthlyTimeRegister.EmployeeId}
<!-- alias: loc:Employee, dom:MonthlyTimeRegister_EmployeeId -->

**ما هو.** الموظف.

**القواعد.** إلزامي. يُختار من قائمة الموظفين.

## Month {#MonthlyTimeRegister.Month}
<!-- alias: loc:Month, dom:MonthlyTimeRegister_Month -->

**ما هو.** الشهر الذي تخصه المجاميع.

**مثال.** `2026-04-01`

**القواعد.** إلزامي. تاريخ.

## AnnualLeaveDays {#MonthlyTimeRegister.AnnualLeaveDays}
<!-- alias: loc:AnnualLeaveDays, dom:MonthlyTimeRegister_AnnualLeaveDays -->

**ما هو.** أيام الإجازة السنوية المأخوذة.

**القواعد.** اختياري. رقم.

## UnpaidLeaveDays {#MonthlyTimeRegister.UnpaidLeaveDays}
<!-- alias: loc:UnpaidLeaveDays, dom:MonthlyTimeRegister_UnpaidLeaveDays -->

**ما هو.** أيام الإجازة غير المدفوعة المأخوذة.

**القواعد.** اختياري. رقم.

## SickLeavePaidDays {#MonthlyTimeRegister.SickLeavePaidDays}
<!-- alias: loc:SickLeavePaidDays, dom:MonthlyTimeRegister_SickLeavePaidDays -->

**ما هو.** أيام الإجازة المرضية المدفوعة.

**القواعد.** اختياري. رقم.

## SickLeaveUnpaidDays {#MonthlyTimeRegister.SickLeaveUnpaidDays}
<!-- alias: loc:SickLeaveUnpaidDays, dom:MonthlyTimeRegister_SickLeaveUnpaidDays -->

**ما هو.** أيام الإجازة المرضية غير المدفوعة.

**القواعد.** اختياري. رقم.

## GratuityDays {#MonthlyTimeRegister.GratuityDays}
<!-- alias: loc:GratuityDays, dom:MonthlyTimeRegister_GratuityDays -->

**ما هو.** أيام مكافأة نهاية الخدمة المستحقة.

**القواعد.** اختياري. رقم.

## GratuityPaidDays {#MonthlyTimeRegister.GratuityPaidDays}
<!-- alias: loc:GratuityPaidDays, dom:MonthlyTimeRegister_GratuityPaidDays -->

**ما هو.** أيام مكافأة نهاية الخدمة المصروفة.

**القواعد.** اختياري. رقم.

## PublicHolidays {#MonthlyTimeRegister.PublicHolidays}
<!-- alias: loc:PublicHolidays, dom:MonthlyTimeRegister_PublicHolidays -->

**ما هو.** العطلات الرسمية.

**القواعد.** اختياري. رقم.

## WeeklyOffs {#MonthlyTimeRegister.WeeklyOffs}
<!-- alias: loc:WeeklyOffs, dom:MonthlyTimeRegister_WeeklyOffs -->

**ما هو.** أيام الراحة الأسبوعية.

**القواعد.** اختياري. رقم.

## RestDays {#MonthlyTimeRegister.RestDays}
<!-- alias: loc:RestDays, dom:MonthlyTimeRegister_RestDays -->

**ما هو.** أيام الراحة.

**القواعد.** اختياري. رقم.

## AnnualLeaveAccrualDays {#MonthlyTimeRegister.AnnualLeaveAccrualDays}
<!-- alias: loc:AnnualLeaveAccrualDays, dom:MonthlyTimeRegister_AnnualLeaveAccrualDays -->

**ما هو.** أيام الإجازة السنوية المستحقة.

**القواعد.** اختياري. رقم.

## AnnualLeaveOpeningBal {#MonthlyTimeRegister.AnnualLeaveOpeningBal}
<!-- alias: loc:AnnualLeaveOpeningBal, dom:MonthlyTimeRegister_AnnualLeaveOpeningBal -->

**ما هو.** رصيد الإجازة السنوية في البداية.

**القواعد.** اختياري. رقم.

## AnnualLeaveClosingBal {#MonthlyTimeRegister.AnnualLeaveClosingBal}
<!-- alias: loc:AnnualLeaveClosingBal, dom:MonthlyTimeRegister_AnnualLeaveClosingBal -->

**ما هو.** رصيد الإجازة السنوية في النهاية.

**القواعد.** اختياري. رقم.

## TicketAccrualDays {#MonthlyTimeRegister.TicketAccrualDays}
<!-- alias: loc:TicketAccrualDays, dom:MonthlyTimeRegister_TicketAccrualDays -->

**ما هو.** استحقاق تذاكر الطيران المتراكم.

**القواعد.** اختياري. رقم.

## TicketAccrualOpeningBal {#MonthlyTimeRegister.TicketAccrualOpeningBal}
<!-- alias: loc:TicketAccrualOpeningBal, dom:MonthlyTimeRegister_TicketAccrualOpeningBal -->

**ما هو.** رصيد تذاكر الطيران في البداية.

**القواعد.** اختياري. رقم.

## TicketAccrualClosingBal {#MonthlyTimeRegister.TicketAccrualClosingBal}
<!-- alias: loc:TicketAccrualClosingBal, dom:MonthlyTimeRegister_TicketAccrualClosingBal -->

**ما هو.** رصيد تذاكر الطيران في النهاية.

**القواعد.** اختياري. رقم.

## AirTicketPaidDays {#MonthlyTimeRegister.AirTicketPaidDays}
<!-- alias: loc:AirTicketPaidDays, dom:MonthlyTimeRegister_AirTicketPaidDays -->

**ما هو.** تذاكر الطيران المصروفة.

**القواعد.** اختياري. رقم.

## ActiveDays {#MonthlyTimeRegister.ActiveDays}
<!-- alias: loc:ActiveDays, dom:MonthlyTimeRegister_ActiveDays -->

**ما هو.** الأيام التي كان فيها الموظف على رأس العمل.

**القواعد.** اختياري. رقم.

## PlannedWorkingMins {#MonthlyTimeRegister.PlannedWorkingMins}
<!-- alias: loc:PlannedWorkingMins, dom:MonthlyTimeRegister_PlannedWorkingMins -->

**ما هو.** دقائق العمل المخططة.

**القواعد.** اختياري. رقم.

## ActualWorkingMins {#MonthlyTimeRegister.ActualWorkingMins}
<!-- alias: loc:ActualWorkingMins, dom:MonthlyTimeRegister_ActualWorkingMins -->

**ما هو.** الدقائق المعمولة فعلاً ضمن المناوبات.

**القواعد.** اختياري. رقم.

## ActualAbsenceMins {#MonthlyTimeRegister.ActualAbsenceMins}
<!-- alias: loc:ActualAbsenceMins, dom:MonthlyTimeRegister_ActualAbsenceMins -->

**ما هو.** دقائق الغياب عن وقت العمل المخطط.

**القواعد.** اختياري. رقم.

## PlannedWorkingDays {#MonthlyTimeRegister.PlannedWorkingDays}
<!-- alias: loc:PlannedWorkingDays, dom:MonthlyTimeRegister_PlannedWorkingDays -->

**ما هو.** أيام العمل المخططة.

**القواعد.** اختياري. رقم.

## ActualWorkingDays {#MonthlyTimeRegister.ActualWorkingDays}
<!-- alias: loc:ActualWorkingDays, dom:MonthlyTimeRegister_ActualWorkingDays -->

**ما هو.** الأيام المعمولة فعلاً.

**القواعد.** اختياري. رقم.

## ExtraWorkingDays {#MonthlyTimeRegister.ExtraWorkingDays}
<!-- alias: loc:ExtraWorkingDays, dom:MonthlyTimeRegister_ExtraWorkingDays -->

**ما هو.** الأيام المعمولة زيادة على المخطط.

**القواعد.** اختياري. رقم.

## LieuDays {#MonthlyTimeRegister.LieuDays}
<!-- alias: loc:LieuDays, dom:MonthlyTimeRegister_LieuDays -->

**ما هو.** الأيام المكتسبة بدلاً.

**القواعد.** اختياري. رقم.

## AdvLeaveRecoveryDays {#MonthlyTimeRegister.AdvLeaveRecoveryDays}
<!-- alias: loc:AdvLeaveRecoveryDays, dom:MonthlyTimeRegister_AdvLeaveRecoveryDays -->

**ما هو.** أيام الإجازة المقدمة المستردة.

**القواعد.** اختياري. رقم.

## NormalDayOTMins {#MonthlyTimeRegister.NormalDayOTMins}
<!-- alias: loc:NormalDayOTMins, dom:MonthlyTimeRegister_NormalDayOTMins -->

**ما هو.** دقائق العمل الإضافي في الأيام العادية.

**القواعد.** اختياري. رقم.

## RestDayOTMins {#MonthlyTimeRegister.RestDayOTMins}
<!-- alias: loc:RestDayOTMins, dom:MonthlyTimeRegister_RestDayOTMins -->

**ما هو.** دقائق العمل الإضافي في أيام الراحة.

**القواعد.** اختياري. رقم.

## HolidayOTMins {#MonthlyTimeRegister.HolidayOTMins}
<!-- alias: loc:HolidayOTMins, dom:MonthlyTimeRegister_HolidayOTMins -->

**ما هو.** دقائق العمل الإضافي في العطلات الرسمية.

**القواعد.** اختياري. رقم.

## WeekendOTMins {#MonthlyTimeRegister.WeekendOTMins}
<!-- alias: loc:WeekendOTMins, dom:MonthlyTimeRegister_WeekendOTMins -->

**ما هو.** دقائق العمل الإضافي في أيام الراحة الأسبوعية.

**القواعد.** اختياري. رقم.

## AdvanceLeavePaids {#MonthlyTimeRegister.AdvanceLeavePaids}
<!-- alias: loc:AdvanceLeavePaids, dom:MonthlyTimeRegister_AdvanceLeavePaids -->

**ما هو.** الإجازات المصروفة مقدماً.

**القواعد.** اختياري. رقم.

## AdvanceTicketPaids {#MonthlyTimeRegister.AdvanceTicketPaids}
<!-- alias: loc:AdvanceTicketPaids, dom:MonthlyTimeRegister_AdvanceTicketPaids -->

**ما هو.** تذاكر الطيران المصروفة مقدماً.

**القواعد.** اختياري. رقم.
