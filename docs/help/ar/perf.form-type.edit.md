---
helpId: perf.form-type.edit
lang: ar
title: أنواع النماذج — إنشاء / تحرير
entity: HCMv3.PerformanceManagement.FormType
surfaces:
  - angular/src/app/admin/performance-management/form-types/create-or-edit-form-type-modal.component.html
audience: [admin]
permissions: [Pages.PerformanceManagement.FormTypes.Create]
---

## Overview

نوع من نماذج التقييم، مع حدود لعدد المراجعين المسموح بهم لتقييم من هذا النوع. ويحدد كل تقييم نوع نموذجه.

## MinReviewerLimit {#FormType.MinReviewerLimit}
<!-- alias: loc:MinReviewerLimit, dom:FormType_MinReviewerLimit -->

**ما هو.** أقل عدد من المراجعين ينبغي أن يكون للتقييم.

**لماذا يهم.** لا يُقرأ حالياً.

**القواعد.** إلزامي. رقم صحيح.

## MaxReviewerLimit {#FormType.MaxReviewerLimit}
<!-- alias: loc:MaxReviewerLimit, dom:FormType_MaxReviewerLimit -->

**ما هو.** أكبر عدد من المراجعين يجوز أن يكون لتقييم من هذا النوع.

**مثال.** `5`

**لماذا يهم.** يُرفض إضافة مراجع يتجاوزه. والقيمة 0 تعني بلا حد.

**القواعد.** إلزامي. رقم صحيح.
