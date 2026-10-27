---
helpId: trn.training-course.edit
lang: en
title: Training Courses — Create / Edit
entity: HCMv3.TrainingManagement.TrainingCourse
surfaces:
  - angular/src/app/admin/training-management/training-courses/create-or-edit-training-course-modal.component.html
audience: [admin]
permissions: [Pages.TrainingManagement.TrainingCourses.Create]
---

## Overview

A training course. Its runs are set up as schedules. Courses are listed on the Training Course Report.

## Active {#TrainingCourse.Active}
<!-- alias: loc:Active, dom:TrainingCourse_Active -->

**What it is.** Whether the course is offered. Shown on the report.

**Rules.** Yes or no.

## DeliveryMethod {#TrainingCourse.DeliveryMethod}
<!-- alias: loc:DeliveryMethod, dom:TrainingCourse_DeliveryMethod -->

**What it is.** How the course is given.

**Example.** Classroom

**Rules.** Required. One of: Online, Classroom.

## Fees {#TrainingCourse.Fees}
<!-- alias: loc:Fees, dom:TrainingCourse_Fees -->

**What it is.** The cost of the course.

**Example.** 250.000

**Rules.** Optional. Number.

## Institution {#TrainingCourse.InstitutionId}
<!-- alias: loc:Institution, dom:TrainingCourse_InstitutionId -->

**What it is.** The institution that runs the course.

**Example.** Kuwait Institute for Scientific Research

**Why it matters.** Recorded for reference; nothing else uses it.

**Rules.** Optional. Chosen from Institution master data.

## Source {#TrainingCourse.Source}
<!-- alias: loc:Source, dom:TrainingCourse_Source -->

**What it is.** Whether the course is run in-house or by an outside provider.

**Example.** External

**Why it matters.** Recorded for reference; nothing else uses it. Courses that existed before this field was added are Internal.

**Rules.** Required. One of: Internal, External.
