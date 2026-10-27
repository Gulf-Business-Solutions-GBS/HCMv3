---
helpId: pay.ticket-type.edit
lang: ar
title: أنواع التذاكر — إنشاء / تحرير
entity: HCMv3.PayrollManagement.TicketType
surfaces:
  - angular/src/app/admin/payroll-management/ticket-types/create-or-edit-ticket-type-modal.component.html
audience: [admin]
permissions: [Pages.PayrollManagement.TicketTypes.Create]
---

## Overview

نوع من تذاكر الطيران -- تذكرة الموظف نفسه، أو تذكرة لنوع من العائلين -- وطريقة تراكم استحقاقها وسقوطه. نوع التذكرة `1` هو تذكرة الموظف نفسه، والبقية تذاكر العائلين. أما عدد التذاكر وقيمتها فتُحدَّد لكل مجموعة فرعية.

## TicketBasisPeriod {#TicketType.TicketBasisPeriod}
<!-- alias: loc:TicketBasisPeriod, dom:TicketType_TicketBasisPeriod -->

**ما هو.** ما إذا كانت قيمة التذكرة على أساس أيام التقويم أو عدد ثابت من الأيام.

**لماذا يهم.** لا يُقرأ حالياً.

**القواعد.** إلزامي. إحدى القيم: أيام التقويم، أيام ثابتة.

## TicketBasisFixedDays {#TicketType.TicketBasisFixedDays}
<!-- alias: loc:TicketBasisFixedDays, dom:TicketType_TicketBasisFixedDays -->

**ما هو.** العدد الثابت من الأيام، لأساس الأيام الثابتة.

**لماذا يهم.** لا يُقرأ حالياً.

**القواعد.** اختياري. رقم.

## Lapse {#TicketType.Lapse}
<!-- alias: loc:Lapse, dom:TicketType_Lapse -->

**ما هو.** ما إذا كان استحقاق التذاكر غير المستخدم يسقط.

**لماذا يهم.** إذا أُشِّر عليه يسري استحقاق التذاكر في دورات مدة كل منها **السقوط بعد** سنة من بداية فترة الاستحقاق، ويبدأ من جديد في كل دورة. وإلا فلا تنتهي الدورة الأولى أبداً.

**القواعد.** نعم أو لا.

## LapseAfter {#TicketType.LapseAfter}
<!-- alias: loc:LapseAfter, dom:TicketType_LapseAfter -->

**ما هو.** عدد السنوات التي تستمر فيها كل دورة تذاكر قبل أن يسقط الاستحقاق غير المستخدم.

**مثال.** `2`

**القواعد.** اختياري. رقم صحيح.

:::admin
إذا أُشِّر على **السقوط** فاجعل هذه القيمة 1 على الأقل. فالنموذج يقبل 0، لكن 0 يجعل احتساب التذاكر لا ينتهي أبداً، مما يوقف محرك الوقت للموظفين الذين لهم نوع التذكرة هذا.
:::

## StopDependantAccrualAfter {#TicketType.StopDependantAccrualAfter}
<!-- alias: loc:StopDependantAccrualAfter, dom:TicketType_StopDependantAccrualAfter -->

**ما هو.** متى يتوقف تراكم تذاكر العائلين.

**لماذا يهم.** لا يُقرأ حالياً.

**القواعد.** اختياري. رقم صحيح.

## MinCompensation {#TicketType.MinCompensation}
<!-- alias: loc:MinCompensation, dom:TicketType_MinCompensation -->

**ما هو.** أقل ما يُصرف عند تعويض التذكرة نقداً.

**لماذا يهم.** لا يُقرأ حالياً.

**القواعد.** اختياري. رقم.

## DeductionPeriod {#TicketType.DeductionPeriod}
<!-- alias: loc:DeductionPeriod, dom:TicketType_DeductionPeriod -->

**ما هو.** ما إذا كانت التذكرة المستخدمة تُخصم من الاستحقاق الحالي أو التالي.

**لماذا يهم.** لا يُقرأ حالياً.

**القواعد.** إلزامي. إحدى القيم: بعد الاستحقاق، مطابق للاستحقاق.

## QuotaEntitlement {#TicketType.QuotaEntitlement}
<!-- alias: loc:QuotaEntitlement, dom:TicketType_QuotaEntitlement -->

**ما هو.** ما إذا كانت التذاكر تتراكم خلال الفترة أو تتاح فوراً.

**مثال.** فترة

**لماذا يهم.** *فترة* تجعل محرك الوقت يراكم الاستحقاق قليلاً كل يوم. و*فوري* لا يراكم شيئاً يوماً بيوم.

**القواعد.** إلزامي. إحدى القيم: فترة، فوري.

## WageTypeId {#TicketType.WageTypeId}
<!-- alias: loc:WageTypeId, dom:TicketType_WageTypeId -->

**ما هو.** نوع الأجر لنوع التذكرة هذا.

**مثال.** تذكرة طيران

**لماذا يهم.** يجب ملؤه للحفظ، لكن لا شيء يقرؤه حالياً.

**القواعد.** اختياري. يُختار من قائمة أنواع الأجور.
