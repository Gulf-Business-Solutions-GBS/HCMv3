---
helpId: tm.time-policy.edit
lang: ar
title: سياسات الوقت — إنشاء / تحرير
entity: HCMv3.TimeManagement.TimePolicy
surfaces:
  - angular/src/app/admin/time-management/time-policies/create-or-edit-time-policy-modal.component.html
audience: [admin]
permissions: [Pages.TimeManagement.TimePolicies.Create]
---

## Overview

طريقة احتساب الحضور لموظفي هذه السياسة: هل يأتي من البصمات أو من خطة أو من كشوف الدوام، وأي البصمات مطلوبة، وكيف يُسمح بالتأخير والانصراف المبكر، والحد الأدنى للعمل الإضافي.

ويحصل الموظفون على سياسة الوقت في سجلات سياسة الوقت الخاصة بهم. وتحدد **حالة إدارة الوقت** الطريقة: *الوقت الفعلي* يقرأ البصمات حول بداية المناوبة ونهايتها، و*الوقت المرن* يقرؤها ضمن نافذة المرونة في الدوام، و*الوقت المخطط* يعدّ المناوبة المخططة وقتاً معمولاً، وأي حالة أخرى، ومنها *كشف الدوام*، تُحتسب من كشوف الدوام. أما *الوقت المتغير* فلا يعالجه محرك الوقت -- إذ تُتجاوز أيامه برسالة.

وإعدادات موقع البصمة وإشعارات البصمة في آخر النموذج لا تُقرأ حالياً.

## ShouldSignIn {#TimePolicy.ShouldSignIn}
<!-- alias: loc:ShouldSignIn, dom:TimePolicy_ShouldSignIn -->

**ما هو.** ما إذا كان يجب على موظفي هذه السياسة تسجيل بصمة الدخول في بداية المناوبة.

**لماذا يهم.** إذا أُشِّر عليه يُحتسب يوم العمل الذي تنقصه هذه البصمة غياباً عن وقت العمل المخطط، ولا يُقاس التأخير أو الانصراف المبكر إلا عند وجود البصمة. وإلا فلا تُتوقع البصمة.

**القواعد.** نعم أو لا.

## ShouldSignOut {#TimePolicy.ShouldSignOut}
<!-- alias: loc:ShouldSignOut, dom:TimePolicy_ShouldSignOut -->

**ما هو.** ما إذا كان يجب على موظفي هذه السياسة تسجيل بصمة الخروج في نهاية المناوبة.

**لماذا يهم.** إذا أُشِّر عليه يُحتسب يوم العمل الذي تنقصه هذه البصمة غياباً عن وقت العمل المخطط، ولا يُقاس التأخير أو الانصراف المبكر إلا عند وجود البصمة. وإلا فلا تُتوقع البصمة.

**القواعد.** نعم أو لا.

## ShouldBreakSignIn {#TimePolicy.ShouldBreakSignIn}
<!-- alias: loc:ShouldBreakSignIn, dom:TimePolicy_ShouldBreakSignIn -->

**ما هو.** ما إذا كان يجب على موظفي هذه السياسة تسجيل بصمة العودة من الاستراحة.

**لماذا يهم.** إذا أُشِّر عليه يُحتسب يوم العمل الذي تنقصه هذه البصمة غياباً عن وقت العمل المخطط، ولا يُقاس التأخير أو الانصراف المبكر إلا عند وجود البصمة. وإلا فلا تُتوقع البصمة.

**القواعد.** نعم أو لا.

## ShouldBreakSignOut {#TimePolicy.ShouldBreakSignOut}
<!-- alias: loc:ShouldBreakSignOut, dom:TimePolicy_ShouldBreakSignOut -->

**ما هو.** ما إذا كان يجب على موظفي هذه السياسة تسجيل بصمة الخروج إلى الاستراحة.

**لماذا يهم.** إذا أُشِّر عليه يُحتسب يوم العمل الذي تنقصه هذه البصمة غياباً عن وقت العمل المخطط، ولا يُقاس التأخير أو الانصراف المبكر إلا عند وجود البصمة. وإلا فلا تُتوقع البصمة.

**القواعد.** نعم أو لا.

## MarkWeekoffOnHoliday {#TimePolicy.MarkWeekoffOnHoliday}
<!-- alias: loc:MarkWeekoffOnHoliday, dom:TimePolicy_MarkWeekoffOnHoliday -->

**ما هو.** ما إذا كانت العطلة الرسمية الواقعة في يوم راحة أسبوعية تُعدّ راحة أسبوعية.

**لماذا يهم.** إذا أُشِّر عليه، وفقط إذا كان الراتب الأساسي (نوع الأجر `1000`) على *أيام ثابتة شهرياً*، يُعامل هذا اليوم راحةً أسبوعية لا عطلة رسمية.

**القواعد.** نعم أو لا.

## IsFaceRequired {#TimePolicy.IsFaceRequired}
<!-- alias: loc:IsFaceRequired, dom:TimePolicy_IsFaceRequired -->

**ما هو.** ما إذا كان التعرف على الوجه مطلوباً عند البصمة.

**لماذا يهم.** لا يُقرأ حالياً.

**القواعد.** نعم أو لا.

## CalcLateInAfterTolerance {#TimePolicy.CalcLateInAfterTolerance}
<!-- alias: loc:CalcLateInAfterTolerance, dom:TimePolicy_CalcLateInAfterTolerance -->

**ما هو.** ما إذا كان التأخير الذي يتجاوز السماح يُخصم عن الدقائق الزائدة عليه فقط.

**لماذا يهم.** إذا أُشِّر عليه يُخصم عن التأخر 15 دقيقة مع سماح 10 دقائق 5 دقائق فقط، وإلا فتُخصم الدقائق الخمس عشرة كلها.

**القواعد.** نعم أو لا.

## LateInTolerance {#TimePolicy.LateInTolerance}
<!-- alias: loc:LateInTolerance, dom:TimePolicy_LateInTolerance -->

**ما هو.** دقائق التأخير المسموح بها قبل أي خصم.

**مثال.** `10`

**لماذا يهم.** لا يُخصم التأخير حتى هذا العدد من الدقائق. وما زاد عليه يُخصم التأخير كله -- أو الجزء الزائد على السماح فقط، إذا أُشِّر على خانة **بعد السماح** المقابلة.

**القواعد.** اختياري. رقم.

## CalcEarlyOutAfterTolerance {#TimePolicy.CalcEarlyOutAfterTolerance}
<!-- alias: loc:CalcEarlyOutAfterTolerance, dom:TimePolicy_CalcEarlyOutAfterTolerance -->

**ما هو.** ما إذا كان الانصراف المبكر الذي يتجاوز السماح يُخصم عن الدقائق الزائدة عليه فقط.

**لماذا يهم.** إذا أُشِّر عليه يُخصم عن التأخر 15 دقيقة مع سماح 10 دقائق 5 دقائق فقط، وإلا فتُخصم الدقائق الخمس عشرة كلها.

**القواعد.** نعم أو لا.

## EarlyOutTolerance {#TimePolicy.EarlyOutTolerance}
<!-- alias: loc:EarlyOutTolerance, dom:TimePolicy_EarlyOutTolerance -->

**ما هو.** دقائق الانصراف المبكر المسموح بها قبل أي خصم.

**مثال.** `10`

**لماذا يهم.** لا يُخصم الانصراف المبكر حتى هذا العدد من الدقائق. وما زاد عليه يُخصم الانصراف المبكر كله -- أو الجزء الزائد على السماح فقط، إذا أُشِّر على خانة **بعد السماح** المقابلة.

**القواعد.** اختياري. رقم.

## CalcBreakLateInAfterTolerance {#TimePolicy.CalcBreakLateInAfterTolerance}
<!-- alias: loc:CalcBreakLateInAfterTolerance, dom:TimePolicy_CalcBreakLateInAfterTolerance -->

**ما هو.** ما إذا كان التأخر في العودة من الاستراحة الذي يتجاوز السماح يُخصم عن الدقائق الزائدة عليه فقط.

**لماذا يهم.** إذا أُشِّر عليه يُخصم عن التأخر 15 دقيقة مع سماح 10 دقائق 5 دقائق فقط، وإلا فتُخصم الدقائق الخمس عشرة كلها.

**القواعد.** نعم أو لا.

## BreakLateInTolerance {#TimePolicy.BreakLateInTolerance}
<!-- alias: loc:BreakLateInTolerance, dom:TimePolicy_BreakLateInTolerance -->

**ما هو.** دقائق التأخر في العودة من الاستراحة المسموح بها قبل أي خصم.

**مثال.** `5`

**لماذا يهم.** لا يُخصم التأخر في العودة من الاستراحة حتى هذا العدد من الدقائق. وما زاد عليه يُخصم التأخر في العودة من الاستراحة كله -- أو الجزء الزائد على السماح فقط، إذا أُشِّر على خانة **بعد السماح** المقابلة.

**القواعد.** اختياري. رقم.

## CalcBreakEarlyOutAfterTolerance {#TimePolicy.CalcBreakEarlyOutAfterTolerance}
<!-- alias: loc:CalcBreakEarlyOutAfterTolerance, dom:TimePolicy_CalcBreakEarlyOutAfterTolerance -->

**ما هو.** ما إذا كان الخروج المبكر إلى الاستراحة الذي يتجاوز السماح يُخصم عن الدقائق الزائدة عليه فقط.

**لماذا يهم.** إذا أُشِّر عليه يُخصم عن التأخر 15 دقيقة مع سماح 10 دقائق 5 دقائق فقط، وإلا فتُخصم الدقائق الخمس عشرة كلها.

**القواعد.** نعم أو لا.

## BreakEarlyOutTolerance {#TimePolicy.BreakEarlyOutTolerance}
<!-- alias: loc:BreakEarlyOutTolerance, dom:TimePolicy_BreakEarlyOutTolerance -->

**ما هو.** دقائق الخروج المبكر إلى الاستراحة المسموح بها قبل أي خصم.

**مثال.** `5`

**لماذا يهم.** لا يُخصم الخروج المبكر إلى الاستراحة حتى هذا العدد من الدقائق. وما زاد عليه يُخصم الخروج المبكر إلى الاستراحة كله -- أو الجزء الزائد على السماح فقط، إذا أُشِّر على خانة **بعد السماح** المقابلة.

**القواعد.** اختياري. رقم.

## MonthlyToleranceType {#TimePolicy.MonthlyToleranceType}
<!-- alias: loc:MonthlyToleranceType, dom:TimePolicy_MonthlyToleranceType -->

**ما هو.** ما إذا كان التأخير والانصراف المبكر يُسمح بهما أيضاً على مدى الشهر، وعلى أي رقم.

**مثال.** احتساب فترة السماح الشهرية على الخصم

**لماذا يهم.** في نهاية كل شهر يجمع محرك الوقت دقائق تأخير الموظف وانصرافه المبكر -- الدقائق المخصومة (*على الخصم*)، أو الدقائق المخصومة مع سماح كل يوم (*على الفعلي*) -- ولا يُحمَّل خصماً للشهر إلا ما زاد من المجموع على **فترة السماح الشهرية بعد**. و*غير محدد* يلغي ذلك.

**القواعد.** إلزامي. إحدى القيم: لم يتم اختياره، احتساب فترة السماح الشهرية على الفعلي، احتساب فترة السماح الشهرية على الخصم.

## MonthlyAfterTolerance {#TimePolicy.MonthlyAfterTolerance}
<!-- alias: loc:MonthlyAfterTolerance, dom:TimePolicy_MonthlyAfterTolerance -->

**ما هو.** دقائق التأخير والانصراف المبكر المسموح بها على مدى الشهر.

**مثال.** `60`

**لماذا يهم.** يُستخدم مع نوع فترة السماح الشهرية، ولا يُخصم إلا ما زاد عليه من مجموع الشهر.

**القواعد.** اختياري. رقم.

## MonthlyLateInTolerance {#TimePolicy.MonthlyLateInTolerance}
<!-- alias: loc:MonthlyLateInTolerance, dom:TimePolicy_MonthlyLateInTolerance -->

**ما هو.** دقائق التأخير المسموح بها شهرياً.

**لماذا يهم.** لا يستخدمه إلا تقرير الحضور الشهري مع فترة السماح المخصص (019)، ولا يقرؤه محرك الوقت.

**القواعد.** اختياري. رقم.

## MonthlyEarlyOutTolerance {#TimePolicy.MonthlyEarlyOutTolerance}
<!-- alias: loc:MonthlyEarlyOutTolerance, dom:TimePolicy_MonthlyEarlyOutTolerance -->

**ما هو.** دقائق الانصراف المبكر المسموح بها شهرياً.

**لماذا يهم.** لا يستخدمه إلا تقرير الحضور الشهري مع فترة السماح المخصص (019)، ولا يقرؤه محرك الوقت.

**القواعد.** اختياري. رقم.

## ClockInLimit {#TimePolicy.ClockInLimit}
<!-- alias: loc:ClockInLimit, dom:TimePolicy_ClockInLimit -->

**ما هو.** عدد الدقائق قبل بداية المناوبة وبعده التي يبحث فيها محرك الوقت عن تلك البصمة.

**مثال.** `120`

**لماذا يهم.** البصمات خارج النافذة لا تُعدّ تلك البصمة. وفي العطلات الرسمية وأيام الراحة الأسبوعية وأيام الراحة يُبحث في اليوم كله بدلاً من ذلك.

**القواعد.** اختياري. رقم.

## ClockOutLimit {#TimePolicy.ClockOutLimit}
<!-- alias: loc:ClockOutLimit, dom:TimePolicy_ClockOutLimit -->

**ما هو.** عدد الدقائق قبل نهاية المناوبة وبعده التي يبحث فيها محرك الوقت عن تلك البصمة.

**مثال.** `240`

**لماذا يهم.** البصمات خارج النافذة لا تُعدّ تلك البصمة. وفي العطلات الرسمية وأيام الراحة الأسبوعية وأيام الراحة يُبحث في اليوم كله بدلاً من ذلك.

**القواعد.** اختياري. رقم.

## BreakInLimit {#TimePolicy.BreakInLimit}
<!-- alias: loc:BreakInLimit, dom:TimePolicy_BreakInLimit -->

**ما هو.** عدد الدقائق قبل نهاية الاستراحة وبعدها التي يبحث فيها محرك الوقت عن بصمة العودة من الاستراحة.

**مثال.** `30`

**القواعد.** اختياري. رقم.

## BreakOutLimit {#TimePolicy.BreakOutLimit}
<!-- alias: loc:BreakOutLimit, dom:TimePolicy_BreakOutLimit -->

**ما هو.** عدد الدقائق قبل بداية الاستراحة وبعدها التي يبحث فيها محرك الوقت عن بصمة الخروج إلى الاستراحة.

**مثال.** `30`

**القواعد.** اختياري. رقم.

## OtMinLimit {#TimePolicy.OTMinLimit}
<!-- alias: loc:OtMinLimit, dom:TimePolicy_OtMinLimit -->

**ما هو.** أقل عمل إضافي يُحتسب، بالدقائق.

**مثال.** `30`

**لماذا يهم.** يُتجاهل العمل الإضافي الأقصر من هذا في اليوم.

**القواعد.** اختياري. رقم.

## TimeEventRule {#TimePolicy.TimeEventRule}
<!-- alias: loc:TimeEventRule, dom:TimePolicy_TimeEventRule -->

**ما هو.** أي بصمة تُحتسب عند وجود عدة بصمات في النافذة.

**مثال.** أول دخول وآخر خروج

**لماذا يهم.** أول دخول أو آخره، وأول خروج أو آخره، بحسب الاسم. أما *غير منطبق* فلا يأخذ أي بصمة، فيبدو كل يوم بلا بصمات.

**القواعد.** إلزامي. إحدى القيم: لا ينطبق، أول دخول وأول خروج، أول دخول وآخر خروج، آخر دخول وأول خروج، آخر دخول وآخر خروج.

## TimeMgmtStatus {#TimePolicy.TimeMgmtStatus}
<!-- alias: loc:TimeMgmtStatus, dom:TimePolicy_TimeMgmtStatus -->

**ما هو.** طريقة احتساب الحضور لموظفي هذه السياسة.

**مثال.** الوقت الفعلي

**لماذا يهم.** انظر النظرة العامة لما تفعله كل حالة.

**القواعد.** إلزامي. إحدى القيم: لا ينطبق، الوقت المخطط، الوقت الفعلي، كشف الدوام، الوقت المرن، الوقت المتغير.

## AllowPunchInFromWorkLocationOnly {#TimePolicy.AllowPunchInFromWorkLocationOnly}
<!-- alias: loc:AllowPunchInFromWorkLocationOnly, dom:TimePolicy_AllowPunchInFromWorkLocationOnly -->

**ما هو.** ما إذا كان تسجيل الدخول مسموحاً من موقع العمل فقط.

**لماذا يهم.** لا يُقرأ حالياً.

**القواعد.** نعم أو لا.

## AllowPunchOutFromWorkLocationOnly {#TimePolicy.AllowPunchOutFromWorkLocationOnly}
<!-- alias: loc:AllowPunchOutFromWorkLocationOnly, dom:TimePolicy_AllowPunchOutFromWorkLocationOnly -->

**ما هو.** ما إذا كان تسجيل الخروج مسموحاً من موقع العمل فقط.

**لماذا يهم.** لا يُقرأ حالياً.

**القواعد.** نعم أو لا.

## AllowPunchInFromExtWorklocation {#TimePolicy.AllowPunchInFromExtWorklocation}
<!-- alias: loc:AllowPunchInFromExtWorklocation, dom:TimePolicy_AllowPunchInFromExtWorklocation -->

**ما هو.** ما إذا كان تسجيل الدخول مسموحاً من موقع عمل خارجي.

**لماذا يهم.** لا يُقرأ حالياً.

**القواعد.** نعم أو لا.

## AllowPunchOutFromExtWorklocation {#TimePolicy.AllowPunchOutFromExtWorklocation}
<!-- alias: loc:AllowPunchOutFromExtWorklocation, dom:TimePolicy_AllowPunchOutFromExtWorklocation -->

**ما هو.** ما إذا كان تسجيل الخروج مسموحاً من موقع عمل خارجي.

**لماذا يهم.** لا يُقرأ حالياً.

**القواعد.** نعم أو لا.

## IsMissedPunchInNotiEnabled {#TimePolicy.IsMissedPunchInNotiEnabled}
<!-- alias: loc:IsMissedPunchInNotiEnabled, dom:TimePolicy_IsMissedPunchInNotiEnabled -->

**ما هو.** ما إذا كان عدم تسجيل الدخول يرسل إشعاراً.

**لماذا يهم.** لا يُقرأ حالياً.

**القواعد.** نعم أو لا.

## MissedPunchInNotiTimeLimit {#TimePolicy.MissedPunchInNotiTimeLimit}
<!-- alias: loc:MissedPunchInNotiTimeLimit, dom:TimePolicy_MissedPunchInNotiTimeLimit -->

**ما هو.** الدقائق بعد بداية المناوبة قبل الإشعار بعدم تسجيل الدخول.

**لماذا يهم.** لا يُقرأ حالياً.

**القواعد.** اختياري. رقم.

## IsLatePunchInNotiEnabled {#TimePolicy.IsLatePunchInNotiEnabled}
<!-- alias: loc:IsLatePunchInNotiEnabled, dom:TimePolicy_IsLatePunchInNotiEnabled -->

**ما هو.** ما إذا كان تسجيل الدخول المتأخر يرسل إشعاراً.

**لماذا يهم.** لا يُقرأ حالياً.

**القواعد.** نعم أو لا.

## LatePunchInNotiTimeLimit {#TimePolicy.LatePunchInNotiTimeLimit}
<!-- alias: loc:LatePunchInNotiTimeLimit, dom:TimePolicy_LatePunchInNotiTimeLimit -->

**ما هو.** دقائق التأخير قبل الإشعار به.

**لماذا يهم.** لا يُقرأ حالياً.

**القواعد.** اختياري. رقم.

## IsMissedPunchOutNotiEnabled {#TimePolicy.IsMissedPunchOutNotiEnabled}
<!-- alias: loc:IsMissedPunchOutNotiEnabled, dom:TimePolicy_IsMissedPunchOutNotiEnabled -->

**ما هو.** ما إذا كان عدم تسجيل الخروج يرسل إشعاراً.

**لماذا يهم.** لا يُقرأ حالياً.

**القواعد.** نعم أو لا.

## MissedPunchOutNotiTimeLimit {#TimePolicy.MissedPunchOutNotiTimeLimit}
<!-- alias: loc:MissedPunchOutNotiTimeLimit, dom:TimePolicy_MissedPunchOutNotiTimeLimit -->

**ما هو.** الدقائق بعد نهاية المناوبة قبل الإشعار بعدم تسجيل الخروج.

**لماذا يهم.** لا يُقرأ حالياً.

**القواعد.** اختياري. رقم.

## IsEarlyPunchOutNotiEnabled {#TimePolicy.IsEarlyPunchOutNotiEnabled}
<!-- alias: loc:IsEarlyPunchOutNotiEnabled, dom:TimePolicy_IsEarlyPunchOutNotiEnabled -->

**ما هو.** ما إذا كان تسجيل الخروج المبكر يرسل إشعاراً.

**لماذا يهم.** لا يُقرأ حالياً.

**القواعد.** نعم أو لا.

## EarlyPunchOutNotiTimeLimit {#TimePolicy.EarlyPunchOutNotiTimeLimit}
<!-- alias: loc:EarlyPunchOutNotiTimeLimit, dom:TimePolicy_EarlyPunchOutNotiTimeLimit -->

**ما هو.** دقائق الانصراف المبكر قبل الإشعار به.

**لماذا يهم.** لا يُقرأ حالياً.

**القواعد.** اختياري. رقم.
