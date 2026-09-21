---
helpId: wf.request-detail-parallel.edit
lang: en
title: Request Detail Parallels — Create / Edit
entity: HCMv3.Workflow.RequestDetailParallel
surfaces:
  - angular/src/app/admin/workflow/request-detail-parallels/create-or-edit-request-detail-parallel-modal.component.html
audience: [admin]
permissions: [Pages.Workflow.RequestDetailParallels.Create]
---

## Overview

An extra approver for a step, named by job title and workflow role. Workflow role cannot be blank. Not read at present: approvers come only from the step itself, so these rows do not add anyone.

## RequestDetails {#RequestDetailParallel.RequestDetailId}
<!-- alias: loc:RequestDetails, dom:RequestDetailParallel_RequestDetailId -->

**What it is.** The step this approver is added to.

**Rules.** Required. Chosen from Request Detail master data.

## JobTitle {#RequestDetailParallel.JobTitleId}
<!-- alias: loc:JobTitle, dom:RequestDetailParallel_JobTitleId -->

**What it is.** The job title of the extra approver.

**Rules.** Optional. Chosen from Job Title master data.

## WorkflowRole {#RequestDetailParallel.WorkflowRoleId}
<!-- alias: loc:WorkflowRole, dom:RequestDetailParallel_WorkflowRoleId -->

**What it is.** The workflow role of the extra approver.

**Rules.** Required. Chosen from Workflow Role master data.
