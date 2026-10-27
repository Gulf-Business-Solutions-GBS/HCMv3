---
helpId: rec.cvqualification.edit
lang: en
title: CV Qualifications — Create / Edit
entity: HCMv3.Recruitment.CVQualification
surfaces:
  - angular/src/app/admin/recruitment/applicant-detail/cv/qualifications/create-or-edit-cv-qualification-modal.component.html
audience: [admin]
permissions: [Pages.Recruitment.CVQualifications.Create]
---

## Overview

A qualification on a CV.

## Qualification {#CVQualification.QualificationId}
<!-- alias: loc:Qualification, dom:CVQualification_QualificationId -->

**What it is.** The qualification.

**Example.** Bachelor's Degree

**Rules.** Optional. Chosen from Qualification master data.

## University {#CVQualification.University}
<!-- alias: loc:University, dom:CVQualification_University -->

**What it is.** Where it was gained.

**Example.** Kuwait University

**Rules.** Optional. Maximum 256 characters.

## Percentage {#CVQualification.Percentage}
<!-- alias: loc:Percentage, dom:CVQualification_Percentage -->

**What it is.** The grade, as a percentage.

**Example.** `82`

**Rules.** Optional. Number.

## PassedYear {#CVQualification.PassedYear}
<!-- alias: loc:PassedYear, dom:CVQualification_PassedYear -->

**What it is.** The year it was gained.

**Example.** `2018`

**Rules.** Optional. Whole number.
