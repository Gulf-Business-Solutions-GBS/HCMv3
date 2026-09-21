---
helpId: trv.travel-detail.edit
lang: ar
title: تفاصيل السفر — إنشاء / تحرير
entity: HCMv3.TravelManagement.TravelDetail
surfaces:
  - angular/src/app/admin/travel-management/travel-details/create-or-edit-travel-detail-modal.component.html
audience: [admin]
permissions: [Pages.TravelManagement.TravelDetails.Create]
---

## Overview

مرحلة واحدة من رحلة: من أين إلى أين، ومتى، وأين يقيم الموظف. لا يمكن حذف المرحلة ما دامت لها تكاليف. ويمكن للنموذج حساب البدل اليومي المتوقع للمرحلة من أسعار البدل اليومي: اختر مجموعة الموظف ومجموعته الفرعية، فيضرب عدد الليالي في السعر المحدد لهذا المسار.

## Travel {#TravelDetail.TravelId}
<!-- alias: loc:Travel, dom:TravelDetail_TravelId -->

**ما هو.** الرحلة.

**القواعد.** إلزامي. يُختار من قائمة السفريات.

## Departure {#TravelDetail.DepartureId}
<!-- alias: loc:Departure, dom:TravelDetail_DepartureId -->

**ما هو.** مكان بدء المرحلة.

**مثال.** الكويت

**القواعد.** إلزامي. يُختار من قائمة وجهات السفر.

## Arrival {#TravelDetail.ArrivalId}
<!-- alias: loc:Arrival, dom:TravelDetail_ArrivalId -->

**ما هو.** مكان انتهاء المرحلة.

**مثال.** دبي

**القواعد.** إلزامي. يُختار من قائمة وجهات السفر.

## DepartureDate {#TravelDetail.DepartureDate}
<!-- alias: loc:DepartureDate, dom:TravelDetail_DepartureDate -->

**ما هو.** تاريخ المغادرة.

**القواعد.** إلزامي. تاريخ.

## DepartureTime {#TravelDetail.DepartureTime}
<!-- alias: loc:DepartureTime, dom:TravelDetail_DepartureTime -->

**ما هو.** وقت المغادرة.

**مثال.** 09:45

**القواعد.** إلزامي. تاريخ.

## ArrivalDate {#TravelDetail.ArrivalDate}
<!-- alias: loc:ArrivalDate, dom:TravelDetail_ArrivalDate -->

**ما هو.** تاريخ الوصول.

**القواعد.** إلزامي. تاريخ.

## ArrivalTime {#TravelDetail.ArrivalTime}
<!-- alias: loc:ArrivalTime, dom:TravelDetail_ArrivalTime -->

**ما هو.** وقت الوصول.

**مثال.** 09:45

**القواعد.** إلزامي. تاريخ.

## Hotel {#TravelDetail.HotelId}
<!-- alias: loc:Hotel, dom:TravelDetail_HotelId -->

**ما هو.** الفندق، إن وُجد.

**القواعد.** اختياري. يُختار من قائمة الفنادق.

## TravelAirlines {#TravelDetail.TravelAirlineId}
<!-- alias: loc:TravelAirlines, dom:TravelDetail_TravelAirlineId -->

**ما هو.** شركة الطيران، إن وُجدت.

**القواعد.** اختياري. يُختار من قائمة شركات الطيران.

## TotalNights {#TravelDetail.TotalNights}
<!-- alias: loc:TotalNights, dom:TravelDetail_TotalNights -->

**ما هو.** عدد الليالي في الوجهة. يُستخدم لحساب البدل اليومي المتوقع.

**مثال.** 3

**القواعد.** اختياري. رقم صحيح.

## NightStay {#TravelDetail.NightStay}
<!-- alias: loc:NightStay, dom:TravelDetail_NightStay -->

**ما هو.** ما إذا كانت المرحلة تتضمن مبيتاً.

**القواعد.** نعم أو لا.
