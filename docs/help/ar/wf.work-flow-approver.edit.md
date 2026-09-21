---
helpId: wf.work-flow-approver.edit
lang: ar
title: معتمدو سير العمل — إنشاء / تحرير
entity: HCMv3.Workflow.WorkFlowApprover
surfaces:
  - angular/src/app/admin/workflow/work-flow-approvers/create-or-edit-work-flow-approver-modal.component.html
audience: [admin]
permissions: [Pages.Workflow.WorkFlowApprovers.Create]
---

## Overview

شخص واحد ينتظره الطلب في خطوته الحالية. وفي الخطوة المتوازية يوجد سجل لكل معتمد. يكتبه محرك سير العمل أثناء تنقّل الطلبات، ولا يوجد عادةً سبب لإضافة قيود أو تعديلها يدوياً.

## WorkflowRequests {#WorkFlowApprover.WorkflowRequestId}
<!-- alias: loc:WorkflowRequests, dom:WorkFlowApprover_WorkflowRequestId -->

**ما هو.** طلب سير العمل الذي يخصه هذا السجل.

**القواعد.** إلزامي. يُختار من قائمة طلبات سير العمل.

## Approver {#WorkFlowApprover.ApproverId}
<!-- alias: loc:Approver, dom:WorkFlowApprover_ApproverId -->

**ما هو.** المعتمد.

**القواعد.** اختياري. يُختار من قائمة الموظفين.

## IsApproved {#WorkFlowApprover.IsApproved}
<!-- alias: loc:IsApproved, dom:WorkFlowApprover_IsApproved -->

**ما هو.** ما إذا كان هذا المعتمد قد وافق.

**القواعد.** نعم أو لا.
