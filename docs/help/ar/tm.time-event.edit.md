---
helpId: tm.time-event.edit
lang: ar
title: أحداث الوقت — إنشاء / تحرير
entity: HCMv3.TimeManagement.TimeEvent
surfaces:
  - angular/src/app/admin/time-management/time-events/create-or-edit-time-event-modal.component.html
audience: [admin]
permissions: [Pages.TimeManagement.TimeEvents.Create]
---

## Overview

بصمة واحدة: تسجيل موظف دخولاً أو خروجاً، أو خروجاً إلى الاستراحة وعودة منها، في تاريخ ووقت. وتصل البصمات من أجهزة الحضور وواجهة الحضور البرمجية والخدمة الذاتية والجوال، أو تُدخل يدوياً هنا، ويقرؤها محرك الوقت لاحتساب حضور كل يوم.

## Employee {#TimeEvent.EmployeeId}
<!-- alias: loc:Employee, dom:TimeEvent_EmployeeId -->

**ما هو.** الموظف صاحب البصمة.

**القواعد.** إلزامي. يُختار من قائمة الموظفين.

## Pin {#TimeEvent.PIN}
<!-- alias: loc:Pin, dom:TimeEvent_Pin -->

**ما هو.** رقم الموظف على جهاز الحضور.

**مثال.** `10457`

**لماذا يهم.** تستخدمه عمليات الاستيراد من الأجهزة، مع التاريخ والوقت، لتجاوز البصمات المستوردة مسبقاً.

**القواعد.** اختياري. الحد الأقصى 50 حرفاً.

## TransType {#TimeEvent.TransTypeId}
<!-- alias: loc:TransType, dom:TimeEvent_TransTypeId -->

**ما هو.** نوع البصمة: دخول أو خروج أو خروج للاستراحة أو عودة منها.

**مثال.** دخول

**لماذا يهم.** يبحث محرك الوقت عن كل نوع من البصمات على حدة، فالبصمة ذات النوع الخاطئ لا تُستخدم لليوم أبداً.

**القواعد.** اختياري. يُختار من قائمة أنواع الإجراءات.

## ActualTransType {#TimeEvent.ActualTransTypeId}
<!-- alias: loc:ActualTransType, dom:TimeEvent_ActualTransTypeId -->

**ما هو.** نوع البصمة الذي أبلغ عنه الجهاز أصلاً، إن تغيّر.

**لماذا يهم.** لا يُقرأ حالياً.

**القواعد.** اختياري. يُختار من قائمة أنواع الإجراءات.

## TransDate {#TimeEvent.TransDate}
<!-- alias: loc:TransDate, dom:TimeEvent_TransDate -->

**ما هو.** تاريخ البصمة.

**مثال.** `2026-04-14`

**القواعد.** إلزامي. تاريخ.

## TransTime {#TimeEvent.TransTime}
<!-- alias: loc:TransTime, dom:TimeEvent_TransTime -->

**ما هو.** وقت البصمة.

**مثال.** `07:42`

**القواعد.** إلزامي. تاريخ.

## TransSource {#TimeEvent.TransSource}
<!-- alias: loc:TransSource, dom:TimeEvent_TransSource -->

**ما هو.** مصدر البصمة: جهاز، أو إدخال يدوي، أو النظام، أو الخدمة الذاتية، أو الجوال، أو واجهة برمجية خارجية.

**مثال.** جهاز

**القواعد.** إلزامي. إحدى القيم: Machine، Manual، System، ESS، MobileAPI، ExternalAPI.

## AccessUnit {#TimeEvent.AccessUnitId}
<!-- alias: loc:AccessUnit, dom:TimeEvent_AccessUnitId -->

**ما هو.** الجهاز الذي سُجّلت عليه البصمة.

**لماذا يهم.** لا يُقرأ حالياً.

**القواعد.** اختياري. يُختار من قائمة وحدات الوصول.

## WorkLocation {#TimeEvent.WorkLocationId}
<!-- alias: loc:WorkLocation, dom:TimeEvent_WorkLocationId -->

**ما هو.** موقع العمل للبصمة.

**مثال.** المكتب الرئيسي

**لماذا يهم.** يستخدمه تقرير ملخص ساعات العمل اليومية.

**القواعد.** اختياري. يُختار من قائمة أماكن العمل.

## IpAddress {#TimeEvent.IPAddress}
<!-- alias: loc:IpAddress, dom:TimeEvent_IpAddress -->

**ما هو.** عنوان الجهاز الذي أرسل البصمة.

**مثال.** `10.0.4.21`

**لماذا يهم.** تستخدمه عمليات الاستيراد من الأجهزة لإيجاد وحدة الدخول أو إنشائها.

**القواعد.** اختياري. الحد الأقصى 50 حرفاً.

## GeoSelfiePath {#TimeEvent.GeoSelfiePath}
<!-- alias: loc:GeoSelfiePath, dom:TimeEvent_GeoSelfiePath -->

**ما هو.** مكان حفظ الصورة الذاتية الملتقطة مع بصمة الجوال.

**لماذا يهم.** تظهر في تقرير الحضور المتأخر والمبكر مع الصور.

**القواعد.** اختياري. الحد الأقصى 1000 حرف.

## Latitude {#TimeEvent.Latitude}
<!-- alias: loc:Latitude, dom:TimeEvent_Latitude -->

**ما هو.** خط العرض لمكان بصمة الجوال.

**لماذا يهم.** لا يُقرأ حالياً.

**القواعد.** اختياري. رقم.

## Longitude {#TimeEvent.Longitude}
<!-- alias: loc:Longitude, dom:TimeEvent_Longitude -->

**ما هو.** خط الطول لمكان بصمة الجوال.

**لماذا يهم.** لا يُقرأ حالياً.

**القواعد.** اختياري. رقم.

## RangeDifference {#TimeEvent.RangeDifference}
<!-- alias: loc:RangeDifference, dom:TimeEvent_RangeDifference -->

**ما هو.** بُعد بصمة الجوال عن موقع العمل.

**لماذا يهم.** لا يُقرأ حالياً.

**القواعد.** اختياري. رقم.

## Temperature {#TimeEvent.Temperature}
<!-- alias: loc:Temperature, dom:TimeEvent_Temperature -->

**ما هو.** درجة حرارة الجسم المسجلة عند البصمة.

**لماذا يهم.** لا يُقرأ حالياً.

**القواعد.** اختياري. رقم.

## PunchInRange {#TimeEvent.PunchInRange}
<!-- alias: loc:PunchInRange, dom:TimeEvent_PunchInRange -->

**ما هو.** ما إذا كانت بصمة الجوال ضمن نطاق موقع العمل.

**لماذا يهم.** لا يُقرأ حالياً.

**القواعد.** نعم أو لا.
