---
helpId: pa.employee-education.edit
lang: ar
title: مؤهلات الموظف الدراسية — إنشاء / تحرير
entity: HCMv3.PersonnelAdministration.EmployeeEducation
surfaces:
  - angular/src/app/admin/personnel-administration/employee-detail/education/create-or-edit-employee-education-modal.component.html
audience: [admin]
permissions: [Pages.PersonnelAdministration.EmployeeEducations.Create]
---

## Overview

مؤهل حصل عليه الموظف من تعليمه.

يُطابَق المؤهل والتخصص مع جدول بدل التعليم في التأمينات الاجتماعية في الرواتب. أما بقية التفاصيل فتُسجَّل للرجوع إليها.

## Education {#EmployeeEducation.EducationId}
<!-- alias: loc:Education, dom:EmployeeEducation_EducationId -->

**ما هو.** المستوى التعليمي.

**مثال.** بكالوريوس

**لماذا يهم.** يُستخدم مع التخصص لبدل التعليم في التأمينات الاجتماعية.

**القواعد.** إلزامي. يُختار من قائمة المؤهلات الدراسية.

## EducationBranch {#EmployeeEducation.EducationBranchId}
<!-- alias: loc:EducationBranch, dom:EmployeeEducation_EducationBranchId -->

**ما هو.** مجال الدراسة.

**مثال.** هندسة

**لماذا يهم.** يُستخدم مع المستوى التعليمي لبدل التعليم في التأمينات الاجتماعية.

**القواعد.** إلزامي. يُختار من قائمة EducationBranch.

## University {#EmployeeEducation.UniversityId}
<!-- alias: loc:University, dom:EmployeeEducation_UniversityId -->

**ما هو.** الجامعة.

**مثال.** جامعة الكويت

**لماذا يهم.** يُسجَّل للرجوع إليه، ولا يقرؤه أي احتساب أو تقرير حالياً.

**القواعد.** اختياري. يُختار من قائمة الجامعات.

## Institution {#EmployeeEducation.InstitutionId}
<!-- alias: loc:Institution, dom:EmployeeEducation_InstitutionId -->

**ما هو.** المؤسسة التعليمية داخل الجامعة.

**مثال.** كلية الهندسة

**لماذا يهم.** يُسجَّل للرجوع إليه، ولا يقرؤه أي احتساب أو تقرير حالياً.

**القواعد.** اختياري. يُختار من قائمة الجهات التعليمية.

## Country {#EmployeeEducation.CountryId}
<!-- alias: loc:Country, dom:EmployeeEducation_CountryId -->

**ما هو.** بلد الدراسة.

**مثال.** الكويت

**لماذا يهم.** يُسجَّل للرجوع إليه، ولا يقرؤه أي احتساب أو تقرير حالياً.

**القواعد.** اختياري. يُختار من قائمة الدول.

## Location {#EmployeeEducation.Location}
<!-- alias: loc:Location, dom:EmployeeEducation_Location -->

**ما هو.** مكان الدراسة، كنص حر.

**مثال.** الشويخ

**القواعد.** اختياري. الحد الأقصى 256 حرفاً.

## DurationOfCourse {#EmployeeEducation.DurationOfCourse}
<!-- alias: loc:DurationOfCourse, dom:EmployeeEducation_DurationOfCourse -->

**ما هو.** مدة الدراسة.

**مثال.** 4

**القواعد.** اختياري. رقم.

## Grade {#EmployeeEducation.Grade}
<!-- alias: loc:Grade, dom:EmployeeEducation_Grade -->

**ما هو.** التقدير الحاصل عليه.

**مثال.** جيد جداً

**القواعد.** اختياري. الحد الأقصى 64 حرفاً.

## YearOfPassing {#EmployeeEducation.YearOfPassing}
<!-- alias: loc:YearOfPassing, dom:EmployeeEducation_YearOfPassing -->

**ما هو.** سنة الحصول على المؤهل.

**مثال.** 2012

**القواعد.** اختياري. الحد الأقصى 16 حرفاً.
