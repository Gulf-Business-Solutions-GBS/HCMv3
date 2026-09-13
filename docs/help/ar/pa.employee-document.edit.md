---
helpId: pa.employee-document.edit
lang: ar
title: وثائق الموظف — إنشاء / تحرير
entity: HCMv3.PersonnelAdministration.EmployeeDocument
surfaces:
  - angular/src/app/admin/personnel-administration/employee-detail/documents/create-or-edit-employee-document-modal.component.html
audience: [admin]
permissions: [Pages.PersonnelAdministration.EmployeeDocuments.Create]
---

## Overview

وثيقة رسمية محفوظة للموظف -- جواز سفر أو بطاقة مدنية أو إقامة أو رخصة -- مع رقمها وصلاحيتها.

تتبع حالة وجود الوثيقة حركاتِها: فما دام هناك سحب لم يُرجَع تظهر الوثيقة خارجة. ويمكن أيضاً إضافة الوثائق عبر الخدمة الذاتية، وسحب جوازات السفر منها.

## DocumentType {#EmployeeDocument.DocumentTypeId}
<!-- alias: loc:DocumentType, dom:EmployeeDocument_DocumentTypeId -->

**ما هو.** نوع الوثيقة.

**مثال.** جواز سفر

**القواعد.** إلزامي. يُختار من قائمة أنواع الوثائق.

:::admin
تُدار من البيانات الأساسية العامة ← أنواع الوثائق. ولا تُطبَّق إعدادات التذكير فيها حالياً، فلا تُرسل تذكيرات بانتهاء الصلاحية.
:::

## DocumentNum {#EmployeeDocument.DocumentNum}
<!-- alias: loc:DocumentNum, dom:EmployeeDocument_DocumentNum -->

**ما هو.** رقم الوثيقة.

**مثال.** P01234567

**لماذا يهم.** إلزامي عند تقديم الوثيقة عبر الخدمة الذاتية.

**القواعد.** اختياري. الحد الأقصى 128 حرفاً.

## IssueDate {#EmployeeDocument.IssueDate}
<!-- alias: loc:IssueDate, dom:EmployeeDocument_IssueDate -->

**ما هو.** تاريخ إصدار الوثيقة.

**مثال.** 3 مارس 2022

**القواعد.** إلزامي. تاريخ.

## ExpiryDate {#EmployeeDocument.ExpiryDate}
<!-- alias: loc:ExpiryDate, dom:EmployeeDocument_ExpiryDate -->

**ما هو.** تاريخ انتهاء صلاحية الوثيقة.

**مثال.** 2 مارس 2032

**لماذا يهم.** تعرض الخدمة الذاتية للموظف وثائقه التي لم تنتهِ صلاحيتها بعد.

**القواعد.** اختياري. تاريخ.

## Description {#EmployeeDocument.Description}
<!-- alias: loc:Description, dom:EmployeeDocument_Description -->

**ما هو.** وصف الوثيقة.

**مثال.** إقامة كويتية، مادة 18

**لماذا يهم.** يظهر في تقريري الوثائق وجوازات السفر.

**القواعد.** اختياري. الحد الأقصى 2000 حرف.
