---
helpId: pa.employee-attendance.edit
lang: en
title: Employee Attendances — Create / Edit
entity: HCMv3.PersonnelAdministration.EmployeeAttendance
surfaces:
  - angular/src/app/admin/personnel-administration/employee-detail/attendances/create-or-edit-employee-attendance-modal.component.html
audience: [admin]
permissions: [Pages.PersonnelAdministration.EmployeeAttendances.Create]
---

## Overview

Time the employee spends away from their normal duty but still counted as attendance -- for example training, a field visit or official business. Attendance records for the same employee cannot overlap.

The time engine reads these records when it builds each day. Regular payroll pays attendance from those days; off-cycle payroll reads the records directly.

## AttendanceTypeId {#EmployeeAttendance.AttendanceTypeId}
<!-- alias: loc:AttendanceTypeId, dom:EmployeeAttendance_AttendanceTypeId -->

**What it is.** The kind of attendance.

**Example.** Official Business

**Why it matters.** Payroll pays attendance per attendance type.

**Rules.** Required. Chosen from Attendance Type master data.

:::admin
Maintained under Payroll Management → Attendance Types.
:::

## AttendanceUnit {#EmployeeAttendance.AttendanceUnit}
<!-- alias: loc:AttendanceUnit, dom:EmployeeAttendance_AttendanceUnit -->

**What it is.** Whether the attendance is recorded as a period, in days or in hours.

**Example.** Hours

**Why it matters.** Decides how Total Attend Days is worked out when the record is saved.

**Rules.** Required. One of: Period, Day, Hours.

## AttendanceHours {#EmployeeAttendance.AttendanceHours}
<!-- alias: loc:AttendanceHours, dom:EmployeeAttendance_AttendanceHours -->

**What it is.** The number of hours, for attendance recorded in hours.

**Example.** 3

**Why it matters.** Shown on the attendance report and in the approval notification.

**Rules.** Optional. Number.

## StartTime {#EmployeeAttendance.StartTime}
<!-- alias: loc:StartTime, dom:EmployeeAttendance_StartTime -->

**What it is.** The time the attendance starts.

**Example.** 09:00

**Why it matters.** The time engine uses the start and end times to work out the attendance minutes on the day.

**Rules.** Optional. Date value.

## EndTime {#EmployeeAttendance.EndTime}
<!-- alias: loc:EndTime, dom:EmployeeAttendance_EndTime -->

**What it is.** The time the attendance ends.

**Example.** 12:00

**Rules.** Optional. Date value.

## TotalAttendDays {#EmployeeAttendance.TotalAttendDays}
<!-- alias: loc:TotalAttendDays, dom:EmployeeAttendance_TotalAttendDays -->

**What it is.** The number of days the attendance counts for, worked out when the record is saved. It is empty until the record has been saved once.

**Example.** 0.5

**Why it matters.** For Period with a start and end time, the hours between the two times (which replace the hours entered) are divided by the employee's planned duty hours that day, or the payroll default working hours when the duty has none, and the result is kept between 0 and 1. For Period without times it is the calendar days from start date to end date. For Hours it is the hours entered divided the same way, with no upper limit. For Day it is not calculated. When the attendance type has Have Quota Accrual ticked, these days are added to the accrued quota of its linked leave type, and taken off again when the record is changed or deleted. The leave provision report also reads them.

**Rules.** Optional. Number.

:::admin
Saving fails when no duty is found for the employee on the start date. The quota row used is the one covering the employee's joining date, as in the old system; if there is none, one is created.
:::
