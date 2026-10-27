---
helpId: pay.absence-ded-rule-detail.edit
lang: ar
title: تفاصيل قواعد الخصم على الغياب — إنشاء / تحرير
entity: HCMv3.PayrollManagement.AbsenceDedRuleDetail
surfaces:
  - angular/src/app/admin/payroll-management/absence-ded-rule-details/create-or-edit-absence-ded-rule-detail-modal.component.html
audience: [admin]
permissions: [Pages.PayrollManagement.AbsenceDedRuleDetails.Create]
---

## Overview

خطوة واحدة من سلّم الخصم على الإجازة المرضية. يعدّ نظام الرواتب أيام الموظف المرضية خلال الدورة، ويأخذ كل يوم أول خطوة يبلغ **عدد الأيام** فيها رقم ذلك اليوم، ويُصرف بنسبة 100% ناقص خصم تلك الخطوة. والأيام المرضية التي تتجاوز آخر خطوة لا تُصرف إطلاقاً، فاجعل آخر خطوة تمتد إلى أقصى ما يمكن أن تبلغه الإجازة المرضية.

## AbsenceDeductionRules {#AbsenceDedRuleDetail.AbsenceDeductionRuleId}
<!-- alias: loc:AbsenceDeductionRules, dom:AbsenceDedRuleDetail_AbsenceDeductionRuleId -->

**ما هو.** السلّم الذي تتبعه هذه الخطوة.

**مثال.** سلّم الإجازة المرضية الكويتي

**القواعد.** إلزامي. يُختار من قائمة قواعد الخصم على الغياب.

## DaysCount {#AbsenceDedRuleDetail.DaysCount}
<!-- alias: loc:DaysCount, dom:AbsenceDedRuleDetail_DaysCount -->

**ما هو.** اليوم المرضي، معدوداً خلال الدورة، الذي تسري هذه الخطوة حتى بلوغه.

**مثال.** `25` -- الأيام من 16 إلى 25، بعد خطوة أولى بـ`15`.

**لماذا يهم.** تُقرأ الخطوات بترتيب هذا الرقم.

**القواعد.** اختياري. رقم.

## DeductionUnit {#AbsenceDedRuleDetail.DeductionUnit}
<!-- alias: loc:DeductionUnit, dom:AbsenceDedRuleDetail_DeductionUnit -->

**ما هو.** ما إذا كان الخصم نسبة مئوية أو مبلغاً ثابتاً.

**لماذا يهم.** لا يُقرأ حالياً: تُعامل قيمة الخصم دائماً على أنها نسبة مئوية.

**القواعد.** إلزامي. إحدى القيم: ثابت، النسبة المئوية.

## DeductionNumber {#AbsenceDedRuleDetail.DeductionNumber}
<!-- alias: loc:DeductionNumber, dom:AbsenceDedRuleDetail_DeductionNumber -->

**ما هو.** النسبة المئوية التي تُخصم من أجر اليوم عن الأيام المرضية في هذه الخطوة.

**مثال.** `25` -- يُصرف كل يوم بنسبة 75%.

**القواعد.** اختياري. رقم.
