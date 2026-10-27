---
helpId: wf.wfread-log.edit
lang: ar
title: سجلات قراءة سير العمل — إنشاء / تحرير
entity: HCMv3.Workflow.WFReadLog
surfaces:
  - angular/src/app/admin/workflow/wf-read-logs/create-wf-read-log-modal.component.html
audience: [admin]
permissions: [Pages.Workflow.WFReadLogs.Create]
---

## Overview

فتح شخص طلباً في خطوة يكون فيها سجل القراءة مفعّلاً. يكتبه محرك سير العمل أثناء تنقّل الطلبات، ولا يوجد عادةً سبب لإضافة قيود أو تعديلها يدوياً.

## WorkflowRequests {#WFReadLog.WorkflowRequestId}
<!-- alias: loc:WorkflowRequests, dom:WFReadLog_WorkflowRequestId -->

**ما هو.** طلب سير العمل الذي يخصه هذا السجل.

**القواعد.** إلزامي. يُختار من قائمة طلبات سير العمل.

## User {#WFReadLog.User}
<!-- alias: loc:User, dom:WFReadLog_User -->

**ما هو.** الموظف الذي فتحه، برقمه الداخلي في النظام.

**القواعد.** اختياري. الحد الأقصى 256 حرفاً.
