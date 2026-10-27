---
helpId: gp.gate-pass-person.edit
lang: ar
title: أشخاص تصاريح الدخول والخروج — إنشاء / تحرير
entity: HCMv3.GatePassManagement.GatePassPerson
surfaces:
  - angular/src/app/admin/gate-pass-management/gate-pass-detail/persons/create-or-edit-gate-pass-person-modal.component.html
audience: [admin]
permissions: [Pages.GatePassManagement.GatePassPersons.Create]
---

## Overview

شخص يشمله تصريح البوابة. وكما في النظام القديم، يلزم إدخال موظف ومورد واسم كامل معاً، حتى للزائر. ولا يُضاف كل نوع شخص إلا مرة واحدة لكل تصريح.

## PersonType {#GatePassPerson.PersonTypeId}
<!-- alias: loc:PersonType, dom:GatePassPerson_PersonTypeId -->

**ما هو.** نوع الشخص.

**مثال.** مقاول

**القواعد.** إلزامي. يُختار من قائمة أنواع الأشخاص.

## Employee {#GatePassPerson.EmployeeId}
<!-- alias: loc:Employee, dom:GatePassPerson_EmployeeId -->

**ما هو.** الموظف المعني.

**القواعد.** إلزامي. يُختار من قائمة الموظفين.

## PersonFullName {#GatePassPerson.PersonFullName}
<!-- alias: loc:PersonFullName, dom:GatePassPerson_PersonFullName -->

**ما هو.** الاسم الكامل للشخص.

**القواعد.** إلزامي. الحد الأقصى 256 حرفاً.

## GatePassVendor {#GatePassPerson.GatePassVendorId}
<!-- alias: loc:GatePassVendor, dom:GatePassPerson_GatePassVendorId -->

**ما هو.** المورد الذي ينتمي إليه الشخص.

**القواعد.** إلزامي. يُختار من قائمة.

## PersonCivildID {#GatePassPerson.PersonCivildID}
<!-- alias: loc:PersonCivildID, dom:GatePassPerson_PersonCivildID -->

**ما هو.** الرقم المدني للشخص.

**القواعد.** اختياري. الحد الأقصى 64 حرفاً.

## PersonPassportNumber {#GatePassPerson.PersonPassportNumber}
<!-- alias: loc:PersonPassportNumber, dom:GatePassPerson_PersonPassportNumber -->

**ما هو.** رقم جواز سفر الشخص.

**القواعد.** اختياري. الحد الأقصى 64 حرفاً.
