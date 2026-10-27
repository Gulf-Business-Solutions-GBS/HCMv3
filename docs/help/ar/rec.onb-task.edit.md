---
helpId: rec.onb-task.edit
lang: ar
title: مهام التهيئة الوظيفية — إنشاء / تحرير
entity: HCMv3.Recruitment.OnbTask
surfaces:
  - angular/src/app/admin/recruitment/onb-tasks/create-or-edit-onb-task-modal.component.html
audience: [admin]
permissions: [Pages.Recruitment.OnbTasks.Create]
---

## Overview

مهمة تهيئة -- أمر يجب أن يقوم به الموظف الجديد أو الموارد البشرية، مثل تقديم المستندات أو استلام جهاز محمول.

التهيئة قائمة مهام يدوية: تُسند المهام إلى المتقدمين يدوياً على شكل مهام المتقدم، ولا شيء ينشئها تلقائياً عند التعيين.

## TaskNameEn {#OnbTask.TaskNameEn}
<!-- alias: loc:TaskNameEn, dom:OnbTask_TaskNameEn -->

**ما هو.** المهمة بالإنجليزية.

**مثال.** Submit Civil ID copy

**القواعد.** إلزامي. الحد الأقصى 256 حرفاً.

## TaskNameAr {#OnbTask.TaskNameAr}
<!-- alias: loc:TaskNameAr, dom:OnbTask_TaskNameAr -->

**ما هو.** المهمة بالعربية.

**مثال.** تقديم نسخة من البطاقة المدنية

**القواعد.** إلزامي. الحد الأقصى 256 حرفاً.

## OnbTemplates {#OnbTask.OnbTemplateId}
<!-- alias: loc:OnbTemplates, dom:OnbTask_OnbTemplateId -->

**ما هو.** نموذج مرافق للمهمة.

**القواعد.** اختياري. يُختار من قائمة قوالب التهيئة الوظيفية.

## DueDays {#OnbTask.DueDays}
<!-- alias: loc:DueDays, dom:OnbTask_DueDays -->

**ما هو.** الأيام المسموح بها لإنجازها.

**مثال.** `7`

**القواعد.** اختياري. رقم صحيح.

## Guideline {#OnbTask.Guideline}
<!-- alias: loc:Guideline, dom:OnbTask_Guideline -->

**ما هو.** تعليمات المهمة.

**القواعد.** اختياري.
