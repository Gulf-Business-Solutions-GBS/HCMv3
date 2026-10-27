---
helpId: hs.incident-register.edit
lang: ar
title: سجلات الحوادث — إنشاء / تحرير
entity: HCMv3.HealthAndSafetyManagement.IncidentRegister
surfaces:
  - angular/src/app/admin/health-and-safety-management/incident-registers/create-or-edit-incident-register-modal.component.html
audience: [admin]
permissions: [Pages.HealthAndSafetyManagement.IncidentRegisters.Create]
---

## Overview

حادث أو إصابة أو حادث وشيك أو أي حادث سلامة آخر. وتُسجَّل عليه التحقيقات والمطالبات والإجراءات.

## IncidentDate {#IncidentRegister.IncidentDate}
<!-- alias: loc:IncidentDate, dom:IncidentRegister_IncidentDate -->

**ما هو.** متى وقع.

**القواعد.** إلزامي. تاريخ.

## IncidentTime {#IncidentRegister.IncidentTime}
<!-- alias: loc:IncidentTime, dom:IncidentRegister_IncidentTime -->

**ما هو.** الوقت من اليوم، إن كان معروفاً.

**مثال.** 14:30

**القواعد.** اختياري. تاريخ.

## IncidentType {#IncidentRegister.IncidentTypeId}
<!-- alias: loc:IncidentType, dom:IncidentRegister_IncidentTypeId -->

**ما هو.** نوع الحادث.

**مثال.** انزلاق أو سقوط

**القواعد.** إلزامي. يُختار من قائمة أنواع الحوادث.

## Status {#IncidentRegister.StatusId}
<!-- alias: loc:Status, dom:IncidentRegister_StatusId -->

**ما هو.** حالة الحادث.

**مثال.** قيد التحقيق

**القواعد.** اختياري. يُختار من قائمة حالات الحوادث.

## Nature {#IncidentRegister.NatureId}
<!-- alias: loc:Nature, dom:IncidentRegister_NatureId -->

**ما هو.** طبيعة الحادث.

**مثال.** إصابة

**القواعد.** إلزامي. يُختار من قائمة طبيعة الحوادث.

## Severity {#IncidentRegister.SeverityId}
<!-- alias: loc:Severity, dom:IncidentRegister_SeverityId -->

**ما هو.** مدى خطورته.

**مثال.** بسيط

**القواعد.** إلزامي. يُختار من قائمة درجات خطورة الحوادث.

## Location {#IncidentRegister.LocationId}
<!-- alias: loc:Location, dom:IncidentRegister_LocationId -->

**ما هو.** موقع العمل، إن وُجد.

**مثال.** المستودع الرئيسي

**القواعد.** اختياري. يُختار من قائمة أماكن العمل.

## ReportedBy {#IncidentRegister.ReportedById}
<!-- alias: loc:ReportedBy, dom:IncidentRegister_ReportedById -->

**ما هو.** من أبلغ عنه.

**القواعد.** اختياري. يُختار من قائمة الموظفين.

## PersonInvolved {#IncidentRegister.PersonInvolvedId}
<!-- alias: loc:PersonInvolved, dom:IncidentRegister_PersonInvolvedId -->

**ما هو.** الموظف المعني.

**القواعد.** اختياري. يُختار من قائمة الموظفين.

## AssignedTo {#IncidentRegister.AssignedToId}
<!-- alias: loc:AssignedTo, dom:IncidentRegister_AssignedToId -->

**ما هو.** من يتولى معالجته.

**القواعد.** اختياري. يُختار من قائمة الموظفين.

## OtherPeopleInvolved {#IncidentRegister.OtherPeopleInvolved}
<!-- alias: loc:OtherPeopleInvolved, dom:IncidentRegister_OtherPeopleInvolved -->

**ما هو.** أي أشخاص آخرين معنيين، مثل الزوار أو المقاولين.

**القواعد.** اختياري.

## DisabilityPercentage {#IncidentRegister.DisabilityPercentage}
<!-- alias: loc:DisabilityPercentage, dom:IncidentRegister_DisabilityPercentage -->

**ما هو.** نسبة العجز الناتج عن الإصابة.

**مثال.** 10%

**لماذا يهم.** يُحفظ كنص للتسجيل، ولا يُستخدم في أي حساب.

**القواعد.** اختياري.

## Description {#IncidentRegister.Description}
<!-- alias: loc:Description, dom:IncidentRegister_Description -->

**ما هو.** ما الذي حدث.

**القواعد.** اختياري.
