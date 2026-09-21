---
helpId: sv.survey-detail.edit
lang: en
title: Survey Questions — Create / Edit
entity: HCMv3.SurveyManagement.SurveyDetail
surfaces:
  - angular/src/app/admin/survey-management/survey-details/create-or-edit-survey-detail-modal.component.html
audience: [admin]
permissions: [Pages.SurveyManagement.SurveyDetails.Create]
---

## Overview

One question in a survey.

## Surveys {#SurveyDetail.SurveyId}
<!-- alias: loc:Surveys, dom:SurveyDetail_SurveyId -->

**What it is.** The survey.

**Rules.** Required. Chosen from Survey master data.

## Question {#SurveyDetail.Question}
<!-- alias: loc:Question, dom:SurveyDetail_Question -->

**What it is.** The question.

**Example.** How satisfied are you with your workspace?

**Rules.** Required. Maximum 1000 characters.

## OrderNo {#SurveyDetail.OrderNo}
<!-- alias: loc:OrderNo, dom:SurveyDetail_OrderNo -->

**What it is.** Where the question appears; lower numbers come first.

**Example.** 1

**Rules.** Optional. Whole number.

## QuestionType {#SurveyDetail.QuestionType}
<!-- alias: loc:QuestionType, dom:SurveyDetail_QuestionType -->

**What it is.** How the question is answered: free text, one choice from a list, or several choices.

**Example.** Radio List

**Rules.** Required. One of: Text Box, Radio List, Checkbox List.

## Options {#SurveyDetail.Options}
<!-- alias: loc:Options, dom:SurveyDetail_Options -->

**What it is.** The choices, separated by semicolons.

**Example.** Very satisfied;Satisfied;Not satisfied

**Why it matters.** Required for Radio List and Checkbox List questions.

**Rules.** Optional. Maximum 2000 characters.
