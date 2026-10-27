---
helpId: pa.employee-task.edit
lang: ar
title: مهام الموظف — إنشاء / تحرير
entity: HCMv3.PersonnelAdministration.EmployeeTask
surfaces:
  - angular/src/app/admin/personnel-administration/employee-detail/tasks/create-or-edit-employee-task-modal.component.html
audience: [admin]
permissions: [Pages.PersonnelAdministration.EmployeeTasks.Create]
---

## Overview

مهمة أو متابعة تخص الموظف، مع تاريخ تذكير.

تُسجَّل للرجوع إليها؛ فلا يُرسل منها تذكير ولا يقرؤها شيء حالياً.

## TaskTypeId {#EmployeeTask.TaskTypeId}
<!-- alias: loc:TaskTypeId, dom:EmployeeTask_TaskTypeId -->

**ما هو.** نوع المهمة.

**مثال.** مراجعة فترة الاختبار

**القواعد.** إلزامي. يُختار من قائمة أنواع المهام.

:::admin
تُدار من البيانات الأساسية العامة ← أنواع المهام.
:::

## TaskDate {#EmployeeTask.TaskDate}
<!-- alias: loc:TaskDate, dom:EmployeeTask_TaskDate -->

**ما هو.** تاريخ استحقاق المهمة.

**مثال.** 30 نوفمبر 2026

**القواعد.** إلزامي. تاريخ.

## TaskStatus {#EmployeeTask.TaskStatus}
<!-- alias: loc:TaskStatus, dom:EmployeeTask_TaskStatus -->

**ما هو.** حالة المهمة، كرقم.

**مثال.** 0

**القواعد.** اختياري. رقم صحيح.

## ReminderDate {#EmployeeTask.ReminderDate}
<!-- alias: loc:ReminderDate, dom:EmployeeTask_ReminderDate -->

**ما هو.** تاريخ التذكير.

**مثال.** 23 نوفمبر 2026

**لماذا يهم.** لا يُرسل منه تذكير حالياً.

**القواعد.** إلزامي. تاريخ.

## MonitoringType {#EmployeeTask.MonitoringType}
<!-- alias: loc:MonitoringType, dom:EmployeeTask_MonitoringType -->

**ما هو.** طريقة متابعة المهمة، كرقم.

**مثال.** 0

**القواعد.** اختياري. رقم صحيح.

## MonitoringCount {#EmployeeTask.MonitoringCount}
<!-- alias: loc:MonitoringCount, dom:EmployeeTask_MonitoringCount -->

**ما هو.** عدد وحدات المتابعة.

**مثال.** 1

**القواعد.** اختياري. رقم صحيح.

## MonitoringUnit {#EmployeeTask.MonitoringUnit}
<!-- alias: loc:MonitoringUnit, dom:EmployeeTask_MonitoringUnit -->

**ما هو.** وحدة المتابعة، كرقم.

**مثال.** 0

**القواعد.** اختياري. رقم صحيح.
