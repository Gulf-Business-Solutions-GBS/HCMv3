---
helpId: pa.employee-work-experience.edit
lang: ar
title: خبرات الموظف العملية — إنشاء / تحرير
entity: HCMv3.PersonnelAdministration.EmployeeWorkExperience
surfaces:
  - angular/src/app/admin/personnel-administration/employee-detail/work-experience/create-or-edit-employee-work-experience-modal.component.html
audience: [admin]
permissions: [Pages.PersonnelAdministration.EmployeeWorkExperiences.Create]
---

## Overview

وظيفة سابقة شغلها الموظف قبل التحاقه. ولا يجوز أن تتداخل فترات الموظف نفسه.

تُسجَّل للرجوع إليها، ولا يقرؤها أي احتساب أو تقرير حالياً.

## Company {#EmployeeWorkExperience.Company}
<!-- alias: loc:Company, dom:EmployeeWorkExperience_Company -->

**ما هو.** جهة العمل السابقة.

**مثال.** شركة الخليج التجارية

**القواعد.** إلزامي. الحد الأقصى 256 حرفاً.

## Position {#EmployeeWorkExperience.Position}
<!-- alias: loc:Position, dom:EmployeeWorkExperience_Position -->

**ما هو.** المسمى الوظيفي فيها، كنص حر.

**مثال.** محاسب

**القواعد.** اختياري. الحد الأقصى 256 حرفاً.

## Location {#EmployeeWorkExperience.Location}
<!-- alias: loc:Location, dom:EmployeeWorkExperience_Location -->

**ما هو.** مكان العمل.

**مثال.** مدينة الكويت

**القواعد.** اختياري. الحد الأقصى 256 حرفاً.

## ReasonForLeaving {#EmployeeWorkExperience.ReasonForLeaving}
<!-- alias: loc:ReasonForLeaving, dom:EmployeeWorkExperience_ReasonForLeaving -->

**ما هو.** سبب ترك العمل.

**مثال.** التطور الوظيفي

**القواعد.** اختياري. الحد الأقصى 1000 حرف.
