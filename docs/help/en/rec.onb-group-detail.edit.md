---
helpId: rec.onb-group-detail.edit
lang: en
title: Onboarding Group Details — Create / Edit
entity: HCMv3.Recruitment.OnbGroupDetail
surfaces:
  - angular/src/app/admin/recruitment/onb-group-details/create-or-edit-onb-group-detail-modal.component.html
audience: [admin]
permissions: [Pages.Recruitment.OnbGroupDetails.Create]
---

## Overview

One task in an onboarding group, optionally for a particular job title.

## OnbGroups {#OnbGroupDetail.OnbGroupId}
<!-- alias: loc:OnbGroups, dom:OnbGroupDetail_OnbGroupId -->

**What it is.** The group.

**Rules.** Required. Chosen from Onboarding Group master data.

## OnbTasks {#OnbGroupDetail.OnbTaskId}
<!-- alias: loc:OnbTasks, dom:OnbGroupDetail_OnbTaskId -->

**What it is.** The task.

**Rules.** Optional. Chosen from Onboarding Task master data.

## JobTitle {#OnbGroupDetail.JobTitleId}
<!-- alias: loc:JobTitle, dom:OnbGroupDetail_JobTitleId -->

**What it is.** The job title the task is for.

**Rules.** Optional. Chosen from Job Title master data.
