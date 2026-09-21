---
helpId: pay.attendance-type.edit
lang: en
title: Attendance Types — Create / Edit
entity: HCMv3.PayrollManagement.AttendanceType
surfaces:
  - angular/src/app/admin/payroll-management/attendance-types/create-or-edit-attendance-type-modal.component.html
audience: [admin]
permissions: [Pages.PayrollManagement.AttendanceTypes.Create]
---

## Overview

A kind of attendance away from the normal routine -- business trip, training, on-site duty -- recorded as employee attendance. It decides whether those days count as paid time, whether they fall on weekends and holidays, and whether they earn leave.

## WageType {#AttendanceType.WageTypeId}
<!-- alias: loc:WageType, dom:AttendanceType_WageTypeId -->

**What it is.** The wagetype attendance of this type is paid under.

**Example.** Business Trip Allowance

**Why it matters.** An employee with attendance of this type in the pay period has this wagetype calculated as attendance pay.

**Rules.** Optional. Chosen from a list.

## AccrualPeriod {#AttendanceType.AccrualPeriod}
<!-- alias: loc:AccrualPeriod, dom:AttendanceType_AccrualPeriod -->

**What it is.** An accrual period for the attendance type.

**Why it matters.** Not read at present.

**Rules.** Required. One of: No Accrual, Calendar Year, Fiscal Year, Work Year, Bi-Annual, Per Employment, Custom.

## LinkLeaveQuota {#AttendanceType.LinkLeaveQuotaId}
<!-- alias: loc:LinkLeaveQuota, dom:AttendanceType_LinkLeaveQuotaId -->

**What it is.** The leave that days of this attendance add to.

**Example.** Compensatory Leave

**Why it matters.** For that leave type, the balance is the attendance days recorded on types linked to it, less what has been taken.

**Rules.** Optional. Chosen from Absence Type master data.

## IsPaid {#AttendanceType.IsPaid}
<!-- alias: loc:IsPaid, dom:AttendanceType_IsPaid -->

**What it is.** Whether attendance of this type counts as paid working time.

**Why it matters.** Ticked, the time register counts it as actual attendance time -- the full planned day or the attendance's own hours, per **Is Full Day**. Unticked, it adds no time.

**Rules.** Yes or no.

## HaveQuotaAccrual {#AttendanceType.HaveQuotaAccrual}
<!-- alias: loc:HaveQuotaAccrual, dom:AttendanceType_HaveQuotaAccrual -->

**What it is.** Whether this attendance earns leave.

**Why it matters.** Ticked, **Link Leave Quota** must be filled in. The leave is earned through the link itself: any attendance type with a linked leave adds to it.

**Rules.** Yes or no.

## IncludeWeekend {#AttendanceType.IncludeWeekend}
<!-- alias: loc:IncludeWeekend, dom:AttendanceType_IncludeWeekend -->

**What it is.** Whether attendance on a weekly off counts.

**Why it matters.** Unticked, attendance of this type on a weekly off adds no time.

**Rules.** Yes or no.

## IncludeRestDay {#AttendanceType.IncludeRestDay}
<!-- alias: loc:IncludeRestDay, dom:AttendanceType_IncludeRestDay -->

**What it is.** Whether attendance on a rest day counts.

**Why it matters.** Unticked, attendance of this type on a rest day adds no time.

**Rules.** Yes or no.

## IncludePublicHoliday {#AttendanceType.IncludePublicHoliday}
<!-- alias: loc:IncludePublicHoliday, dom:AttendanceType_IncludePublicHoliday -->

**What it is.** Whether attendance on a public holiday counts.

**Why it matters.** Ticked, the day counts as attendance and is no longer treated as a holiday in the time register. Unticked, attendance of this type on a public holiday adds no time.

**Rules.** Yes or no.

## CalcOnCalendarDays {#AttendanceType.CalcOnCalendarDays}
<!-- alias: loc:CalcOnCalendarDays, dom:AttendanceType_CalcOnCalendarDays -->

**What it is.** Whether attendance days are counted on calendar days.

**Why it matters.** Not read at present.

**Rules.** Yes or no.

## IsFullDay {#AttendanceType.IsFullDay}
<!-- alias: loc:IsFullDay, dom:AttendanceType_IsFullDay -->

**What it is.** Whether attendance of this type counts as a full working day.

**Why it matters.** Ticked, it counts the employee's full planned minutes for the day. Unticked, it counts the hours between the attendance's start and end time.

**Rules.** Yes or no.
