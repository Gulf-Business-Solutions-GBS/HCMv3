---
helpId: trv.travel-per-diem.edit
lang: ar
title: بدلات السفر اليومية — إنشاء / تحرير
entity: HCMv3.TravelManagement.TravelPerDiem
surfaces:
  - angular/src/app/admin/travel-management/travel-per-diems/create-or-edit-travel-per-diem-modal.component.html
audience: [admin]
permissions: [Pages.TravelManagement.TravelPerDiems.Create]
---

## Overview

البدل اليومي لمسار ومجموعة موظفين ومجموعة فرعية. ولا يوجد إلا سعر واحد لكل تركيبة. ويُستخدم فقط لعرض البدل اليومي المتوقع في مرحلة الرحلة.

## FromDestination {#TravelPerDiem.FromDestinationId}
<!-- alias: loc:FromDestination, dom:TravelPerDiem_FromDestinationId -->

**ما هو.** بداية المسار.

**مثال.** الكويت

**القواعد.** إلزامي. يُختار من قائمة وجهات السفر.

## ToDestination {#TravelPerDiem.ToDestinationId}
<!-- alias: loc:ToDestination, dom:TravelPerDiem_ToDestinationId -->

**ما هو.** نهاية المسار.

**مثال.** لندن

**القواعد.** إلزامي. يُختار من قائمة وجهات السفر.

## EmployeeGroup {#TravelPerDiem.EmployeeGroupId}
<!-- alias: loc:EmployeeGroup, dom:TravelPerDiem_EmployeeGroupId -->

**ما هو.** مجموعة الموظفين.

**القواعد.** إلزامي. يُختار من قائمة.

## EmployeeSubGroup {#TravelPerDiem.EmployeeSubGroupId}
<!-- alias: loc:EmployeeSubGroup, dom:TravelPerDiem_EmployeeSubGroupId -->

**ما هو.** المجموعة الفرعية للموظفين.

**القواعد.** إلزامي. يُختار من قائمة.

## Amount {#TravelPerDiem.Amount}
<!-- alias: loc:Amount, dom:TravelPerDiem_Amount -->

**ما هو.** البدل لكل ليلة.

**مثال.** 45.000

**القواعد.** إلزامي. رقم.
