---
helpId: hs.first-aid-box-detail.edit
lang: ar
title: تفاصيل صناديق الإسعافات الأولية — إنشاء / تحرير
entity: HCMv3.HealthAndSafetyManagement.FirstAidBoxDetail
surfaces:
  - angular/src/app/admin/health-and-safety-management/first-aid-box-detail/details/create-or-edit-first-aid-box-detail-modal.component.html
audience: [admin]
permissions: [Pages.HealthAndSafetyManagement.FirstAidBoxDetails.Create]
---

## Overview

دواء أو مادة واحدة في صندوق إسعافات أولية.

## Medicine {#FirstAidBoxDetail.MedicineId}
<!-- alias: loc:Medicine, dom:FirstAidBoxDetail_MedicineId -->

**ما هو.** الدواء أو المادة.

**مثال.** ضمادات

**القواعد.** إلزامي. يُختار من قائمة أدوية الإسعافات الأولية.

## AllocatedBy {#FirstAidBoxDetail.AllocatedById}
<!-- alias: loc:AllocatedBy, dom:FirstAidBoxDetail_AllocatedById -->

**ما هو.** من وضعه في الصندوق.

**القواعد.** إلزامي. يُختار من قائمة الموظفين.

## ExpiryDate {#FirstAidBoxDetail.ExpiryDate}
<!-- alias: loc:ExpiryDate, dom:FirstAidBoxDetail_ExpiryDate -->

**ما هو.** متى تنتهي صلاحيته.

**لماذا يهم.** للتسجيل فقط: لا يُرسل تذكير عند حلوله.

**القواعد.** إلزامي. تاريخ.

## AllocationDate {#FirstAidBoxDetail.AllocationDate}
<!-- alias: loc:AllocationDate, dom:FirstAidBoxDetail_AllocationDate -->

**ما هو.** متى وُضع في الصندوق.

**القواعد.** إلزامي. تاريخ.

## IsExpired {#FirstAidBoxDetail.IsExpired}
<!-- alias: loc:IsExpired, dom:FirstAidBoxDetail_IsExpired -->

**ما هو.** ما إذا كانت صلاحيته قد انتهت.

**لماذا يهم.** يُحدَّد يدوياً، ولا يُضبط من تاريخ انتهاء الصلاحية.

**القواعد.** نعم أو لا.

## IsReplaced {#FirstAidBoxDetail.IsReplaced}
<!-- alias: loc:IsReplaced, dom:FirstAidBoxDetail_IsReplaced -->

**ما هو.** ما إذا كان قد استُبدل.

**القواعد.** نعم أو لا.
