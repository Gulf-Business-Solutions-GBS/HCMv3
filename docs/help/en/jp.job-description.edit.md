---
helpId: jp.job-description.edit
lang: en
title: Job Descriptions — Create / Edit
entity: HCMv3.JobProfileManagement.JobDescription
surfaces:
  - angular/src/app/admin/job-profile-management/job-descriptions/create-or-edit-job-description-modal.component.html
audience: [admin]
permissions: [Pages.JobProfileManagement.JobDescriptions.Create]
---

## Overview

The description of a job title: a short summary, the full text, and lists of the responsibilities, skills, competencies, degrees, certificates and interview questions that go with it. Both short descriptions must be unique. Kept for reference: vacancies carry their own description text and do not use this one.

## JobTitle {#JobDescription.JobTitleId}
<!-- alias: loc:JobTitle, dom:JobDescription_JobTitleId -->

**What it is.** The job title described.

**Example.** HR Officer

**Rules.** Required. Chosen from Job Title master data.

## Version {#JobDescription.Version}
<!-- alias: loc:Version, dom:JobDescription_Version -->

**What it is.** The version of the description.

**Example.** 2.0

**Rules.** Optional. Maximum 64 characters.

## VersionChangedOn {#JobDescription.VersionChangedOn}
<!-- alias: loc:VersionChangedOn, dom:JobDescription_VersionChangedOn -->

**What it is.** When this version took effect.

**Rules.** Optional. Date value.

## ShortDescriptionEn {#JobDescription.ShortDescriptionEn}
<!-- alias: loc:ShortDescriptionEn, dom:JobDescription_ShortDescriptionEn -->

**What it is.** A one-line summary in English.

**Rules.** Required. Maximum 256 characters.

## ShortDescriptionAr {#JobDescription.ShortDescriptionAr}
<!-- alias: loc:ShortDescriptionAr, dom:JobDescription_ShortDescriptionAr -->

**What it is.** A one-line summary in Arabic.

**Rules.** Required. Maximum 256 characters.

## LongDescriptionEn {#JobDescription.LongDescriptionEn}
<!-- alias: loc:LongDescriptionEn, dom:JobDescription_LongDescriptionEn -->

**What it is.** The full description in English.

**Rules.** Required.

## LongDescriptionAr {#JobDescription.LongDescriptionAr}
<!-- alias: loc:LongDescriptionAr, dom:JobDescription_LongDescriptionAr -->

**What it is.** The full description in Arabic.

**Rules.** Required.
