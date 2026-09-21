---
helpId: trn.employee-training.edit
lang: ar
title: تدريب الموظفين — إنشاء / تحرير
entity: HCMv3.TrainingManagement.EmployeeTraining
surfaces:
  - angular/src/app/admin/training-management/employee-trainings/create-or-edit-employee-training-modal.component.html
audience: [admin]
permissions: [Pages.TrainingManagement.EmployeeTrainings.Create]
---

## Overview

موظف مسجَّل في مرة انعقاد لدورة، ونتيجة ذلك. ولا يُسجَّل الموظف إلا مرة واحدة لكل دورة وجدول. وتظهر التسجيلات في تقرير تدريب الموظفين.

## EmployeeId {#EmployeeTraining.EmployeeId}
<!-- alias: loc:EmployeeId, dom:EmployeeTraining_EmployeeId -->

**ما هو.** الموظف.

**القواعد.** إلزامي. يُختار من قائمة الموظفين.

## TrainingCourseId {#EmployeeTraining.TrainingCourseId}
<!-- alias: loc:TrainingCourseId, dom:EmployeeTraining_TrainingCourseId -->

**ما هو.** الدورة.

**القواعد.** إلزامي. يُختار من قائمة الدورات التدريبية.

## TrainingScheduleId {#EmployeeTraining.TrainingScheduleId}
<!-- alias: loc:TrainingScheduleId, dom:EmployeeTraining_TrainingScheduleId -->

**ما هو.** مرة انعقاد الدورة.

**القواعد.** إلزامي. يُختار من قائمة جداول التدريب.

## CompletionStatus {#EmployeeTraining.CompletionStatus}
<!-- alias: loc:CompletionStatus, dom:EmployeeTraining_CompletionStatus -->

**ما هو.** ما إذا كان الموظف قد أكمل الدورة.

**مثال.** مكتمل

**القواعد.** إلزامي. إحدى القيم: مكتمل، قيد الانتظار.

## Score {#EmployeeTraining.Score}
<!-- alias: loc:Score, dom:EmployeeTraining_Score -->

**ما هو.** درجة الموظف، إن قُيّم.

**مثال.** 85

**القواعد.** اختياري. رقم.
