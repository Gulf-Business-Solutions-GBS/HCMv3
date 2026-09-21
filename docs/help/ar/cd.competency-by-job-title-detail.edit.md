---
helpId: cd.competency-by-job-title-detail.edit
lang: ar
title: تفاصيل الكفاءات حسب المسمى الوظيفي — إنشاء / تحرير
entity: HCMv3.CompetencyDevelopment.CompetencyByJobTitleDetail
surfaces:
  - angular/src/app/admin/competency-development/competency-by-job-title-details/create-or-edit-competency-by-job-title-detail-modal.component.html
audience: [admin]
permissions: []
---

## Overview

كفاءة واحدة متوقعة من مسمى وظيفي، ووزنها. لا تُدرج الكفاءة إلا مرة واحدة لكل مسمى وظيفي، ولا يجوز أن يتجاوز مجموع الأوزان 100.

## CompetencyByJobTitle {#CompetencyByJobTitleDetail.CompetencyByJobTitleId}
<!-- alias: loc:CompetencyByJobTitle, dom:CompetencyByJobTitleDetail_CompetencyByJobTitleId -->

**ما هو.** قائمة كفاءات المسمى الوظيفي.

**القواعد.** إلزامي. يُختار من قائمة الكفاءات حسب المسمى الوظيفي.

## Competency {#CompetencyByJobTitleDetail.CompetencyId}
<!-- alias: loc:Competency, dom:CompetencyByJobTitleDetail_CompetencyId -->

**ما هو.** الكفاءة.

**مثال.** التفاوض

**القواعد.** إلزامي. يُختار من قائمة الكفاءات.

## Weight {#CompetencyByJobTitleDetail.Weight}
<!-- alias: loc:Weight, dom:CompetencyByJobTitleDetail_Weight -->

**ما هو.** مقدار أهمية الكفاءة، كنسبة مئوية.

**مثال.** 25

**القواعد.** إلزامي. رقم.

## Target {#CompetencyByJobTitleDetail.Target}
<!-- alias: loc:Target, dom:CompetencyByJobTitleDetail_Target -->

**ما هو.** ما هو متوقع.

**القواعد.** اختياري. الحد الأقصى 512 حرفاً.
