---
helpId: wf.request-log.edit
lang: ar
title: سجلات الطلبات — إنشاء / تحرير
entity: HCMv3.Workflow.RequestLog
surfaces:
  - angular/src/app/admin/workflow/request-logs/create-request-log-modal.component.html
audience: [admin]
permissions: [Pages.Workflow.RequestLogs.Create]
---

## Overview

إجراء واحد اتُّخذ على طلب -- إنشاء أو موافقة أو رفض أو إعادة أو تصعيد أو قراءة. يغذّي سجل نشاط الطلب وسجل الموافقات في رسائل البريد. يكتبه محرك سير العمل أثناء تنقّل الطلبات، ولا يوجد عادةً سبب لإضافة قيود أو تعديلها يدوياً.

## WorkflowRequests {#RequestLog.WorkflowRequestId}
<!-- alias: loc:WorkflowRequests, dom:RequestLog_WorkflowRequestId -->

**ما هو.** طلب سير العمل الذي يخصه هذا السجل.

**القواعد.** إلزامي. يُختار من قائمة طلبات سير العمل.

## ActionBy {#RequestLog.ActionById}
<!-- alias: loc:ActionBy, dom:RequestLog_ActionById -->

**ما هو.** من اتخذ الإجراء.

**القواعد.** اختياري. يُختار من قائمة الموظفين.

## ActionType {#RequestLog.ActionType}
<!-- alias: loc:ActionType, dom:RequestLog_ActionType -->

**ما هو.** ما الذي تم.

**مثال.** موافق عليه

**القواعد.** إلزامي. إحدى القيم: Created، معتمد، مرفوض، Escalated، Read، SentBack، مغلق، Remarked، Assigned، محوّل، SentBackToCreator، RecordedDirectly.

## WorkflowStep {#RequestLog.WorkflowStep}
<!-- alias: loc:WorkflowStep, dom:RequestLog_WorkflowStep -->

**ما هو.** الخطوة التي تم فيها.

**القواعد.** إلزامي. رقم صحيح.

## ActionDays {#RequestLog.ActionDays}
<!-- alias: loc:ActionDays, dom:RequestLog_ActionDays -->

**ما هو.** الأيام التي استغرقها الإجراء.

**لماذا يهم.** لا يُقرأ حالياً.

**القواعد.** إلزامي. رقم صحيح.

## Platform {#RequestLog.Platform}
<!-- alias: loc:Platform, dom:RequestLog_Platform -->

**ما هو.** المصدر الذي جاء منه الإجراء.

**القواعد.** إلزامي. إحدى القيم: Core، ESS، الهاتف المتنقل.

## RequestActionComment {#RequestLog.RequestActionComment}
<!-- alias: loc:RequestActionComment, dom:RequestLog_RequestActionComment -->

**ما هو.** التعليق المرفق بالإجراء.

**القواعد.** اختياري. الحد الأقصى 4000 حرف.
