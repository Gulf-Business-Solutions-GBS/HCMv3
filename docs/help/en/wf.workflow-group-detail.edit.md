---
helpId: wf.workflow-group-detail.edit
lang: en
title: Workflow Group Details — Create / Edit
entity: HCMv3.Workflow.WorkflowGroupDetail
surfaces:
  - angular/src/app/admin/workflow/workflow-group-detail/workflow-group-details/create-or-edit-workflow-group-detail-modal.component.html
audience: [admin]
permissions: [Pages.Workflow.WorkflowGroupDetails.Create]
---

## Overview

Which chain a workflow group uses for one kind of request. Within the group the most specific row wins: type with sub-type and custom sub-type, then type with sub-type, then the row for the type alone. If the group has no match, the transaction type's default chain is used. A request type can be given to only one chain per group.

## RequestMaster {#WorkflowGroupDetail.RequestMasterId}
<!-- alias: loc:RequestMaster, dom:WorkflowGroupDetail_RequestMasterId -->

**What it is.** The chain to use.

**Rules.** Required. Chosen from Request Master master data.

## RequestType {#WorkflowGroupDetail.RequestTypeName}
<!-- alias: loc:RequestType, dom:WorkflowGroupDetail_RequestTypeName -->

**What it is.** The kind of request.

**Example.** Leave request

**Rules.** Required. Maximum 256 characters.

## RequestSubType {#WorkflowGroupDetail.RequestSubTypeId}
<!-- alias: loc:RequestSubType, dom:WorkflowGroupDetail_RequestSubTypeId -->

**What it is.** Narrows the row to one sub-type, such as one leave type.

**Example.** Annual leave

**Rules.** Optional. Chosen from a list.

## CustomSubType {#WorkflowGroupDetail.CustomSubTypeId}
<!-- alias: loc:CustomSubType, dom:WorkflowGroupDetail_CustomSubTypeId -->

**What it is.** Narrows the row further.

**Why it matters.** Requests are routed without a custom sub-type at present, so this does not narrow anything: a row with a sub-type is matched on the sub-type alone, and a row with only this filled in is never chosen.

**Rules.** Optional. Chosen from a list.

## RequestActionAppliesTo {#WorkflowGroupDetail.RequestAction}
<!-- alias: loc:RequestActionAppliesTo, dom:WorkflowGroupDetail_RequestAction -->

**What it is.** The request action this row applies to: a new request, an amendment, a cancellation or a return from leave.

**Example.** Cancellation

**Why it matters.** Lets one workflow group send, for example, leave cancellations down a shorter chain than new requests. A row naming the request's own action wins; rows left on **All actions** are used only when no row names that action.

**Rules.** Optional. One of: All actions.
