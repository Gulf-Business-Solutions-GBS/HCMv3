---
helpId: acc.accommodation-services.edit
lang: ar
title: خدمات السكن — إنشاء / تحرير
entity: HCMv3.AccommodationManagement.AccommodationServices
surfaces:
  - angular/src/app/admin/accommodation-management/accommodation-services/create-or-edit-accommodation-service-modal.component.html
audience: [admin]
permissions: [Pages.AccommodationManagement.AccommodationServices.Create]
---

## Overview

خدمة تُقدَّم للموظف أثناء إقامة، لفترة من التواريخ. لا يمكن أن يكون تاريخ الانتهاء قبل تاريخ البدء. وتظهر الخدمات في تقارير سكن الموظفين، والموظفون حسب الوظيفة، والرواتب والمزايا.

## EmployeeAccommodation {#AccommodationServices.EmployeeAccommodationId}
<!-- alias: loc:EmployeeAccommodation, dom:AccommodationService_EmployeeAccommodationId -->

**ما هو.** الإقامة التي تخصها الخدمة.

**القواعد.** إلزامي. يُختار من قائمة.

## AccommodationServiceType {#AccommodationServices.AccommodationServiceTypeId}
<!-- alias: loc:AccommodationServiceType, dom:AccommodationService_AccommodationServiceTypeId -->

**ما هو.** الخدمة.

**مثال.** الغسيل

**القواعد.** إلزامي. يُختار من قائمة أنواع خدمات السكن.
