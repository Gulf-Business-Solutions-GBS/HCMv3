---
helpId: gp.gate-pass-person.edit
lang: en
title: Gate Pass Persons — Create / Edit
entity: HCMv3.GatePassManagement.GatePassPerson
surfaces:
  - angular/src/app/admin/gate-pass-management/gate-pass-detail/persons/create-or-edit-gate-pass-person-modal.component.html
audience: [admin]
permissions: [Pages.GatePassManagement.GatePassPersons.Create]
---

## Overview

A person covered by a gate pass. As in the old system, an employee, a vendor and a full name are all required, even for a visitor. Each person type can be added once per pass.

## PersonType {#GatePassPerson.PersonTypeId}
<!-- alias: loc:PersonType, dom:GatePassPerson_PersonTypeId -->

**What it is.** What kind of person.

**Example.** Contractor

**Rules.** Required. Chosen from Person Type master data.

## Employee {#GatePassPerson.EmployeeId}
<!-- alias: loc:Employee, dom:GatePassPerson_EmployeeId -->

**What it is.** The employee concerned.

**Rules.** Required. Chosen from the employee list.

## PersonFullName {#GatePassPerson.PersonFullName}
<!-- alias: loc:PersonFullName, dom:GatePassPerson_PersonFullName -->

**What it is.** The person's full name.

**Rules.** Required. Maximum 256 characters.

## GatePassVendor {#GatePassPerson.GatePassVendorId}
<!-- alias: loc:GatePassVendor, dom:GatePassPerson_GatePassVendorId -->

**What it is.** The vendor the person comes from.

**Rules.** Required. Chosen from a list.

## PersonCivildID {#GatePassPerson.PersonCivildID}
<!-- alias: loc:PersonCivildID, dom:GatePassPerson_PersonCivildID -->

**What it is.** The person's civil ID.

**Rules.** Optional. Maximum 64 characters.

## PersonPassportNumber {#GatePassPerson.PersonPassportNumber}
<!-- alias: loc:PersonPassportNumber, dom:GatePassPerson_PersonPassportNumber -->

**What it is.** The person's passport number.

**Rules.** Optional. Maximum 64 characters.
