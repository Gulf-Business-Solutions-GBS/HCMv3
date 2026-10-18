---
helpId: pa.employee-dependant.edit
lang: ar
title: عائلو الموظف — إنشاء / تحرير
entity: HCMv3.PersonnelAdministration.EmployeeDependant
surfaces:
  - angular/src/app/admin/personnel-administration/employee-detail/dependants/create-or-edit-employee-dependant-modal.component.html
audience: [admin]
permissions: [Pages.PersonnelAdministration.EmployeeDependants.Create]
---

## Overview

فرد من الأسرة يعوله الموظف -- زوج أو ابن أو والد -- والمزايا التي يُحتسب ضمنها. ولا يجوز أن تتداخل فترات المعالين من نوع التذكرة نفسه للموظف نفسه.

يدخل المعالون في استحقاق تعويض المصاريف الصحية، وتذاكر طيران المعالين، وبدل الأبناء في التأمينات الاجتماعية.

## NameEn {#EmployeeDependant.NameEn}

**ما هو.** اسم المعال بالإنجليزية.

**مثال.** Sara Ahmed Al-Mutairi

**القواعد.** إلزامي. الحد الأقصى 256 حرفاً.

## NameAr {#EmployeeDependant.NameAr}

**ما هو.** اسم المعال بالعربية.

**مثال.** سارة أحمد المطيري

**القواعد.** إلزامي. الحد الأقصى 256 حرفاً.

## DependantTypeId {#EmployeeDependant.DependantTypeId}
<!-- alias: loc:DependantTypeId, dom:EmployeeDependant_DependantTypeId -->

**ما هو.** صفة العائل بالنسبة إلى الموظف.

**مثال.** ابنة

**لماذا يهم.** يُحدَّد استحقاق تعويض المصاريف الصحية لكل صفة عائل في الدرجة للموظف.

**القواعد.** إلزامي. يُختار من قائمة أنواع صفات العائل.

:::admin
تُدار من البيانات الأساسية العامة ← أنواع صفات العائل.
:::

## BirthDate {#EmployeeDependant.BirthDate}
<!-- alias: loc:BirthDate, dom:EmployeeDependant_BirthDate -->

**ما هو.** تاريخ ميلاد المعال.

**مثال.** 12 مايو 2015

**لماذا يهم.** يطبّق تعويض المصاريف الصحية حد العمر لصفة العائل بناءً عليه، وقد تعتمد أجرة تذكرة الطيران على عمر المعال.

**القواعد.** إلزامي. تاريخ.

## TicketTypeId {#EmployeeDependant.TicketTypeId}
<!-- alias: loc:TicketTypeId, dom:EmployeeDependant_TicketTypeId -->

**ما هو.** نوع التذكرة الذي تستخدمه تذاكر طيران المعال.

**مثال.** المعال 1

**لماذا يهم.** يحتسب محرك الوقت تراكم تذاكر المعال الساري في كل يوم.

**القواعد.** إلزامي. يُختار من قائمة أنواع التذاكر.

## SpecialNeed {#EmployeeDependant.SpecialNeed}
<!-- alias: loc:SpecialNeed, dom:EmployeeDependant_SpecialNeed -->

**ما هو.** هل المعال من ذوي الاحتياجات الخاصة.

**لماذا يهم.** تصرف التأمينات الاجتماعية بدل الأبناء من ذوي الاحتياجات الخاصة لهؤلاء المعالين بدلاً من بدل الأبناء العادي.

**القواعد.** نعم أو لا.

## IncludeInSocialSecurity {#EmployeeDependant.IncludeInSocialSecurity}
<!-- alias: loc:IncludeInSocialSecurity, dom:EmployeeDependant_IncludeInSocialSecurity -->

**ما هو.** هل يُحتسب المعال في بدلات الأبناء في التأمينات الاجتماعية.

**لماذا يهم.** لا يُحتسب إلا المعالون المحدد لهم هذا الخيار، وللموظفين الذكور فقط.

**القواعد.** نعم أو لا.

## IncludeInHealthInsurance {#EmployeeDependant.IncludeInHealthInsurance}
<!-- alias: loc:IncludeInHealthInsurance, dom:EmployeeDependant_IncludeInHealthInsurance -->

**ما هو.** هل المعال مشمول بالتأمين الصحي.

**لماذا يهم.** يُسجَّل للرجوع إليه، ولا يقرؤه أي احتساب أو تقرير حالياً.

**القواعد.** نعم أو لا.
