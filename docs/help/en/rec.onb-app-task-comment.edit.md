---
helpId: rec.onb-app-task-comment.edit
lang: en
title: Onb App Task Comment
entity: HCMv3.Recruitment.OnbAppTaskComments
surfaces:
  - angular/src/app/admin/recruitment/onb-app-task-comments/create-or-edit-onb-app-task-comment-modal.component.html
audience: [admin]
permissions: [Pages.Recruitment.OnbTasks.Create]
---

## Overview

A comment on an applicant's onboarding task.

## OnbApplicantTasks {#OnbAppTaskComments.OnbApplicantTasksId}
<!-- alias: loc:OnbApplicantTasks, dom:OnbAppTaskComment_OnbApplicantTasksId -->

**What it is.** The applicant task.

**Rules.** Required. Chosen from Onboarding Applicant Tasks master data.

## IsByAdmin {#OnbAppTaskComments.IsByAdmin}
<!-- alias: loc:IsByAdmin, dom:OnbAppTaskComment_IsByAdmin -->

**What it is.** Whether HR wrote the comment rather than the applicant.

**Rules.** Yes or no.
