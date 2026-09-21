---
helpId: rec.vacancy.edit
lang: en
title: Vacancies — Create / Edit
entity: HCMv3.Recruitment.Vacancy
surfaces:
  - angular/src/app/admin/recruitment/vacancies/create-or-edit-vacancy-modal.component.html
audience: [admin]
permissions: [Pages.Recruitment.Vacancies.Create]
---

## Overview

A job opening. An *Open* vacancy whose closing date has not passed is shown on the careers site, where candidates can apply with one of their CVs.

The publish date is shown to candidates but does not hide the vacancy before it -- a vacancy is visible from the moment it is open. The vacancy status report lists vacancies and their applicants.

## VacancyTitle {#Vacancy.VacancyTitle}
<!-- alias: loc:VacancyTitle, dom:Vacancy_VacancyTitle -->

**What it is.** The vacancy's title.

**Example.** Senior Accountant

**Why it matters.** Shown and searched on the careers site.

**Rules.** Required.

## JobTitle {#Vacancy.JobTitleId}
<!-- alias: loc:JobTitle, dom:Vacancy_JobTitleId -->

**What it is.** The job title the vacancy is for.

**Why it matters.** Candidates on the careers site can filter vacancies by it.

**Rules.** Required. Chosen from Job Title master data.

## OrganizationUnit {#Vacancy.OrganizationUnitId}
<!-- alias: loc:OrganizationUnit, dom:Vacancy_OrganizationUnitId -->

**What it is.** The department the vacancy is in.

**Why it matters.** Shown as the department on the careers site.

**Rules.** Required. Chosen from a list.

## RequestionDate {#Vacancy.RequestionDate}
<!-- alias: loc:RequestionDate, dom:Vacancy_RequestionDate -->

**What it is.** The date the vacancy was requested.

**Why it matters.** Not read at present -- stored for reference.

**Rules.** Optional. Date value.

## PublishOn {#Vacancy.PublishOn}
<!-- alias: loc:PublishOn, dom:Vacancy_PublishOn -->

**What it is.** The date the vacancy is published.

**Why it matters.** Shown on the careers site and in the vacancy reports. It does not hide the vacancy before that date.

**Rules.** Optional. Date value.

## ClosingDate {#Vacancy.ClosingDate}
<!-- alias: loc:ClosingDate, dom:Vacancy_ClosingDate -->

**What it is.** The last day candidates can see the vacancy.

**Example.** `2026-05-31`

**Why it matters.** After it, the vacancy drops off the careers site.

**Rules.** Optional. Date value.

## VacancyStatus {#Vacancy.VacancyStatus}
<!-- alias: loc:VacancyStatus, dom:Vacancy_VacancyStatus -->

**What it is.** Where the vacancy stands.

**Example.** Open

**Why it matters.** Only *Open* vacancies are shown on the careers site.

**Rules.** Required. One of: Open, Filled, On Hold, Dropped.

## EmploymentType {#Vacancy.EmploymentType}
<!-- alias: loc:EmploymentType, dom:Vacancy_EmploymentType -->

**What it is.** Full time or part time.

**Why it matters.** Shown on the careers site.

**Rules.** Required. One of: FullTime, PartTime.

## SlaDaysToFill {#Vacancy.SLADaysToFill}
<!-- alias: loc:SlaDaysToFill, dom:Vacancy_SlaDaysToFill -->

**What it is.** The days allowed to fill the vacancy.

**Why it matters.** Not read at present -- stored for reference.

**Rules.** Optional. Whole number.

## TotalVacancy {#Vacancy.TotalVacancy}
<!-- alias: loc:TotalVacancy, dom:Vacancy_TotalVacancy -->

**What it is.** How many posts the vacancy is for.

**Why it matters.** Not read at present -- stored for reference.

**Rules.** Optional. Whole number.

## YearsOfExperience {#Vacancy.YearsOfExperience}
<!-- alias: loc:YearsOfExperience, dom:Vacancy_YearsOfExperience -->

**What it is.** The years of experience asked for.

**Why it matters.** Shown on the careers site.

**Rules.** Optional. Whole number.

## MonthlySalaryRange {#Vacancy.MonthlySalaryRange}
<!-- alias: loc:MonthlySalaryRange, dom:Vacancy_MonthlySalaryRange -->

**What it is.** The salary range offered.

**Why it matters.** Not read at present -- stored for reference.

**Rules.** Optional. Maximum 100 characters.

## Qualification {#Vacancy.QualificationId}
<!-- alias: loc:Qualification, dom:Vacancy_QualificationId -->

**What it is.** The qualification asked for.

**Why it matters.** Shown on the careers site.

**Rules.** Optional. Chosen from Qualification master data.

## IsBudgeted {#Vacancy.IsBudgeted}
<!-- alias: loc:IsBudgeted, dom:Vacancy_IsBudgeted -->

**What it is.** Whether the post is in the budget.

**Why it matters.** Not read at present -- stored for reference.

**Rules.** Yes or no.

## TemplateOnbGroupId {#Vacancy.TemplateOnbGroupId}
<!-- alias: loc:TemplateOnbGroupId, dom:Vacancy_TemplateOnbGroupId -->

**What it is.** An onboarding group template for the vacancy.

**Why it matters.** Not read at present -- stored for reference.

**Rules.** Optional. Chosen from a list.

## OnbGroup {#Vacancy.OnbGroupId}
<!-- alias: loc:OnbGroup, dom:Vacancy_OnbGroupId -->

**What it is.** The onboarding group for people hired to the vacancy.

**Why it matters.** Not read at present -- stored for reference.

**Rules.** Optional. Chosen from Onboarding Group master data.

## JobDescription {#Vacancy.JobDescription}
<!-- alias: loc:JobDescription, dom:Vacancy_JobDescription -->

**What it is.** The job description.

**Why it matters.** Shown on the vacancy's page on the careers site.

**Rules.** Optional.
