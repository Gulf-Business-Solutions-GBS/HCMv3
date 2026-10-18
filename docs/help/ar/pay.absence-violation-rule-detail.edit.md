---
helpId: pay.absence-violation-rule-detail.edit
lang: ar
title: تفاصيل قواعد مخالفات الغياب — إنشاء / تحرير
entity: HCMv3.PayrollManagement.AbsenceViolationRuleDetail
surfaces:
  - angular/src/app/admin/payroll-management/absence-violation-rule-details/create-or-edit-absence-violation-rule-detail-modal.component.html
audience: [admin]
permissions: [Pages.PayrollManagement.AbsenceViolationRuleDetails.Create]
---

## Overview

خطوة واحدة من قاعدة مخالفة الغياب: بعد كم يوماً، وما الإجراء -- إشعار أو مخالفة تأديبية أو إجازة.

لا يُشغَّل حالياً: لا توجد عملية تصعّد حالات الغياب باستخدام هذه القواعد. فهي تُحفظ للرجوع إليها، ولا يحدث شيء عندما يتغيب الموظف.

## AbsenceViolationRules {#AbsenceViolationRuleDetail.AbsenceViolationRuleId}
<!-- alias: loc:AbsenceViolationRules, dom:AbsenceViolationRuleDetail_AbsenceViolationRuleId -->

**ما هو.** القاعدة التي تتبعها هذه الخطوة.

**مثال.** الغياب دون إذن

**القواعد.** إلزامي. يُختار من قائمة قواعد مخالفات الغياب.

## Step {#AbsenceViolationRuleDetail.Step}
<!-- alias: loc:Step, dom:AbsenceViolationRuleDetail_Step -->

**ما هو.** موقع الخطوة في التصعيد.

**مثال.** `2`

**لماذا يهم.** يجب أن يكون أكبر من صفر، ولا يُستخدم رقم الخطوة إلا مرة واحدة في القاعدة.

**القواعد.** اختياري. رقم صحيح.

## DayInterval {#AbsenceViolationRuleDetail.DayInterval}
<!-- alias: loc:DayInterval, dom:AbsenceViolationRuleDetail_DayInterval -->

**ما هو.** أيام الغياب التي تُطبَّق هذه الخطوة بعدها.

**مثال.** `3`

**القواعد.** اختياري. رقم صحيح.

## Action {#AbsenceViolationRuleDetail.Action}
<!-- alias: loc:Action, dom:AbsenceViolationRuleDetail_Action -->

**ما هو.** ما تفعله الخطوة: إرسال إشعار، أو تسجيل مخالفة تأديبية، أو تسجيل إجازة.

**مثال.** مخالفة

**القواعد.** إلزامي. إحدى القيم: إشعار، المخالفة، الإجازة.

## ActionType {#AbsenceViolationRuleDetail.ActionTypeId}
<!-- alias: loc:ActionType, dom:AbsenceViolationRuleDetail_ActionTypeId -->

**ما هو.** نوع الإجازة أو نوع المخالفة التأديبية الذي يستخدمه الإجراء، بحسب الإجراء.

**مثال.** إجازة غير مدفوعة

**القواعد.** اختياري. يُختار من قائمة.

## NotificationConfig {#AbsenceViolationRuleDetail.NotificationConfigId}
<!-- alias: loc:NotificationConfig, dom:AbsenceViolationRuleDetail_NotificationConfigId -->

**ما هو.** إعداد الإخطار المستخدم لإبلاغ المعنيين عند الوصول إلى هذه الخطوة.

**مثال.** تصعيد الغياب – إبلاغ الموارد البشرية والمدير المباشر

**لماذا يهم.** يتلقى مستلموه إشعاراً داخل النظام، وبريداً إلكترونياً إذا كان للإعداد قالب. وإذا تُرك فارغاً تُنفَّذ الخطوة دون إشعار أحد. ولا يُلغى إجراء الخطوة إذا تعذّر الإشعار.

**القواعد.** اختياري. يُختار من قائمة إعدادات الإخطار.

## HoldTillPunchFoundHint {#AbsenceViolationRuleDetail.HoldTillPunchFound}
<!-- alias: loc:HoldTillPunchFoundHint, dom:AbsenceViolationRuleDetail_HoldTillPunchFound -->

**ما هو.** ما إذا كانت هذه الخطوة تنتظر حتى يسجّل الموظف حضوره مرة أخرى.

**لماذا يهم.** إذا أُشِّر عليه، لا يُطبَّق إجراء الخطوة -- إجازة خصم أو سجل مخالفة -- إلا بعد عودة الموظف ووجود تسجيل حضور معتمد له، فلا يُعاقب من لا يزال غائباً قبل الأوان.

**القواعد.** نعم أو لا.
