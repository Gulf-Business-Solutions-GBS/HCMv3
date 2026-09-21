---
helpId: tm.monthly-time-register.edit
lang: en
title: Monthly Time Registers — Create / Edit
entity: HCMv3.TimeManagement.MonthlyTimeRegister
surfaces:
  - angular/src/app/admin/time-management/monthly-time-registers/create-or-edit-monthly-time-register-modal.component.html
audience: [admin]
permissions: [Pages.TimeManagement.MonthlyTimeRegisters.Create]
---

## Overview

A month's totals of an employee's time register.

Not written at present: no process fills these registers and nothing reads them. Payroll and the reports work from the daily time register.

## Employee {#MonthlyTimeRegister.EmployeeId}
<!-- alias: loc:Employee, dom:MonthlyTimeRegister_EmployeeId -->

**What it is.** The employee.

**Rules.** Required. Chosen from the employee list.

## Month {#MonthlyTimeRegister.Month}
<!-- alias: loc:Month, dom:MonthlyTimeRegister_Month -->

**What it is.** The month the totals are for.

**Example.** `2026-04-01`

**Rules.** Required. Date value.

## AnnualLeaveDays {#MonthlyTimeRegister.AnnualLeaveDays}
<!-- alias: loc:AnnualLeaveDays, dom:MonthlyTimeRegister_AnnualLeaveDays -->

**What it is.** Annual leave days taken.

**Rules.** Optional. Number.

## UnpaidLeaveDays {#MonthlyTimeRegister.UnpaidLeaveDays}
<!-- alias: loc:UnpaidLeaveDays, dom:MonthlyTimeRegister_UnpaidLeaveDays -->

**What it is.** Unpaid leave days taken.

**Rules.** Optional. Number.

## SickLeavePaidDays {#MonthlyTimeRegister.SickLeavePaidDays}
<!-- alias: loc:SickLeavePaidDays, dom:MonthlyTimeRegister_SickLeavePaidDays -->

**What it is.** Paid sick leave days.

**Rules.** Optional. Number.

## SickLeaveUnpaidDays {#MonthlyTimeRegister.SickLeaveUnpaidDays}
<!-- alias: loc:SickLeaveUnpaidDays, dom:MonthlyTimeRegister_SickLeaveUnpaidDays -->

**What it is.** Unpaid sick leave days.

**Rules.** Optional. Number.

## GratuityDays {#MonthlyTimeRegister.GratuityDays}
<!-- alias: loc:GratuityDays, dom:MonthlyTimeRegister_GratuityDays -->

**What it is.** Indemnity days accrued.

**Rules.** Optional. Number.

## GratuityPaidDays {#MonthlyTimeRegister.GratuityPaidDays}
<!-- alias: loc:GratuityPaidDays, dom:MonthlyTimeRegister_GratuityPaidDays -->

**What it is.** Indemnity days paid.

**Rules.** Optional. Number.

## PublicHolidays {#MonthlyTimeRegister.PublicHolidays}
<!-- alias: loc:PublicHolidays, dom:MonthlyTimeRegister_PublicHolidays -->

**What it is.** Public holidays.

**Rules.** Optional. Number.

## WeeklyOffs {#MonthlyTimeRegister.WeeklyOffs}
<!-- alias: loc:WeeklyOffs, dom:MonthlyTimeRegister_WeeklyOffs -->

**What it is.** Weekly off days.

**Rules.** Optional. Number.

## RestDays {#MonthlyTimeRegister.RestDays}
<!-- alias: loc:RestDays, dom:MonthlyTimeRegister_RestDays -->

**What it is.** Rest days.

**Rules.** Optional. Number.

## AnnualLeaveAccrualDays {#MonthlyTimeRegister.AnnualLeaveAccrualDays}
<!-- alias: loc:AnnualLeaveAccrualDays, dom:MonthlyTimeRegister_AnnualLeaveAccrualDays -->

**What it is.** Annual leave days accrued.

**Rules.** Optional. Number.

## AnnualLeaveOpeningBal {#MonthlyTimeRegister.AnnualLeaveOpeningBal}
<!-- alias: loc:AnnualLeaveOpeningBal, dom:MonthlyTimeRegister_AnnualLeaveOpeningBal -->

**What it is.** Annual leave balance at the start.

**Rules.** Optional. Number.

## AnnualLeaveClosingBal {#MonthlyTimeRegister.AnnualLeaveClosingBal}
<!-- alias: loc:AnnualLeaveClosingBal, dom:MonthlyTimeRegister_AnnualLeaveClosingBal -->

**What it is.** Annual leave balance at the end.

**Rules.** Optional. Number.

## TicketAccrualDays {#MonthlyTimeRegister.TicketAccrualDays}
<!-- alias: loc:TicketAccrualDays, dom:MonthlyTimeRegister_TicketAccrualDays -->

**What it is.** Air ticket entitlement accrued.

**Rules.** Optional. Number.

## TicketAccrualOpeningBal {#MonthlyTimeRegister.TicketAccrualOpeningBal}
<!-- alias: loc:TicketAccrualOpeningBal, dom:MonthlyTimeRegister_TicketAccrualOpeningBal -->

**What it is.** Air ticket balance at the start.

**Rules.** Optional. Number.

## TicketAccrualClosingBal {#MonthlyTimeRegister.TicketAccrualClosingBal}
<!-- alias: loc:TicketAccrualClosingBal, dom:MonthlyTimeRegister_TicketAccrualClosingBal -->

**What it is.** Air ticket balance at the end.

**Rules.** Optional. Number.

## AirTicketPaidDays {#MonthlyTimeRegister.AirTicketPaidDays}
<!-- alias: loc:AirTicketPaidDays, dom:MonthlyTimeRegister_AirTicketPaidDays -->

**What it is.** Air tickets paid.

**Rules.** Optional. Number.

## ActiveDays {#MonthlyTimeRegister.ActiveDays}
<!-- alias: loc:ActiveDays, dom:MonthlyTimeRegister_ActiveDays -->

**What it is.** Days the employee was employed.

**Rules.** Optional. Number.

## PlannedWorkingMins {#MonthlyTimeRegister.PlannedWorkingMins}
<!-- alias: loc:PlannedWorkingMins, dom:MonthlyTimeRegister_PlannedWorkingMins -->

**What it is.** Planned working minutes.

**Rules.** Optional. Number.

## ActualWorkingMins {#MonthlyTimeRegister.ActualWorkingMins}
<!-- alias: loc:ActualWorkingMins, dom:MonthlyTimeRegister_ActualWorkingMins -->

**What it is.** Minutes actually worked within the shifts.

**Rules.** Optional. Number.

## ActualAbsenceMins {#MonthlyTimeRegister.ActualAbsenceMins}
<!-- alias: loc:ActualAbsenceMins, dom:MonthlyTimeRegister_ActualAbsenceMins -->

**What it is.** Minutes absent from planned working time.

**Rules.** Optional. Number.

## PlannedWorkingDays {#MonthlyTimeRegister.PlannedWorkingDays}
<!-- alias: loc:PlannedWorkingDays, dom:MonthlyTimeRegister_PlannedWorkingDays -->

**What it is.** Planned working days.

**Rules.** Optional. Number.

## ActualWorkingDays {#MonthlyTimeRegister.ActualWorkingDays}
<!-- alias: loc:ActualWorkingDays, dom:MonthlyTimeRegister_ActualWorkingDays -->

**What it is.** Days actually worked.

**Rules.** Optional. Number.

## ExtraWorkingDays {#MonthlyTimeRegister.ExtraWorkingDays}
<!-- alias: loc:ExtraWorkingDays, dom:MonthlyTimeRegister_ExtraWorkingDays -->

**What it is.** Days worked beyond the plan.

**Rules.** Optional. Number.

## LieuDays {#MonthlyTimeRegister.LieuDays}
<!-- alias: loc:LieuDays, dom:MonthlyTimeRegister_LieuDays -->

**What it is.** Days earned in lieu.

**Rules.** Optional. Number.

## AdvLeaveRecoveryDays {#MonthlyTimeRegister.AdvLeaveRecoveryDays}
<!-- alias: loc:AdvLeaveRecoveryDays, dom:MonthlyTimeRegister_AdvLeaveRecoveryDays -->

**What it is.** Advance leave days recovered.

**Rules.** Optional. Number.

## NormalDayOTMins {#MonthlyTimeRegister.NormalDayOTMins}
<!-- alias: loc:NormalDayOTMins, dom:MonthlyTimeRegister_NormalDayOTMins -->

**What it is.** Overtime minutes on normal days.

**Rules.** Optional. Number.

## RestDayOTMins {#MonthlyTimeRegister.RestDayOTMins}
<!-- alias: loc:RestDayOTMins, dom:MonthlyTimeRegister_RestDayOTMins -->

**What it is.** Overtime minutes on rest days.

**Rules.** Optional. Number.

## HolidayOTMins {#MonthlyTimeRegister.HolidayOTMins}
<!-- alias: loc:HolidayOTMins, dom:MonthlyTimeRegister_HolidayOTMins -->

**What it is.** Overtime minutes on public holidays.

**Rules.** Optional. Number.

## WeekendOTMins {#MonthlyTimeRegister.WeekendOTMins}
<!-- alias: loc:WeekendOTMins, dom:MonthlyTimeRegister_WeekendOTMins -->

**What it is.** Overtime minutes on weekly offs.

**Rules.** Optional. Number.

## AdvanceLeavePaids {#MonthlyTimeRegister.AdvanceLeavePaids}
<!-- alias: loc:AdvanceLeavePaids, dom:MonthlyTimeRegister_AdvanceLeavePaids -->

**What it is.** Leave paid in advance.

**Rules.** Optional. Number.

## AdvanceTicketPaids {#MonthlyTimeRegister.AdvanceTicketPaids}
<!-- alias: loc:AdvanceTicketPaids, dom:MonthlyTimeRegister_AdvanceTicketPaids -->

**What it is.** Air tickets paid in advance.

**Rules.** Optional. Number.
