---
helpId: wf.request-detail.edit
lang: ar
title: تفاصيل الطلبات — إنشاء / تحرير
entity: HCMv3.Workflow.RequestDetail
surfaces:
  - angular/src/app/admin/workflow/request-details/create-or-edit-request-detail-modal.component.html
audience: [admin]
permissions: [Pages.Workflow.RequestDetails.Create]
---

## Overview

خطوة واحدة من سلسلة موافقات: من يوافق عليها، وهل يكفي واحد منهم أم يجب أن يوافقوا جميعاً، وما المسموح لهم، ومتى يُذكَّر بالطلب أو يُصعَّد. تُرقّم الخطوات من 1 وينتقل الطلب خطوة واحدة عند كل موافقة، والموافقة على الخطوة الأخيرة هي موافقة على الطلب. ويجب أن تكون أيام التذكير أقل من أيام التصعيد.

## RequestMaster {#RequestDetail.RequestMasterId}
<!-- alias: loc:RequestMaster, dom:RequestDetail_RequestMasterName -->

**ما هو.** سلسلة الموافقات التي تنتمي إليها هذه الخطوة.

**القواعد.** اختياري. يُختار من قائمة الطلبات الأساسية.

## Step {#RequestDetail.Step}
<!-- alias: loc:Step, dom:RequestDetail_Step -->

**ما هو.** موقع الخطوة في السلسلة، بدءاً من 1.

**مثال.** 2

**القواعد.** إلزامي. رقم صحيح.

## WorkflowRole {#RequestDetail.WorkflowRoleId}
<!-- alias: loc:WorkflowRole, dom:RequestDetail_WorkflowRoleId -->

**ما هو.** دور سير العمل الذي يوافق شاغلوه على هذه الخطوة.

**مثال.** مسؤول الرواتب

**لماذا يهم.** يُستخدم فقط عندما يكون الشخص المعتمد "شخص": يصبح كل موظف يشغل الدور معتمداً، مع التضييق حسب مجموعة دور سير العمل. ولا يُستخدم مع خيارات المديرين.

**القواعد.** اختياري. يُختار من قائمة أدوار سير العمل.

## ApprovalRequire {#RequestDetail.ApprovalRequire}
<!-- alias: loc:ApprovalRequire, dom:RequestDetail_ApprovalRequire -->

**ما هو.** ما إذا كان معتمد واحد يكفي أم يجب أن يوافق الجميع.

**لماذا يهم.** فردي: أول معتمد يوافق يُكمل الخطوة. متوازٍ: لا تكتمل الخطوة إلا عندما يوافق كل المعتمدين عليها.

**القواعد.** إلزامي. إحدى القيم: Single، Parallel.

## WorkflowRoleGroup {#RequestDetail.WorkflowRoleGroup}
<!-- alias: loc:WorkflowRoleGroup, dom:RequestDetail_WorkflowRoleGroup -->

**ما هو.** يحصر شاغلي الدور في جزء المنظمة الخاص بمقدّم الطلب.

**لماذا يهم.** القسم يُبقي الشاغلين في الوحدة التنظيمية الحالية لمقدّم الطلب، والفرع من هم في موقع العمل نفسه، ومركز التكلفة من هم على مركز التكلفة نفسه. ولا شيء والشركة يُبقيان الجميع. وإذا لم يطابق أحد، يُبقى جميع الشاغلين بدلاً من لا أحد.

**القواعد.** إلزامي. إحدى القيم: لا شيء، الشركة، الإدارة، الفرع، مركز التكلفة.

## OrganizationUnit {#RequestDetail.WorkflowRoleOrgId}
<!-- alias: loc:OrgLevel, dom:RequestDetail_WorkflowRoleOrgId -->

**ما هو.** مستوى وحدة تنظيمية للدور.

**لماذا يهم.** لا يُقرأ حالياً.

**القواعد.** اختياري. يُختار من قائمة أنواع وحدات الشركة.

## Person {#RequestDetail.Person}
<!-- alias: loc:Person, dom:RequestDetail_Person -->

**ما هو.** طلب من تنطبق عليه الخطوة.

**لماذا يهم.** لا يُقرأ حالياً.

**القواعد.** إلزامي. إحدى القيم: لا شيء، Themselves، الإدارة، SelfSubordinates.

## ApproverPerson {#RequestDetail.ApproverPerson}
<!-- alias: loc:ApproverPerson, dom:RequestDetail_ApproverPerson -->

**ما هو.** من يوافق: شاغلو الدور، أو مدير أعلى من مقدّم الطلب.

**مثال.** مدير الشخص

**لماذا يهم.** "شخص" يستخدم دور سير العمل. و"مدير الشخص" هو المدير المباشر لمقدّم الطلب، ومستوى المدير 2 إلى 7 يصعد ذلك العدد من المستويات في خط الإبلاغ. وإذا انتهى الخط قبل ذلك المستوى، أو كان الخيار "لا شيء"، يُستخدم المعتمد الافتراضي للسلسلة.

**القواعد.** إلزامي. إحدى القيم: لا شيء، الشخص، PersonManager، ManagerLevel2، ManagerLevel3، ManagerLevel4، ManagerLevel5، ManagerLevel6، ManagerLevel7.

## EscalateAfterDays {#RequestDetail.EscalateAfterDays}
<!-- alias: loc:EscalateAfterDays, dom:RequestDetail_EscalateAfterDays -->

**ما هو.** عدد الأيام دون إجراء قبل تصعيد الطلب.

**مثال.** 5

**لماذا يهم.** تُحسب من آخر إجراء مسجّل على الطلب. ولا يُستخدم إلا عند تحديد "يجب التصعيد"، والقيمة 0 توقف التصعيد.

**القواعد.** اختياري. رقم صحيح.

## EscalateWorkflowRole {#RequestDetail.EscalateWorkflowRoleId}
<!-- alias: loc:EscalateWorkflowRole, dom:RequestDetail_EscalateWorkflowRoleId -->

**ما هو.** دور سير العمل الذي يُسلَّم إليه الطلب المتوقف.

**لماذا يهم.** يُستخدم عندما لا يُحدَّد "التصعيد للمستوى التالي": يبقى الطلب في الخطوة نفسها ويُستبدل معتمدوه بشاغلي هذا الدور، مع استبعاد من كان عليه أصلاً.

**القواعد.** اختياري. يُختار من قائمة أدوار سير العمل.

## EscalateWorkflowRoleGroup {#RequestDetail.EscalateWorkflowRoleGroup}
<!-- alias: loc:EscalateWorkflowRoleGroup, dom:RequestDetail_EscalateWorkflowRoleGroup -->

**ما هو.** يحصر شاغلي دور التصعيد في جزء المنظمة الخاص بمقدّم الطلب.

**لماذا يهم.** يعمل بالطريقة نفسها التي تعمل بها مجموعة دور سير العمل.

**القواعد.** إلزامي. إحدى القيم: لا شيء، الشركة، الإدارة، الفرع، مركز التكلفة.

## EscalateWorkflowRoleOrg {#RequestDetail.EscalateWorkflowRoleOrgId}
<!-- alias: loc:EscalateWorkflowRoleOrg, dom:RequestDetail_EscalateWorkflowRoleOrgId -->

**ما هو.** مستوى وحدة تنظيمية لدور التصعيد.

**لماذا يهم.** لا يُقرأ حالياً.

**القواعد.** اختياري. يُختار من قائمة أنواع وحدات الشركة.

## ReminderDays {#RequestDetail.ReminderDays}
<!-- alias: loc:ReminderDays, dom:RequestDetail_ReminderDays -->

**ما هو.** عدد الأيام بين كل تذكير للمعتمدين الذين لم يتخذوا إجراءً.

**مثال.** 2 -- تذكيرات في الأيام 2 و4 و6

**لماذا يهم.** يُرسل تذكير إلى المعتمدين الذين لم يتخذوا إجراءً في كل مرة تبلغ فيها الأيام منذ آخر إجراء مضاعفاً لهذا العدد. والقيمة 0 لا ترسل تذكيرات.

**القواعد.** اختياري. رقم صحيح.

## OnSubmit {#RequestDetail.SubmitNotificationConfigId}
<!-- alias: loc:OnSubmit, dom:RequestDetail_SubmitNotificationConfigId -->

**ما هو.** من يتم إشعاره عند وصول الطلب إلى هذه الخطوة.

**لماذا يهم.** يُضبط كل إجراء على حدة، فمن يُشعَر بالرفض ليس بالضرورة من يُشعَر بالاعتماد. وإذا تُرك فارغاً فلن يُرسل أي إشعار عن ذلك الإجراء.

**ماذا يُرسل.** الإعداد المختار هو من يحدد المستلمين وطريقة الإرسال: إشعار داخل النظام أو بريد إلكتروني أو كليهما. والإعداد الذي لا يحمل قالب بريد يُشعر داخل النظام فقط.

**القواعد.** اختياري. يُختار من قائمة إعدادات الإخطار.

## OnApprove {#RequestDetail.NotificationConfigId}
<!-- alias: loc:OnApprove, dom:RequestDetail_NotificationConfigId -->

**ما هو.** من يتم إشعاره عند اعتماد هذه الخطوة.

**لماذا يهم.** يُضبط كل إجراء على حدة، فمن يُشعَر بالرفض ليس بالضرورة من يُشعَر بالاعتماد. وإذا تُرك فارغاً فلن يُرسل أي إشعار عن ذلك الإجراء.

**ماذا يُرسل.** الإعداد المختار هو من يحدد المستلمين وطريقة الإرسال: إشعار داخل النظام أو بريد إلكتروني أو كليهما. والإعداد الذي لا يحمل قالب بريد يُشعر داخل النظام فقط.

**القواعد.** اختياري. يُختار من قائمة إعدادات الإخطار.

## OnReject {#RequestDetail.RejectNotificationConfigId}
<!-- alias: loc:OnReject, dom:RequestDetail_RejectNotificationConfigId -->

**ما هو.** من يتم إشعاره عند رفض هذه الخطوة.

**لماذا يهم.** يُضبط كل إجراء على حدة، فمن يُشعَر بالرفض ليس بالضرورة من يُشعَر بالاعتماد. وإذا تُرك فارغاً فلن يُرسل أي إشعار عن ذلك الإجراء.

**ماذا يُرسل.** الإعداد المختار هو من يحدد المستلمين وطريقة الإرسال: إشعار داخل النظام أو بريد إلكتروني أو كليهما. والإعداد الذي لا يحمل قالب بريد يُشعر داخل النظام فقط.

**القواعد.** اختياري. يُختار من قائمة إعدادات الإخطار.

## OnSendBack {#RequestDetail.SendBackNotificationConfigId}
<!-- alias: loc:OnSendBack, dom:RequestDetail_SendBackNotificationConfigId -->

**ما هو.** من يتم إشعاره عند إعادة الطلب من هذه الخطوة.

**لماذا يهم.** يُضبط كل إجراء على حدة، فمن يُشعَر بالرفض ليس بالضرورة من يُشعَر بالاعتماد. وإذا تُرك فارغاً فلن يُرسل أي إشعار عن ذلك الإجراء.

**ماذا يُرسل.** الإعداد المختار هو من يحدد المستلمين وطريقة الإرسال: إشعار داخل النظام أو بريد إلكتروني أو كليهما. والإعداد الذي لا يحمل قالب بريد يُشعر داخل النظام فقط.

**القواعد.** اختياري. يُختار من قائمة إعدادات الإخطار.

## OnRemark {#RequestDetail.RemarksNotificationConfigId}
<!-- alias: loc:OnRemark, dom:RequestDetail_RemarksNotificationConfigId -->

**ما هو.** من يتم إشعاره عند إضافة تعليق على الطلب.

**لماذا يهم.** يُضبط كل إجراء على حدة، فمن يُشعَر بالرفض ليس بالضرورة من يُشعَر بالاعتماد. وإذا تُرك فارغاً فلن يُرسل أي إشعار عن ذلك الإجراء.

**ماذا يُرسل.** الإعداد المختار هو من يحدد المستلمين وطريقة الإرسال: إشعار داخل النظام أو بريد إلكتروني أو كليهما. والإعداد الذي لا يحمل قالب بريد يُشعر داخل النظام فقط.

**القواعد.** اختياري. يُختار من قائمة إعدادات الإخطار.

## OnReassign {#RequestDetail.AssignNotificationConfigId}
<!-- alias: loc:OnReassign, dom:RequestDetail_AssignNotificationConfigId -->

**ما هو.** من يتم إشعاره عند إعادة إسناد الطلب.

**لماذا يهم.** يُضبط كل إجراء على حدة، فمن يُشعَر بالرفض ليس بالضرورة من يُشعَر بالاعتماد. وإذا تُرك فارغاً فلن يُرسل أي إشعار عن ذلك الإجراء.

**ماذا يُرسل.** الإعداد المختار هو من يحدد المستلمين وطريقة الإرسال: إشعار داخل النظام أو بريد إلكتروني أو كليهما. والإعداد الذي لا يحمل قالب بريد يُشعر داخل النظام فقط.

**القواعد.** اختياري. يُختار من قائمة إعدادات الإخطار.

## OnReminder {#RequestDetail.ReminderNotificationConfigId}
<!-- alias: loc:OnReminder, dom:RequestDetail_ReminderNotificationConfigId -->

**ما هو.** من يتم إشعاره عند بقاء الطلب بانتظار الإجراء.

**لماذا يهم.** يُضبط كل إجراء على حدة، فمن يُشعَر بالرفض ليس بالضرورة من يُشعَر بالاعتماد. وإذا تُرك فارغاً فلن يُرسل أي إشعار عن ذلك الإجراء.

**ماذا يُرسل.** الإعداد المختار هو من يحدد المستلمين وطريقة الإرسال: إشعار داخل النظام أو بريد إلكتروني أو كليهما. والإعداد الذي لا يحمل قالب بريد يُشعر داخل النظام فقط.

**القواعد.** اختياري. يُختار من قائمة إعدادات الإخطار.

## ShowPayment {#RequestDetail.ShowPayment}
<!-- alias: loc:ShowPayment, dom:RequestDetail_ShowPayment -->

**ما هو.** ما إذا كان المعتمد يرى تفاصيل الدفع.

**لماذا يهم.** لا يُقرأ حالياً.

**القواعد.** نعم أو لا.

## CanSendBack {#RequestDetail.CanSendBack}
<!-- alias: loc:CanSendBack, dom:RequestDetail_CanSendBack -->

**ما هو.** ما إذا كان يجوز للمعتمدين في هذه الخطوة إعادة الطلب.

**لماذا يهم.** الإعادة تُرجع الطلب إلى منشئه كمسودة في الخطوة 1، حيث يمكن تعديله وتقديمه من جديد. وعند عدم التحديد يُخفى الخيار ويُرفض.

**القواعد.** نعم أو لا.

## CanReject {#RequestDetail.CanReject}
<!-- alias: loc:CanReject, dom:RequestDetail_CanReject -->

**ما هو.** ما إذا كان يجوز للمعتمدين في هذه الخطوة رفض الطلب.

**لماذا يهم.** الرفض يُنهي الطلب. وعند عدم التحديد يُخفى الخيار ويُرفض.

**القواعد.** نعم أو لا.

## ShouldEscalate {#RequestDetail.ShouldEscalate}
<!-- alias: loc:ShouldEscalate, dom:RequestDetail_ShouldEscalate -->

**ما هو.** ما إذا كان الطلب المتوقف في هذه الخطوة يُصعَّد.

**لماذا يهم.** يتطلب أن تكون أيام التصعيد أكبر من 0.

**القواعد.** نعم أو لا.

## EscalateNextLevel {#RequestDetail.EscalateNextLevel}
<!-- alias: loc:EscalateNextLevel, dom:RequestDetail_EscalateNextLevel -->

**ما هو.** ما إذا كان التصعيد ينقل الطلب إلى الخطوة التالية.

**لماذا يهم.** عند التحديد، ينتقل الطلب المتوقف إلى الخطوة التالية كأنه وُوفق عليه، لكن المعاملة نفسها لا تُطبَّق -- فذلك لا يحدث إلا بموافقة شخص على الخطوة الأخيرة. ويُترك الطلب الذي في خطوته الأخيرة مكانه. وعند عدم التحديد، يتولى دور التصعيد الخطوة نفسها.

**القواعد.** نعم أو لا.

## EnableReadLog {#RequestDetail.EnableReadLog}
<!-- alias: loc:EnableReadLog, dom:RequestDetail_EnableReadLog -->

**ما هو.** ما إذا كان فتح الطلب في هذه الخطوة يُسجَّل.

**لماذا يهم.** في أول مرة يفتحه فيها كل شخص في هذه الخطوة، يُضاف قيد قراءة إلى سجل القراءة وإلى سجل النشاط.

**القواعد.** نعم أو لا.

## IsCommentRequire {#RequestDetail.IsCommentRequire}
<!-- alias: loc:IsCommentRequire, dom:RequestDetail_IsCommentRequire -->

**ما هو.** ما إذا كان على المعتمدين كتابة تعليق للموافقة.

**لماذا يهم.** تُرفض الموافقة دون تعليق.

**القواعد.** نعم أو لا.

## SignatoryStep {#RequestDetail.SignatoryStep}
<!-- alias: loc:SignatoryStep, dom:RequestDetail_SignatoryStep -->

**ما هو.** ما إذا كانت هذه خطوة توقيع تحتاج شخصاً لم يوقّع من قبل.

**لماذا يهم.** يُستبعد هنا كل من وافق على خطوة توقيع سابقة من الطلب نفسه، حتى لا يوقّع شخص واحد مرتين. وإذا لم يبقَ أحد، يُستخدم المعتمد الافتراضي للسلسلة.

**القواعد.** نعم أو لا.
