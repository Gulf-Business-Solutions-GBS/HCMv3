---
helpId: wf.workflow-group.edit
lang: en
title: Workflow Groups — Create / Edit
entity: HCMv3.Workflow.WorkflowGroup
surfaces:
  - angular/src/app/admin/workflow/workflow-groups/create-or-edit-workflow-group-modal.component.html
audience: [admin]
permissions: [Pages.Workflow.WorkflowGroups.Create]
---

## Overview

A set of chain choices for a group of employees, so that, for example, head-office staff use a different leave chain. An employee's group comes from their current organisation assignment; an employee with none uses the default group. Groups are used only when the workflow setting EnableWorkflowGroup is on.

## IsDefault {#WorkflowGroup.IsDefault}
<!-- alias: loc:IsDefault, dom:WorkflowGroup_IsDefault -->

**What it is.** Whether this is the group for employees who have none.

**Why it matters.** Only one group can be the default.

**Rules.** Yes or no.
