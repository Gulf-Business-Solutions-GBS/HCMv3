---
helpId: sv.employee-survey-detail.edit
lang: en
title: Employee Survey Answers — Create / Edit
entity: HCMv3.SurveyManagement.EmployeeSurveyDetail
surfaces:
  - angular/src/app/admin/survey-management/employee-survey-details/create-or-edit-employee-survey-detail-modal.component.html
audience: [admin]
permissions: [Pages.SurveyManagement.EmployeeSurveyDetails.Create]
---

## Overview

An employee's answer to one question. An unanswered question is stored with an empty answer.

## EmployeeSurveys {#EmployeeSurveyDetail.EmployeeSurveyId}
<!-- alias: loc:EmployeeSurveys, dom:EmployeeSurveyDetail_EmployeeSurveyId -->

**What it is.** The employee's response.

**Rules.** Required. Chosen from a list.

## Question {#EmployeeSurveyDetail.QuestionId}
<!-- alias: loc:Question, dom:EmployeeSurveyDetail_QuestionId -->

**What it is.** The question.

**Rules.** Required. Chosen from Survey Detail master data.

## Answer {#EmployeeSurveyDetail.Answer}
<!-- alias: loc:Answer, dom:EmployeeSurveyDetail_Answer -->

**What it is.** The answer given.

**Example.** Satisfied

**Rules.** Optional. Maximum 2000 characters.
