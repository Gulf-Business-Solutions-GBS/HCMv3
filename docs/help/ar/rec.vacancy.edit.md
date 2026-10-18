---
helpId: rec.vacancy.edit
lang: ar
title: الوظائف الشاغرة — إنشاء / تحرير
entity: HCMv3.Recruitment.Vacancy
surfaces:
  - angular/src/app/admin/recruitment/vacancies/create-or-edit-vacancy-modal.component.html
audience: [admin]
permissions: [Pages.Recruitment.Vacancies.Create]
---

## Overview

فرصة عمل. والشاغر *المفتوح* الذي لم يمضِ تاريخ إغلاقه يُعرض في موقع الوظائف، حيث يمكن للمرشحين التقديم بإحدى سيرهم الذاتية.

ويُعرض تاريخ النشر للمرشحين لكنه لا يخفي الشاغر قبله -- فالشاغر يظهر منذ لحظة فتحه. ويسرد تقرير حالة الشواغر الشواغرَ ومتقدميها.

## VacancyTitle {#Vacancy.VacancyTitle}
<!-- alias: loc:VacancyTitle, dom:Vacancy_VacancyTitle -->

**ما هو.** مسمى الشاغر.

**مثال.** محاسب أول

**لماذا يهم.** يُعرض ويُبحث عنه في موقع الوظائف.

**القواعد.** إلزامي.

## JobTitle {#Vacancy.JobTitleId}
<!-- alias: loc:JobTitle, dom:Vacancy_JobTitleId -->

**ما هو.** المسمى الوظيفي الذي يخصه الشاغر.

**لماذا يهم.** يمكن للمرشحين في موقع الوظائف تصفية الشواغر به.

**القواعد.** إلزامي. يُختار من قائمة المسميات الوظيفية.

## OrganizationUnit {#Vacancy.OrganizationUnitId}
<!-- alias: loc:OrganizationUnit, dom:Vacancy_OrganizationUnitId -->

**ما هو.** الإدارة التي يتبعها الشاغر.

**لماذا يهم.** يُعرض إدارةً في موقع الوظائف.

**القواعد.** إلزامي. يُختار من قائمة.

## RequestionDate {#Vacancy.RequestionDate}
<!-- alias: loc:RequestionDate, dom:Vacancy_RequestionDate -->

**ما هو.** تاريخ طلب الشاغر.

**لماذا يهم.** لا يُقرأ حالياً -- يُحفظ للرجوع إليه.

**القواعد.** اختياري. تاريخ.

## PublishOn {#Vacancy.PublishOn}
<!-- alias: loc:PublishOn, dom:Vacancy_PublishOn -->

**ما هو.** تاريخ نشر الشاغر.

**لماذا يهم.** يُعرض في موقع الوظائف وفي تقارير الشواغر، ولا يخفي الشاغر قبل ذلك التاريخ.

**القواعد.** اختياري. تاريخ.

## ClosingDate {#Vacancy.ClosingDate}
<!-- alias: loc:ClosingDate, dom:Vacancy_ClosingDate -->

**ما هو.** آخر يوم يستطيع فيه المرشحون رؤية الشاغر.

**مثال.** `2026-05-31`

**لماذا يهم.** بعده يختفي الشاغر من موقع الوظائف.

**القواعد.** اختياري. تاريخ.

## VacancyStatus {#Vacancy.VacancyStatus}
<!-- alias: loc:VacancyStatus, dom:Vacancy_VacancyStatus -->

**ما هو.** وضع الشاغر.

**مثال.** مفتوح

**لماذا يهم.** لا تُعرض في موقع الوظائف إلا الشواغر *المفتوحة*.

**القواعد.** إلزامي. إحدى القيم: مفتوح، Filled، معلق، Dropped.

## EmploymentType {#Vacancy.EmploymentType}
<!-- alias: loc:EmploymentType, dom:Vacancy_EmploymentType -->

**ما هو.** دوام كامل أو جزئي.

**لماذا يهم.** يُعرض في موقع الوظائف.

**القواعد.** إلزامي. إحدى القيم: FullTime، PartTime.

## SlaDaysToFill {#Vacancy.SLADaysToFill}
<!-- alias: loc:SlaDaysToFill, dom:Vacancy_SlaDaysToFill -->

**ما هو.** الأيام المسموح بها لشغل الشاغر.

**لماذا يهم.** لا يُقرأ حالياً -- يُحفظ للرجوع إليه.

**القواعد.** اختياري. رقم صحيح.

## TotalVacancy {#Vacancy.TotalVacancy}
<!-- alias: loc:TotalVacancy, dom:Vacancy_TotalVacancy -->

**ما هو.** عدد الوظائف التي يخصها الشاغر.

**لماذا يهم.** لا يُقرأ حالياً -- يُحفظ للرجوع إليه.

**القواعد.** اختياري. رقم صحيح.

## YearsOfExperience {#Vacancy.YearsOfExperience}
<!-- alias: loc:YearsOfExperience, dom:Vacancy_YearsOfExperience -->

**ما هو.** سنوات الخبرة المطلوبة.

**لماذا يهم.** يُعرض في موقع الوظائف.

**القواعد.** اختياري. رقم صحيح.

## MonthlySalaryRange {#Vacancy.MonthlySalaryRange}
<!-- alias: loc:MonthlySalaryRange, dom:Vacancy_MonthlySalaryRange -->

**ما هو.** نطاق الراتب المعروض.

**لماذا يهم.** لا يُقرأ حالياً -- يُحفظ للرجوع إليه.

**القواعد.** اختياري. الحد الأقصى 100 حرف.

## Qualification {#Vacancy.QualificationId}
<!-- alias: loc:Qualification, dom:Vacancy_QualificationId -->

**ما هو.** المؤهل المطلوب.

**لماذا يهم.** يُعرض في موقع الوظائف.

**القواعد.** اختياري. يُختار من قائمة المؤهلات.

## Position {#Vacancy.PositionId}
<!-- alias: loc:Position, dom:Vacancy_PositionId -->

**ما هو.** الوظيفة التي يُراد شغلها بهذا الشاغر.

**مثال.** `P-0142` - محاسب أول، المالية

**لماذا يهم.** لا يُقرأ حالياً -- يُحفظ للرجوع إليه.

**القواعد.** اختياري. يُختار من قائمة الوظائف.

## Recruiter {#Vacancy.RecruiterId}
<!-- alias: loc:Recruiter, dom:Vacancy_RecruiterId -->

**ما هو.** الموظف المسؤول عن شغل الشاغر.

**لماذا يهم.** لا يُقرأ حالياً -- يُحفظ للرجوع إليه.

**القواعد.** اختياري. يُختار من قائمة الموظفين.

## Location {#Vacancy.WorkLocationId}
<!-- alias: loc:Location, dom:Vacancy_WorkLocationId -->

**ما هو.** مكان العمل الذي تقع فيه الوظيفة.

**مثال.** المكتب الرئيسي

**لماذا يهم.** لا يُقرأ حالياً -- يُحفظ للرجوع إليه.

**القواعد.** اختياري. يُختار من قائمة.

## IsBudgeted {#Vacancy.IsBudgeted}
<!-- alias: loc:IsBudgeted, dom:Vacancy_IsBudgeted -->

**ما هو.** ما إذا كانت الوظيفة ضمن الموازنة.

**لماذا يهم.** لا يُقرأ حالياً -- يُحفظ للرجوع إليه.

**القواعد.** نعم أو لا.

## TemplateOnbGroupId {#Vacancy.TemplateOnbGroupId}
<!-- alias: loc:TemplateOnbGroupId, dom:Vacancy_TemplateOnbGroupId -->

**ما هو.** نموذج مجموعة تهيئة للشاغر.

**لماذا يهم.** لا يُقرأ حالياً -- يُحفظ للرجوع إليه.

**القواعد.** اختياري. يُختار من قائمة.

## OnbGroup {#Vacancy.OnbGroupId}
<!-- alias: loc:OnbGroup, dom:Vacancy_OnbGroupId -->

**ما هو.** مجموعة التهيئة لمن يُعيَّنون على الشاغر.

**لماذا يهم.** لا يُقرأ حالياً -- يُحفظ للرجوع إليه.

**القواعد.** اختياري. يُختار من قائمة مجموعات التهيئة الوظيفية.

## JobDescription {#Vacancy.JobDescription}
<!-- alias: loc:JobDescription, dom:Vacancy_JobDescription -->

**ما هو.** الوصف الوظيفي.

**لماذا يهم.** يُعرض في صفحة الشاغر في موقع الوظائف.

**القواعد.** اختياري.
