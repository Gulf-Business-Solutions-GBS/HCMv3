---
helpId: wf.workflow-role.edit
lang: en
title: Workflow Roles — Create / Edit
entity: HCMv3.Workflow.WorkflowRole
surfaces:
  - angular/src/app/admin/workflow/workflow-roles/create-or-edit-workflow-role-modal.component.html
audience: [admin]
permissions: [Pages.Workflow.WorkflowRoles.Create]
---

## Overview

An approver role, such as Payroll Officer. Employees are given roles on their record, and a step whose Approver Person is Person goes to everyone holding its role.

## WorkflowRoleGroup {#WorkflowRole.WorkflowRoleGroup}
<!-- alias: loc:WorkflowRoleGroup, dom:WorkflowRole_WorkflowRoleGroup -->

**What it is.** A default organisation scope for the role.

**Why it matters.** Not read at present. The scope that narrows approvers is the one on each step.

**Rules.** Required. One of: None, Company, Department, Branch, CostCenter.

## OrganizationUnit {#WorkflowRole.WorkflowRoleOrgId}
<!-- alias: loc:OrgLevel, dom:WorkflowRole_WorkflowRoleOrgId -->

**What it is.** An organisation unit level for the role.

**Why it matters.** Not read at present.

**Rules.** Optional. Chosen from Organization Unit Type master data.
