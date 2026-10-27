---
helpId: sys.time-settings.edit
lang: ar
title: Time settings
entity: HCMv3.SystemSettings.ManageTimeSettings
surfaces:
  - angular/src/app/admin/system-settings/manage-time-settings/create-or-edit-manage-time-settings-modal.component.html
audience: [admin]
permissions: [Pages.SystemSettings.ManageTimeSettings]
---

## Overview

سلوك الوقت والحضور على مستوى الشركة: من أين يبدأ الاحتساب، والمناوبات، وتكاملات الحضور، وكيف تجتمع العطلات مع أيام الراحة، والإجازة بالساعات، والعمل الإضافي، ومصادرة الإجازات، واعتماد سجلات الوقت.

وكثير من قواعد طلب الإجازة في النصف السفلي لا تُطبَّق حالياً، ومبيَّن ذلك.

لكل شركة سجل إعدادات واحد، ولا يمكن حذفه.

## EarliestEvaluationDate {#ManageTimeSettings.EarliestEvaluationDate}
<!-- alias: loc:EarliestEvaluationDate, dom:TimeSettings_EarliestEvaluationDate -->

**ما هو.** أول تاريخ يحتسبه محرك الوقت.

**مثال.** `2026-01-01`

**لماذا يهم.** لا يحتسب محرك الوقت الأيام السابقة له أبداً، وتبدأ منه عمليات استيراد الحضور إذا لم يُستورد شيء بعد، ويعامل نظام الرواتب تغييرات البيانات الأساسية المؤرخة قبله كأنها تمت فيه. اجعله تاريخ بدء التشغيل.

**القواعد.** اختياري. تاريخ.

## FutureEvaluationInDays {#ManageTimeSettings.FutureEvaluationInDays}
<!-- alias: loc:FutureEvaluationInDays, dom:TimeSettings_FutureEvaluationInDays -->

**ما هو.** عدد الأيام المقبلة التي يحتسبها محرك الوقت.

**لماذا يهم.** لا يُقرأ حالياً -- تغييره لا أثر له.

**القواعد.** اختياري. رقم صحيح.

## RosterPlannerFrequency {#ManageTimeSettings.RosterPlannerFrequency}
<!-- alias: loc:RosterPlannerFrequency, dom:TimeSettings_RosterPlannerFrequency -->

**ما هو.** عدد مرات تشغيل مخطط المناوبات.

**لماذا يهم.** لا يُقرأ حالياً -- تغييره لا أثر له.

**القواعد.** إلزامي. إحدى القيم: إسبوعي، شهري.

## EnableRosterDuty {#ManageTimeSettings.EnableRosterDuty}
<!-- alias: loc:EnableRosterDuty, dom:TimeSettings_EnableRosterDuty -->

**ما هو.** ما إذا كانت الدوامات تأتي من المناوبات.

**لماذا يهم.** إذا أُشِّر عليه يجد محرك الوقت ونظام الرواتب دوام كل يوم عبر ترتيب المناوبات -- الدوام البديل، ثم فترة دوام الموظف مع دورتها، ثم مجموعة المناوبة. وإلا فيبقى الدوام البديل مقدَّماً، ثم تُستخدم دوامات أيام الأسبوع في فترة دوام الموظف، وتُتجاهل دورات الأسابيع ومجموعات المناوبة.

**القواعد.** نعم أو لا.

## EnableTimeEngineAutorun {#ManageTimeSettings.EnableTimeEngineAutorun}
<!-- alias: loc:EnableTimeEngineAutorun, dom:TimeSettings_EnableTimeEngineAutorun -->

**ما هو.** ما إذا كان محرك الوقت يعمل تلقائياً.

**لماذا يهم.** إذا أُشِّر عليه تشغّل مهمة خلفية محرك الوقت تلقائياً، ويتغير أسلوب تشغيل الرواتب في اختيار الموظفين وتواريخ التشغيل خارج الدورة (انظر تشغيل الرواتب). وإلا فلا يعمل محرك الوقت إلا عند تشغيله يدوياً.

**القواعد.** نعم أو لا.

## EnableMiddleTierTimeEvents {#ManageTimeSettings.EnableMiddleTierTimeEvents}
<!-- alias: loc:EnableMiddleTierTimeEvents, dom:TimeSettings_EnableMiddleTierTimeEvents -->

**ما هو.** ما إذا كانت البصمات تُؤخذ من قاعدة بيانات الحضور الوسيطة.

**لماذا يهم.** إذا أُشِّر عليه تعمل مزامنة البصمات الوسيطة، وإلا فلا تفعل شيئاً.

**القواعد.** نعم أو لا.

## EnableGeoFence {#ManageTimeSettings.EnableGeoFence}
<!-- alias: loc:EnableGeoFence, dom:TimeSettings_EnableGeoFence -->

**ما هو.** ما إذا كان يجب أن تكون بصمات الجوال ضمن نطاق موقع العمل.

**لماذا يهم.** لا يُقرأ حالياً -- تغييره لا أثر له.

**القواعد.** نعم أو لا.

## EnableTimeAttendanceIntegration {#ManageTimeSettings.EnableTimeAttendanceIntegration}
<!-- alias: loc:EnableTimeAttendanceIntegration, dom:TimeSettings_EnableTimeAttendanceIntegration -->

**ما هو.** ما إذا كانت البصمات تُستورد من أجهزة الحضور.

**لماذا يهم.** إذا أُشِّر عليه يعمل الاستيراد من الأجهزة وإعادة السحب، وإلا فلا يفعلان شيئاً.

**القواعد.** نعم أو لا.

## EnableAttendanceOnHoliday {#ManageTimeSettings.EnableAttendanceOnHoliday}
<!-- alias: loc:EnableAttendanceOnHoliday, dom:TimeSettings_EnableAttendanceOnHoliday -->

**ما هو.** ما إذا كان محرك الوقت يسجّل المواظبة تلقائياً.

**لماذا يهم.** إذا أُشِّر عليه ينشئ محرك الوقت سجلات مواظبة الموظفين أو يحدّثها بنفسه أثناء معالجة كل يوم.

**القواعد.** نعم أو لا.

## EnableAttendanceOnWeekOff {#ManageTimeSettings.EnableAttendanceOnWeekOff}
<!-- alias: loc:EnableAttendanceOnWeekOff, dom:TimeSettings_EnableAttendanceOnWeekOff -->

**ما هو.** ما إذا كانت المواظبة تُسجَّل تلقائياً في أيام الراحة الأسبوعية.

**لماذا يهم.** لا يُقرأ حالياً -- تغييره لا أثر له.

**القواعد.** نعم أو لا.

## EnableAttendanceOnRestDay {#ManageTimeSettings.EnableAttendanceOnRestDay}
<!-- alias: loc:EnableAttendanceOnRestDay, dom:TimeSettings_EnableAttendanceOnRestDay -->

**ما هو.** ما إذا كانت المواظبة تُسجَّل تلقائياً في أيام الراحة.

**لماذا يهم.** لا يُقرأ حالياً -- تغييره لا أثر له.

**القواعد.** نعم أو لا.

## CountWeekoffOnPH {#ManageTimeSettings.CountWeekoffOnPH}
<!-- alias: loc:CountWeekoffOnPH, dom:TimeSettings_CountWeekoffOnPH -->

**ما هو.** ما إذا كانت العطلة الرسمية الواقعة في راحة أسبوعية تُعدّ راحة أسبوعية.

**لماذا يهم.** لا يمكن أن يكون اليوم الاثنين معاً. فإذا أُشِّر عليه يُعدّ راحة أسبوعية، وإلا فعطلة رسمية. وهذا يغيّر ما يصرفه نظام الرواتب عن اليوم.

**القواعد.** نعم أو لا.

## CountRestDayOnPH {#ManageTimeSettings.CountRestDayOnPH}
<!-- alias: loc:CountRestDayOnPH, dom:TimeSettings_CountRestDayOnPH -->

**ما هو.** ما إذا كانت العطلة الرسمية الواقعة في يوم راحة تُعدّ يوم راحة.

**لماذا يهم.** إذا أُشِّر عليه يُعدّ يوم راحة، وإلا فعطلة رسمية.

**القواعد.** نعم أو لا.

## EnableTimeAttIntegrationAPI {#ManageTimeSettings.EnableTimeAttIntegrationAPI}
<!-- alias: loc:EnableTimeAttIntegrationAPI, dom:TimeSettings_EnableTimeAttIntegrationAPI -->

**ما هو.** ما إذا كانت البصمات تُستورد من الواجهة البرمجية لنظام الحضور (BioTime).

**لماذا يهم.** هو المفتاح نفسه في **مركز التكامل ← BioTime**.

**القواعد.** نعم أو لا.

## AllowDuplicateInTimeEvent {#ManageTimeSettings.AllowDuplicateInTimeEvent}
<!-- alias: loc:AllowDuplicateInTimeEvent, dom:TimeSettings_AllowDuplicateInTimeEvent -->

**ما هو.** ما إذا كان يجوز الاحتفاظ ببصمتين متطابقتين.

**لماذا يهم.** لا يُقرأ حالياً -- تغييره لا أثر له.

**القواعد.** نعم أو لا.

## UseActualTransInPlannedTimePolicy {#ManageTimeSettings.UseActualTransInPlannedTimePolicy}
<!-- alias: loc:UseActualTransInPlannedTimePolicy, dom:TimeSettings_UseActualTransInPlannedTimePolicy -->

**ما هو.** ما إذا كانت بصمات موظفي الوقت المخطط تُقرأ رغم ذلك.

**لماذا يهم.** إذا لم يُؤشَّر عليه يُعدّ موظفو سياسة *الوقت المخطط* قد عملوا المناوبة المخططة. وإذا أُشِّر عليه تُقرأ بصمات دخولهم وخروجهم أيضاً.

**القواعد.** نعم أو لا.

## EnableMySqlIntegration {#ManageTimeSettings.EnableMySqlIntegration}
<!-- alias: loc:EnableMySqlIntegration, dom:TimeSettings_EnableMySqlIntegration -->

**ما هو.** ما إذا كانت البصمات تُستورد من قاعدة بيانات حضور MySQL.

**لماذا يهم.** إذا أُشِّر عليه يعمل الاستيراد من MySQL، وإلا فلا يفعل شيئاً.

**القواعد.** نعم أو لا.

## TimeEventImportPath {#ManageTimeSettings.TimeEventImportPath}
<!-- alias: loc:TimeEventImportPath, dom:TimeSettings_TimeEventImportPath -->

**ما هو.** المجلد الذي تُستورد منه ملفات البصمات.

**مثال.** `D:\Attendance\Import`

**لماذا يهم.** يفحص الاستيراد من الملفات هذا المجلد بحثاً عن ملفات البصمات.

**القواعد.** اختياري.

## TimeEventSyncBatchCount {#ManageTimeSettings.TimeEventSyncBatchCount}
<!-- alias: loc:TimeEventSyncBatchCount, dom:TimeSettings_TimeEventSyncBatchCount -->

**ما هو.** عدد البصمات المزامنة في المرة الواحدة.

**لماذا يهم.** لا يُقرأ حالياً -- تغييره لا أثر له.

**القواعد.** اختياري. رقم صحيح.

## TimeEventSyncFreq {#ManageTimeSettings.TimeEventSyncFreq}
<!-- alias: loc:TimeEventSyncFreq, dom:TimeSettings_TimeEventSyncFreq -->

**ما هو.** عدد مرات مزامنة البصمات.

**لماذا يهم.** لا يُقرأ حالياً -- تغييره لا أثر له.

**القواعد.** اختياري. رقم صحيح.

## MidTierSyncBatchCount {#ManageTimeSettings.MidTierSyncBatchCount}
<!-- alias: loc:MidTierSyncBatchCount, dom:TimeSettings_MidTierSyncBatchCount -->

**ما هو.** عدد البصمات الوسيطة المزامنة في المرة الواحدة.

**لماذا يهم.** لا يُقرأ حالياً -- تغييره لا أثر له.

**القواعد.** اختياري. رقم صحيح.

## MidTierSyncFreq {#ManageTimeSettings.MidTierSyncFreq}
<!-- alias: loc:MidTierSyncFreq, dom:TimeSettings_MidTierSyncFreq -->

**ما هو.** عدد مرات مزامنة البصمات الوسيطة.

**لماذا يهم.** لا يُقرأ حالياً -- تغييره لا أثر له.

**القواعد.** اختياري. رقم صحيح.

## ExcludePaidLeaveAccrualOnAbsent {#ManageTimeSettings.ExcludePaidLeaveAccrualOnAbsent}
<!-- alias: loc:ExcludePaidLeaveAccrualOnAbsent, dom:TimeSettings_ExcludePaidLeaveAccrualOnAbsent -->

**ما هو.** ما إذا كانت أيام الغياب لا تُكسب إجازة سنوية.

**لماذا يهم.** إذا أُشِّر عليه لا يراكم محرك الوقت إجازة سنوية عن يوم غاب فيه الموظف.

**القواعد.** نعم أو لا.

## HourlyLeavelogicEnable {#ManageTimeSettings.HourlyLeavelogicEnable}
<!-- alias: loc:HourlyLeavelogicEnable, dom:TimeSettings_HourlyLeavelogicEnable -->

**ما هو.** ما إذا كانت الإجازة بالساعات في بداية المناوبة أو نهايتها تنقل البصمة المتوقعة.

**لماذا يهم.** إذا أُشِّر عليه يأخذ محرك الوقت الإجازة بالساعات في الحسبان عند قراءة الدخول والخروج لموظفي الوقت الفعلي، فلا يُعدّ الانصراف المبكر أو التأخر بسبب الإجازة تأخيراً.

**القواعد.** نعم أو لا.

## HourlyLeaveShouldSignIn {#ManageTimeSettings.HourlyLeaveShouldSignIn}
<!-- alias: loc:HourlyLeaveShouldSignIn, dom:TimeSettings_HourlyLeaveShouldSignIn -->

**ما هو.** ما إذا كان يجب على الموظف تسجيل الدخول بعد الإجازة بالساعات في بداية المناوبة.

**لماذا يهم.** يُستخدم مع منطق الإجازة بالساعات.

**القواعد.** نعم أو لا.

## HourlyLeaveShouldSignOut {#ManageTimeSettings.HourlyLeaveShouldSignOut}
<!-- alias: loc:HourlyLeaveShouldSignOut, dom:TimeSettings_HourlyLeaveShouldSignOut -->

**ما هو.** ما إذا كان يجب على الموظف تسجيل الخروج قبل الإجازة بالساعات في نهاية المناوبة.

**لماذا يهم.** يُستخدم مع منطق الإجازة بالساعات.

**القواعد.** نعم أو لا.

## RestrictUnpaidleaveIfAnnualleaveAvailable {#ManageTimeSettings.RestrictUnpaidleaveIfAnnualleaveAvailable}
<!-- alias: loc:RestrictUnpaidleaveIfAnnualleaveAvailable, dom:TimeSettings_RestrictUnpaidleaveIfAnnualleaveAvailable -->

**ما هو.** ما إذا كانت الإجازة غير المدفوعة تُرفض ما دام رصيد إجازة سنوية باقياً.

**لماذا يهم.** لا يُقرأ حالياً -- تغييره لا أثر له.

**القواعد.** نعم أو لا.

## AllowPaidleaveBackdateEntry {#ManageTimeSettings.AllowPaidleaveBackdateEntry}
<!-- alias: loc:AllowPaidleaveBackdateEntry, dom:TimeSettings_AllowPaidleaveBackdateEntry -->

**ما هو.** ما إذا كان يجوز إدخال الإجازة المدفوعة بتاريخ سابق.

**لماذا يهم.** لا يُقرأ حالياً -- تغييره لا أثر له.

**القواعد.** نعم أو لا.

## SickLeaveAttachMandatory {#ManageTimeSettings.SickLeaveAttachMandatory}
<!-- alias: loc:SickLeaveAttachMandatory, dom:TimeSettings_SickLeaveAttachMandatory -->

**ما هو.** ما إذا كانت الإجازة المرضية تتطلب مرفقاً.

**لماذا يهم.** لا يُقرأ حالياً -- تغييره لا أثر له.

**القواعد.** نعم أو لا.

## EnableAutoReturnInConsecutiveLeave {#ManageTimeSettings.EnableAutoReturnInConsecutiveLeave}
<!-- alias: loc:EnableAutoReturnInConsecutiveLeave, dom:TimeSettings_EnableAutoReturnInConsecutiveLeave -->

**ما هو.** ما إذا كانت العودة تُسجَّل تلقائياً بين الإجازات المتتالية.

**لماذا يهم.** لا يُقرأ حالياً -- تغييره لا أثر له.

**القواعد.** نعم أو لا.

## AllowFutureDatedReturnFromLeave {#ManageTimeSettings.AllowFutureDatedReturnFromLeave}
<!-- alias: loc:AllowFutureDatedReturnFromLeave, dom:TimeSettings_AllowFutureDatedReturnFromLeave -->

**ما هو.** ما إذا كان يجوز أن يكون تاريخ العودة من الإجازة في المستقبل.

**لماذا يهم.** لا يُقرأ حالياً -- تغييره لا أثر له.

**القواعد.** نعم أو لا.

## EnableAutoHalfDayLeave {#ManageTimeSettings.EnableAutoHalfDayLeave}
<!-- alias: loc:EnableAutoHalfDayLeave, dom:TimeSettings_EnableAutoHalfDayLeave -->

**ما هو.** ما إذا كانت إجازة نصف اليوم تُنشأ تلقائياً.

**لماذا يهم.** لا يُقرأ حالياً -- تغييره لا أثر له.

**القواعد.** نعم أو لا.

## EnableSettleFractionalLeaveBalance {#ManageTimeSettings.EnableSettleFractionalLeaveBalance}
<!-- alias: loc:EnableSettleFractionalLeaveBalance, dom:TimeSettings_EnableSettleFractionalLeaveBalance -->

**ما هو.** ما إذا كانت أرصدة الإجازات الكسرية تُسوّى.

**لماذا يهم.** لا يُقرأ حالياً -- تغييره لا أثر له.

**القواعد.** نعم أو لا.

## EnableBookingQuotaInLeaveRequest {#ManageTimeSettings.EnableBookingQuotaInLeaveRequest}
<!-- alias: loc:EnableBookingQuotaInLeaveRequest, dom:TimeSettings_EnableBookingQuotaInLeaveRequest -->

**ما هو.** ما إذا كان طلب الإجازة يحجز الرصيد فوراً.

**لماذا يهم.** لا يُقرأ حالياً -- تغييره لا أثر له.

**القواعد.** نعم أو لا.

## LeaveDeductionStartMonth {#ManageTimeSettings.LeaveDeductionStartMonth}
<!-- alias: loc:LeaveDeductionStartMonth, dom:TimeSettings_LeaveDeductionStartMonth -->

**ما هو.** ما إذا كانت الإجازة تُخصم من الشهر الذي تبدأ فيه.

**لماذا يهم.** لا يُقرأ حالياً -- تغييره لا أثر له.

**القواعد.** اختياري. رقم صحيح.

## ForfeitRule {#ManageTimeSettings.ForfeitRule}
<!-- alias: loc:ForfeitRule, dom:TimeSettings_ForfeitRule -->

**ما هو.** متى تُصادر الإجازة السنوية التي تتجاوز الحد الأقصى للمجموعة الفرعية.

**مثال.** لا شيء

**لماذا يهم.** *لا شيء* يخفض الرصيد إلى الحد الأقصى يوماً بيوم أثناء تشغيل محرك الوقت. و*شهري* و*سنوي* يسمحان بتجاوزه ويسجّلان المصادرة في نهاية كل شهر أو سنة. وتتبع تقارير مخصص الإجازات والإجازة السنوية القاعدة نفسها.

**القواعد.** إلزامي. إحدى القيم: لا شيء، شهري، سنوي.

## AbsenceViolationRuleId {#ManageTimeSettings.AbsenceViolationRuleId}
<!-- alias: loc:AbsenceViolationRuleId, dom:TimeSettings_AbsenceViolationRuleId -->

**ما هو.** قاعدة مخالفة الغياب المطبقة على الموظفين.

**لماذا يهم.** لا يُقرأ حالياً -- تغييره لا أثر له.

**القواعد.** اختياري. يُختار من قائمة.

## AbsenceViolationEffectiveDate {#ManageTimeSettings.AbsenceViolationEffectiveDate}
<!-- alias: loc:AbsenceViolationEffectiveDate, dom:TimeSettings_AbsenceViolationEffectiveDate -->

**ما هو.** التاريخ الذي يبدأ منه فحص مخالفات الغياب.

**لماذا يهم.** لا يُقرأ حالياً -- تغييره لا أثر له.

**القواعد.** اختياري. تاريخ.

## EnableOvertimeInTimeEngine {#ManageTimeSettings.EnableOvertimeInTimeEngine}
<!-- alias: loc:EnableOvertimeInTimeEngine, dom:TimeSettings_EnableOvertimeInTimeEngine -->

**ما هو.** ما إذا كان محرك الوقت يحتسب العمل الإضافي.

**لماذا يهم.** إذا لم يُؤشَّر عليه لا تُنتج أي دقائق عمل إضافي أو سجلات عمل إضافي، مهما كانت الدوامات والسياسات.

**القواعد.** نعم أو لا.

## EnableIsTravelling {#ManageTimeSettings.EnableIsTravelling}
<!-- alias: loc:EnableIsTravelling, dom:TimeSettings_EnableIsTravelling -->

**ما هو.** ما إذا كان يمكن تأشير الموظفين على أنهم مسافرون.

**لماذا يهم.** لا يُقرأ حالياً -- تغييره لا أثر له.

**القواعد.** نعم أو لا.

## EnablePayFullWithPayroll {#ManageTimeSettings.EnablePayFullWithPayroll}
<!-- alias: loc:EnablePayFullWithPayroll, dom:TimeSettings_EnablePayFullWithPayroll -->

**ما هو.** ما إذا كان يمكن صرف الإجازة كاملة مع الرواتب.

**لماذا يهم.** لا يُقرأ حالياً -- تغييره لا أثر له.

**القواعد.** نعم أو لا.

## EnableTimeRegisterApproval {#ManageTimeSettings.EnableTimeRegisterApproval}
<!-- alias: loc:EnableTimeRegisterApproval, dom:TimeSettings_EnableTimeRegisterApproval -->

**ما هو.** ما إذا كان يجب اعتماد سجلات الوقت قبل الرواتب.

**لماذا يهم.** إذا أُشِّر عليه يستخدم نظام الرواتب الأرقام المعتمدة ويرفض التشغيل للموظف الذي له أي يوم غير معتمد في الفترة.

**القواعد.** نعم أو لا.

## AutoApproveTimeLog {#ManageTimeSettings.AutoApproveTimeLog}
<!-- alias: loc:AutoApproveTimeLog, dom:TimeSettings_AutoApproveTimeLog -->

**ما هو.** ما إذا كانت الأيام التي لا شيء فيها للمراجعة تُعتمد تلقائياً.

**لماذا يهم.** يُستخدم مع اعتماد سجل الوقت: يعتمد محرك الوقت اليوم بنفسه إذا لم يكن فيه ما يستحق الاعتماد.

**القواعد.** نعم أو لا.
