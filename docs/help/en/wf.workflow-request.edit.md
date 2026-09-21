---
helpId: wf.workflow-request.edit
lang: en
title: Workflow Requests — Create / Edit
entity: HCMv3.Workflow.WorkflowRequest
surfaces:
  - angular/src/app/admin/workflow/workflow-requests/create-or-edit-workflow-request-modal.component.html
audience: [admin]
permissions: [Pages.Workflow.WorkflowRequests.Create]
---

## Overview

One request moving through its approval chain -- a leave, a loan, a document request. It is created when the request is submitted. Written by the workflow engine as requests move; there is normally no reason to add or change entries by hand.

## Employee {#WorkflowRequest.EmployeeId}
<!-- alias: loc:Employee, dom:WorkflowRequest_EmployeeId -->

**What it is.** The employee the request is for.

**Rules.** Optional. Chosen from the employee list.

## Approver {#WorkflowRequest.ApproverId}
<!-- alias: loc:Approver, dom:WorkflowRequest_ApproverId -->

**What it is.** The approver it is currently waiting on.

**Rules.** Optional. Chosen from the employee list.

## RequestType {#WorkflowRequest.RequestType}
<!-- alias: loc:RequestType, dom:WorkflowRequest_RequestType -->

**What it is.** The kind of request, which decides the chain and what happens on approval.

**Example.** Leave request

**Rules.** Optional. Maximum 256 characters.

## RequestNumber {#WorkflowRequest.RequestNumber}
<!-- alias: loc:RequestNumber, dom:WorkflowRequest_RequestNumber -->

**What it is.** The request's number.

**Rules.** Optional. Maximum 64 characters.

## WorkflowStep {#WorkflowRequest.WorkflowStep}
<!-- alias: loc:WorkflowStep, dom:WorkflowRequest_WorkflowStep -->

**What it is.** The step the request is on.

**Rules.** Required. Whole number.

## RequestStatus {#WorkflowRequest.RequestStatus}
<!-- alias: loc:RequestStatus, dom:WorkflowRequest_RequestStatus -->

**What it is.** Where the request stands.

**Why it matters.** Open while waiting for approval, Approved after the last step, Rejected, or Draft when sent back to its creator.

**Rules.** Required. One of: Open, Approved, Rejected, Closed, Draft, Transferred.

## ApprovalProcessTime {#WorkflowRequest.ApprovalProcessTime}
<!-- alias: loc:ApprovalProcessTime, dom:WorkflowRequest_ApprovalProcessTime -->

**What it is.** Whole days from the request's last change to its final approval.

**Rules.** Required. Whole number.
