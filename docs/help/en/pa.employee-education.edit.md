---
helpId: pa.employee-education.edit
lang: en
title: Employee Educations — Create / Edit
entity: HCMv3.PersonnelAdministration.EmployeeEducation
surfaces:
  - angular/src/app/admin/personnel-administration/employee-detail/education/create-or-edit-employee-education-modal.component.html
audience: [admin]
permissions: [Pages.PersonnelAdministration.EmployeeEducations.Create]
---

## Overview

A qualification the employee holds from their education.

The education and branch are matched against the social security education allowance table in payroll. The other details are recorded for reference.

## Education {#EmployeeEducation.EducationId}
<!-- alias: loc:Education, dom:EmployeeEducation_EducationId -->

**What it is.** The level of education.

**Example.** Bachelor's Degree

**Why it matters.** Used with the branch for the social security education allowance.

**Rules.** Required. Chosen from Education master data.

## EducationBranch {#EmployeeEducation.EducationBranchId}
<!-- alias: loc:EducationBranch, dom:EmployeeEducation_EducationBranchId -->

**What it is.** The field of study.

**Example.** Engineering

**Why it matters.** Used with the education level for the social security education allowance.

**Rules.** Required. Chosen from Education Branch master data.

## University {#EmployeeEducation.UniversityId}
<!-- alias: loc:University, dom:EmployeeEducation_UniversityId -->

**What it is.** The university.

**Example.** Kuwait University

**Why it matters.** Recorded for reference; no calculation or report reads it at present.

**Rules.** Optional. Chosen from University master data.

## Institution {#EmployeeEducation.InstitutionId}
<!-- alias: loc:Institution, dom:EmployeeEducation_InstitutionId -->

**What it is.** The institution within the university.

**Example.** College of Engineering

**Why it matters.** Recorded for reference; no calculation or report reads it at present.

**Rules.** Optional. Chosen from Institution master data.

## Country {#EmployeeEducation.CountryId}
<!-- alias: loc:Country, dom:EmployeeEducation_CountryId -->

**What it is.** The country of study.

**Example.** Kuwait

**Why it matters.** Recorded for reference; no calculation or report reads it at present.

**Rules.** Optional. Chosen from Country master data.

## Location {#EmployeeEducation.Location}
<!-- alias: loc:Location, dom:EmployeeEducation_Location -->

**What it is.** Where the study took place, as free text.

**Example.** Shuwaikh

**Rules.** Optional. Maximum 256 characters.

## DurationOfCourse {#EmployeeEducation.DurationOfCourse}
<!-- alias: loc:DurationOfCourse, dom:EmployeeEducation_DurationOfCourse -->

**What it is.** The length of the course.

**Example.** 4

**Rules.** Optional. Number.

## Grade {#EmployeeEducation.Grade}
<!-- alias: loc:Grade, dom:EmployeeEducation_Grade -->

**What it is.** The grade achieved.

**Example.** Very Good

**Rules.** Optional. Maximum 64 characters.

## YearOfPassing {#EmployeeEducation.YearOfPassing}
<!-- alias: loc:YearOfPassing, dom:EmployeeEducation_YearOfPassing -->

**What it is.** The year the qualification was completed.

**Example.** 2012

**Rules.** Optional. Maximum 16 characters.
