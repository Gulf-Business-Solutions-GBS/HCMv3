---
helpId: pay.employee-sub-group-absence.edit
lang: ar
title: غيابات المجموعة الفرعية للموظفين — إنشاء / تحرير
entity: HCMv3.PayrollManagement.EmployeeSubGroupAbsence
surfaces:
  - angular/src/app/admin/payroll-management/employee-sub-group-detail/absences/create-or-edit-employee-sub-group-absence-modal.component.html
audience: [admin]
permissions: [Pages.PayrollManagement.EmployeeSubGroupAbsences.Create]
---

## Overview

استحقاق المجموعة الفرعية من نوع واحد من الإجازات، اعتباراً من تاريخ معيّن: كم يوماً في السنة، وأقصى ما يمكن تجميعه، و-- للإجازة المرضية -- قاعدة الخصم.

ويمكن لعدة أسطر لنوع الإجازة نفسه أن ترفع الاستحقاق بحسب مدة الخدمة. فكل سطر يسري ما دامت خدمة الموظف أقل من **تغيير السنة** فيه، ويُطبَّق السطر الذي تغيير السنة فيه هو الأصغر بين ما يتجاوز خدمة الموظف.

## AbsenceType {#EmployeeSubGroupAbsence.AbsenceTypeId}
<!-- alias: loc:AbsenceType, dom:EmployeeSubGroupAbsence_AbsenceTypeId -->

**ما هو.** نوع الإجازة الذي يخصه هذا الاستحقاق.

**مثال.** الإجازة السنوية

**القواعد.** اختياري. يُختار من قائمة أنواع الغياب.

## AbsenceDeductionRule {#EmployeeSubGroupAbsence.AbsenceDeductionRuleId}
<!-- alias: loc:AbsenceDeductionRule, dom:EmployeeSubGroupAbsence_AbsenceDeductionRuleId -->

**ما هو.** للإجازة المرضية، الخطوات التي يُخفَّض بها الأجر كلما تراكمت الأيام المرضية.

**مثال.** سلّم الإجازة المرضية الكويتي

**لماذا يهم.** لا يُقرأ إلا في سطر الإجازة المرضية للمجموعة الفرعية -- النوع ذو الرمز 3، أو نوع الحصة إجازة مرضية. وإذا تُرك فارغاً تُصرف الإجازة المرضية كاملة.

**القواعد.** اختياري. يُختار من قائمة قواعد الخصم على الغياب.

## ChangeYear {#EmployeeSubGroupAbsence.ChangeYear}
<!-- alias: loc:ChangeYear, dom:EmployeeSubGroupAbsence_ChangeYear -->

**ما هو.** سنوات الخدمة التي يسري هذا السطر حتى بلوغها.

**مثال.** `5` -- 30 يوماً في السنة للسنوات الخمس الأولى، وسطر ثانٍ بـ`99` يعطي 45 يوماً بعدها.

**لماذا يهم.** تُختار الأسطر بحسب خدمة الموظف في ذلك اليوم: يُطبَّق أصغر تغيير سنة لا يزال أكبر منها. وإذا لم ينطبق أي سطر يُستخدم أي سطر لنوع الإجازة سارٍ في ذلك اليوم.

**القواعد.** اختياري. رقم.

## TotalDays {#EmployeeSubGroupAbsence.TotalDays}
<!-- alias: loc:TotalDays, dom:EmployeeSubGroupAbsence_TotalDays -->

**ما هو.** أيام هذه الإجازة التي يستحقها الموظف كل سنة.

**مثال.** `30`

**لماذا يهم.** تتراكم الإجازة السنوية يوماً بيوم بمعدل هذا العدد من الأيام في السنة، وتعطي الإجازة المرضية هذا العدد لكل دورة. ويحل محل **مجموع الأيام المسموح بها** في نوع الغياب لموظفي هذه المجموعة الفرعية.

**القواعد.** اختياري. رقم صحيح.

## MaxLimit {#EmployeeSubGroupAbsence.MaxLimit}
<!-- alias: loc:MaxLimit, dom:EmployeeSubGroupAbsence_MaxLimit -->

**ما هو.** أقصى عدد من أيام هذه الإجازة يمكن أن يحمله الرصيد.

**مثال.** `60`

**لماذا يهم.** يُخفَّض رصيد الإجازة السنوية الذي يتجاوزه إليه أثناء تشغيل محرك الوقت، إذا كانت قاعدة المصادرة في إعدادات الوقت *لا شيء*، وتُحدّ به أيضاً الأرصدة المحتسبة للرواتب. والقيمة 0 تعني بلا حد.

**القواعد.** اختياري. رقم صحيح.

## MaxLimitReachNotificationInDays {#EmployeeSubGroupAbsence.MaxLimitReachNotificationInDays}
<!-- alias: loc:MaxLimitReachNotificationInDays, dom:EmployeeSubGroupAbsence_MaxLimitReachNotificationInDays -->

**ما هو.** كم يجب أن ينخفض الرصيد تحت الحد الأقصى قبل أن يمكن إرسال إشعار "بلوغ الحد الأقصى" مرة أخرى.

**مثال.** `5`

**لماذا يهم.** لا أثر له حالياً: فهو يعيد تهيئة إشعار بلوغ الحد الأقصى للإجازة السنوية فقط، ولا شيء يرسل ذلك الإشعار.

**القواعد.** اختياري. رقم صحيح.
