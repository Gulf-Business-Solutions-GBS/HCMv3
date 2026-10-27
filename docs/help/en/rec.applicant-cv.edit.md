---
helpId: rec.applicant-cv.edit
lang: en
title: Applicant CVs — Create / Edit
entity: HCMv3.Recruitment.ApplicantCV
surfaces:
  - angular/src/app/admin/recruitment/applicant-detail/cv/applicant-cv.component.html
audience: [admin]
permissions: []
---

## Overview

One of an applicant's CVs. A candidate can keep several CVs on the careers site and chooses one when applying. Its languages, qualifications, skills and work experience are held on its details. Each applicant's CV names must be different, and a CV with an application past *Applied* can no longer be changed by the candidate.

## JobTitle {#ApplicantCV.JobTitleId}
<!-- alias: loc:JobTitle, dom:ApplicantCV_JobTitleId -->

**What it is.** The job title the CV is aimed at.

**Example.** Accountant

**Rules.** Optional. Chosen from Job Title master data.

## CvName {#ApplicantCV.CVName}
<!-- alias: loc:CvName, dom:ApplicantCV_CvName -->

**What it is.** A name for the CV, to tell it apart from the applicant's others.

**Example.** Finance roles

**Rules.** Optional. Maximum 256 characters.

## UploadDate {#ApplicantCV.UploadDate}
<!-- alias: loc:UploadDate, dom:ApplicantCV_UploadDate -->

**What it is.** When the CV was added. The careers site lists the newest first.

**Rules.** Optional. Date value.
