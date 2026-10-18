---
helpId: gmi.clearance-request-field-detail.edit
lang: en
title: Clearance Request Field Details — Create / Edit
entity: HCMv3.GeneralMasterInfo.ClearanceRequestFieldDetail
surfaces:
  - angular/src/app/admin/general-master-info/clearance-request-field-details/create-or-edit-clearance-request-field-detail-modal.component.html
audience: [admin]
permissions: [Pages.GeneralMasterInfo.ClearanceRequestFieldDetails.Create]
---

## Overview

Assigns one line of the exit clearance checklist to a department. On a parallel approval step, every department's approver looks at the same clearance at the same time, and each is shown only the lines assigned to their role -- IT answers the IT lines, Finance the Finance lines. One line can be assigned to more than one department.

## ClearanceRequestField {#ClearanceRequestFieldDetail.ClearanceRequestFieldId}
<!-- alias: loc:ClearanceRequestField, dom:ClearanceRequestFieldDetail_ClearanceRequestFieldId -->

**What it is.** The checklist line being assigned.

**Rules.** Optional. Chosen from Clearance Request Field master data.

## WorkflowRole {#ClearanceRequestFieldDetail.WorkflowRoleId}
<!-- alias: loc:WorkflowRole, dom:ClearanceRequestFieldDetail_WorkflowRoleId -->

**What it is.** The workflow role that answers this checklist line during a clearance approval.

**Example.** IT Department, for the line "Laptop returned"

**Why it matters.** When a clearance request reaches an approver, they see only the checklist lines whose role they hold on that day, and the line is recorded against them. An approver without the role does not see the line, so a line with a role nobody holds is never answered.

**Rules.** Required. Chosen from Workflow Role master data.
