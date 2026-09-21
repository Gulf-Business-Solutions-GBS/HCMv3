---
helpId: perf.parating.edit
lang: ar
title: تقديرات تقييم الأداء — إنشاء / تحرير
entity: HCMv3.PerformanceManagement.PARating
surfaces:
  - angular/src/app/admin/performance-management/pa-ratings/create-or-edit-pa-rating-modal.component.html
audience: [admin]
permissions: [Pages.PerformanceManagement.PARatings.Create]
---

## Overview

تقدير يمكن للمراجعين إعطاؤه، مع نطاق الدرجة الذي يمثله.

و**رمزه** هو قيمته: إذ تحسب بطاقة الأداء متوسط رموز التقديرات الممنوحة، فيجب أن تكون الرموز أرقاماً، وأعلاها لأفضل تقدير. ويصنّف نطاق **من** و**إلى** الدرجةَ النهائية ضمن هذا التقدير في تقريري بطاقة الأداء وتقييم المدير.

## FromRange {#PARating.FromRange}
<!-- alias: loc:FromRange, dom:PARating_FromRange -->

**ما هو.** أدنى درجة نهائية تُعدّ هذا التقدير.

**مثال.** `70`

**القواعد.** إلزامي. رقم.

## ToRange {#PARating.ToRange}
<!-- alias: loc:ToRange, dom:PARating_ToRange -->

**ما هو.** أعلى درجة نهائية تُعدّ هذا التقدير.

**مثال.** `85`

**القواعد.** إلزامي. رقم.

## MaxLimit {#PARating.MaxLimit}
<!-- alias: loc:MaxLimit, dom:PARating_MaxLimit -->

**ما هو.** أكبر عدد من المرات يجوز فيه إعطاء التقدير في النموذج الواحد.

**لماذا يهم.** لا يُقرأ حالياً.

**القواعد.** إلزامي. رقم صحيح.

## DescriptionEn {#PARating.DescriptionEn}
<!-- alias: loc:DescriptionEn, dom:PARating_DescriptionEn -->

**ما هو.** وصف التقدير بالإنجليزية.

**لماذا يهم.** لا يُقرأ حالياً.

**القواعد.** اختياري. الحد الأقصى 4000 حرف.

## DescriptionAr {#PARating.DescriptionAr}
<!-- alias: loc:DescriptionAr, dom:PARating_DescriptionAr -->

**ما هو.** وصف التقدير بالعربية.

**لماذا يهم.** لا يُقرأ حالياً.

**القواعد.** اختياري. الحد الأقصى 4000 حرف.

## IsCommentRequired {#PARating.IsCommentRequired}
<!-- alias: loc:IsCommentRequired, dom:PARating_IsCommentRequired -->

**ما هو.** ما إذا كان إعطاء هذا التقدير يتطلب تعليقاً.

**لماذا يهم.** لا يُقرأ حالياً.

**القواعد.** نعم أو لا.

## Status {#PARating.Status}
<!-- alias: loc:Status, dom:PARating_Status -->

**ما هو.** ما إذا كان التقدير مفعّلاً.

**لماذا يهم.** لا يُقرأ حالياً.

**القواعد.** نعم أو لا.
