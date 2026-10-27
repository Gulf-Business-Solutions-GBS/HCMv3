---
helpId: wf.workflow-remark.edit
lang: ar
title: ملاحظات سير العمل — إنشاء / تحرير
entity: HCMv3.Workflow.WorkflowRemark
surfaces:
  - angular/src/app/admin/workflow/workflow-remarks/create-or-edit-workflow-remark-modal.component.html
audience: [admin]
permissions: [Pages.Workflow.WorkflowRemarks.Create]
---

## Overview

طلب رأي: يُطلب من شخص من خارج سلسلة الموافقات إبداء رأيه في طلب. ويمكنه عندئذ فتح الطلب وسجل نشاطه، حيث يظهر الطلب كحدث طلب رأي. ولا يمكن سؤال الشخص نفسه إلا مرة واحدة لكل طلب.

## WorkflowRequests {#WorkflowRemark.WorkflowRequestId}
<!-- alias: loc:WorkflowRequests, dom:WorkflowRemark_WorkflowRequestId -->

**ما هو.** طلب سير العمل الذي يخصه هذا السجل.

**القواعد.** إلزامي. يُختار من قائمة طلبات سير العمل.

## CCReceiver {#WorkflowRemark.CCReceiverId}
<!-- alias: loc:CCReceiver, dom:WorkflowRemark_CCReceiverId -->

**ما هو.** الشخص المطلوب رأيه.

**القواعد.** إلزامي. يُختار من قائمة الموظفين.
