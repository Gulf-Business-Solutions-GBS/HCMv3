---
helpId: acc.employee-accommodation.edit
lang: ar
title: سكن الموظفين — إنشاء / تحرير
entity: HCMv3.AccommodationManagement.EmployeeAccommodation
surfaces:
  - angular/src/app/admin/accommodation-management/employee-accommodations/create-or-edit-employee-accommodation-modal.component.html
audience: [admin]
permissions: [Pages.AccommodationManagement.EmployeeAccommodations.Create]
---

## Overview

مكان سكن الموظف ومدته. تعرض كل قائمة أسفل الموقع الأماكن الموجودة داخل ما اختير فوقها فقط. لا يمكن أن يكون تاريخ الانتهاء قبل تاريخ البدء، ولا يجوز أن تتداخل إقامات الموظف. ولا يوجد ما يمنع إعطاء السرير نفسه لموظفَين. وتظهر الإقامات في تقرير سكن الموظفين.

## Employee {#EmployeeAccommodation.EmployeeId}
<!-- alias: loc:Employee, dom:EmployeeAccommodation_EmployeeId -->

**ما هو.** الموظف الذي يُسكَّن.

**القواعد.** إلزامي. يُختار من قائمة الموظفين.

## AccommodationType {#EmployeeAccommodation.AccommodationTypeId}
<!-- alias: loc:AccommodationType, dom:EmployeeAccommodation_AccommodationTypeId -->

**ما هو.** نوع السكن.

**مثال.** مشترك

**القواعد.** إلزامي. يُختار من قائمة أنواع السكن.

## DeductionAmount {#EmployeeAccommodation.DeductionAmount}
<!-- alias: loc:DeductionAmount, dom:EmployeeAccommodation_DeductionAmount -->

**ما هو.** مبلغ يُخصم من الموظف مقابل السكن.

**مثال.** 25.000

**لماذا يهم.** لا يُقرأ حالياً: لا يخصمه نظام الرواتب. استخدم نوع أجر للخصم على الموظف لاسترداد التكلفة.

**القواعد.** إلزامي. رقم.

## Location {#EmployeeAccommodation.LocationId}
<!-- alias: loc:Location, dom:EmployeeAccommodation_LocationId -->

**ما هو.** الموقع.

**القواعد.** إلزامي. يُختار من قائمة المواقع.

## Building {#EmployeeAccommodation.BuildingId}
<!-- alias: loc:Building, dom:EmployeeAccommodation_BuildingId -->

**ما هو.** المبنى.

**القواعد.** إلزامي. يُختار من قائمة المباني.

## Floor {#EmployeeAccommodation.FloorId}
<!-- alias: loc:Floor, dom:EmployeeAccommodation_FloorId -->

**ما هو.** الطابق.

**القواعد.** إلزامي. يُختار من قائمة الطوابق.

## Flat {#EmployeeAccommodation.FlatId}
<!-- alias: loc:Flat, dom:EmployeeAccommodation_FlatId -->

**ما هو.** الشقة.

**القواعد.** إلزامي. يُختار من قائمة الشقق.

## Room {#EmployeeAccommodation.RoomId}
<!-- alias: loc:Room, dom:EmployeeAccommodation_RoomId -->

**ما هو.** الغرفة.

**القواعد.** إلزامي. يُختار من قائمة الغرف.

## Bed {#EmployeeAccommodation.BedId}
<!-- alias: loc:Bed, dom:EmployeeAccommodation_BedId -->

**ما هو.** السرير.

**القواعد.** إلزامي. يُختار من قائمة الأسرّة.
