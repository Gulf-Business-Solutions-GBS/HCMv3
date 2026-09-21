---
helpId: sv.employee-survey.edit
lang: en
title: Employee Surveys — Create / Edit
entity: HCMv3.SurveyManagement.EmployeeSurvey
surfaces:
  - angular/src/app/admin/survey-management/employee-surveys/create-or-edit-employee-survey-modal.component.html
audience: [admin]
permissions: [Pages.SurveyManagement.EmployeeSurveys.Create]
---

## Overview

An employee's response to a survey. It is created the first time answers are submitted on the Take Survey screen, and later submissions update the same answers.

## Employees {#EmployeeSurvey.EmployeeId}
<!-- alias: loc:Employees, dom:EmployeeSurvey_EmployeeId -->

**What it is.** The employee answering.

**Rules.** Required. Chosen from the employee list.

## Surveys {#EmployeeSurvey.SurveyId}
<!-- alias: loc:Surveys, dom:EmployeeSurvey_SurveyId -->

**What it is.** The survey.

**Rules.** Required. Chosen from Survey master data.
