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

**Rules.** Optional. Chosen from Leave Type master data.

## IncludePublicHoliday {#AttendanceType.IncludePublicHoliday}
<!-- alias: loc:IncludePublicHoliday, dom:AttendanceType_IncludePublicHoliday -->

**What it is.** Whether attendance on a public holiday counts.

**Why it matters.** Ticked, the day counts as attendance and is no longer treated as a holiday in the time register. Unticked, attendance of this type on a public holiday adds no time.

**Rules.** Yes or no.

## HaveQuotaAccrual {#AttendanceType.HaveQuotaAccrual}
<!-- alias: loc:HaveQuotaAccrual, dom:AttendanceType_HaveQuotaAccrual -->

**What it is.** Whether this attendance earns leave.

**Why it matters.** Ticked, **Link Leave Quota** must be filled in. The leave is earned through the link itself: any attendance type with a linked leave adds to it.

**Rules.** Yes or no.
