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

**Why it matters.** Recorded only; no calculation reads it at present.

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
