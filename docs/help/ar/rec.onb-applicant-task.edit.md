---
helpId: rec.onb-applicant-task.edit
lang: ar
title: Onb Applicant Task
entity: HCMv3.Recruitment.OnbApplicantTasks
surfaces:
  - angular/src/app/admin/recruitment/onb-applicant-tasks/create-or-edit-onb-applicant-task-modal.component.html
audience: [admin]
permissions: [Pages.Recruitment.OnbTasks.Create]
---

## Overview

مهمة تهيئة مُسندة إلى متقدم واحد، وما إذا أُنجزت.

## Applicant {#OnbApplicantTasks.ApplicantId}
<!-- alias: loc:Applicant, dom:OnbApplicantTask_ApplicantId -->

**ما هو.** المتقدم.

**القواعد.** إلزامي. يُختار من قائمة المتقدمون.

## OnbTasks {#OnbApplicantTasks.OnbTaskId}
<!-- alias: loc:OnbTasks, dom:OnbApplicantTask_OnbTaskId -->

**ما هو.** المهمة.

**القواعد.** اختياري. يُختار من قائمة.

## DueDays {#OnbApplicantTasks.DueDays}
<!-- alias: loc:DueDays, dom:OnbApplicantTask_DueDays -->

**ما هو.** الأيام المسموح بها لإنجازها.

**مثال.** `7`

**القواعد.** اختياري. رقم صحيح.

## IsCompleted {#OnbApplicantTasks.IsCompleted}
<!-- alias: loc:IsCompleted, dom:OnbApplicantTask_IsCompleted -->

**ما هو.** ما إذا أُنجزت.

**القواعد.** نعم أو لا.

## CompletedOn {#OnbApplicantTasks.CompletedOn}
<!-- alias: loc:CompletedOn, dom:OnbApplicantTask_CompletedOn -->

**ما هو.** تاريخ إنجازها.

**القواعد.** اختياري. تاريخ.

## TemplateUserData {#OnbApplicantTasks.TemplateUserData}
<!-- alias: loc:TemplateUserData, dom:OnbApplicantTask_TemplateUserData -->

**ما هو.** ما عبّأه المتقدم في نموذج المهمة.

**القواعد.** اختياري. الحد الأقصى 4000 حرف.
