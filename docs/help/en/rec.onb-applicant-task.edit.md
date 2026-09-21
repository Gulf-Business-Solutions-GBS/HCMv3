---
helpId: rec.onb-applicant-task.edit
lang: en
title: Onb Applicant Task
entity: HCMv3.Recruitment.OnbApplicantTasks
surfaces:
  - angular/src/app/admin/recruitment/onb-applicant-tasks/create-or-edit-onb-applicant-task-modal.component.html
audience: [admin]
permissions: [Pages.Recruitment.OnbTasks.Create]
---

## Overview

An onboarding task given to one applicant, and whether it is done.

## Applicant {#OnbApplicantTasks.ApplicantId}
<!-- alias: loc:Applicant, dom:OnbApplicantTask_ApplicantId -->

**What it is.** The applicant.

**Rules.** Required. Chosen from Applicant master data.

## OnbTasks {#OnbApplicantTasks.OnbTaskId}
<!-- alias: loc:OnbTasks, dom:OnbApplicantTask_OnbTaskId -->

**What it is.** The task.

**Rules.** Optional. Chosen from a list.

## DueDays {#OnbApplicantTasks.DueDays}
<!-- alias: loc:DueDays, dom:OnbApplicantTask_DueDays -->

**What it is.** The days allowed to complete it.

**Example.** `7`

**Rules.** Optional. Whole number.

## IsCompleted {#OnbApplicantTasks.IsCompleted}
<!-- alias: loc:IsCompleted, dom:OnbApplicantTask_IsCompleted -->

**What it is.** Whether it is done.

**Rules.** Yes or no.

## CompletedOn {#OnbApplicantTasks.CompletedOn}
<!-- alias: loc:CompletedOn, dom:OnbApplicantTask_CompletedOn -->

**What it is.** When it was done.

**Rules.** Optional. Date value.

## TemplateUserData {#OnbApplicantTasks.TemplateUserData}
<!-- alias: loc:TemplateUserData, dom:OnbApplicantTask_TemplateUserData -->

**What it is.** What the applicant filled in on the task's template.

**Rules.** Optional. Maximum 4000 characters.
