---
helpId: pay.absence-type.edit
lang: ar
title: أنواع الإجازات — إنشاء / تحرير
entity: HCMv3.PayrollManagement.AbsenceType
surfaces:
  - angular/src/app/admin/payroll-management/absence-types/create-or-edit-absence-type-modal.component.html
audience: [admin]
permissions: [Pages.PayrollManagement.AbsenceTypes.Create]
---

## Overview

نوع من الإجازات أو الغياب -- السنوية والمرضية وغير المدفوعة والحج والأمومة وغيرها -- وطريقة عدّه وصرفه واستحقاقه.

ويُحدَّد ما يستحقه كل موظف لكل مجموعة فرعية للموظفين، في قواعد الإجازات للمجموعة الفرعية، أما هذا النموذج فيحدد سلوك النوع نفسه.

**كثير من الإعدادات في هذا النموذج تُحفظ ولا تُطبَّق.** فالحدود والقيود على طلب الإجازة -- الحد الأقصى والأدنى للأيام، والأيام التي لا يجوز أن تبدأ فيها، والمرفقات، وفترة التجربة، وفترة الإنذار، والجنس، والديانة، والرصيد السالب، وحدود الساعات -- لا يُتحقق منها عند إدخال الإجازة أو اعتمادها، وكل حقل من هذا النوع مبيَّن أدناه. ولتطبيق سياسة للإجازات استخدم قاعدة عمل على الإجازة من **إعداد قواعد العمل**، إذ يمكن للقواعد أن تقرأ رمز نوع الإجازة، وهل هي مدفوعة، وهل هي مفعّلة، والحد الأدنى لأيام الخدمة.

## Code {#AbsenceType.Code}
<!-- alias: loc:Code, dom:AbsenceType_Code -->

**ما هو.** رمز نوع الإجازة.

**مثال.** `1` -- الإجازة السنوية.

**لماذا يهم.** يتعرّف نظام الرواتب ومحرك الوقت على بعض أنواع الإجازات برمزها: `1` الإجازة السنوية، و`2` الإجازة غير المدفوعة، و`3` الإجازة المرضية، و`15` الغياب غير المعتمد، ولكل منها قواعده الخاصة في الأرصدة ومكافأة نهاية الخدمة والصرف. فلا تستخدم هذه الرموز إلا لهذه الأنواع.

**القواعد.** اختياري. إذا تُرك فارغاً، يُمنح الرقم التالي عند حفظ السجل. يجب ألا يتكرر في هذه القائمة.

## IsActive {#AbsenceType.IsActive}
<!-- alias: loc:IsActive, dom:AbsenceType_IsActive -->

**ما هو.** ما إذا كان نوع الإجازة مستخدماً.

**لماذا يهم.** لا يُطبَّق حالياً: يبقى النوع غير المفعّل معروضاً عند إدخال الإجازة، بما في ذلك الخدمة الذاتية. ويمكن لقواعد العمل على الإجازة قراءته، فتستطيع قاعدة أن ترفض الإجازة من نوع غير مفعّل.

**القواعد.** نعم أو لا.

## LeaveBasisPeriod {#AbsenceType.LeaveBasisPeriod}
<!-- alias: loc:LeaveBasisPeriod, dom:AbsenceType_LeaveBasisPeriod -->

**ما هو.** ما إذا كان يوم الإجازة يُقيَّم على أساس أيام التقويم أو عدد ثابت من الأيام.

**لماذا يهم.** لا يُتحقق منه حالياً -- يُحفظ الإعداد، لكن لا شيء يطبّقه عند إدخال الإجازة أو اعتمادها.

**القواعد.** إلزامي. إحدى القيم: أيام التقويم، أيام ثابتة.

## LeaveBasisFixedDays {#AbsenceType.LeaveBasisFixedDays}
<!-- alias: loc:LeaveBasisFixedDays, dom:AbsenceType_LeaveBasisFixedDays -->

**ما هو.** العدد الثابت من الأيام، لأساس الأيام الثابتة.

**لماذا يهم.** لا يُتحقق منه حالياً -- يُحفظ الإعداد، لكن لا شيء يطبّقه عند إدخال الإجازة أو اعتمادها.

**القواعد.** اختياري. رقم.

## IsPaid {#AbsenceType.IsPaid}
<!-- alias: loc:IsPaid, dom:AbsenceType_IsPaid -->

**ما هو.** ما إذا كانت الإجازة مدفوعة.

**لماذا يهم.** لا تُصرف مقدماً إلا الإجازة المدفوعة: فالتشغيل خارج الدورة للإجازة المقدمة لا يلتقط إلا إجازات الأنواع المدفوعة المضبوطة على *الدفع مقدماً*. وإذا كان إعداد الرواتب *التحويل من ثابت إلى فعلي عند وجود إجازة غير مدفوعة* مفعّلاً، يُصرف الشهر الذي فيه إجازة من نوع غير مدفوع على أساس الأيام الفعلية بدلاً من الأيام الثابتة. ويمكن لقواعد العمل على الإجازة قراءته.

**القواعد.** نعم أو لا.

## WageType {#AbsenceType.WageTypeId}
<!-- alias: loc:WageType, dom:AbsenceType_WageTypeId -->

**ما هو.** نوع الأجر الذي تُصرف تحته هذه الإجازة.

**مثال.** أجر الإجازة السنوية

**لماذا يهم.** تُصرف دفعة الإجازة لهذا النوع على نوع الأجر هذا، وتحدد طريقة فوترته كيف يُقيَّم يوم الإجازة المستحق. أما أنواع الأجور التي يتكوّن منها المبلغ اليومي فتُحدَّد في **إجازات أنواع الأجور**.

**القواعد.** اختياري. يُختار من قائمة.

## IncludeWeekend {#AbsenceType.IncludeWeekend}
<!-- alias: loc:IncludeWeekend, dom:AbsenceType_IncludeWeekend -->

**ما هو.** ما إذا كان يوم الراحة الأسبوعية الواقع ضمن الإجازة يُحتسب يوم إجازة.

**لماذا يهم.** إذا أُشِّر عليه تُحتسب أيام الراحة الأسبوعية ضمن الإجازة أيامَ إجازة وتُخصم من الرصيد، وإلا فلا.

**القواعد.** نعم أو لا.

## IncludeRestDay {#AbsenceType.IncludeRestDay}
<!-- alias: loc:IncludeRestDay, dom:AbsenceType_IncludeRestDay -->

**ما هو.** ما إذا كان يوم الراحة الواقع ضمن الإجازة يُحتسب يوم إجازة.

**لماذا يهم.** إذا أُشِّر عليه تُحتسب أيام الراحة ضمن الإجازة أيامَ إجازة وتُخصم من الرصيد، وإلا فلا.

**القواعد.** نعم أو لا.

## IncludePublicHoliday {#AbsenceType.IncludePublicHoliday}
<!-- alias: loc:IncludePublicHoliday, dom:AbsenceType_IncludePublicHoliday -->

**ما هو.** ما إذا كانت العطلة الرسمية الواقعة ضمن الإجازة تُحتسب يوم إجازة.

**لماذا يهم.** إذا أُشِّر عليها تُحتسب العطلات الرسمية ضمن الإجازة أيامَ إجازة وتُخصم من الرصيد، وإلا فلا.

**القواعد.** نعم أو لا.

## IncludeInWorkflow {#AbsenceType.IncludeInWorkflow}
<!-- alias: loc:IncludeInWorkflow, dom:AbsenceType_IncludeInWorkflow -->

**ما هو.** ما إذا كانت الإجازة من هذا النوع تمر بموافقة سير العمل.

**لماذا يهم.** لا يُتحقق منه حالياً -- يُحفظ الإعداد، لكن لا شيء يطبّقه عند إدخال الإجازة أو اعتمادها.

**القواعد.** نعم أو لا.

## HaveQuotaAccrual {#AbsenceType.HaveQuotaAccrual}
<!-- alias: loc:HaveQuotaAccrual, dom:AbsenceType_HaveQuotaAccrual -->

**ما هو.** ما إذا كان الموظف يراكم رصيداً من هذه الإجازة مع الوقت.

**لماذا يهم.** إذا أُشِّر عليه يُحتسب الرصيد من سجلات حصص الإجازة للموظف، ويُظهر تقريرا رصيد حصص الإجازات ومخصص الحصص رصيداً متراكماً. وإلا فلا يُحفظ رصيد متراكم.

**القواعد.** نعم أو لا.

## AccrualPeriod {#AbsenceType.AccrualPeriod}
<!-- alias: loc:AccrualPeriod, dom:AbsenceType_AccrualPeriod -->

**ما هو.** الدورة التي يسري عليها رصيد الإجازة.

**مثال.** سنة العمل

**لماذا يهم.** *السنة الميلادية* تبدأ كل دورة في 1 يناير، و*السنة المالية* في 1 أبريل، و*سنة العمل* في ذكرى التحاق الموظف، و*مخصص* في أول يوم من **شهر البداية المخصص**. أما *لكل فترة توظيف* فتجعل دورة واحدة من تاريخ الالتحاق إلى نهاية آخر عقد للموظف -- وتفشل للموظف الذي ليس له عقد. أما *نصف سنوي* و*بلا تراكم* فتُحتسبان من تاريخ الالتحاق مثل *سنة العمل*. وأياً كان الاختيار فإن الدورة تستمر **إعادة التهيئة بعد** سنة.

**القواعد.** إلزامي. إحدى القيم: بلا تراكم، السنة الميلادية، السنة المالية، سنة العمل، نصف سنوي، لكل فترة توظيف، مخصص.

## CustomStartMonth {#AbsenceType.CustomStartMonth}
<!-- alias: loc:CustomStartMonth, dom:AbsenceType_CustomStartMonth -->

**ما هو.** الشهر الذي تبدأ فيه الدورة، لفترة الاستحقاق *مخصص*.

**مثال.** يوليو

**القواعد.** إلزامي. إحدى القيم: لا شيء، يناير، فبراير، مارس، أبريل، مايو، يونيو، يوليو، أغسطس، سبتمبر، أكتوبر، نوفمبر، ديسمبر.

## ExcludePaidLeaveAccrual {#AbsenceType.ExcludePaidLeaveAccrual}
<!-- alias: loc:ExcludePaidLeaveAccrual, dom:AbsenceType_ExcludePaidLeaveAccrual -->

**ما هو.** ما إذا كانت أيام هذه الإجازة توقف تراكم الإجازة السنوية.

**لماذا يهم.** إذا أُشِّر عليه لا يراكم محرك الوقت أي إجازة سنوية عن الأيام التي يقضيها الموظف في هذا النوع من الإجازات.

**القواعد.** نعم أو لا.

## MinActiveDaysInService {#AbsenceType.MinActiveDaysInService}
<!-- alias: loc:MinActiveDaysInService, dom:AbsenceType_MinActiveDaysInService -->

**ما هو.** أيام الخدمة التي يحتاجها الموظف قبل أن يأخذ هذه الإجازة.

**مثال.** `730` -- إجازة الحج بعد سنتين.

**لماذا يهم.** لا يُتحقق منه وحده. ويمكن لقواعد العمل على الإجازة قراءته، فتستطيع قاعدة أن ترفض الإجازة للموظف الذي أيام خدمته أقل.

**القواعد.** اختياري. رقم.

## CanApplyAgain {#AbsenceType.CanApplyAgain}
<!-- alias: loc:CanApplyAgain, dom:AbsenceType_CanApplyAgain -->

**ما هو.** ما إذا كان يمكن أخذ الإجازة أكثر من مرة.

**لماذا يهم.** لا يُتحقق منه حالياً -- يُحفظ الإعداد، لكن لا شيء يطبّقه عند إدخال الإجازة أو اعتمادها.

**القواعد.** نعم أو لا.

## CanApplyAfterDays {#AbsenceType.CanApplyAfterDays}
<!-- alias: loc:CanApplyAfterDays, dom:AbsenceType_CanApplyAfterDays -->

**ما هو.** الأيام التي يجب أن تمضي قبل أخذ الإجازة مرة أخرى.

**لماذا يهم.** لا يُتحقق منه حالياً -- يُحفظ الإعداد، لكن لا شيء يطبّقه عند إدخال الإجازة أو اعتمادها.

**القواعد.** اختياري. رقم.

## CanApplyBeforeDays {#AbsenceType.CanApplyBeforeDays}
<!-- alias: loc:CanApplyBeforeDays, dom:AbsenceType_CanApplyBeforeDays -->

**ما هو.** عدد الأيام التي يجب طلب الإجازة قبلها.

**لماذا يهم.** لا يُتحقق منه حالياً -- يُحفظ الإعداد، لكن لا شيء يطبّقه عند إدخال الإجازة أو اعتمادها.

**القواعد.** اختياري. رقم صحيح.

## ReInitialize {#AbsenceType.ReInitialize}
<!-- alias: loc:ReInitialize, dom:AbsenceType_ReInitialize -->

**ما هو.** ما إذا كان الرصيد يبدأ من جديد في نهاية كل دورة.

**لماذا يهم.** إذا أُشِّر عليه تبدأ الدورة من جديد كل **إعادة التهيئة بعد** سنة من بدايتها. وإلا فلا تنتهي الدورة الأولى أبداً.

**القواعد.** نعم أو لا.

## ReInitializeAfter {#AbsenceType.ReInitializeAfter}
<!-- alias: loc:ReInitializeAfter, dom:AbsenceType_ReInitializeAfter -->

**ما هو.** عدد السنوات التي تستمر فيها كل دورة قبل أن يبدأ الرصيد من جديد.

**مثال.** `1`

**لماذا يهم.** يُستخدم إذا أُشِّر على **إعادة التهيئة**، ويجب أن يكون أكبر من صفر عندئذ.

**القواعد.** اختياري. رقم صحيح.

## ApplyLeaveInAdvance {#AbsenceType.ApplyLeaveInAdvance}
<!-- alias: loc:ApplyLeaveInAdvance, dom:AbsenceType_ApplyLeaveInAdvance -->

**ما هو.** ما إذا كان يجوز أخذ الإجازة قبل استحقاقها.

**لماذا يهم.** لا يُتحقق منه حالياً -- يُحفظ الإعداد، لكن لا شيء يطبّقه عند إدخال الإجازة أو اعتمادها.

**القواعد.** نعم أو لا.

## ApplyLeaveInAdvanceDays {#AbsenceType.ApplyLeaveInAdvanceDays}
<!-- alias: loc:ApplyLeaveInAdvanceDays, dom:AbsenceType_ApplyLeaveInAdvanceDays -->

**ما هو.** عدد الأيام غير المستحقة التي يجوز أخذها مقدماً.

**لماذا يهم.** لا يُتحقق منه حالياً -- يُحفظ الإعداد، لكن لا شيء يطبّقه عند إدخال الإجازة أو اعتمادها.

**القواعد.** اختياري. رقم صحيح.

## ApplyMaxLeave {#AbsenceType.ApplyMaxLeave}
<!-- alias: loc:ApplyMaxLeave, dom:AbsenceType_ApplyMaxLeave -->

**ما هو.** ما إذا كان هناك حد أقصى لمدة الإجازة الواحدة.

**لماذا يهم.** لا يُتحقق منه حالياً -- يُحفظ الإعداد، لكن لا شيء يطبّقه عند إدخال الإجازة أو اعتمادها.

**القواعد.** نعم أو لا.

## MaxLimitInDays {#AbsenceType.MaxLimitInDays}
<!-- alias: loc:MaxLimitInDays, dom:AbsenceType_MaxLimitInDays -->

**ما هو.** أقصى عدد من الأيام يجوز أن تستغرقه الإجازة الواحدة.

**مثال.** `30`

**لماذا يهم.** لا يُتحقق منه حالياً -- يُحفظ الإعداد، لكن لا شيء يطبّقه عند إدخال الإجازة أو اعتمادها.

**القواعد.** اختياري. رقم صحيح.

## CreateUnpaidAboveMaxLimitDays {#AbsenceType.CreateUnpaidAboveMaxLimitDays}
<!-- alias: loc:CreateUnpaidAboveMaxLimitDays, dom:AbsenceType_CreateUnpaidAboveMaxLimitDays -->

**ما هو.** ما إذا كانت الأيام التي تتجاوز الحد الأقصى تتحول إلى إجازة غير مدفوعة.

**لماذا يهم.** لا يُتحقق منه حالياً -- يُحفظ الإعداد، لكن لا شيء يطبّقه عند إدخال الإجازة أو اعتمادها.

**القواعد.** نعم أو لا.

## ApplyMinLeave {#AbsenceType.ApplyMinLeave}
<!-- alias: loc:ApplyMinLeave, dom:AbsenceType_ApplyMinLeave -->

**ما هو.** ما إذا كان هناك حد أدنى لمدة الإجازة الواحدة.

**لماذا يهم.** لا يُتحقق منه حالياً -- يُحفظ الإعداد، لكن لا شيء يطبّقه عند إدخال الإجازة أو اعتمادها.

**القواعد.** نعم أو لا.

## MinLimitInDays {#AbsenceType.MinLimitInDays}
<!-- alias: loc:MinLimitInDays, dom:AbsenceType_MinLimitInDays -->

**ما هو.** أقل عدد من الأيام يجوز أن تستغرقه الإجازة الواحدة.

**مثال.** `3`

**لماذا يهم.** لا يُتحقق منه حالياً -- يُحفظ الإعداد، لكن لا شيء يطبّقه عند إدخال الإجازة أو اعتمادها.

**القواعد.** اختياري. رقم صحيح.

## ResetFrequency {#AbsenceType.ResetFrequency}
<!-- alias: loc:ResetFrequency, dom:AbsenceType_ResetFrequency -->

**ما هو.** للإجازة بالساعات: الفترة التي تسري عليها حدود الساعات.

**لماذا يهم.** لا يُتحقق منه حالياً -- يُحفظ الإعداد، لكن لا شيء يطبّقه عند إدخال الإجازة أو اعتمادها.

**القواعد.** إلزامي. إحدى القيم: يومي، إسبوعي، شهري، ربع سنوي، سنوياً.

## TotalMinutes {#AbsenceType.TotalMinutes}
<!-- alias: loc:TotalMinutes, dom:AbsenceType_TotalMinutes -->

**ما هو.** للإجازة بالساعات: مجموع الدقائق المسموح بها في الفترة الواحدة.

**لماذا يهم.** لا يُتحقق منه حالياً -- يُحفظ الإعداد، لكن لا شيء يطبّقه عند إدخال الإجازة أو اعتمادها.

**القواعد.** اختياري. رقم صحيح.

## MaxMinutes {#AbsenceType.MaxMinutes}
<!-- alias: loc:MaxMinutes, dom:AbsenceType_MaxMinutes -->

**ما هو.** للإجازة بالساعات: أقصى عدد من الدقائق في الطلب الواحد.

**لماذا يهم.** لا يُتحقق منه حالياً -- يُحفظ الإعداد، لكن لا شيء يطبّقه عند إدخال الإجازة أو اعتمادها.

**القواعد.** اختياري. رقم صحيح.

## MaxCount {#AbsenceType.MaxCount}
<!-- alias: loc:MaxCount, dom:AbsenceType_MaxCount -->

**ما هو.** للإجازة بالساعات: أقصى عدد من الطلبات في الفترة الواحدة.

**لماذا يهم.** لا يُتحقق منه حالياً -- يُحفظ الإعداد، لكن لا شيء يطبّقه عند إدخال الإجازة أو اعتمادها.

**القواعد.** اختياري. رقم صحيح.

## RestrictType {#AbsenceType.PolicyRestrictType}
<!-- alias: loc:RestrictType, dom:AbsenceType_PolicyRestrictType -->

**ما هو.** ما إذا كان تجاوز حدود الساعات يُنبّه أو يَرفض.

**لماذا يهم.** لا يُتحقق منه حالياً -- يُحفظ الإعداد، لكن لا شيء يطبّقه عند إدخال الإجازة أو اعتمادها.

**القواعد.** إلزامي. إحدى القيم: تحذير، خطأ.

## AbsenceViolationRules {#AbsenceType.AbsenceViolationRuleId}
<!-- alias: loc:AbsenceViolationRules, dom:AbsenceType_AbsenceViolationRuleId -->

**ما هو.** قاعدة مخالفة غياب مرتبطة بنوع الإجازة هذا.

**لماذا يهم.** لا يُتحقق منه حالياً -- يُحفظ الإعداد، لكن لا شيء يطبّقه عند إدخال الإجازة أو اعتمادها.

**القواعد.** اختياري. يُختار من قائمة قواعد مخالفات الغياب.

## LeaveGender {#AbsenceType.LeaveGender}
<!-- alias: loc:LeaveGender, dom:AbsenceType_LeaveGender -->

**ما هو.** الجنس الذي تخصه الإجازة.

**مثال.** أنثى -- إجازة الأمومة.

**لماذا يهم.** لا يُتحقق منه حالياً -- يُحفظ الإعداد، لكن لا شيء يطبّقه عند إدخال الإجازة أو اعتمادها.

**القواعد.** إلزامي. إحدى القيم: الجميع، ذكر، أنثى.

## LeaveReligion {#AbsenceType.LeaveReligion}
<!-- alias: loc:LeaveReligion, dom:AbsenceType_LeaveReligion -->

**ما هو.** الديانة التي تخصها الإجازة.

**مثال.** مسلم -- إجازة الحج.

**لماذا يهم.** لا يُتحقق منه حالياً -- يُحفظ الإعداد، لكن لا شيء يطبّقه عند إدخال الإجازة أو اعتمادها.

**القواعد.** إلزامي. إحدى القيم: الجميع، مسلم، غير مسلم.

## QuotaType {#AbsenceType.QuotaType}
<!-- alias: loc:QuotaType, dom:AbsenceType_QuotaType -->

**ما هو.** ما إذا كان نوع الإجازة هذا يتبع قواعد رصيد الإجازة السنوية أو المرضية.

**مثال.** الإجازة السنوية

**لماذا يهم.** *الإجازة السنوية* تجعل رصيده كالإجازة السنوية: يتراكم يوماً بيوم خلال السنة، بحدّ أقصى هو حد الدرجة. و*الإجازة المرضية* تجعل رصيده كالإجازة المرضية: استحقاق سنوي لكل دورة ناقص ما أُخذ منه. و*لا شيء* لا يجعله يتبع أياً منهما.

**القواعد.** إلزامي. إحدى القيم: لا شيء، الإجازة السنوية، الإجازة المرضية.

## LeaveEntitlement {#AbsenceType.LeaveEntitlement}
<!-- alias: loc:LeaveEntitlement, dom:AbsenceType_LeaveEntitlement -->

**ما هو.** ما إذا كان الموظف الذي يلتحق في منتصف الدورة يحصل على الاستحقاق كاملاً أو على حصة منه.

**مثال.** بالتناسب

**لماذا يهم.** يُستخدم للإجازة التي يُحتسب رصيدها كالإجازة المرضية، ومع فترة الاستحقاق *السنة الميلادية* فقط: *بالتناسب* يخفض استحقاق السنة الأولى بنسبة ما بقي من السنة بعد الالتحاق.

**القواعد.** إلزامي. إحدى القيم: كامل، بالتناسب.

## AllowBackDatedDays {#AbsenceType.AllowBackDatedDays}
<!-- alias: loc:AllowBackDatedDays, dom:AbsenceType_AllowBackDatedDays -->

**ما هو.** عدد الأيام التي يجوز إدخال الإجازة بأثر رجعي عنها.

**لماذا يهم.** لا يُتحقق منه حالياً -- يُحفظ الإعداد، لكن لا شيء يطبّقه عند إدخال الإجازة أو اعتمادها.

**القواعد.** اختياري. رقم صحيح.

## TotalDaysAllowed {#AbsenceType.TotalDaysAllowed}
<!-- alias: loc:TotalDaysAllowed, dom:AbsenceType_TotalDaysAllowed -->

**ما هو.** الاستحقاق السنوي، للموظفين الذين لا تحدد مجموعتهم الفرعية استحقاقاً.

**مثال.** `30`

**لماذا يهم.** إذا كان لمجموعة الموظف الفرعية قاعدة إجازة لهذا النوع يُستخدم مجموع أيامها بدلاً منه. ويُظهر تقرير رصيد حصص الإجازات الرقم المستخدم.

**القواعد.** اختياري. رقم.

## NegativeBalanceLimit {#AbsenceType.NegativeBalanceLimit}
<!-- alias: loc:NegativeBalanceLimit, dom:AbsenceType_NegativeBalanceLimit -->

**ما هو.** إلى أي حد يجوز أن ينزل الرصيد تحت الصفر.

**لماذا يهم.** لا يُتحقق منه حالياً -- يُحفظ الإعداد، لكن لا شيء يطبّقه عند إدخال الإجازة أو اعتمادها.

**القواعد.** اختياري. رقم صحيح.

## EnableNegativeBalance {#AbsenceType.EnableNegativeBalance}
<!-- alias: loc:EnableNegativeBalance, dom:AbsenceType_EnableNegativeBalance -->

**ما هو.** ما إذا كان يجوز أن ينزل الرصيد تحت الصفر.

**لماذا يهم.** لا يُتحقق منه حالياً -- يُحفظ الإعداد، لكن لا شيء يطبّقه عند إدخال الإجازة أو اعتمادها.

**القواعد.** نعم أو لا.

## IncludeInSalaryProfile {#AbsenceType.IncludeInSalaryProfile}
<!-- alias: loc:IncludeInSalaryProfile, dom:AbsenceType_IncludeInSalaryProfile -->

**ما هو.** ما إذا كانت الإجازة تظهر في ملف الراتب.

**لماذا يهم.** لا يُتحقق منه حالياً -- يُحفظ الإعداد، لكن لا شيء يطبّقه عند إدخال الإجازة أو اعتمادها.

**القواعد.** نعم أو لا.

## AllowDuringProbation {#AbsenceType.AllowDuringProbation}
<!-- alias: loc:AllowDuringProbation, dom:AbsenceType_AllowDuringProbation -->

**ما هو.** ما إذا كان يجوز أخذ الإجازة خلال فترة التجربة.

**لماذا يهم.** لا يُتحقق منه حالياً -- يُحفظ الإعداد، لكن لا شيء يطبّقه عند إدخال الإجازة أو اعتمادها.

**القواعد.** نعم أو لا.

## AllowToReturnEarly {#AbsenceType.AllowToReturnEarly}
<!-- alias: loc:AllowToReturnEarly, dom:AbsenceType_AllowToReturnEarly -->

**ما هو.** ما إذا كان يمكن تقصير الإجازة.

**لماذا يهم.** إذا لم يُؤشَّر عليه يُرفض طلب تقصير الإجازة أو العودة منها مبكراً برسالة "يجب أخذ … كاملة، لذا لا يمكن تقصيرها أو العودة منها مبكراً".

**القواعد.** نعم أو لا.

## AllowInTerminationNoticeOnly {#AbsenceType.AllowInTerminationNoticeOnly}
<!-- alias: loc:AllowInTerminationNoticeOnly, dom:AbsenceType_AllowInTerminationNoticeOnly -->

**ما هو.** ما إذا كان لا يجوز أخذ الإجازة إلا خلال فترة إنذار إنهاء الخدمة.

**لماذا يهم.** لا يُتحقق منه حالياً -- يُحفظ الإعداد، لكن لا شيء يطبّقه عند إدخال الإجازة أو اعتمادها.

**القواعد.** نعم أو لا.

## IsReturnDateRequired {#AbsenceType.IsReturnDateRequired}
<!-- alias: loc:IsReturnDateRequired, dom:AbsenceType_IsReturnDateRequired -->

**ما هو.** ما إذا كان يجب تسجيل عودة الموظف من هذه الإجازة.

**لماذا يهم.** الموظف الذي انتهت إجازته من هذا النوع دون تسجيل عودته يُعامل على أنه لم يعد، ويُرسل تذكير بالعودة من الإجازة إذا كان هذا الإشعار مُعدّاً. وإذا كان إعداد الرواتب *تفعيل منطق عدم العودة من الإجازة* مفعّلاً، لا يُصرف راتبه الأساسي في ذلك التشغيل، ويحتسب محرك الوقت أيام راحته -- وأيام الراحة الأسبوعية إذا كان الراتب الأساسي على أيام التقويم -- غياباً إلى أن تُسجَّل عودته.

**القواعد.** نعم أو لا.

## IsAttachmentRequired {#AbsenceType.IsAttachmentRequired}
<!-- alias: loc:IsAttachmentRequired, dom:AbsenceType_IsAttachmentRequired -->

**ما هو.** ما إذا كان يجب إرفاق مستند بالإجازة.

**لماذا يهم.** لا يُتحقق منه حالياً -- يُحفظ الإعداد، لكن لا شيء يطبّقه عند إدخال الإجازة أو اعتمادها.

**القواعد.** نعم أو لا.

## AllowPartialLeave {#AbsenceType.AllowPartialLeave}
<!-- alias: loc:AllowPartialLeave, dom:AbsenceType_AllowPartialLeave -->

**ما هو.** ما إذا كان يجوز أخذ جزء من يوم.

**لماذا يهم.** لا يُتحقق منه حالياً -- يُحفظ الإعداد، لكن لا شيء يطبّقه عند إدخال الإجازة أو اعتمادها.

**القواعد.** نعم أو لا.

## SendNotification {#AbsenceType.SendNotification}
<!-- alias: loc:SendNotification, dom:AbsenceType_SendNotification -->

**ما هو.** ما إذا كان يُرسل إشعار لهذه الإجازة.

**لماذا يهم.** لا يُتحقق منه حالياً -- يُحفظ الإعداد، لكن لا شيء يطبّقه عند إدخال الإجازة أو اعتمادها.

**القواعد.** نعم أو لا.

## RequirePolicyAcceptance {#AbsenceType.RequirePolicyAcceptance}
<!-- alias: loc:RequirePolicyAcceptance, dom:AbsenceType_RequirePolicyAcceptance -->

**ما هو.** ما إذا كان يجب على الموظف الموافقة على سياسة الإجازات قبل إرسال طلب لنوع الإجازة هذا.

**لماذا يهم.** إذا أُشِّر عليه، تعرض الخدمة الذاتية أحدث مستند سياسة مرفق بنوع الإجازة مع خانة للموافقة، ولا يمكن إرسال الطلب حتى يُؤشَّر عليها.

**القواعد.** نعم أو لا.

## LeaveDurationType {#AbsenceType.LeaveDurationType}
<!-- alias: loc:LeaveDurationType, dom:AbsenceType_LeaveDurationType -->

**ما هو.** ما إذا كانت هذه الإجازة تؤخذ بأيام كاملة، أو بالساعات، أو بكليهما.

**مثال.** يوم كامل فقط

**لماذا يهم.** مع «يوم كامل فقط» يُرفض الطلب الذي فيه ساعات أو نصف يوم. ومع «جزئية (بالساعات) فقط» يُرفض الطلب الذي ليس فيه ساعات أو نصف يوم. أما «يوم كامل أو جزئية» فيترك الأمر لإعداد **السماح بالإجازة الجزئية**.

**القواعد.** إلزامي. إحدى القيم: الجميع، FullDay، Partial.

## MaxFutureDays {#AbsenceType.MaxFutureDays}
<!-- alias: loc:MaxFutureDays, dom:AbsenceType_MaxFutureDays -->

**ما هو.** أقصى عدد من الأيام مقدَّماً يجوز أن تبدأ بعدها إجازة جديدة من هذا النوع.

**مثال.** 90

**لماذا يهم.** يُرفض الطلب الجديد الذي يبدأ بعد تاريخ اليوم مضافاً إليه هذا العدد من الأيام، ويتوقف منتقي التاريخ في الخدمة الذاتية عنده. والقيمة 0 تعني بلا حد. ولا يُتحقق من طلبات التعديل.

**القواعد.** اختياري. رقم صحيح.

## Sunday {#AbsenceType.DontAllowToStartOnSunday}
<!-- alias: loc:Sunday, dom:AbsenceType_DontAllowToStartOnSunday -->

**ما هو.** ما إذا كان لا يجوز أن تبدأ الإجازة من هذا النوع يوم الأحد.

**لماذا يهم.** لا يُتحقق منه حالياً -- يُحفظ الإعداد، لكن لا شيء يطبّقه عند إدخال الإجازة أو اعتمادها.

**القواعد.** نعم أو لا.

## Monday {#AbsenceType.DontAllowToStartOnMonday}
<!-- alias: loc:Monday, dom:AbsenceType_DontAllowToStartOnMonday -->

**ما هو.** ما إذا كان لا يجوز أن تبدأ الإجازة من هذا النوع يوم الاثنين.

**لماذا يهم.** لا يُتحقق منه حالياً -- يُحفظ الإعداد، لكن لا شيء يطبّقه عند إدخال الإجازة أو اعتمادها.

**القواعد.** نعم أو لا.

## Tuesday {#AbsenceType.DontAllowToStartOnTuesday}
<!-- alias: loc:Tuesday, dom:AbsenceType_DontAllowToStartOnTuesday -->

**ما هو.** ما إذا كان لا يجوز أن تبدأ الإجازة من هذا النوع يوم الثلاثاء.

**لماذا يهم.** لا يُتحقق منه حالياً -- يُحفظ الإعداد، لكن لا شيء يطبّقه عند إدخال الإجازة أو اعتمادها.

**القواعد.** نعم أو لا.

## Wednesday {#AbsenceType.DontAllowToStartOnWednesday}
<!-- alias: loc:Wednesday, dom:AbsenceType_DontAllowToStartOnWednesday -->

**ما هو.** ما إذا كان لا يجوز أن تبدأ الإجازة من هذا النوع يوم الأربعاء.

**لماذا يهم.** لا يُتحقق منه حالياً -- يُحفظ الإعداد، لكن لا شيء يطبّقه عند إدخال الإجازة أو اعتمادها.

**القواعد.** نعم أو لا.

## Thursday {#AbsenceType.DontAllowToStartOnThursday}
<!-- alias: loc:Thursday, dom:AbsenceType_DontAllowToStartOnThursday -->

**ما هو.** ما إذا كان لا يجوز أن تبدأ الإجازة من هذا النوع يوم الخميس.

**لماذا يهم.** لا يُتحقق منه حالياً -- يُحفظ الإعداد، لكن لا شيء يطبّقه عند إدخال الإجازة أو اعتمادها.

**القواعد.** نعم أو لا.

## Friday {#AbsenceType.DontAllowToStartOnFriday}
<!-- alias: loc:Friday, dom:AbsenceType_DontAllowToStartOnFriday -->

**ما هو.** ما إذا كان لا يجوز أن تبدأ الإجازة من هذا النوع يوم الجمعة.

**لماذا يهم.** لا يُتحقق منه حالياً -- يُحفظ الإعداد، لكن لا شيء يطبّقه عند إدخال الإجازة أو اعتمادها.

**القواعد.** نعم أو لا.

## Saturday {#AbsenceType.DontAllowToStartOnSaturday}
<!-- alias: loc:Saturday, dom:AbsenceType_DontAllowToStartOnSaturday -->

**ما هو.** ما إذا كان لا يجوز أن تبدأ الإجازة من هذا النوع يوم السبت.

**لماذا يهم.** لا يُتحقق منه حالياً -- يُحفظ الإعداد، لكن لا شيء يطبّقه عند إدخال الإجازة أو اعتمادها.

**القواعد.** نعم أو لا.
