---
helpId: trn.training-course.edit
lang: ar
title: الدورات التدريبية — إنشاء / تحرير
entity: HCMv3.TrainingManagement.TrainingCourse
surfaces:
  - angular/src/app/admin/training-management/training-courses/create-or-edit-training-course-modal.component.html
audience: [admin]
permissions: [Pages.TrainingManagement.TrainingCourses.Create]
---

## Overview

دورة تدريبية. وتُعدّ مرات انعقادها كجداول. وتُدرج الدورات في تقرير الدورات التدريبية.

## Active {#TrainingCourse.Active}
<!-- alias: loc:Active, dom:TrainingCourse_Active -->

**ما هو.** ما إذا كانت الدورة معروضة. يظهر في التقرير.

**القواعد.** نعم أو لا.

## DeliveryMethod {#TrainingCourse.DeliveryMethod}
<!-- alias: loc:DeliveryMethod, dom:TrainingCourse_DeliveryMethod -->

**ما هو.** طريقة تقديم الدورة.

**مثال.** قاعة تدريب

**القواعد.** إلزامي. إحدى القيم: متصل، قاعة تدريب.

## Fees {#TrainingCourse.Fees}
<!-- alias: loc:Fees, dom:TrainingCourse_Fees -->

**ما هو.** تكلفة الدورة.

**مثال.** 250.000

**القواعد.** اختياري. رقم.
