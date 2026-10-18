---
helpId: sys.payroll-settings.edit
lang: ar
title: Payroll settings
entity: HCMv3.SystemSettings.ManagePayrollSettings
surfaces:
  - angular/src/app/admin/system-settings/manage-payroll-settings/create-or-edit-manage-payroll-settings-modal.component.html
audience: [admin]
permissions: [Pages.SystemSettings.ManagePayrollSettings]
---

## Overview

سلوك الرواتب على مستوى الشركة: التقريب، وساعات العمل، وطريقة الاحتساب التناسبي للأجر بالأيام الثابتة، وقواعد أجر الإجازة ومخصصها، ومعالجة عدم العودة من الإجازة، والتشغيل خارج الدورة، وملفات البنك.

ومعظمها يطابق مفاتيح في نظام الرواتب السابق ويُطبَّق كما كان. وبعضها لا يُقرأ حالياً، ومبيَّن ذلك أدناه.

ولكل شركة سجل إعدادات واحد، ولا يمكن حذفه.

## ClientId {#ManagePayrollSettings.ClientId}
<!-- alias: loc:ClientId, dom:PayrollSettings_ClientId -->

**ما هو.** رمز العميل في النظام السابق الذي كان يختار نسخ التقارير الخاصة بالعملاء.

**لماذا يهم.** لا يُقرأ حالياً -- تغييره لا أثر له.

**القواعد.** اختياري.

## DefaultWorkingHours {#ManagePayrollSettings.DefaultWorkingHours}
<!-- alias: loc:DefaultWorkingHours, dom:PayrollSettings_DefaultWorkingHours -->

**ما هو.** ساعات العمل في اليوم العادي.

**مثال.** `8`

**لماذا يهم.** يُستخدم لتحويل الساعات إلى أيام: أجر ساعة العمل الإضافي إذا كان **أساس ساعات العمل الإضافي** *ساعات ثابتة*، والدفعات المُدخلة بالساعات، وخصومات التأخير والانصراف المبكر بعد السماح الشهري، والإجازة بالساعات في دوام بلا أوقات محددة.

**القواعد.** اختياري. رقم.

## DecimalInSalaryRegister {#ManagePayrollSettings.DecimalInSalaryRegister}
<!-- alias: loc:DecimalInSalaryRegister, dom:PayrollSettings_DecimalInSalaryRegister -->

**ما هو.** المنازل العشرية التي تُقرَّب إليها مبالغ كشف الراتب.

**مثال.** `3`

**لماذا يهم.** القيمة 0 تعني 3.

**القواعد.** اختياري. رقم صحيح.

## Decimal6RoundingFrom {#ManagePayrollSettings.Decimal6RoundingFrom}
<!-- alias: loc:Decimal6RoundingFrom, dom:PayrollSettings_Decimal6RoundingFrom -->

**ما هو.** الشهر الذي يسري منه التقريب الحالي.

**مثال.** `2024-01-01`

**لماذا يهم.** تُقرَّب كشوف الرواتب للأشهر السابقة لهذا التاريخ بالطريقة القديمة، إلى المنازل العشرية للعملة. وإذا تُرك فارغاً يسري التقريب الحالي على كل الأشهر.

**القواعد.** اختياري. تاريخ.

## PayslipSignature {#ManagePayrollSettings.PayslipSignature}
<!-- alias: loc:PayslipSignature, dom:PayrollSettings_PayslipSignature -->

**ما هو.** ما إذا كان كشف الراتب يعرض خانة التوقيع.

**لماذا يهم.** لا يُقرأ حالياً -- تغييره لا أثر له.

**القواعد.** نعم أو لا.

## RoundNetPay {#ManagePayrollSettings.RoundNetPay}
<!-- alias: loc:RoundNetPay, dom:PayrollSettings_RoundNetPay -->

**ما هو.** ما إذا كان صافي الراتب يُقرَّب إلى منزلتين عشريتين.

**لماذا يهم.** إذا أُشِّر عليه يُقرَّب صافي الراتب إلى منزلتين. وإلا فيُقرَّب إلى منازل العملة العشرية (3 للدينار الكويتي).

**القواعد.** نعم أو لا.

## EnablePayrollEngineAutorun {#ManagePayrollSettings.EnablePayrollEngineAutorun}
<!-- alias: loc:EnablePayrollEngineAutorun, dom:PayrollSettings_EnablePayrollEngineAutorun -->

**ما هو.** ما إذا كان محرك الرواتب يعمل تلقائياً وفق جدول.

**لماذا يهم.** لا يُقرأ حالياً -- تغييره لا أثر له.

**القواعد.** نعم أو لا.

## AllowSimulationRun {#ManagePayrollSettings.AllowSimulationRun}
<!-- alias: loc:AllowSimulationRun, dom:PayrollSettings_AllowSimulationRun -->

**ما هو.** ما إذا كان التشغيل التجريبي للرواتب مسموحاً.

**لماذا يهم.** لا يُقرأ حالياً -- تغييره لا أثر له.

**القواعد.** نعم أو لا.

## DisableRetroCalculation {#ManagePayrollSettings.DisableRetroCalculation}
<!-- alias: loc:DisableRetroCalculation, dom:PayrollSettings_DisableRetroCalculation -->

**ما هو.** ما إذا كانت التعديلات المؤرخة في فترة رواتب معالجة تُرفض.

**لماذا يهم.** إذا أُشِّر عليه يُرفض إضافة السجلات المتعلقة بالرواتب أو تعديلها أو حذفها إذا كان تاريخها سابقاً للفترة الحالية -- قبل بداية الفترة ما دامت مفتوحة أو مفتوحة للتشغيل، وحتى نهايتها بعد إقفالها. وإلا فيُسمح بهذه التعديلات ويحتسب نظام الرواتب الفرق بأثر رجعي.

**القواعد.** نعم أو لا.

## LockSalaryRegisterOnPayrollExit {#ManagePayrollSettings.LockSalaryRegisterOnPayrollExit}
<!-- alias: loc:LockSalaryRegisterOnPayrollExit, dom:PayrollSettings_LockSalaryRegisterOnPayrollExit -->

**ما هو.** ما إذا كانت كشوف الرواتب تُقفل عند انتقال الفترة.

**لماذا يهم.** إذا أُشِّر عليه فإن إقفال الفترة يُقفل كشوف الرواتب العادية لذلك الشهر، وفتحها لتشغيل الرواتب يُقفل كشوفه خارج الدورة، فلا يعود ممكناً تعديلها أو حذفها.

**القواعد.** نعم أو لا.

## DefaultLeavePaymentType {#ManagePayrollSettings.DefaultLeavePaymentType}
<!-- alias: loc:DefaultLeavePaymentType, dom:PayrollSettings_DefaultLeavePaymentType -->

**ما هو.** نوع سداد الإجازة المعروض افتراضياً في الإجازة الجديدة.

**لماذا يهم.** لا يُقرأ حالياً -- تغييره لا أثر له.

**القواعد.** إلزامي. إحدى القيم: لا شيء، الدفع مع الرواتب، الدفع مقدماً، الدفع كاملاً مع الرواتب.

## LeaveAdvPaymentdateBefore15 {#ManagePayrollSettings.LeaveAdvPaymentdateBefore15}
<!-- alias: loc:LeaveAdvPaymentdateBefore15, dom:PayrollSettings_LeaveAdvPaymentdateBefore15 -->

**ما هو.** تاريخ الدفعة المقدمة للإجازة التي تبدأ قبل يوم 15.

**لماذا يهم.** لا يُقرأ حالياً -- تغييره لا أثر له.

**القواعد.** نعم أو لا.

## LeaveAdvPaymentdateAfter15 {#ManagePayrollSettings.LeaveAdvPaymentdateAfter15}
<!-- alias: loc:LeaveAdvPaymentdateAfter15, dom:PayrollSettings_LeaveAdvPaymentdateAfter15 -->

**ما هو.** تاريخ الدفعة المقدمة للإجازة التي تبدأ بعد يوم 15.

**لماذا يهم.** لا يُقرأ حالياً -- تغييره لا أثر له.

**القواعد.** نعم أو لا.

## LeaveProvisionAsPerCurrentSalary {#ManagePayrollSettings.LeaveProvisionAsPerCurrentSalary}
<!-- alias: loc:LeaveProvisionAsPerCurrentSalary, dom:PayrollSettings_LeaveProvisionAsPerCurrentSalary -->

**ما هو.** ما إذا كان مخصص الإجازة السنوية يقيّم الرصيد كله بالراتب الحالي.

**لماذا يهم.** رغم اسمه: إذا أُشِّر عليه يُحتسب المخصص فترةً بفترة على سجلات الراتب الأساسي السابقة للموظف. وإلا فيُقيَّم رصيد الإجازة كله بأجر الشهر الحالي.

**القواعد.** نعم أو لا.

## EnableNotReturnFromLeaveLogic {#ManagePayrollSettings.EnableNotReturnFromLeaveLogic}
<!-- alias: loc:EnableNotReturnFromLeaveLogic, dom:PayrollSettings_EnableNotReturnFromLeaveLogic -->

**ما هو.** ما إذا كان الموظفون الذين لم يعودوا من الإجازة يُعاملون معاملة خاصة.

**لماذا يهم.** إذا أُشِّر عليه، فالموظف الذي انتهت إجازته التي تتطلب تاريخ عودة دون تسجيل عودته لا يُصرف له راتب أساسي في التشغيل، ويحتسب محرك الوقت أيام راحته غياباً.

**القواعد.** نعم أو لا.

## GenerateRegisterOnNotReturnFromLeave {#ManagePayrollSettings.GenerateRegisterOnNotReturnFromLeave}
<!-- alias: loc:GenerateRegisterOnNotReturnFromLeave, dom:PayrollSettings_GenerateRegisterOnNotReturnFromLeave -->

**ما هو.** ما إذا كان كشف الراتب يُنتج رغم ذلك للموظف الذي لم يعد من الإجازة.

**لماذا يهم.** إذا لم يُؤشَّر عليه يتوقف التشغيل العادي عند هذا الموظف برسالة ولا يُنتج كشف راتب. ويتجاهل التشغيل خارج الدورة هذا الإعداد.

**القواعد.** نعم أو لا.

## ApplyAnnualleaveNoImpactLogic {#ManagePayrollSettings.ApplyAnnualleaveNoImpactLogic}
<!-- alias: loc:ApplyAnnualleaveNoImpactLogic, dom:PayrollSettings_ApplyAnnualleaveNoImpactLogic -->

**ما هو.** ما إذا كانت إجازة سنوية خاصة "بلا أثر" يُتعرَّف عليها في الأجر بالأيام الثابتة.

**لماذا يهم.** إذا أُشِّر عليه، عندما يُحتسب الشهر بالأيام الفعلية، تُعامل الإجازة من النوع المسمّى بالضبط "Annual Leave (No impact indemnity)" معاملة مختلفة عن غيرها. ويُتعرَّف على النوع باسمه الإنجليزي.

**القواعد.** نعم أو لا.

## FullMonthLeaveAdjForFixedDays {#ManagePayrollSettings.FullMonthLeaveAdjForFixedDays}
<!-- alias: loc:FullMonthLeaveAdjForFixedDays, dom:PayrollSettings_FullMonthLeaveAdjForFixedDays -->

**ما هو.** ما إذا كان الشهر الميلادي الكامل في الإجازة السنوية يُعدّ 26 يوم إجازة.

**لماذا يهم.** إذا أُشِّر عليه، ففي الإجازة السنوية التي تزيد على 27 يوماً يُعدّ كل شهر ميلادي كامل تشمله 26 يوم إجازة (ناقص العطلات الرسمية وأيام الراحة) بدلاً من عدّه يوماً بيوم.

**القواعد.** نعم أو لا.

## PayLeaveAsPerCutoff {#ManagePayrollSettings.PayLeaveAsPerCutoff}
<!-- alias: loc:PayLeaveAsPerCutoff, dom:PayrollSettings_PayLeaveAsPerCutoff -->

**ما هو.** ما إذا كانت الإجازة التي لم يُعد منها بعد تُصرف بحسب يوم الإقفال لمنطقة الرواتب.

**لماذا يهم.** إذا أُشِّر عليه، ففي التشغيل العادي لا يُعالَج من هذه الإجازات في ذلك الشهر إلا ما بدأ في يوم الإقفال للمنطقة أو قبله.

**القواعد.** نعم أو لا.

## RegularLeaveEncashOnBasicPayOnly {#ManagePayrollSettings.RegularLeaveEncashOnBasicPayOnly}
<!-- alias: loc:RegularLeaveEncashOnBasicPayOnly, dom:PayrollSettings_RegularLeaveEncashOnBasicPayOnly -->

**ما هو.** ما إذا كان صرف الإجازة نقداً في التشغيل العادي والمقدم يُقيَّم على الراتب الأساسي فقط.

**لماذا يهم.** إذا أُشِّر عليه يستخدم الصرف النقدي نوع الأجر `1000` وحده بدلاً من كل أنواع الأجور المكوّنة لأجر الإجازة.

**القواعد.** نعم أو لا.

## FnFLeaveEncashOnBasicPayOnly {#ManagePayrollSettings.FnFLeaveEncashOnBasicPayOnly}
<!-- alias: loc:FnFLeaveEncashOnBasicPayOnly, dom:PayrollSettings_FnFLeaveEncashOnBasicPayOnly -->

**ما هو.** ما إذا كان صرف الإجازة نقداً عند نهاية الخدمة يُقيَّم على الراتب الأساسي فقط.

**لماذا يهم.** إذا أُشِّر عليه، ففي تشغيل نهاية الخدمة يستخدم الصرف النقدي نوع الأجر `1000` وحده.

**القواعد.** نعم أو لا.

## EnableBasicpayMoreThenFixedDays {#ManagePayrollSettings.EnableBasicpayMoreThenFixedDays}
<!-- alias: loc:EnableBasicpayMoreThenFixedDays, dom:PayrollSettings_EnableBasicpayMoreThenFixedDays -->

**ما هو.** ما إذا كان الراتب الأساسي يُصرف رغم ذلك في شهر فيه 26 يوم إجازة أو أكثر.

**لماذا يهم.** إذا لم يُؤشَّر عليه لا يصرف الشهر الذي فيه 26 يوم إجازة أو أكثر أي أيام عمل من الراتب الأساسي -- إذ تغطي الإجازة الشهر. وإذا أُشِّر عليه تُعدّ أيام العمل رغم ذلك.

**القواعد.** نعم أو لا.

## EnableUnpaidDedSlabWise {#ManagePayrollSettings.EnableUnpaidDedSlabWise}
<!-- alias: loc:EnableUnpaidDedSlabWise, dom:PayrollSettings_EnableUnpaidDedSlabWise -->

**ما هو.** ما إذا كانت الأيام غير المدفوعة تُخصم من مكافأة نهاية الخدمة شريحةً بشريحة.

**لماذا يهم.** إذا أُشِّر عليه تُخصم أيام الإجازة غير المدفوعة والغياب من الخدمة في كل شريحة مكافأة تقع فيها. وإلا فتُخصم من مجموع الخدمة قبل تقسيمها على الشرائح.

**القواعد.** نعم أو لا.

## EnableFixedDaysLogicChange {#ManagePayrollSettings.EnableFixedDaysLogicChange}
<!-- alias: loc:EnableFixedDaysLogicChange, dom:PayrollSettings_EnableFixedDaysLogicChange -->

**ما هو.** ما إذا كان منطق معدَّل للأيام الثابتة مستخدماً.

**لماذا يهم.** لا يُقرأ حالياً -- تغييره لا أثر له.

**القواعد.** نعم أو لا.

## ChangePartialPeriodFactoringInFixedDaysBilling {#ManagePayrollSettings.ChangePartialPeriodFactoringInFixedDaysBilling}
<!-- alias: loc:ChangePartialPeriodFactoringInFixedDaysBilling, dom:PayrollSettings_ChangePartialPeriodFactoringInFixedDaysBilling -->

**ما هو.** ما إذا كان الموظف الذي يلتحق في بداية الشهر يبقى على الأجر بالأيام الثابتة.

**لماذا يهم.** إذا أُشِّر عليه، ومع تحديد **يوم إقفال الفترة الجزئية**، يُصرف للموظف الذي يلتحق قبل ذلك اليوم بالأيام الثابتة ناقص أيام العمل قبل التحاقه، بدلاً من الأيام الفعلية.

**القواعد.** نعم أو لا.

## FixedToActualWhenLeaveOverlapOnMonthStart {#ManagePayrollSettings.FixedToActualWhenLeaveOverlapOnMonthStart}
<!-- alias: loc:FixedToActualWhenLeaveOverlapOnMonthStart, dom:PayrollSettings_FixedToActualWhenLeaveOverlapOnMonthStart -->

**ما هو.** ما إذا كان الشهر يُصرف على أساس أيام العمل الفعلية عندما تمتد إجازة عبر بداية الشهر.

**لماذا يهم.** في الراتب الأساسي على *أيام ثابتة شهرياً* يُصرف الشهر عادةً بعدد الأيام الثابتة ناقص الغياب. وإذا أُشِّر عليه، يُحتسب الشهر الذي تمتد إجازة عبر بداية الشهر فيه على أساس أيام العمل الفعلية بدلاً من ذلك.

**القواعد.** نعم أو لا.

## FixedToActualWhenLeaveOverlapOnMonthEnd {#ManagePayrollSettings.FixedToActualWhenLeaveOverlapOnMonthEnd}
<!-- alias: loc:FixedToActualWhenLeaveOverlapOnMonthEnd, dom:PayrollSettings_FixedToActualWhenLeaveOverlapOnMonthEnd -->

**ما هو.** ما إذا كان الشهر يُصرف على أساس أيام العمل الفعلية عندما تمتد إجازة بعد نهاية الشهر.

**لماذا يهم.** في الراتب الأساسي على *أيام ثابتة شهرياً* يُصرف الشهر عادةً بعدد الأيام الثابتة ناقص الغياب. وإذا أُشِّر عليه، يُحتسب الشهر الذي تمتد إجازة بعد نهاية الشهر فيه على أساس أيام العمل الفعلية بدلاً من ذلك.

**القواعد.** نعم أو لا.

## FixedToActualWhenUpaidLeaveFound {#ManagePayrollSettings.FixedToActualWhenUpaidLeaveFound}
<!-- alias: loc:FixedToActualWhenUpaidLeaveFound, dom:PayrollSettings_FixedToActualWhenUpaidLeaveFound -->

**ما هو.** ما إذا كان الشهر يُصرف على أساس أيام العمل الفعلية عندما يكون للموظف إجازة غير مدفوعة.

**لماذا يهم.** في الراتب الأساسي على *أيام ثابتة شهرياً* يُصرف الشهر عادةً بعدد الأيام الثابتة ناقص الغياب. وإذا أُشِّر عليه، يُحتسب الشهر الذي يكون للموظف إجازة غير مدفوعة فيه على أساس أيام العمل الفعلية بدلاً من ذلك.

**القواعد.** نعم أو لا.

## FixedToActualWhenAbsentDayFound {#ManagePayrollSettings.FixedToActualWhenAbsentDayFound}
<!-- alias: loc:FixedToActualWhenAbsentDayFound, dom:PayrollSettings_FixedToActualWhenAbsentDayFound -->

**ما هو.** ما إذا كان الشهر يُصرف على أساس أيام العمل الفعلية عندما يكون للموظف أيام غياب.

**لماذا يهم.** في الراتب الأساسي على *أيام ثابتة شهرياً* يُصرف الشهر عادةً بعدد الأيام الثابتة ناقص الغياب. وإذا أُشِّر عليه، يُحتسب الشهر الذي يكون للموظف أيام غياب فيه على أساس أيام العمل الفعلية بدلاً من ذلك.

**القواعد.** نعم أو لا.

## PartialPeriodCutoffDayInFixedDaysBilling {#ManagePayrollSettings.PartialPeriodCutoffDayInFixedDaysBilling}
<!-- alias: loc:PartialPeriodCutoffDayInFixedDaysBilling, dom:PayrollSettings_PartialPeriodCutoffDayInFixedDaysBilling -->

**ما هو.** يوم الشهر الذي يبقى الملتحق قبله على الأجر بالأيام الثابتة.

**مثال.** `10`

**لماذا يهم.** يُستخدم مع **تغيير الاحتساب التناسبي للفترة الجزئية**. واليوم الذي يتجاوز طول الشهر يعني آخر يوم فيه.

**القواعد.** اختياري. رقم صحيح.

## OvertimeHoursBasis {#ManagePayrollSettings.OvertimeHoursBasis}
<!-- alias: loc:OvertimeHoursBasis, dom:PayrollSettings_OvertimeHoursBasis -->

**ما هو.** الساعات التي تُقيَّم بها ساعة العمل الإضافي.

**مثال.** ساعات ثابتة

**لماذا يهم.** *ساعات ثابتة* تقسم أجر اليوم على **ساعات العمل الافتراضية**، و*ساعات الدوام* تستخدم ساعات دوام الموظف في ذلك اليوم.

**القواعد.** إلزامي. إحدى القيم: ساعات ثابتة، ساعات الدوام.

## SocialSecurityRateRule {#ManagePayrollSettings.SocialSecurityRateRule}
<!-- alias: loc:SocialSecurityRateRule, dom:PayrollSettings_SocialSecurityRateRule -->

**ما هو.** الأجر الذي تُحتسب عليه التأمينات الاجتماعية.

**مثال.** الأجر والراتب

**لماذا يهم.** *الأجر والراتب* يستخدم أنواع الأجور المؤشَّر عليها *إدراج في التأمينات الاجتماعية*، و*راتب العقد* يستخدم راتب العقد في سجل الكفيل للموظف.

**القواعد.** إلزامي. إحدى القيم: الأجر والراتب، راتب العقد.

## CarryAllowanceOnSalaryImport {#ManagePayrollSettings.CarryAllowanceOnSalaryImport}
<!-- alias: loc:CarryAllowanceOnSalaryImport, dom:PayrollSettings_CarryAllowanceOnSalaryImport -->

**ما هو.** ما إذا كانت البدلات تُرحَّل عند استيراد الرواتب.

**لماذا يهم.** لا يُقرأ حالياً -- تغييره لا أثر له.

**القواعد.** نعم أو لا.

## AllowHealthReimburseBalCarryFwd {#ManagePayrollSettings.AllowHealthReimburseBalCarryFwd}
<!-- alias: loc:AllowHealthReimburseBalCarryFwd, dom:PayrollSettings_AllowHealthReimburseBalCarryFwd -->

**ما هو.** ما إذا كان رصيد استرداد المصاريف الصحية غير المستخدم يُرحَّل إلى سنة التأمين التالية.

**لماذا يهم.** إذا أُشِّر عليه يُضاف الرصيد غير المستخدم إلى رصيد السنة التالية، مع بقاء حد الدرجة.

**القواعد.** نعم أو لا.

## OffcycleBankFileCutoff {#ManagePayrollSettings.OffcycleBankFileCutoff}
<!-- alias: loc:OffcycleBankFileCutoff, dom:PayrollSettings_OffcycleBankFileCutoff -->

**ما هو.** يوم الشهر المستخدم تاريخَ إقفال لملفات البنك خارج الدورة.

**مثال.** `20`

**لماذا يهم.** القيمة 0 تعني اليوم الأول، واليوم الذي يتجاوز طول الشهر يعني آخر يوم فيه.

**القواعد.** اختياري. رقم صحيح.

## BankFileMinAmount {#ManagePayrollSettings.BankFileMinAmount}
<!-- alias: loc:BankFileMinAmount, dom:PayrollSettings_BankFileMinAmount -->

**ما هو.** أقل صافي راتب يُدرج في ملف البنك.

**مثال.** `1`

**لماذا يهم.** تُستبعد من الملف كشوف الرواتب التي صافيها أقل من هذا. والقيمة 0 تُدرج الكل.

**القواعد.** اختياري. رقم.

## PickOffcyclePayment {#ManagePayrollSettings.PickOffcyclePayment}
<!-- alias: loc:PickOffcyclePayment, dom:PayrollSettings_PickOffcyclePayment -->

**ما هو.** إلى أي مدى يبحث التشغيل التلقائي خارج الدورة عن الدفعات المقدمة.

**مثال.** التاريخ الحالي

**لماذا يهم.** مع التشغيل التلقائي لمحرك الوقت، *التاريخ الحالي* يلتقط الإجازات والدفعات المقدمة المؤرخة حتى اليوم، و*الشهر الحالي* يلتقط كل شيء حتى تاريخ الدفع.

**القواعد.** إلزامي. إحدى القيم: التاريخ الحالي، الشهر الحالي.

## RunOffCycleForFutureDate {#ManagePayrollSettings.RunOffCycleForFutureDate}
<!-- alias: loc:RunOffCycleForFutureDate, dom:PayrollSettings_RunOffCycleForFutureDate -->

**ما هو.** ما إذا كان التشغيل خارج الدورة يمكن أن يلتقط بنوداً مؤرخة بعد اليوم.

**لماذا يهم.** إذا لم يُؤشَّر عليه، لا يبحث التشغيل خارج الدورة للإجازة المقدمة إلا حتى اليوم، حتى لو كان تاريخ الدفع لاحقاً.

**القواعد.** نعم أو لا.

## AutoUpdateLoanRecoveryEndDate {#ManagePayrollSettings.AutoUpdateLoanRecoveryEndDate}
<!-- alias: loc:AutoUpdateLoanRecoveryEndDate, dom:PayrollSettings_AutoUpdateLoanRecoveryEndDate -->

**ما هو.** ما إذا كان تاريخ انتهاء استرداد القرض يتغير عند إيقاف الاسترداد.

**لماذا يهم.** إذا أُشِّر عليه فإن إضافة إعفاء من القرض أو تعديله يعيد احتساب تاريخ انتهاء استرداد القرض.

**القواعد.** نعم أو لا.

## EnableEffectiveDateInRecPayment {#ManagePayrollSettings.EnableEffectiveDateInRecPayment}
<!-- alias: loc:EnableEffectiveDateInRecPayment, dom:PayrollSettings_EnableEffectiveDateInRecPayment -->

**ما هو.** ما إذا كانت الدفعات المتكررة تستخدم تاريخ سريان.

**لماذا يهم.** لا يُقرأ حالياً -- تغييره لا أثر له.

**القواعد.** نعم أو لا.

## AllowDeleteBackdatedSalaryRegister {#ManagePayrollSettings.AllowDeleteBackdatedSalaryRegister}
<!-- alias: loc:AllowDeleteBackdatedSalaryRegister, dom:PayrollSettings_AllowDeleteBackdatedSalaryRegister -->

**ما هو.** ما إذا كان يجوز حذف كشوف الرواتب ذات التاريخ السابق.

**لماذا يهم.** لا يُقرأ حالياً -- تغييره لا أثر له.

**القواعد.** نعم أو لا.

## AutoFillFnFCompensation {#ManagePayrollSettings.AutoFillFnFCompensation}
<!-- alias: loc:AutoFillFnFCompensation, dom:PayrollSettings_AutoFillFnFCompensation -->

**ما هو.** ما إذا كانت التسوية النهائية تملأ أيام التعويض تلقائياً.

**لماذا يهم.** إذا أُشِّر عليه تُملأ أيام الإجازة المطلوب تعويضها برصيد الإجازة المتبقي، وأيام المكافأة بأيام المكافأة المقترحة.

**القواعد.** نعم أو لا.

## EditableAutoFillFnFCompensation {#ManagePayrollSettings.EditableAutoFillFnFCompensation}
<!-- alias: loc:EditableAutoFillFnFCompensation, dom:PayrollSettings_EditableAutoFillFnFCompensation -->

**ما هو.** ما إذا كان يمكن تعديل أيام التعويض المملوءة تلقائياً.

**لماذا يهم.** لا يُقرأ حالياً -- تغييره لا أثر له.

**القواعد.** نعم أو لا.

## ShowBillingTypeInPaymentScreen {#ManagePayrollSettings.ShowBillingTypeInPaymentScreen}
<!-- alias: loc:ShowBillingTypeInPaymentScreen, dom:PayrollSettings_ShowBillingTypeInPaymentScreen -->

**ما هو.** ما إذا كانت شاشة الدفعات تعرض نوع الفوترة.

**لماذا يهم.** لا يُقرأ حالياً -- تغييره لا أثر له.

**القواعد.** نعم أو لا.

## IncludeSalaryIncProvDiffInSalReg {#ManagePayrollSettings.IncludeSalaryIncProvDiffInSalReg}
<!-- alias: loc:IncludeSalaryIncProvDiffInSalReg, dom:PayrollSettings_IncludeSalaryIncProvDiffInSalReg -->

**ما هو.** ما إذا كان فرق المخصص الناتج عن زيادة الراتب يُكتب في كشف الراتب.

**لماذا يهم.** إذا أُشِّر عليه، ابتداءً من التاريخ أدناه، يسجّل مخصصا الإجازة السنوية ومكافأة نهاية الخدمة الفرق يوماً بيوم الناتج عن تغيير الراتب.

**القواعد.** نعم أو لا.

## IncludeSalaryIncProvDiffInSalRegEffDt {#ManagePayrollSettings.IncludeSalaryIncProvDiffInSalRegEffDt}
<!-- alias: loc:IncludeSalaryIncProvDiffInSalRegEffDt, dom:PayrollSettings_IncludeSalaryIncProvDiffInSalRegEffDt -->

**ما هو.** التاريخ الذي يُسجَّل منه فرق المخصص الناتج عن زيادة الراتب.

**مثال.** `2025-01-01`

**القواعد.** اختياري. تاريخ.
