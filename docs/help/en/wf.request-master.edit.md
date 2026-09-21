---
helpId: wf.request-master.edit
lang: en
title: Request Masters — Create / Edit
entity: HCMv3.Workflow.RequestMaster
surfaces:
  - angular/src/app/admin/workflow/request-masters/create-or-edit-request-master-modal.component.html
audience: [admin]
permissions: [Pages.Workflow.RequestMasters.Create]
---

## Overview

An approval chain: the named sequence of steps a request goes through. Each step is set up on the chain's request details. A request finds its chain in this order: a business rule that names a chain by code, then the employee's workflow group (when the workflow setting EnableWorkflowGroup is on), then the transaction type's default chain. If none gives a chain, the request cannot be submitted and says so.

## SecurityLevel {#RequestMaster.SecurityLevel}
<!-- alias: loc:SecurityLevel, dom:RequestMaster_SecurityLevel -->

**What it is.** Who may see requests on this chain.

**Why it matters.** Not read at present.

**Rules.** Required. One of: EmployeeOnly, EmployeeManager, EmployeeDepartment, Everyone.

## DefaultApprover {#RequestMaster.DefaultApproverId}
<!-- alias: loc:DefaultApprover, dom:RequestMaster_DefaultApproverId -->

**What it is.** The employee who approves when a step finds nobody.

**Example.** HR Manager

**Why it matters.** Used as the last resort at any step: when the step's role has no holders, the manager chain runs out, or every candidate already signed an earlier signatory step. If that employee has delegated their approvals, the request goes to their stand-in.

**Rules.** Optional. Chosen from the employee list.

## AutoCloseInDays {#RequestMaster.AutoCloseInDays}
<!-- alias: loc:AutoCloseInDays, dom:RequestMaster_AutoCloseInDays -->

**What it is.** How many days after approval a request closes by itself.

**Why it matters.** Not read at present.

**Rules.** Required. Whole number.

## DueInDays {#RequestMaster.DueInDays}
<!-- alias: loc:DueInDays, dom:RequestMaster_DueInDays -->

**What it is.** How many days a request has to be completed.

**Why it matters.** Not read at present.

**Rules.** Required. Whole number.

## AutoClose {#RequestMaster.AutoClose}
<!-- alias: loc:AutoClose, dom:RequestMaster_AutoClose -->

**What it is.** Whether approved requests close by themselves.

**Why it matters.** Not read at present.

**Rules.** Yes or no.

## IsCommentRequire {#RequestMaster.IsCommentRequire}
<!-- alias: loc:IsCommentRequire, dom:RequestMaster_IsCommentRequire -->

**What it is.** Whether approvers must write a comment.

**Why it matters.** Not read at present. The same box on each step is the one that is enforced.

**Rules.** Yes or no.
