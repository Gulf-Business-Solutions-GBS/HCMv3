---
helpId: wf.workflow-remark.edit
lang: en
title: Workflow Remarks — Create / Edit
entity: HCMv3.Workflow.WorkflowRemark
surfaces:
  - angular/src/app/admin/workflow/workflow-remarks/create-or-edit-workflow-remark-modal.component.html
audience: [admin]
permissions: [Pages.Workflow.WorkflowRemarks.Create]
---

## Overview

A request for feedback: someone outside the approval chain is asked for their view on a request. They can then open the request and its activity feed, where the request shows as a feedback event. The same person can be asked only once per request.

## WorkflowRequests {#WorkflowRemark.WorkflowRequestId}
<!-- alias: loc:WorkflowRequests, dom:WorkflowRemark_WorkflowRequestId -->

**What it is.** The workflow request this record belongs to.

**Rules.** Required. Chosen from Workflow Request master data.

## CCReceiver {#WorkflowRemark.CCReceiverId}
<!-- alias: loc:CCReceiver, dom:WorkflowRemark_CCReceiverId -->

**What it is.** The person asked for feedback.

**Rules.** Required. Chosen from the employee list.
