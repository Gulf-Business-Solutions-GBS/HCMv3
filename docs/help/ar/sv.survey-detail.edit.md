---
helpId: sv.survey-detail.edit
lang: ar
title: أسئلة الاستبيان — إنشاء / تحرير
entity: HCMv3.SurveyManagement.SurveyDetail
surfaces:
  - angular/src/app/admin/survey-management/survey-details/create-or-edit-survey-detail-modal.component.html
audience: [admin]
permissions: [Pages.SurveyManagement.SurveyDetails.Create]
---

## Overview

سؤال واحد في استبيان.

## Surveys {#SurveyDetail.SurveyId}
<!-- alias: loc:Surveys, dom:SurveyDetail_SurveyId -->

**ما هو.** الاستبيان.

**القواعد.** إلزامي. يُختار من قائمة الاستبيانات.

## Question {#SurveyDetail.Question}
<!-- alias: loc:Question, dom:SurveyDetail_Question -->

**ما هو.** السؤال.

**مثال.** ما مدى رضاك عن مكان عملك؟

**القواعد.** إلزامي. الحد الأقصى 1000 حرف.

## OrderNo {#SurveyDetail.OrderNo}
<!-- alias: loc:OrderNo, dom:SurveyDetail_OrderNo -->

**ما هو.** موضع السؤال؛ الأرقام الأصغر تأتي أولاً.

**مثال.** 1

**القواعد.** اختياري. رقم صحيح.

## QuestionType {#SurveyDetail.QuestionType}
<!-- alias: loc:QuestionType, dom:SurveyDetail_QuestionType -->

**ما هو.** طريقة الإجابة عن السؤال: نص حر، أو اختيار واحد من قائمة، أو عدة اختيارات.

**مثال.** قائمة اختيار واحد

**القواعد.** إلزامي. إحدى القيم: مربع نص، قائمة اختيار واحد، قائمة اختيار متعدد.

## Options {#SurveyDetail.Options}
<!-- alias: loc:Options, dom:SurveyDetail_Options -->

**ما هو.** الخيارات، مفصولة بفواصل منقوطة.

**مثال.** راضٍ جداً;راضٍ;غير راضٍ

**لماذا يهم.** مطلوب لأسئلة "قائمة اختيار واحد" و"قائمة اختيار متعدد".

**القواعد.** اختياري. الحد الأقصى 2000 حرف.
