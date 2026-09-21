---
helpId: rec.application-status.edit
lang: en
title: Application Statuses — Create / Edit
entity: HCMv3.Recruitment.ApplicationStatus
surfaces:
  - angular/src/app/admin/recruitment/application-statuses/create-or-edit-application-status-modal.component.html
audience: [admin]
permissions: [Pages.Recruitment.ApplicationStatuses.Create]
---

## Overview

A candidate's application to a vacancy with one of their CVs, and how far it has got -- applied, shortlisted, interviewed, offered, hired, rejected or joined.

Setting it to *Joined* creates the employee record from the applicant (see the applicant page); the applicant's joining and probation end dates must be filled in first. Once an application moves past *Applied*, the candidate can no longer change or delete that CV on the careers site.

## Vacancy {#ApplicationStatus.VacancyId}
<!-- alias: loc:Vacancy, dom:ApplicationStatus_VacancyId -->

**What it is.** The vacancy applied for.

**Example.** Senior Accountant

**Rules.** Optional. Chosen from Vacancy master data.

## ApplicantCVs {#ApplicationStatus.ApplicantCVId}
<!-- alias: loc:ApplicantCVs, dom:ApplicationStatus_ApplicantCVId -->

**What it is.** The CV the candidate applied with.

**Rules.** Optional. Chosen from Applicant CV master data.

## AppStatus {#ApplicationStatus.AppStatus}
<!-- alias: loc:AppStatus, dom:ApplicationStatus_AppStatus -->

**What it is.** How far the application has got.

**Example.** Shortlisted

**Why it matters.** *Joined* creates the employee. The vacancy status report can filter by it.

**Rules.** Required. One of: Applied, Shortlisted, Interviewed, JobOffer, Hired, Rejected, Joined.

:::admin
The employee created on *Joined* has no employee code, and its full names are set to the first names only. Open the new employee and save it once: saving gives it the next code and builds the full names from all the name parts.
:::

## CoverLetter {#ApplicationStatus.CoverLetter}
<!-- alias: loc:CoverLetter, dom:ApplicationStatus_CoverLetter -->

**What it is.** The candidate's cover letter.

**Rules.** Optional. Maximum 4000 characters.
