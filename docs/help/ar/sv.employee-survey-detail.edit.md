---
helpId: sv.employee-survey-detail.edit
lang: ar
title: إجابات استبيانات الموظفين — إنشاء / تحرير
entity: HCMv3.SurveyManagement.EmployeeSurveyDetail
surfaces:
  - angular/src/app/admin/survey-management/employee-survey-details/create-or-edit-employee-survey-detail-modal.component.html
audience: [admin]
permissions: [Pages.SurveyManagement.EmployeeSurveyDetails.Create]
---

## Overview

إجابة موظف عن سؤال واحد. ويُحفظ السؤال الذي لم يُجب عنه بإجابة فارغة.

## EmployeeSurveys {#EmployeeSurveyDetail.EmployeeSurveyId}
<!-- alias: loc:EmployeeSurveys, dom:EmployeeSurveyDetail_EmployeeSurveyId -->

**ما هو.** استجابة الموظف.

**القواعد.** إلزامي. يُختار من قائمة.

## Question {#EmployeeSurveyDetail.QuestionId}
<!-- alias: loc:Question, dom:EmployeeSurveyDetail_QuestionId -->

**ما هو.** السؤال.

**القواعد.** إلزامي. يُختار من قائمة أسئلة الاستبيان.

## Answer {#EmployeeSurveyDetail.Answer}
<!-- alias: loc:Answer, dom:EmployeeSurveyDetail_Answer -->

**ما هو.** الإجابة المقدمة.

**مثال.** راضٍ

**القواعد.** اختياري. الحد الأقصى 2000 حرف.
