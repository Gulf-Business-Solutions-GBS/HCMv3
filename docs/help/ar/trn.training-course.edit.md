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

## Institution {#TrainingCourse.InstitutionId}
<!-- alias: loc:Institution, dom:TrainingCourse_InstitutionId -->

**ما هو.** الجهة التعليمية التي تنظّم الدورة.

**مثال.** معهد الكويت للأبحاث العلمية

**لماذا يهم.** تُسجَّل للرجوع إليها، ولا يستخدمها شيء آخر.

**القواعد.** اختياري. يُختار من قائمة الجهات التعليمية.

## Source {#TrainingCourse.Source}
<!-- alias: loc:Source, dom:TrainingCourse_Source -->

**ما هو.** ما إذا كانت الدورة تُعقد داخل الشركة أم لدى جهة خارجية.

**مثال.** خارجي

**لماذا يهم.** يُسجَّل للرجوع إليه، ولا يستخدمه شيء آخر. والدورات التي كانت موجودة قبل إضافة هذا الحقل داخلية.

**القواعد.** إلزامي. إحدى القيم: داخلي، خارجي.
