---
helpId: trn.training-attendance.edit
lang: en
title: Training Attendances — Create / Edit
entity: HCMv3.TrainingManagement.TrainingAttendance
surfaces:
  - angular/src/app/admin/training-management/training-attendances/create-or-edit-training-attendance-modal.component.html
audience: [admin]
permissions: [Pages.TrainingManagement.TrainingAttendances.Create]
---

## Overview

Whether an employee attended a training run on one day. The day must fall within the run's dates, and each employee can have one record per run per day. It does not affect time or payroll.

## TrainingScheduleId {#TrainingAttendance.TrainingScheduleId}
<!-- alias: loc:TrainingScheduleId, dom:TrainingAttendance_TrainingScheduleId -->

**What it is.** The run of the course.

**Rules.** Required. Chosen from Training Schedule master data.

## EmployeeId {#TrainingAttendance.EmployeeId}
<!-- alias: loc:EmployeeId, dom:TrainingAttendance_EmployeeId -->

**What it is.** The employee.

**Rules.** Required. Chosen from the employee list.

## DayDate {#TrainingAttendance.DayDate}
<!-- alias: loc:DayDate, dom:TrainingAttendance_DayDate -->

**What it is.** The day.

**Rules.** Required. Date value.

## Attendance {#TrainingAttendance.Attendance}
<!-- alias: loc:Attendance, dom:TrainingAttendance_Attendance -->

**What it is.** Present or absent.

**Example.** Present

**Rules.** Required. One of: Present, Absent.
