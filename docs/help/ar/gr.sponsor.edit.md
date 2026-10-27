---
helpId: gr.sponsor.edit
lang: ar
title: الكفلاء — إنشاء / تحرير
entity: HCMv3.GovtRelationsManagement.Sponsor
surfaces:
  - angular/src/app/admin/govt-relations-management/sponsors/create-or-edit-sponsor-modal.component.html
audience: [admin]
permissions: [Pages.GovtRelationsManagement.Sponsors.Create]
---

## Overview

الكفيل: شركة أو مالك مرخّص يُكفل الموظفون تحته لأغراض الإقامة وتصاريح العمل. يُربط الموظفون بالكفيل في سجلات كفلاء الموظف، ويظهر اسم الكفيل عندها في تقارير الموظفين والرواتب. وتُحفظ تراخيصه والمخوّلون بالتوقيع عنه وممثلوه وحساباته البنكية وحصص مسمياته الوظيفية في الشاشات المرتبطة.

## BusinessOwnerNameEn {#Sponsor.BusinessOwnerNameEn}
<!-- alias: loc:BusinessOwnerNameEn, dom:Sponsor_BusinessOwnerNameEn -->

**ما هو.** اسم مالك النشاط بالإنجليزية.

**لماذا يهم.** لا يُقرأ حالياً.

**القواعد.** اختياري. الحد الأقصى 256 حرفاً.

## BusinessOwnerNameAr {#Sponsor.BusinessOwnerNameAr}
<!-- alias: loc:BusinessOwnerNameAr, dom:Sponsor_BusinessOwnerNameAr -->

**ما هو.** اسم مالك النشاط بالعربية.

**لماذا يهم.** لا يُقرأ حالياً.

**القواعد.** اختياري. الحد الأقصى 256 حرفاً.

## Gender {#Sponsor.Gender}
<!-- alias: loc:Gender, dom:Sponsor_Gender -->

**ما هو.** جنس المالك، إذا كان الكفيل شخصاً.

**لماذا يهم.** يظهر في تقرير الكفلاء.

**القواعد.** إلزامي. إحدى القيم: ذكر، أنثى.

## Nationality {#Sponsor.NationalityId}
<!-- alias: loc:Nationality, dom:Sponsor_NationalityId -->

**ما هو.** جنسية المالك.

**لماذا يهم.** يظهر في تقرير الكفلاء.

**القواعد.** إلزامي. يُختار من قائمة الجنسيات.

## BusinessType {#Sponsor.BusinessTypeId}
<!-- alias: loc:BusinessType, dom:Sponsor_BusinessTypeId -->

**ما هو.** نوع النشاط التجاري.

**مثال.** تجارة

**لماذا يهم.** يظهر في تقرير الكفلاء.

**القواعد.** إلزامي. يُختار من قائمة أنواع النشاط التجاري.

## Governorate {#Sponsor.GovernorateId}
<!-- alias: loc:Governorate, dom:Sponsor_GovernorateId -->

**ما هو.** المحافظة في العنوان.

**مثال.** العاصمة

**لماذا يهم.** يظهر في تقرير الكفلاء.

**القواعد.** إلزامي. يُختار من قائمة المحافظات.

## Profession {#Sponsor.Profession}
<!-- alias: loc:Profession, dom:Sponsor_Profession -->

**ما هو.** مهنة المالك.

**لماذا يهم.** يظهر في تقرير الكفلاء.

**القواعد.** اختياري. الحد الأقصى 256 حرفاً.

## MOSALID {#Sponsor.MOSALID}
<!-- alias: loc:MOSALID, dom:Sponsor_MOSALID -->

**ما هو.** رقم ملف الكفيل لدى وزارة الشؤون الاجتماعية والعمل.

**لماذا يهم.** يظهر في تقرير الكفلاء وفي تقارير كشف الرواتب.

**القواعد.** إلزامي. رقم صحيح.

## UnifiedNum {#Sponsor.UnifiedNum}
<!-- alias: loc:UnifiedNum, dom:Sponsor_UnifiedNum -->

**ما هو.** الرقم الموحد للكفيل لدى الجهات الحكومية.

**لماذا يهم.** يظهر في تقرير الكفلاء.

**القواعد.** إلزامي. رقم صحيح.

## OrganizationUnit {#Sponsor.OrganizationUnitId}
<!-- alias: loc:OrganizationUnit, dom:Sponsor_OrganizationUnitId -->

**ما هو.** جزء المنظمة الذي ينتمي إليه الكفيل.

**لماذا يهم.** يظهر في تقرير الكفلاء.

**القواعد.** إلزامي. يُختار من قائمة الوحدات التنظيمية.

## Building {#Sponsor.Building}
<!-- alias: loc:Building, dom:Sponsor_Building -->

**ما هو.** المبنى في عنوان الكفيل.

**لماذا يهم.** يظهر في تقرير الكفلاء.

**القواعد.** اختياري. الحد الأقصى 256 حرفاً.

## Street {#Sponsor.Street}
<!-- alias: loc:Street, dom:Sponsor_Street -->

**ما هو.** الشارع.

**لماذا يهم.** يظهر في تقرير الكفلاء.

**القواعد.** اختياري. الحد الأقصى 256 حرفاً.

## Area {#Sponsor.Area}
<!-- alias: loc:Area, dom:Sponsor_Area -->

**ما هو.** المنطقة.

**مثال.** الشويخ

**لماذا يهم.** يظهر في تقرير الكفلاء.

**القواعد.** اختياري. الحد الأقصى 256 حرفاً.

## Email {#Sponsor.Email}
<!-- alias: loc:Email, dom:Sponsor_Email -->

**ما هو.** البريد الإلكتروني للكفيل.

**لماذا يهم.** يظهر في تقرير الكفلاء.

**القواعد.** اختياري. الحد الأقصى 256 حرفاً.

## Telephone {#Sponsor.Telephone}
<!-- alias: loc:Telephone, dom:Sponsor_Telephone -->

**ما هو.** رقم الهاتف.

**لماذا يهم.** يظهر في تقرير الكفلاء.

**القواعد.** إلزامي. رقم صحيح.

## Block {#Sponsor.Block}
<!-- alias: loc:Block, dom:Sponsor_Block -->

**ما هو.** القطعة.

**لماذا يهم.** يظهر في تقرير الكفلاء.

**القواعد.** اختياري. الحد الأقصى 256 حرفاً.

## SocialSecurityNum {#Sponsor.SocialSecurityNum}
<!-- alias: loc:SocialSecurityNum, dom:Sponsor_SocialSecurityNum -->

**ما هو.** رقم تسجيل الكفيل في التأمينات الاجتماعية.

**لماذا يهم.** يظهر في تقرير الكفلاء.

**القواعد.** إلزامي. رقم صحيح.

## Fax {#Sponsor.Fax}
<!-- alias: loc:Fax, dom:Sponsor_Fax -->

**ما هو.** رقم الفاكس.

**لماذا يهم.** يظهر في تقرير الكفلاء.

**القواعد.** اختياري. الحد الأقصى 50 حرفاً.

## POBox {#Sponsor.POBox}
<!-- alias: loc:POBox, dom:Sponsor_POBox -->

**ما هو.** صندوق البريد.

**لماذا يهم.** يظهر في تقرير الكفلاء.

**القواعد.** اختياري. الحد الأقصى 50 حرفاً.
