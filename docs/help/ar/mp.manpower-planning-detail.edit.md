---
helpId: mp.manpower-planning-detail.edit
lang: ar
title: تفاصيل خطط القوى العاملة — إنشاء / تحرير
entity: HCMv3.ManpowerPlanningManagement.ManpowerPlanningDetail
surfaces:
  - angular/src/app/admin/manpower-planning-management/manpower-planning-detail/create-or-edit-manpower-planning-detail-modal.component.html
audience: [admin]
permissions: [Pages.ManpowerPlanningManagement.ManpowerPlanningDetails.Create]
---

## Overview

مسمى وظيفي واحد في خطة القوى العاملة، مع ميزانيته لكل شهر. وبعد أن تصبح للسطر وظيفة، يجب أن يقع مبلغ كل شهر (غير 0) بين الحد الأدنى والأقصى لراتب تلك الوظيفة.

## JobTitle {#ManpowerPlanningDetail.JobTitleId}
<!-- alias: loc:JobTitle, dom:ManpowerPlanningDetail_JobTitleId -->

**ما هو.** المسمى الوظيفي المطلوب.

**مثال.** محاسب

**لماذا يهم.** عند الموافقة يُسمّي الوظيفة الجديدة ويصبح مسماها الوظيفي.

**القواعد.** إلزامي. يُختار من قائمة المسميات الوظيفية.

## Holder {#ManpowerPlanningDetail.HolderId}
<!-- alias: loc:Holder, dom:ManpowerPlanningDetail_HolderId -->

**ما هو.** الموظف المتوقع أن يشغل الوظيفة.

**لماذا يهم.** لا يُقرأ حالياً.

**القواعد.** اختياري. يُختار من قائمة الموظفين.

## January {#ManpowerPlanningDetail.January}
<!-- alias: loc:January, dom:ManpowerPlanningDetail_January -->

**ما هو.** المبلغ الشهري المعتمد في الميزانية لشهر يناير.

**مثال.** 850.000

**لماذا يهم.** عند الموافقة على الخطة، يُضبط الحد الأدنى والمتوسط والأقصى لراتب الوظيفة الجديدة جميعها على هذا المبلغ.

**القواعد.** اختياري. رقم.

## February {#ManpowerPlanningDetail.February}
<!-- alias: loc:February, dom:ManpowerPlanningDetail_February -->

**ما هو.** المبلغ الشهري المعتمد في الميزانية لشهر فبراير.

**القواعد.** اختياري. رقم.

## March {#ManpowerPlanningDetail.March}
<!-- alias: loc:March, dom:ManpowerPlanningDetail_March -->

**ما هو.** المبلغ الشهري المعتمد في الميزانية لشهر مارس.

**القواعد.** اختياري. رقم.

## April {#ManpowerPlanningDetail.April}
<!-- alias: loc:April, dom:ManpowerPlanningDetail_April -->

**ما هو.** المبلغ الشهري المعتمد في الميزانية لشهر أبريل.

**القواعد.** اختياري. رقم.

## May {#ManpowerPlanningDetail.May}
<!-- alias: loc:May, dom:ManpowerPlanningDetail_May -->

**ما هو.** المبلغ الشهري المعتمد في الميزانية لشهر مايو.

**القواعد.** اختياري. رقم.

## June {#ManpowerPlanningDetail.June}
<!-- alias: loc:June, dom:ManpowerPlanningDetail_June -->

**ما هو.** المبلغ الشهري المعتمد في الميزانية لشهر يونيو.

**القواعد.** اختياري. رقم.

## July {#ManpowerPlanningDetail.July}
<!-- alias: loc:July, dom:ManpowerPlanningDetail_July -->

**ما هو.** المبلغ الشهري المعتمد في الميزانية لشهر يوليو.

**القواعد.** اختياري. رقم.

## August {#ManpowerPlanningDetail.August}
<!-- alias: loc:August, dom:ManpowerPlanningDetail_August -->

**ما هو.** المبلغ الشهري المعتمد في الميزانية لشهر أغسطس.

**القواعد.** اختياري. رقم.

## September {#ManpowerPlanningDetail.September}
<!-- alias: loc:September, dom:ManpowerPlanningDetail_September -->

**ما هو.** المبلغ الشهري المعتمد في الميزانية لشهر سبتمبر.

**القواعد.** اختياري. رقم.

## October {#ManpowerPlanningDetail.October}
<!-- alias: loc:October, dom:ManpowerPlanningDetail_October -->

**ما هو.** المبلغ الشهري المعتمد في الميزانية لشهر أكتوبر.

**القواعد.** اختياري. رقم.

## November {#ManpowerPlanningDetail.November}
<!-- alias: loc:November, dom:ManpowerPlanningDetail_November -->

**ما هو.** المبلغ الشهري المعتمد في الميزانية لشهر نوفمبر.

**القواعد.** اختياري. رقم.

## December {#ManpowerPlanningDetail.December}
<!-- alias: loc:December, dom:ManpowerPlanningDetail_December -->

**ما هو.** المبلغ الشهري المعتمد في الميزانية لشهر ديسمبر.

**القواعد.** اختياري. رقم.
