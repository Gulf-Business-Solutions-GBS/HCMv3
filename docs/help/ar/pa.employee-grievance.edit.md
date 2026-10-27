---
helpId: pa.employee-grievance.edit
lang: ar
title: تظلمات الموظف — إنشاء / تحرير
entity: HCMv3.PersonnelAdministration.EmployeeGrievance
surfaces:
  - angular/src/app/admin/personnel-administration/employee-detail/grievances/create-or-edit-employee-grievance-modal.component.html
audience: [admin]
permissions: [Pages.PersonnelAdministration.EmployeeGrievances.Create]
---

## Overview

تظلّم قدّمه الموظف. ولا يجوز تسجيل النوع نفسه من التظلم مرتين في التاريخ نفسه.

يُسجَّل للرجوع إليه، ولا يقرؤه أي احتساب أو تقرير حالياً.

## GrievanceTypeId {#EmployeeGrievance.GrievanceTypeId}
<!-- alias: loc:GrievanceTypeId, dom:EmployeeGrievance_GrievanceTypeId -->

**ما هو.** نوع التظلم.

**مثال.** ظروف العمل

**القواعد.** إلزامي. يُختار من قائمة أنواع التظلمات.

:::admin
تُدار من البيانات الأساسية العامة ← أنواع التظلمات.
:::

## GrievanceDate {#EmployeeGrievance.GrievanceDate}
<!-- alias: loc:GrievanceDate, dom:EmployeeGrievance_GrievanceDate -->

**ما هو.** تاريخ تقديم التظلم.

**مثال.** 2 سبتمبر 2026

**القواعد.** إلزامي. تاريخ.
