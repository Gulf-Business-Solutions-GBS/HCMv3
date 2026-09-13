---
helpId: pa.employee-visa-info.edit
lang: ar
title: بيانات تأشيرة الموظف
entity: HCMv3.PersonnelAdministration.EmployeeVisaInfo
surfaces:
  - angular/src/app/admin/personnel-administration/employee-detail/visa-info/employee-visa-info.component.html
audience: [admin]
permissions: []
---

## Overview

بيانات تأشيرة الموظف وإقامته وجواز سفره، ومنها التفاصيل المنقولة من كفيل سابق.

تطبع بعض النماذج الحكومية رقم التأشيرة ورقم الإقامة ورقم الملف الحالي من هنا. أما بقية التفاصيل فتُسجَّل للرجوع إليها.

## CurrentFileNo {#EmployeeVisaInfo.CurrentFileNo}
<!-- alias: loc:CurrentFileNo, dom:VisaInfo_CurrentFileNo -->

**ما هو.** رقم الملف الحالي لدى الجهات الرسمية.

**مثال.** 2026/45871

**لماذا يهم.** يُطبع على النماذج الحكومية.

**القواعد.** اختياري. الحد الأقصى 64 حرفاً.

## CurrentContractNo {#EmployeeVisaInfo.CurrentContractNo}
<!-- alias: loc:CurrentContractNo, dom:VisaInfo_CurrentContractNo -->

**ما هو.** رقم عقد العمل الحالي.

**مثال.** C-889214

**القواعد.** اختياري. الحد الأقصى 64 حرفاً.

## CurrentLicenseNo {#EmployeeVisaInfo.CurrentLicenseNo}
<!-- alias: loc:CurrentLicenseNo, dom:VisaInfo_CurrentLicenseNo -->

**ما هو.** رقم إذن العمل الحالي.

**مثال.** L-55120

**القواعد.** اختياري. الحد الأقصى 64 حرفاً.

## OldFileNo {#EmployeeVisaInfo.OldFileNo}
<!-- alias: loc:OldFileNo, dom:VisaInfo_OldFileNo -->

**ما هو.** رقم الملف لدى الكفيل السابق.

**مثال.** 2019/11234

**القواعد.** اختياري. الحد الأقصى 64 حرفاً.

## OldContractNo {#EmployeeVisaInfo.OldContractNo}
<!-- alias: loc:OldContractNo, dom:VisaInfo_OldContractNo -->

**ما هو.** رقم العقد لدى الكفيل السابق.

**مثال.** C-440182

**القواعد.** اختياري. الحد الأقصى 64 حرفاً.

## OldLicenseNo {#EmployeeVisaInfo.OldLicenseNo}
<!-- alias: loc:OldLicenseNo, dom:VisaInfo_OldLicenseNo -->

**ما هو.** رقم إذن العمل لدى الكفيل السابق.

**مثال.** L-30211

**القواعد.** اختياري. الحد الأقصى 64 حرفاً.

## MoiFileNumber {#EmployeeVisaInfo.MOIFileNumber}
<!-- alias: loc:MoiFileNumber, dom:VisaInfo_MoiFileNumber -->

**ما هو.** رقم الملف لدى وزارة الداخلية.

**مثال.** 7712045

**القواعد.** اختياري. الحد الأقصى 64 حرفاً.

## VisaNum {#EmployeeVisaInfo.VisaNum}
<!-- alias: loc:VisaNum, dom:VisaInfo_VisaNum -->

**ما هو.** رقم التأشيرة.

**مثال.** 18001234567

**لماذا يهم.** يُطبع على النماذج الحكومية.

**القواعد.** اختياري. رقم صحيح.

## ResidencyNum {#EmployeeVisaInfo.ResidencyNum}
<!-- alias: loc:ResidencyNum, dom:VisaInfo_ResidencyNum -->

**ما هو.** رقم الإقامة.

**مثال.** 28803140012

**لماذا يهم.** يُطبع على النماذج الحكومية.

**القواعد.** اختياري. رقم صحيح.

## AuthorizedSign {#EmployeeVisaInfo.AuthorizedSign}
<!-- alias: loc:AuthorizedSign, dom:VisaInfo_AuthorizedSign -->

**ما هو.** المفوَّض بالتوقيع، كنص حر.

**مثال.** مدير الموارد البشرية

**القواعد.** اختياري. الحد الأقصى 128 حرفاً.

## PreviousResidencyPeriod {#EmployeeVisaInfo.PreviousResidencyPeriod}
<!-- alias: loc:PreviousResidencyPeriod, dom:VisaInfo_PreviousResidencyPeriod -->

**ما هو.** مدة الإقامة لدى الكفيل السابق.

**مثال.** 3 سنوات

**القواعد.** اختياري. الحد الأقصى 64 حرفاً.

## CurrentResidencyPeriod {#EmployeeVisaInfo.CurrentResidencyPeriod}
<!-- alias: loc:CurrentResidencyPeriod, dom:VisaInfo_CurrentResidencyPeriod -->

**ما هو.** مدة الإقامة الحالية.

**مثال.** سنتان

**القواعد.** اختياري. الحد الأقصى 64 حرفاً.

## OldContractSalary {#EmployeeVisaInfo.OldContractSalary}
<!-- alias: loc:OldContractSalary, dom:VisaInfo_OldContractSalary -->

**ما هو.** راتب العقد لدى الكفيل السابق.

**مثال.** 450.000

**القواعد.** اختياري. رقم.

## CurrentContractSalary {#EmployeeVisaInfo.CurrentContractSalary}
<!-- alias: loc:CurrentContractSalary, dom:VisaInfo_CurrentContractSalary -->

**ما هو.** راتب العقد الحالي.

**مثال.** 600.000

**القواعد.** اختياري. رقم.

## NewProfession {#EmployeeVisaInfo.NewProfesion}
<!-- alias: loc:NewProfession, dom:VisaInfo_NewProfesion -->

**ما هو.** المهنة الجديدة في التأشيرة، بالإنجليزية.

**مثال.** Accountant

**القواعد.** اختياري. الحد الأقصى 256 حرفاً.

## NewProfessionAr {#EmployeeVisaInfo.NewProfessionAr}
<!-- alias: loc:NewProfessionAr, dom:VisaInfo_NewProfessionAr -->

**ما هو.** المهنة الجديدة في التأشيرة، بالعربية.

**مثال.** محاسب

**القواعد.** اختياري. الحد الأقصى 256 حرفاً.

## OldProfession {#EmployeeVisaInfo.OldProfession}
<!-- alias: loc:OldProfession, dom:VisaInfo_OldProfession -->

**ما هو.** المهنة لدى الكفيل السابق.

**مثال.** كاتب

**القواعد.** اختياري. الحد الأقصى 256 حرفاً.

## OldCompany {#EmployeeVisaInfo.OldCompany}
<!-- alias: loc:OldCompany, dom:VisaInfo_OldCompany -->

**ما هو.** جهة العمل السابقة.

**مثال.** شركة الخليج التجارية

**القواعد.** اختياري. الحد الأقصى 256 حرفاً.

## OldSponsor {#EmployeeVisaInfo.OldSponser}
<!-- alias: loc:OldSponsor, dom:VisaInfo_OldSponser -->

**ما هو.** الكفيل السابق.

**مثال.** شركة الخليج التجارية

**القواعد.** اختياري. الحد الأقصى 256 حرفاً.

## PassportType {#EmployeeVisaInfo.PassportTypeId}
<!-- alias: loc:PassportType, dom:VisaInfo_PassportTypeId -->

**ما هو.** نوع جواز السفر، من القائمة.

**مثال.** عادي

**القواعد.** اختياري. يُختار من قائمة أنواع جوازات السفر.

## TypeOfPassport {#EmployeeVisaInfo.TypeOfPassport}
<!-- alias: loc:TypeOfPassport, dom:VisaInfo_TypeOfPassport -->

**ما هو.** نوع جواز السفر، كنص حر.

**مثال.** عادي

**القواعد.** اختياري. الحد الأقصى 64 حرفاً.

## IssuePlacePassport {#EmployeeVisaInfo.IssuePlacePassport}
<!-- alias: loc:IssuePlacePassport, dom:VisaInfo_IssuePlacePassport -->

**ما هو.** مكان إصدار جواز السفر.

**مثال.** القاهرة

**القواعد.** اختياري. الحد الأقصى 128 حرفاً.

## PassportIssueDate {#EmployeeVisaInfo.PassportIssueDate}
<!-- alias: loc:PassportIssueDate, dom:VisaInfo_PassportIssueDate -->

**ما هو.** تاريخ إصدار جواز السفر.

**مثال.** 3 مارس 2022

**القواعد.** اختياري. تاريخ.

## PassportExpiryDate {#EmployeeVisaInfo.PassportExpiryDate}
<!-- alias: loc:PassportExpiryDate, dom:VisaInfo_PassportExpiryDate -->

**ما هو.** تاريخ انتهاء جواز السفر.

**مثال.** 2 مارس 2032

**القواعد.** اختياري. تاريخ.

## EntryDate {#EmployeeVisaInfo.EntryDate}
<!-- alias: loc:EntryDate, dom:VisaInfo_EntryDate -->

**ما هو.** تاريخ دخول الموظف إلى البلاد.

**مثال.** 10 أغسطس 2019

**القواعد.** اختياري. تاريخ.

## TerminationDate {#EmployeeVisaInfo.TerminationDate}
<!-- alias: loc:TerminationDate, dom:VisaInfo_TerminationDate -->

**ما هو.** تاريخ انتهاء التأشيرة أو الإقامة.

**مثال.** 31 أغسطس 2026

**القواعد.** اختياري. تاريخ.
