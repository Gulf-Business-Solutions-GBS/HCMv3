---
helpId: pa.emp-performance-history.edit
lang: ar
title: سجل أداء الموظف — إنشاء / تحرير
entity: HCMv3.SuccessionManagement.EmpPerformanceHistory
surfaces:
  - angular/src/app/admin/personnel-administration/employee-detail/performance-histories/create-or-edit-employee-performance-history-modal.component.html
audience: [admin]
permissions: [Pages.SuccessionManagement.EmpPerformanceHistories.Create]
---

## Overview

درجة أداء للموظف خلال فترة، لأغراض تخطيط التعاقب الوظيفي. ولا يجوز أن تتداخل فترات الموظف نفسه.

تُسجَّل للرجوع إليها، ولا يقرؤها أي احتساب أو تقرير حالياً.

## Description {#EmpPerformanceHistory.Description}
<!-- alias: loc:Description, dom:EmpPerformanceHistory_Description -->

**ما هو.** ملاحظات على الدرجة.

**مثال.** تجاوز أهداف المبيعات للسنة.

**القواعد.** اختياري. الحد الأقصى 2000 حرف.

## Score {#EmpPerformanceHistory.Score}
<!-- alias: loc:Score, dom:EmpPerformanceHistory_Score -->

**ما هو.** درجة الأداء.

**مثال.** 4.2

**القواعد.** إلزامي. رقم.

## Source {#EmpPerformanceHistory.Source}
<!-- alias: loc:Source, dom:EmpPerformanceHistory_Source -->

**ما هو.** هل جاءت الدرجة من تقييم فعلي أم أُدخلت يدوياً.

**مثال.** يدوي

**القواعد.** إلزامي. إحدى القيم: منشور، Manual.

## RatingId {#EmpPerformanceHistory.RatingId}
<!-- alias: loc:RatingId, dom:EmpPerformanceHistory_RatingId -->

**ما هو.** تصنيف التعاقب الوظيفي.

**مثال.** أداء مرتفع

**القواعد.** إلزامي. يُختار من قائمة تقديرات التعاقب الوظيفي.

:::admin
تُدار من إدارة التعاقب الوظيفي.
:::

## NineBoxRatingId {#EmpPerformanceHistory.NineBoxRatingId}
<!-- alias: loc:NineBoxRatingId, dom:EmpPerformanceHistory_NineBoxRatingId -->

**ما هو.** تصنيف المصفوفة التساعية.

**مثال.** نجم

**القواعد.** إلزامي. يُختار من قائمة تقديرات المصفوفة التساعية.

:::admin
تُدار من إدارة التعاقب الوظيفي.
:::
