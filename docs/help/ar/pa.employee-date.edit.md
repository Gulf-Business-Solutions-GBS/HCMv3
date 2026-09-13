---
helpId: pa.employee-date.edit
lang: ar
title: تواريخ الموظف — إنشاء / تحرير
entity: HCMv3.PersonnelAdministration.EmployeeDate
surfaces:
  - angular/src/app/admin/personnel-administration/employee-detail/dates/create-or-edit-employee-date-modal.component.html
audience: [admin]
permissions: [Pages.PersonnelAdministration.EmployeeDates.Create]
---

## Overview

تاريخ مهم للموظف -- كفحص طبي أو مراجعة عقد -- مع عدد أيام اختياري.

يُسجَّل للرجوع إليه، ولا يقرؤه أي احتساب أو تقرير حالياً.

## DateType {#EmployeeDate.DateTypeId}
<!-- alias: loc:DateType, dom:EmployeeDate_DateTypeId -->

**ما هو.** نوع التاريخ.

**مثال.** فحص طبي

**القواعد.** اختياري. يُختار من قائمة أنواع التواريخ.

:::admin
تُدار من البيانات الأساسية العامة ← أنواع التواريخ.
:::

## DateData {#EmployeeDate.DateData}
<!-- alias: loc:DateData, dom:EmployeeDate_DateData -->

**ما هو.** التاريخ نفسه.

**مثال.** 15 يناير 2027

**القواعد.** إلزامي. تاريخ.

## DayData {#EmployeeDate.DayData}
<!-- alias: loc:DayData, dom:EmployeeDate_DayData -->

**ما هو.** عدد أيام يُسجَّل مع التاريخ.

**مثال.** 0

**القواعد.** اختياري. رقم.
