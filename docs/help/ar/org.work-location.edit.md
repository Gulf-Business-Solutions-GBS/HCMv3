---
helpId: org.work-location.edit
lang: ar
title: أماكن العمل — إنشاء / تحرير
entity: HCMv3.OrganizationManagement.WorkLocation
surfaces:
  - angular/src/app/admin/organization-management/work-locations/create-or-edit-work-location-modal.component.html
audience: [admin]
permissions: [Pages.OrganizationManagement.WorkLocations.Create]
---

## Overview

مكان يعمل فيه الموظفون، مثل مكتب أو موقع. يوضع الموظفون في أحدها في تعيينهم التنظيمي. وعند تحديد الموقع الجغرافي والنطاق، يكون المكان الذي تتحقق البصمة الجغرافية في الخدمة الذاتية من وجود الموظف فيه.

## Latitude {#WorkLocation.Latitude}
<!-- alias: loc:Latitude, dom:WorkLocation_Latitude -->

**ما هو.** خط عرض الموقع.

**مثال.** 29.3759

**القواعد.** اختياري. رقم.

## Longitude {#WorkLocation.Longitude}
<!-- alias: loc:Longitude, dom:WorkLocation_Longitude -->

**ما هو.** خط طول الموقع.

**مثال.** 47.9774

**القواعد.** اختياري. رقم.

## GeoRange {#WorkLocation.GeoRange}
<!-- alias: loc:GeoRange, dom:WorkLocation_GeoRange -->

**ما هو.** المسافة بالأمتار من النقطة التي تُعدّ فيها البصمة الجغرافية داخل الموقع.

**مثال.** 200

**لماذا يهم.** يقيس الخادم المسافة بين هاتف الموظف والنقطة. داخل النطاق تُقبل البصمة، وخارجه لا تمر إلا عند تفعيل إعداد الخدمة الذاتية AllowPunchOutsideRange.

**القواعد.** اختياري. رقم.

## Address {#WorkLocation.Address}
<!-- alias: loc:Address, dom:WorkLocation_Address -->

**ما هو.** العنوان.

**القواعد.** اختياري. الحد الأقصى 1000 حرف.

## GeoFenceCoordinates {#WorkLocation.GeoFenceCoordinates}
<!-- alias: loc:GeoFenceCoordinates, dom:WorkLocation_GeoFenceCoordinates -->

**ما هو.** حدود مرسومة حول الموقع.

**لماذا يهم.** لا يُقرأ حالياً.

**القواعد.** اختياري. الحد الأقصى 2000 حرف.

## Sponsor {#WorkLocation.SponsorId}
<!-- alias: loc:Sponsor, dom:WorkLocation_SponsorId -->

**ما هو.** الكفيل -- صاحب الترخيص -- الذي يتبعه هذا الموقع.

**مثال.** شركة المطوع التجارية

**لماذا يهم.** يُسجَّل للرجوع إليه، ولا يستخدمه شيء آخر.

**القواعد.** اختياري. يُختار من قائمة الكفلاء.

## IsActive {#WorkLocation.IsActive}
<!-- alias: loc:IsActive, dom:WorkLocation_IsActive -->

**ما هو.** ما إذا كان الموقع مستخدماً.

**لماذا يهم.** لا يُقرأ حالياً: يمكن اختيار المواقع غير النشطة والبصمة فيها.

**القواعد.** نعم أو لا.
