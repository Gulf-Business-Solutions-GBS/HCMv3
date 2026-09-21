---
helpId: wf.wfread-log.edit
lang: en
title: Workflow Read Logs — Create / Edit
entity: HCMv3.Workflow.WFReadLog
surfaces:
  - angular/src/app/admin/workflow/wf-read-logs/create-wf-read-log-modal.component.html
audience: [admin]
permissions: [Pages.Workflow.WFReadLogs.Create]
---

## Overview

Someone opened a request at a step where the read log is on. Written by the workflow engine as requests move; there is normally no reason to add or change entries by hand.

## WorkflowRequests {#WFReadLog.WorkflowRequestId}
<!-- alias: loc:WorkflowRequests, dom:WFReadLog_WorkflowRequestId -->

**What it is.** The workflow request this record belongs to.

**Rules.** Required. Chosen from Workflow Request master data.

## User {#WFReadLog.User}
<!-- alias: loc:User, dom:WFReadLog_User -->

**What it is.** The employee who opened it, by employee number in the system.

**Rules.** Optional. Maximum 256 characters.
