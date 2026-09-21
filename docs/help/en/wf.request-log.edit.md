---
helpId: wf.request-log.edit
lang: en
title: Request Logs — Create / Edit
entity: HCMv3.Workflow.RequestLog
surfaces:
  - angular/src/app/admin/workflow/request-logs/create-request-log-modal.component.html
audience: [admin]
permissions: [Pages.Workflow.RequestLogs.Create]
---

## Overview

One action taken on a request -- created, approved, rejected, sent back, escalated, read. It feeds the request's activity feed and the approval history in emails. Written by the workflow engine as requests move; there is normally no reason to add or change entries by hand.

## WorkflowRequests {#RequestLog.WorkflowRequestId}
<!-- alias: loc:WorkflowRequests, dom:RequestLog_WorkflowRequestId -->

**What it is.** The workflow request this record belongs to.

**Rules.** Required. Chosen from Workflow Request master data.

## ActionBy {#RequestLog.ActionById}
<!-- alias: loc:ActionBy, dom:RequestLog_ActionById -->

**What it is.** Who took the action.

**Rules.** Optional. Chosen from the employee list.

## ActionType {#RequestLog.ActionType}
<!-- alias: loc:ActionType, dom:RequestLog_ActionType -->

**What it is.** What was done.

**Example.** Approved

**Rules.** Required. One of: Created, Approved, Rejected, Escalated, Read, SentBack, Closed, Remarked, Assigned, Transferred, SentBackToCreator.

## WorkflowStep {#RequestLog.WorkflowStep}
<!-- alias: loc:WorkflowStep, dom:RequestLog_WorkflowStep -->

**What it is.** The step it was done at.

**Rules.** Required. Whole number.

## ActionDays {#RequestLog.ActionDays}
<!-- alias: loc:ActionDays, dom:RequestLog_ActionDays -->

**What it is.** Days taken for the action.

**Why it matters.** Not read at present.

**Rules.** Required. Whole number.

## Platform {#RequestLog.Platform}
<!-- alias: loc:Platform, dom:RequestLog_Platform -->

**What it is.** Where the action came from.

**Rules.** Required. One of: Core, ESS, Mobile.

## RequestActionComment {#RequestLog.RequestActionComment}
<!-- alias: loc:RequestActionComment, dom:RequestLog_RequestActionComment -->

**What it is.** The comment given with the action.

**Rules.** Optional. Maximum 4000 characters.
