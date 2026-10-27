---
helpId: pa.employee-separation.edit
lang: ar
title: حالات فصل الموظف — إنشاء / تحرير
entity: HCMv3.PersonnelAdministration.EmployeeSeparation
surfaces:
  - angular/src/app/admin/personnel-administration/employee-detail/separations/create-or-edit-employee-separation-modal.component.html
audience: [admin]
permissions: [Pages.PersonnelAdministration.EmployeeSeparations.Create]
---

## Overview

خروج الموظف: متى استقال أو أُنهيت خدمته، وآخر يوم له في الخدمة. ويبقى الفصل سارياً حتى يُلغى.

تتوقف الرواتب عن احتساب معظم الأجر بعد نهاية شهر آخر يوم في الخدمة، وتحتاج مسيرة نهاية الخدمة إلى فصل غير ملغى. ولا يؤدي حفظ الفصل إلى حذف سجلات الوقت اللاحقة؛ فتلك خطوة منفصلة.

ويمكن من خلال **بيانات إيقاف الصرف** إيقاف صرف أجر الموظف في الوقت نفسه. فحفظ الفصل مع **تاريخ بداية الإيقاف** واختيار إيقاف واحد على الأقل يُنشئ سجل إيقاف الصرف للموظف بأشهر كاملة: يُنقل تاريخ البداية إلى أول شهره، وتاريخ النهاية إلى آخر يوم في شهره. وإعادة الحفظ تستبدل الإيقاف الذي أنشأه هذا الفصل من قبل، وحذف الفصل يحذفه. والرواتب تقرأ سجل إيقاف الصرف لا الخيارات المؤشَّرة في هذا النموذج، ولذلك يظهر الإيقاف أيضاً، ويمكن مراجعته، ضمن حالات الإيقاف للموظف.

وإذا أُدخل للموظف إيقاف صرف لاحق — يبدأ بعد تاريخ بداية الإيقاف في هذا الفصل — فلا يمكن بعدها إضافة الفصل أو تعديله أو حذفه، وتذكر الرسالة تاريخَي ذلك الإيقاف.

## SubmissionDate {#EmployeeSeparation.SubmissionDate}
<!-- alias: loc:SubmissionDate, dom:EmployeeSeparation_SubmissionDate -->

**ما هو.** تاريخ تقديم الاستقالة أو إنهاء الخدمة.

**مثال.** 1 أغسطس 2026

**لماذا يهم.** لا يجوز أن يسبقه آخر يوم في الخدمة. وتستخدم مسيرة نهاية الخدمة أحدث فصل مقدَّم.

**القواعد.** إلزامي. تاريخ.

## LastDayInService {#EmployeeSeparation.LastDayInService}
<!-- alias: loc:LastDayInService, dom:EmployeeSeparation_LastDayInService -->

**ما هو.** آخر يوم عمل للموظف.

**مثال.** 31 أغسطس 2026

**لماذا يهم.** لا تحتسب الرواتب أنواع الأجور لتواريخ دفع بعد نهاية هذا الشهر، وتتوقف اقتطاعات الصندوق، وتُحتسب مكافأة نهاية الخدمة كما في هذا التاريخ.

**القواعد.** إلزامي. تاريخ.

## NoticePeriodDays {#EmployeeSeparation.NoticePeriodDays}
<!-- alias: loc:NoticePeriodDays, dom:EmployeeSeparation_NoticePeriodDays -->

**ما هو.** مدة فترة الإشعار بالأيام.

**مثال.** 30

**لماذا يهم.** يظهر في تقرير الفصل، ولا تُعاد حساب التواريخ منه.

**القواعد.** إلزامي. رقم صحيح.

## PlannedLastDayInService {#EmployeeSeparation.PlannedLastDayInService}
<!-- alias: loc:PlannedLastDayInService, dom:EmployeeSeparation_PlannedLastDayInService -->

**ما هو.** آخر يوم كان مخططاً في الأصل، قبل أي تغيير.

**مثال.** 31 أغسطس 2026

**لماذا يهم.** يظهر في تقرير الفصل.

**القواعد.** إلزامي. تاريخ.

## SeparationType {#EmployeeSeparation.SeparationType}
<!-- alias: loc:SeparationType, dom:EmployeeSeparation_SeparationType -->

**ما هو.** هل استقال الموظف أم أُنهيت خدمته.

**مثال.** استقالة

**لماذا يهم.** يظهر في تقارير الموظفين والتكلفة الإجمالية للموظف.

**القواعد.** إلزامي. إحدى القيم: استقالة، إنهاء خدمة.

## SeparationReason {#EmployeeSeparation.SeparationReasonId}
<!-- alias: loc:SeparationReason, dom:EmployeeSeparation_SeparationReasonId -->

**ما هو.** سبب مغادرة الموظف.

**مثال.** فرصة أفضل

**القواعد.** إلزامي. يُختار من قائمة أسباب الفصل.

:::admin
تُدار من البيانات الأساسية العامة ← أسباب الفصل.
:::

## RevokeDate {#EmployeeSeparation.RevokeDate}
<!-- alias: loc:RevokeDate, dom:EmployeeSeparation_RevokeDate -->

**ما هو.** تاريخ سحب الفصل، إن سُحب.

**مثال.** 10 أغسطس 2026

**لماذا يهم.** بعد تحديده، تتجاهل الرواتب واحتساب مكافأة نهاية الخدمة هذا الفصل.

**القواعد.** اختياري. تاريخ.

## RevokeComments {#EmployeeSeparation.RevokeComments}
<!-- alias: loc:RevokeComments, dom:EmployeeSeparation_RevokeComments -->

**ما هو.** سبب إلغاء الفصل، في جملة أو جملتين.

**مثال.** تراجع الموظف عن استقالته بعد عرض مضاد.

**لماذا يهم.** يُسجَّل مع الفصل فقط، ولا يعرضه أي تقرير حالياً.

**القواعد.** اختياري. الحد الأقصى 500 حرف.

## OnHoldFromDate {#EmployeeSeparation.OnHoldFromDate}
<!-- alias: loc:OnHoldFromDate, dom:EmployeeSeparation_OnHoldFromDate -->

**ما هو.** الشهر الذي يبدأ فيه إيقاف الصرف. ويكفي أي يوم من الشهر، إذ يصبح عند الحفظ أول ذلك الشهر.

**مثال.** 15 أغسطس 2026، ويُحفظ 1 أغسطس 2026

**لماذا يهم.** لا يُوقف أي شيء ما لم يُؤشَّر أيضاً على إيقاف واحد على الأقل مما يلي. والتاريخ المحفوظ هو ما يميّز الإيقاف الذي أنشأه هذا الفصل، وبه تستبدله إعادة الحفظ ويحذفه حذف الفصل.

**القواعد.** اختياري. تاريخ.

## OnHoldTillDate {#EmployeeSeparation.OnHoldTillDate}
<!-- alias: loc:OnHoldTillDate, dom:EmployeeSeparation_OnHoldTillDate -->

**ما هو.** الشهر الذي ينتهي فيه إيقاف الصرف. ويصبح عند الحفظ آخر يوم في ذلك الشهر.

**مثال.** 10 سبتمبر 2026، ويُحفظ 30 سبتمبر 2026

**لماذا يهم.** لازم كلما أُشِّر على إيقاف: فبدونه تقع نهاية الإيقاف قبل بدايته ويُرفض الحفظ. ولا يجوز أن تتداخل الفترة مع إيقاف آخر للموظف نفسه.

**القواعد.** اختياري. تاريخ.

## BankTransfer {#EmployeeSeparation.BankTransfer}
<!-- alias: loc:BankTransfer, dom:EmployeeSeparation_BankTransfer -->

**ما هو.** هل يُوقف تحويل راتب الموظف خلال فترة الإيقاف.

**مثال.** إيقاف

**لماذا يهم.** يُنسخ إلى سجل إيقاف الصرف، ومنه تقرؤه ملفات البنك. ولا يُنشئ إيقافاً بمفرده، بل يلزم معه أحد الخيارات التالية.

**القواعد.** إلزامي. إحدى القيم: لا ينطبق، إيقاف، إلغاء الإيقاف.

## OnHoldAll {#EmployeeSeparation.OnHoldAll}
<!-- alias: loc:OnHoldAll, dom:EmployeeSeparation_onHoldAll -->

**ما هو.** يوقف صرف كل شيء خلال فترة الإيقاف.

**لماذا يهم.** يُنسخ إلى خيار **معلق** في سجل إيقاف الصرف، فتعامل الرواتب كل خيارات الإيقاف كأنها مؤشَّرة.

**القواعد.** نعم أو لا.

## OnHoldBasicPay {#EmployeeSeparation.OnHoldBasicPay}
<!-- alias: loc:OnHoldBasicPay, dom:EmployeeSeparation_onHoldBasicPay -->

**ما هو.** يوقف صرف الراتب الأساسي خلال فترة الإيقاف.

**لماذا يهم.** يُنسخ إلى سجل إيقاف الصرف، ولا تصرف الرواتب الراتب الأساسي ما دام سارياً.

**القواعد.** نعم أو لا.

## OnHoldLeavePay {#EmployeeSeparation.OnHoldLeavePay}
<!-- alias: loc:OnHoldLeavePay, dom:EmployeeSeparation_onHoldLeavePay -->

**ما هو.** يوقف صرف بدل الإجازة خلال فترة الإيقاف.

**لماذا يهم.** يُنسخ إلى سجل إيقاف الصرف.

**القواعد.** نعم أو لا.

## OnHoldIndemnityPay {#EmployeeSeparation.OnHoldIndemnityPay}
<!-- alias: loc:OnHoldIndemnityPay, dom:EmployeeSeparation_onHoldIndemnityPay -->

**ما هو.** يوقف صرف مكافأة نهاية الخدمة خلال فترة الإيقاف.

**لماذا يهم.** يُنسخ إلى سجل إيقاف الصرف — وهو الخيار المعتاد ما دام إخلاء طرف الموظف المغادر مفتوحاً.

**القواعد.** نعم أو لا.

## OnHoldAirTicketPay {#EmployeeSeparation.OnHoldAirTicketPay}
<!-- alias: loc:OnHoldAirTicketPay, dom:EmployeeSeparation_onHoldAirTicketPay -->

**ما هو.** يوقف صرف بدل تذكرة الطيران خلال فترة الإيقاف.

**لماذا يهم.** يُنسخ إلى سجل إيقاف الصرف، حيث يُسجَّل فقط؛ إذ لا تتحقق منه الرواتب حالياً عند صرف التذاكر.

**القواعد.** نعم أو لا.

## OnHoldLoanDeduction {#EmployeeSeparation.OnHoldLoanDeduction}
<!-- alias: loc:OnHoldLoanDeduction, dom:EmployeeSeparation_onHoldLoanDeduction -->

**ما هو.** يوقف خصم أقساط القروض خلال فترة الإيقاف.

**لماذا يهم.** يُنسخ إلى سجل إيقاف الصرف، ولا تسترد الرواتب القروض ما دام سارياً.

**القواعد.** نعم أو لا.

## OnHoldAllEarnings {#EmployeeSeparation.OnHoldAllEarnings}
<!-- alias: loc:OnHoldAllEarnings, dom:EmployeeSeparation_onHoldAllEarnings -->

**ما هو.** يوقف صرف جميع الاستحقاقات خلال فترة الإيقاف.

**لماذا يهم.** يُنسخ إلى سجل إيقاف الصرف.

**القواعد.** نعم أو لا.

## OnHoldAllDeductions {#EmployeeSeparation.OnHoldAllDeductions}
<!-- alias: loc:OnHoldAllDeductions, dom:EmployeeSeparation_onHoldAllDeductions -->

**ما هو.** يوقف جميع الخصومات خلال فترة الإيقاف.

**لماذا يهم.** يُنسخ إلى سجل إيقاف الصرف.

**القواعد.** نعم أو لا.
