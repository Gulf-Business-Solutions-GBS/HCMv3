---
helpId: trn.employee-training.edit
lang: en
title: Employee Trainings — Create / Edit
entity: HCMv3.TrainingManagement.EmployeeTraining
surfaces:
  - angular/src/app/admin/training-management/employee-trainings/create-or-edit-employee-training-modal.component.html
audience: [admin]
permissions: [Pages.TrainingManagement.EmployeeTrainings.Create]
---

## Overview

An employee enrolled on a run of a course, and how it went. An employee can be enrolled once per course and schedule. Enrolments appear on the Employee Training Report.

## EmployeeId {#EmployeeTraining.EmployeeId}
<!-- alias: loc:EmployeeId, dom:EmployeeTraining_EmployeeId -->

**What it is.** The employee.

**Rules.** Required. Chosen from the employee list.

## TrainingCourseId {#EmployeeTraining.TrainingCourseId}
<!-- alias: loc:TrainingCourseId, dom:EmployeeTraining_TrainingCourseId -->

**What it is.** The course.

**Rules.** Required. Chosen from Training Course master data.

## TrainingScheduleId {#EmployeeTraining.TrainingScheduleId}
<!-- alias: loc:TrainingScheduleId, dom:EmployeeTraining_TrainingScheduleId -->

**What it is.** The run of the course.

**Rules.** Required. Chosen from Training Schedule master data.

## CompletionStatus {#EmployeeTraining.CompletionStatus}
<!-- alias: loc:CompletionStatus, dom:EmployeeTraining_CompletionStatus -->

**What it is.** Whether the employee has completed the course.

**Example.** Complete

**Rules.** Required. One of: Complete, Pending.

## Score {#EmployeeTraining.Score}
<!-- alias: loc:Score, dom:EmployeeTraining_Score -->

**What it is.** The employee's score, if assessed.

**Example.** 85

**Rules.** Optional. Number.

## Certificate {#EmployeeTraining.EmployeeCertificationId}
<!-- alias: loc:Certificate, dom:EmployeeTraining_EmployeeCertificationId -->

**What it is.** The certificate the employee gained from this training, from their own certifications.

**Example.** Certified Payroll Professional (CPP)

**Why it matters.** Only the employee's certifications of type Training can be chosen; record the certificate on the employee's certifications first. A newly chosen certificate that is not the employee's, or not a Training one, is refused. A certificate already linked stays even if its type is changed later.

**Rules.** Optional. Chosen from a list.
