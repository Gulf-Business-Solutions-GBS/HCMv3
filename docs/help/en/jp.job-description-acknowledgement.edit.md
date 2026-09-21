---
helpId: jp.job-description-acknowledgement.edit
lang: en
title: Job Description Acknowledgements — Create / Edit
entity: HCMv3.JobProfileManagement.JobDescriptionAcknowledgement
surfaces:
  - angular/src/app/admin/job-profile-management/job-description-acknowledgements/create-or-edit-job-description-acknowledgement-modal.component.html
audience: [admin]
permissions: [Pages.JobProfileManagement.JobDescriptionAcknowledgements.Create]
---

## Overview

A record that an employee has read and accepted a job description. An employee can have one acknowledgement per job title and job description, so a new version is recorded by changing the existing one.

## Employee {#JobDescriptionAcknowledgement.EmployeeId}
<!-- alias: loc:Employee, dom:JobDescriptionAcknowledgement_EmployeeId -->

**What it is.** The employee.

**Rules.** Required. Chosen from the employee list.

## JobTitle {#JobDescriptionAcknowledgement.JobTitleId}
<!-- alias: loc:JobTitle, dom:JobDescriptionAcknowledgement_JobTitleId -->

**What it is.** The job title.

**Rules.** Required. Chosen from Job Title master data.

## JobDescription {#JobDescriptionAcknowledgement.JobDescriptionId}
<!-- alias: loc:JobDescription, dom:JobDescriptionAcknowledgement_JobDescriptionId -->

**What it is.** The job description acknowledged.

**Rules.** Required. Chosen from Job Description master data.

## Version {#JobDescriptionAcknowledgement.Version}
<!-- alias: loc:Version, dom:JobDescriptionAcknowledgement_Version -->

**What it is.** The version acknowledged.

**Example.** 2.0

**Why it matters.** Typed in; it is not copied from the job description.

**Rules.** Optional.

## Acknowledged {#JobDescriptionAcknowledgement.Acknowledged}
<!-- alias: loc:Acknowledged, dom:JobDescriptionAcknowledgement_Acknowledged -->

**What it is.** Whether the employee has acknowledged it.

**Rules.** Yes or no.
