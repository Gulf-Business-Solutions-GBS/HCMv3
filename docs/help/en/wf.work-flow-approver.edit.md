---
helpId: wf.work-flow-approver.edit
lang: en
title: Workflow Approvers — Create / Edit
entity: HCMv3.Workflow.WorkFlowApprover
surfaces:
  - angular/src/app/admin/workflow/work-flow-approvers/create-or-edit-work-flow-approver-modal.component.html
audience: [admin]
permissions: [Pages.Workflow.WorkFlowApprovers.Create]
---

## Overview

One person a request is waiting on at its current step. On a Parallel step there is one row per approver. Written by the workflow engine as requests move; there is normally no reason to add or change entries by hand.

## WorkflowRequests {#WorkFlowApprover.WorkflowRequestId}
<!-- alias: loc:WorkflowRequests, dom:WorkFlowApprover_WorkflowRequestId -->

**What it is.** The workflow request this record belongs to.

**Rules.** Required. Chosen from Workflow Request master data.

## Approver {#WorkFlowApprover.ApproverId}
<!-- alias: loc:Approver, dom:WorkFlowApprover_ApproverId -->

**What it is.** The approver.

**Rules.** Optional. Chosen from the employee list.

## IsApproved {#WorkFlowApprover.IsApproved}
<!-- alias: loc:IsApproved, dom:WorkFlowApprover_IsApproved -->

**What it is.** Whether this approver has approved.

**Rules.** Yes or no.
