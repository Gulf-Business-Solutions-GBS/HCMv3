---
helpId: org.job-title-detail.edit
lang: en
title: Job Title Details — Create / Edit
entity: HCMv3.OrganizationManagement.JobTitleDetail
surfaces:
  - angular/src/app/admin/organization-management/job-title-details/create-or-edit-job-title-detail-modal.component.html
audience: [admin]
permissions: [Pages.OrganizationManagement.JobTitleDetails.Create]
---

## Overview

The default workflow roles of a job title: each row lists one workflow role that holders of that job title take on.

The rows are kept ready for parallel approval steps and are not read at present. Approvers are still routed by the roles given on each employee's own record.

## JobTitle {#JobTitleDetail.JobTitleId}
<!-- alias: loc:JobTitle, dom:JobTitleDetail_JobTitleId -->

**What it is.** The job title the workflow role is listed for.

**Example.** HR Manager

**Rules.** Required. Chosen from Job Title master data.

## WorkflowRole {#JobTitleDetail.WorkflowRoleId}
<!-- alias: loc:WorkflowRole, dom:JobTitleDetail_WorkflowRoleId -->

**What it is.** A workflow role that holders of the job title take on by default.

**Example.** HR Approver

**Why it matters.** A job title can list several roles, but the same role only once. Kept for parallel approval steps, which fall back to these roles when a step names none; HCMv3 has no parallel approval step yet, so nothing reads them at present.

**Rules.** Required. Chosen from Workflow Role master data.
