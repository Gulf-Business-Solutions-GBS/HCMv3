---
helpId: pa.emp-potential-assessment.edit
lang: ar
title: تقييمات إمكانات الموظف — إنشاء / تحرير
entity: HCMv3.SuccessionManagement.EmpPotentialAssessment
surfaces:
  - angular/src/app/admin/personnel-administration/employee-detail/potential-assessments/create-or-edit-employee-potential-assessment-modal.component.html
audience: [admin]
permissions: [Pages.SuccessionManagement.EmpPotentialAssessments.Create]
---

## Overview

تقييم لإمكانات الموظف خلال فترة، لأغراض تخطيط التعاقب الوظيفي. ولا يجوز أن تتداخل فترات الموظف نفسه.

يُسجَّل للرجوع إليه، ولا يقرؤه أي احتساب أو تقرير حالياً.

## Description {#EmpPotentialAssessment.Description}
<!-- alias: loc:Description, dom:EmpPotentialAssessment_Description -->

**ما هو.** ملاحظات على التقييم.

**مثال.** جاهز لدور قائد فريق خلال سنة.

**القواعد.** اختياري. الحد الأقصى 2000 حرف.

## Score {#EmpPotentialAssessment.Score}
<!-- alias: loc:Score, dom:EmpPotentialAssessment_Score -->

**ما هو.** درجة الإمكانات.

**مثال.** 3.8

**القواعد.** إلزامي. رقم.

## Source {#EmpPotentialAssessment.Source}
<!-- alias: loc:Source, dom:EmpPotentialAssessment_Source -->

**ما هو.** هل جاءت الدرجة من تقييم فعلي أم أُدخلت يدوياً.

**مثال.** يدوي

**القواعد.** إلزامي. إحدى القيم: منشور، Manual.

## RatingId {#EmpPotentialAssessment.RatingId}
<!-- alias: loc:RatingId, dom:EmpPotentialAssessment_RatingId -->

**ما هو.** تصنيف التعاقب الوظيفي.

**مثال.** إمكانات مرتفعة

**القواعد.** إلزامي. يُختار من قائمة تقديرات التعاقب الوظيفي.

:::admin
تُدار من إدارة التعاقب الوظيفي.
:::

## NineBoxRatingId {#EmpPotentialAssessment.NineBoxRatingId}
<!-- alias: loc:NineBoxRatingId, dom:EmpPotentialAssessment_NineBoxRatingId -->

**ما هو.** تصنيف المصفوفة التساعية.

**مثال.** نجم مستقبلي

**القواعد.** إلزامي. يُختار من قائمة تقديرات المصفوفة التساعية.

:::admin
تُدار من إدارة التعاقب الوظيفي.
:::
