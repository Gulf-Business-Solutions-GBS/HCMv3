---
helpId: hs.first-aid-box.edit
lang: ar
title: إدارة الصحة والسلامة — إنشاء / تحرير
entity: HCMv3.HealthAndSafetyManagement.FirstAidBox
surfaces:
  - angular/src/app/admin/health-and-safety-management/first-aid-boxes/create-or-edit-first-aid-box-modal.component.html
audience: [admin]
permissions: []
---

## Overview

صندوق إسعافات أولية، ومكانه ومن يتولى رعايته. وتُدرج أدويته كتفاصيل. اترك الرمز فارغاً ليُعطى الرقم التالي.

## Location {#FirstAidBox.LocationId}
<!-- alias: loc:Location, dom:FirstAidBox_LocationId -->

**ما هو.** موقع العمل.

**مثال.** المستودع الرئيسي

**القواعد.** إلزامي. يُختار من قائمة أماكن العمل.

## AllocationDate {#FirstAidBox.AllocationDate}
<!-- alias: loc:AllocationDate, dom:FirstAidBox_AllocationDate -->

**ما هو.** متى وُضع الصندوق.

**القواعد.** إلزامي. تاريخ.

## ResponsiblePerson {#FirstAidBox.ResponsiblePersonId}
<!-- alias: loc:ResponsiblePerson, dom:FirstAidBox_ResponsiblePersonId -->

**ما هو.** من يتولى رعاية الصندوق.

**القواعد.** إلزامي. يُختار من قائمة الموظفين.
