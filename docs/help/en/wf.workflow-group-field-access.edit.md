---
helpId: wf.workflow-group-field-access.edit
lang: en
title: Workflow Group Field Accesses — Create / Edit
entity: HCMv3.Workflow.WorkflowGroupFieldAccess
surfaces:
  - angular/src/app/admin/workflow/workflow-group-detail/workflow-group-field-accesses/create-or-edit-workflow-group-field-access-modal.component.html
audience: [admin]
permissions: [Pages.Workflow.WorkflowGroupFieldAccesses.Create]
---

## Overview

Whether a field on the request form is shown, editable or required at a given step. The step must exist on the chain, and a field can be listed once per step. Not read at present: the request forms do not apply these rows.

## Step {#WorkflowGroupFieldAccess.Step}
<!-- alias: loc:Step, dom:WorkflowGroupFieldAccess_Step -->

**What it is.** The step the rule applies at.

**Example.** 2

**Rules.** Required. Whole number.

## Field {#WorkflowGroupFieldAccess.Field}
<!-- alias: loc:Field, dom:WorkflowGroupFieldAccess_Field -->

**What it is.** The form field.

**Rules.** Required. Maximum 256 characters.

## IsVisible {#WorkflowGroupFieldAccess.IsVisible}
<!-- alias: loc:IsVisible, dom:WorkflowGroupFieldAccess_IsVisible -->

**What it is.** Whether the field is shown.

**Rules.** Yes or no.

## IsEditable {#WorkflowGroupFieldAccess.IsEditable}
<!-- alias: loc:IsEditable, dom:WorkflowGroupFieldAccess_IsEditable -->

**What it is.** Whether the field can be changed.

**Rules.** Yes or no.

## IsRequired {#WorkflowGroupFieldAccess.IsRequired}
<!-- alias: loc:IsRequired, dom:WorkflowGroupFieldAccess_IsRequired -->

**What it is.** Whether the field must be filled in.

**Rules.** Yes or no.
