---
helpId: rec.onb-task.edit
lang: en
title: Onboarding Tasks — Create / Edit
entity: HCMv3.Recruitment.OnbTask
surfaces:
  - angular/src/app/admin/recruitment/onb-tasks/create-or-edit-onb-task-modal.component.html
audience: [admin]
permissions: [Pages.Recruitment.OnbTasks.Create]
---

## Overview

An onboarding task -- something a new joiner or HR must do, such as submitting documents or collecting a laptop.

Onboarding is a manual checklist: tasks are given to applicants by hand as applicant tasks; nothing creates them automatically when someone is hired.

## TaskNameEn {#OnbTask.TaskNameEn}
<!-- alias: loc:TaskNameEn, dom:OnbTask_TaskNameEn -->

**What it is.** The task in English.

**Example.** Submit Civil ID copy

**Rules.** Required. Maximum 256 characters.

## TaskNameAr {#OnbTask.TaskNameAr}
<!-- alias: loc:TaskNameAr, dom:OnbTask_TaskNameAr -->

**What it is.** The task in Arabic.

**Example.** تقديم نسخة من البطاقة المدنية

**Rules.** Required. Maximum 256 characters.

## OnbTemplates {#OnbTask.OnbTemplateId}
<!-- alias: loc:OnbTemplates, dom:OnbTask_OnbTemplateId -->

**What it is.** A template that goes with the task.

**Rules.** Optional. Chosen from Onboarding Template master data.

## DueDays {#OnbTask.DueDays}
<!-- alias: loc:DueDays, dom:OnbTask_DueDays -->

**What it is.** The days allowed to complete it.

**Example.** `7`

**Rules.** Optional. Whole number.

## Guideline {#OnbTask.Guideline}
<!-- alias: loc:Guideline, dom:OnbTask_Guideline -->

**What it is.** Instructions for the task.

**Rules.** Optional.
