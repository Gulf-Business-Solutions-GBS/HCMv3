---
helpId: gr.organization-document.edit
lang: ar
title: وثائق الشركة — إنشاء / تحرير
entity: HCMv3.GovtRelationsManagement.OrganizationDocument
surfaces:
  - angular/src/app/admin/govt-relations-management/organization-documents/create-or-edit-organization-document-modal.component.html
audience: [admin]
permissions: [Pages.GovtRelationsManagement.OrganizationDocuments.Create]
---

## Overview

مستند أو ترخيص تحمله المنظمة، مثل الرخصة التجارية. لا يمكن أن يكون تاريخ الانتهاء قبل تاريخ الإصدار. ويعرض تقرير وثائق الشركة المستندات السارية في فترة من التواريخ والأيام المتبقية قبل انتهاء كل منها.

## OrganizationUnit {#OrganizationDocument.OrganizationUnitId}
<!-- alias: loc:OrganizationUnit, dom:OrganizationDocument_OrganizationUnitId -->

**ما هو.** جزء المنظمة الذي يخصه المستند.

**القواعد.** إلزامي. يُختار من قائمة الوحدات التنظيمية.

## DocumentType {#OrganizationDocument.DocumentTypeId}
<!-- alias: loc:DocumentType, dom:OrganizationDocument_DocumentTypeId -->

**ما هو.** نوع المستند.

**مثال.** الرخصة التجارية

**القواعد.** إلزامي. يُختار من قائمة أنواع الوثائق.

## DocumentNum {#OrganizationDocument.DocumentNum}
<!-- alias: loc:DocumentNum, dom:OrganizationDocument_DocumentNum -->

**ما هو.** رقم المستند.

**القواعد.** اختياري. الحد الأقصى 256 حرفاً.

## Sponsor {#OrganizationDocument.SponsorId}
<!-- alias: loc:Sponsor, dom:OrganizationDocument_SponsorId -->

**ما هو.** الكفيل الذي يخصه المستند، إن وُجد.

**القواعد.** اختياري. يُختار من قائمة الكفلاء.

## IssueDate {#OrganizationDocument.IssueDate}
<!-- alias: loc:IssueDate, dom:OrganizationDocument_IssueDate -->

**ما هو.** متى صدر.

**القواعد.** إلزامي. تاريخ.

## ExpiryDate {#OrganizationDocument.ExpiryDate}
<!-- alias: loc:ExpiryDate, dom:OrganizationDocument_ExpiryDate -->

**ما هو.** متى ينتهي.

**القواعد.** إلزامي. تاريخ.

## DocumentPlace {#OrganizationDocument.DocumentPlace}
<!-- alias: loc:DocumentPlace, dom:OrganizationDocument_DocumentPlace -->

**ما هو.** مكان حفظ الأصل.

**القواعد.** اختياري. الحد الأقصى 256 حرفاً.

## NotificationSent {#OrganizationDocument.NotificationSent}
<!-- alias: loc:NotificationSent, dom:OrganizationDocument_NotificationSent -->

**ما هو.** ما إذا أُرسل إشعار انتهاء.

**لماذا يهم.** لا يُقرأ حالياً.

**القواعد.** نعم أو لا.
