---
helpId: wf.workflow-request.edit
lang: ar
title: طلبات سير العمل — إنشاء / تحرير
entity: HCMv3.Workflow.WorkflowRequest
surfaces:
  - angular/src/app/admin/workflow/workflow-requests/create-or-edit-workflow-request-modal.component.html
audience: [admin]
permissions: [Pages.Workflow.WorkflowRequests.Create]
---

## Overview

طلب واحد يمر عبر سلسلة موافقاته -- إجازة أو قرض أو طلب مستند. يُنشأ عند تقديم الطلب. يكتبه محرك سير العمل أثناء تنقّل الطلبات، ولا يوجد عادةً سبب لإضافة قيود أو تعديلها يدوياً.

## Employee {#WorkflowRequest.EmployeeId}
<!-- alias: loc:Employee, dom:WorkflowRequest_EmployeeId -->

**ما هو.** الموظف الذي يخصه الطلب.

**القواعد.** اختياري. يُختار من قائمة الموظفين.

## Approver {#WorkflowRequest.ApproverId}
<!-- alias: loc:Approver, dom:WorkflowRequest_ApproverId -->

**ما هو.** المعتمد الذي ينتظره الطلب حالياً.

**القواعد.** اختياري. يُختار من قائمة الموظفين.

## RequestType {#WorkflowRequest.RequestType}
<!-- alias: loc:RequestType, dom:WorkflowRequest_RequestType -->

**ما هو.** نوع الطلب، الذي يحدد السلسلة وما يحدث عند الموافقة.

**مثال.** طلب إجازة

**القواعد.** اختياري. الحد الأقصى 256 حرفاً.

## RequestNumber {#WorkflowRequest.RequestNumber}
<!-- alias: loc:RequestNumber, dom:WorkflowRequest_RequestNumber -->

**ما هو.** رقم الطلب.

**القواعد.** اختياري. الحد الأقصى 64 حرفاً.

## WorkflowStep {#WorkflowRequest.WorkflowStep}
<!-- alias: loc:WorkflowStep, dom:WorkflowRequest_WorkflowStep -->

**ما هو.** الخطوة التي فيها الطلب.

**القواعد.** إلزامي. رقم صحيح.

## RequestStatus {#WorkflowRequest.RequestStatus}
<!-- alias: loc:RequestStatus, dom:WorkflowRequest_RequestStatus -->

**ما هو.** حالة الطلب.

**لماذا يهم.** مفتوح أثناء انتظار الموافقة، وموافق عليه بعد الخطوة الأخيرة، ومرفوض، أو مسودة عند إعادته إلى منشئه.

**القواعد.** إلزامي. إحدى القيم: مفتوح، معتمد، مرفوض، مغلق، مسودة، محوّل.

## ApprovalProcessTime {#WorkflowRequest.ApprovalProcessTime}
<!-- alias: loc:ApprovalProcessTime, dom:WorkflowRequest_ApprovalProcessTime -->

**ما هو.** الأيام الكاملة من آخر تغيير على الطلب حتى الموافقة النهائية عليه.

**القواعد.** إلزامي. رقم صحيح.
