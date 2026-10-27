---
helpId: tm.yearly-time-register.edit
lang: ar
title: سجلات الوقت السنوية — إنشاء / تحرير
entity: HCMv3.TimeManagement.YearlyTimeRegister
surfaces:
  - angular/src/app/admin/time-management/yearly-time-registers/create-or-edit-yearly-time-register-modal.component.html
audience: [admin]
permissions: [Pages.TimeManagement.YearlyTimeRegisters.Create]
---

## Overview

مجاميع سنة من سجل وقت الموظف.

لا تُكتب حالياً: لا توجد عملية تملأ هذه السجلات ولا شيء يقرؤها. ويعمل نظام الرواتب والتقارير من سجل الوقت اليومي.

## Employee {#YearlyTimeRegister.EmployeeId}
<!-- alias: loc:Employee, dom:YearlyTimeRegister_EmployeeId -->

**ما هو.** الموظف.

**القواعد.** إلزامي. يُختار من قائمة الموظفين.

## Year {#YearlyTimeRegister.Year}
<!-- alias: loc:Year, dom:YearlyTimeRegister_Year -->

**ما هو.** السنة التي تخصها المجاميع.

**مثال.** `2026`

**القواعد.** إلزامي. تاريخ.

## AnnualLeaveDays {#YearlyTimeRegister.AnnualLeaveDays}
<!-- alias: loc:AnnualLeaveDays, dom:YearlyTimeRegister_AnnualLeaveDays -->

**ما هو.** أيام الإجازة السنوية المأخوذة.

**القواعد.** اختياري. رقم.

## UnpaidLeaveDays {#YearlyTimeRegister.UnpaidLeaveDays}
<!-- alias: loc:UnpaidLeaveDays, dom:YearlyTimeRegister_UnpaidLeaveDays -->

**ما هو.** أيام الإجازة غير المدفوعة المأخوذة.

**القواعد.** اختياري. رقم.

## SickLeavePaidDays {#YearlyTimeRegister.SickLeavePaidDays}
<!-- alias: loc:SickLeavePaidDays, dom:YearlyTimeRegister_SickLeavePaidDays -->

**ما هو.** أيام الإجازة المرضية المدفوعة.

**القواعد.** اختياري. رقم.

## SickLeaveUnpaidDays {#YearlyTimeRegister.SickLeaveUnpaidDays}
<!-- alias: loc:SickLeaveUnpaidDays, dom:YearlyTimeRegister_SickLeaveUnpaidDays -->

**ما هو.** أيام الإجازة المرضية غير المدفوعة.

**القواعد.** اختياري. رقم.

## GratuityDays {#YearlyTimeRegister.GratuityDays}
<!-- alias: loc:GratuityDays, dom:YearlyTimeRegister_GratuityDays -->

**ما هو.** أيام مكافأة نهاية الخدمة المستحقة.

**القواعد.** اختياري. رقم.

## GratuityPaidDays {#YearlyTimeRegister.GratuityPaidDays}
<!-- alias: loc:GratuityPaidDays, dom:YearlyTimeRegister_GratuityPaidDays -->

**ما هو.** أيام مكافأة نهاية الخدمة المصروفة.

**القواعد.** اختياري. رقم.

## PublicHolidays {#YearlyTimeRegister.PublicHolidays}
<!-- alias: loc:PublicHolidays, dom:YearlyTimeRegister_PublicHolidays -->

**ما هو.** العطلات الرسمية.

**القواعد.** اختياري. رقم.

## WeeklyOffs {#YearlyTimeRegister.WeeklyOffs}
<!-- alias: loc:WeeklyOffs, dom:YearlyTimeRegister_WeeklyOffs -->

**ما هو.** أيام الراحة الأسبوعية.

**القواعد.** اختياري. رقم.

## RestDays {#YearlyTimeRegister.RestDays}
<!-- alias: loc:RestDays, dom:YearlyTimeRegister_RestDays -->

**ما هو.** أيام الراحة.

**القواعد.** اختياري. رقم.

## AnnualLeaveAccrualDays {#YearlyTimeRegister.AnnualLeaveAccrualDays}
<!-- alias: loc:AnnualLeaveAccrualDays, dom:YearlyTimeRegister_AnnualLeaveAccrualDays -->

**ما هو.** أيام الإجازة السنوية المستحقة.

**القواعد.** اختياري. رقم.

## AnnualLeaveOpeningBal {#YearlyTimeRegister.AnnualLeaveOpeningBal}
<!-- alias: loc:AnnualLeaveOpeningBal, dom:YearlyTimeRegister_AnnualLeaveOpeningBal -->

**ما هو.** رصيد الإجازة السنوية في البداية.

**القواعد.** اختياري. رقم.

## AnnualLeaveClosingBal {#YearlyTimeRegister.AnnualLeaveClosingBal}
<!-- alias: loc:AnnualLeaveClosingBal, dom:YearlyTimeRegister_AnnualLeaveClosingBal -->

**ما هو.** رصيد الإجازة السنوية في النهاية.

**القواعد.** اختياري. رقم.

## TicketAccrualDays {#YearlyTimeRegister.TicketAccrualDays}
<!-- alias: loc:TicketAccrualDays, dom:YearlyTimeRegister_TicketAccrualDays -->

**ما هو.** استحقاق تذاكر الطيران المتراكم.

**القواعد.** اختياري. رقم.

## TicketAccrualOpeningBal {#YearlyTimeRegister.TicketAccrualOpeningBal}
<!-- alias: loc:TicketAccrualOpeningBal, dom:YearlyTimeRegister_TicketAccrualOpeningBal -->

**ما هو.** رصيد تذاكر الطيران في البداية.

**القواعد.** اختياري. رقم.

## TicketAccrualClosingBal {#YearlyTimeRegister.TicketAccrualClosingBal}
<!-- alias: loc:TicketAccrualClosingBal, dom:YearlyTimeRegister_TicketAccrualClosingBal -->

**ما هو.** رصيد تذاكر الطيران في النهاية.

**القواعد.** اختياري. رقم.

## AirTicketPaidDays {#YearlyTimeRegister.AirTicketPaidDays}
<!-- alias: loc:AirTicketPaidDays, dom:YearlyTimeRegister_AirTicketPaidDays -->

**ما هو.** تذاكر الطيران المصروفة.

**القواعد.** اختياري. رقم.

## ActiveDays {#YearlyTimeRegister.ActiveDays}
<!-- alias: loc:ActiveDays, dom:YearlyTimeRegister_ActiveDays -->

**ما هو.** الأيام التي كان فيها الموظف على رأس العمل.

**القواعد.** اختياري. رقم.

## PlannedWorkingMins {#YearlyTimeRegister.PlannedWorkingMins}
<!-- alias: loc:PlannedWorkingMins, dom:YearlyTimeRegister_PlannedWorkingMins -->

**ما هو.** دقائق العمل المخططة.

**القواعد.** اختياري. رقم.

## ActualWorkingMins {#YearlyTimeRegister.ActualWorkingMins}
<!-- alias: loc:ActualWorkingMins, dom:YearlyTimeRegister_ActualWorkingMins -->

**ما هو.** الدقائق المعمولة فعلاً ضمن المناوبات.

**القواعد.** اختياري. رقم.

## ActualAbsenceMins {#YearlyTimeRegister.ActualAbsenceMins}
<!-- alias: loc:ActualAbsenceMins, dom:YearlyTimeRegister_ActualAbsenceMins -->

**ما هو.** دقائق الغياب عن وقت العمل المخطط.

**القواعد.** اختياري. رقم.

## PlannedWorkingDays {#YearlyTimeRegister.PlannedWorkingDays}
<!-- alias: loc:PlannedWorkingDays, dom:YearlyTimeRegister_PlannedWorkingDays -->

**ما هو.** أيام العمل المخططة.

**القواعد.** اختياري. رقم.

## ActualWorkingDays {#YearlyTimeRegister.ActualWorkingDays}
<!-- alias: loc:ActualWorkingDays, dom:YearlyTimeRegister_ActualWorkingDays -->

**ما هو.** الأيام المعمولة فعلاً.

**القواعد.** اختياري. رقم.

## ExtraWorkingDays {#YearlyTimeRegister.ExtraWorkingDays}
<!-- alias: loc:ExtraWorkingDays, dom:YearlyTimeRegister_ExtraWorkingDays -->

**ما هو.** الأيام المعمولة زيادة على المخطط.

**القواعد.** اختياري. رقم.

## LieuDays {#YearlyTimeRegister.LieuDays}
<!-- alias: loc:LieuDays, dom:YearlyTimeRegister_LieuDays -->

**ما هو.** الأيام المكتسبة بدلاً.

**القواعد.** اختياري. رقم.

## AdvLeaveRecoveryDays {#YearlyTimeRegister.AdvLeaveRecoveryDays}
<!-- alias: loc:AdvLeaveRecoveryDays, dom:YearlyTimeRegister_AdvLeaveRecoveryDays -->

**ما هو.** أيام الإجازة المقدمة المستردة.

**القواعد.** اختياري. رقم.

## NormalDayOTMins {#YearlyTimeRegister.NormalDayOTMins}
<!-- alias: loc:NormalDayOTMins, dom:YearlyTimeRegister_NormalDayOTMins -->

**ما هو.** دقائق العمل الإضافي في الأيام العادية.

**القواعد.** اختياري. رقم.

## RestDayOTMins {#YearlyTimeRegister.RestDayOTMins}
<!-- alias: loc:RestDayOTMins, dom:YearlyTimeRegister_RestDayOTMins -->

**ما هو.** دقائق العمل الإضافي في أيام الراحة.

**القواعد.** اختياري. رقم.

## HolidayOTMins {#YearlyTimeRegister.HolidayOTMins}
<!-- alias: loc:HolidayOTMins, dom:YearlyTimeRegister_HolidayOTMins -->

**ما هو.** دقائق العمل الإضافي في العطلات الرسمية.

**القواعد.** اختياري. رقم.

## WeekendOTMins {#YearlyTimeRegister.WeekendOTMins}
<!-- alias: loc:WeekendOTMins, dom:YearlyTimeRegister_WeekendOTMins -->

**ما هو.** دقائق العمل الإضافي في أيام الراحة الأسبوعية.

**القواعد.** اختياري. رقم.

## AdvanceLeavePaids {#YearlyTimeRegister.AdvanceLeavePaids}
<!-- alias: loc:AdvanceLeavePaids, dom:YearlyTimeRegister_AdvanceLeavePaids -->

**ما هو.** الإجازات المصروفة مقدماً.

**القواعد.** اختياري. رقم.

## AdvanceTicketPaids {#YearlyTimeRegister.AdvanceTicketPaids}
<!-- alias: loc:AdvanceTicketPaids, dom:YearlyTimeRegister_AdvanceTicketPaids -->

**ما هو.** تذاكر الطيران المصروفة مقدماً.

**القواعد.** اختياري. رقم.
