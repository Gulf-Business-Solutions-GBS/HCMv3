---
helpId: sys.workflow-settings.edit
lang: en
title: Workflow settings
entity: HCMv3.SystemSettings.ManageWorkflowSettings
surfaces:
  - angular/src/app/admin/system-settings/manage-workflow-settings/create-or-edit-manage-workflow-settings-modal.component.html
audience: [admin]
permissions: [Pages.SystemSettings.ManageWorkflowSettings]
---

## Overview

Settings for approval workflow.

Only **Enable Workflow Group** is read at present; the others have no effect.

There is one settings record per company; it cannot be deleted.

## EnableWorkflowGroup {#ManageWorkflowSettings.EnableWorkflowGroup}
<!-- alias: loc:EnableWorkflowGroup, dom:WorkflowSettings_EnableWorkflowGroup -->

**What it is.** Whether approval chains depend on the employee's workflow group.

**Why it matters.** Ticked, the approval configuration for a request is looked up in the workflow group on the employee's current organisation assignment, falling back to the default group. Unticked, every employee gets the same approval chain for a request type.

**Rules.** Yes or no.

## EnableOrgUnitTypeInWorkflow {#ManageWorkflowSettings.EnableOrgUnitTypeInWorkflow}
<!-- alias: loc:EnableOrgUnitTypeInWorkflow, dom:WorkflowSettings_EnableOrgUnitTypeInWorkflow -->

**What it is.** Whether workflow uses organisation unit types.

**Why it matters.** Not read at present -- changing it has no effect.

**Rules.** Yes or no.

## HideLeavePayDetailsInWorkflow {#ManageWorkflowSettings.HideLeavePayDetailsInWorkflow}
<!-- alias: loc:HideLeavePayDetailsInWorkflow, dom:WorkflowSettings_HideLeavePayDetailsInWorkflow -->

**What it is.** Whether leave pay details are hidden from approvers.

**Why it matters.** Not read at present -- changing it has no effect.

**Rules.** Yes or no.

## DynamicWorkflowSteps {#ManageWorkflowSettings.DynamicWorkflowSteps}
<!-- alias: loc:DynamicWorkflowSteps, dom:WorkflowSettings_DynamicWorkflowSteps -->

**What it is.** Whether approval steps are built dynamically.

**Why it matters.** Not read at present -- changing it has no effect.

**Rules.** Yes or no.

## SendBackToCreator {#ManageWorkflowSettings.SendBackToCreator}
<!-- alias: loc:SendBackToCreator, dom:WorkflowSettings_SendBackToCreator -->

**What it is.** Whether a request can be sent back to its creator.

**Why it matters.** Not read at present -- changing it has no effect.

**Rules.** Yes or no.

## DelegateEmployeeListByOrganizationUnitTypeId {#ManageWorkflowSettings.DelegateEmployeeListByOrganizationUnitTypeId}
<!-- alias: loc:DelegateEmployeeListByOrganizationUnitTypeId, dom:WorkflowSettings_DelegateEmployeeListByOrganizationUnitTypeId -->

**What it is.** The organisation unit type that limits who can be delegated to.

**Why it matters.** Not read at present -- changing it has no effect.

**Rules.** Optional. Chosen from a list.

## EnableTravelCostOnWorkflowstep {#ManageWorkflowSettings.EnableTravelCostOnWorkflowstep}
<!-- alias: loc:EnableTravelCostOnWorkflowstep, dom:WorkflowSettings_EnableTravelCostOnWorkflowstep -->

**What it is.** Whether travel cost is shown on workflow steps.

**Why it matters.** Not read at present -- changing it has no effect.

**Rules.** Optional. Whole number.

## OvertimeRequestModificationStep {#ManageWorkflowSettings.OvertimeRequestModificationStep}
<!-- alias: loc:OvertimeRequestModificationStep, dom:WorkflowSettings_OvertimeRequestModificationStep -->

**What it is.** The workflow step at which an overtime request can be changed.

**Why it matters.** Not read at present -- changing it has no effect.

**Rules.** Optional. Whole number.
