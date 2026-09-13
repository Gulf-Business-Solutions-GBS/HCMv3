---
helpId: pa.employee-relationship.edit
lang: en
title: Employee Relationships — Create / Edit
entity: HCMv3.PersonnelAdministration.EmployeeRelationship
surfaces:
  - angular/src/app/admin/personnel-administration/employee-detail/relationships/create-or-edit-employee-relationship-modal.component.html
audience: [admin]
permissions: [Pages.PersonnelAdministration.EmployeeRelationships.Create]
---

## Overview

A relationship between this employee and another employee -- for example a spouse or relative working in the same organisation.

It is recorded for reference; no calculation or approval step reads it at present.

## RelationshipType {#EmployeeRelationship.RelationshipTypeId}
<!-- alias: loc:RelationshipType, dom:EmployeeRelationship_RelationshipTypeId -->

**What it is.** How the two employees are related.

**Example.** Spouse

**Rules.** Required. Chosen from Relationship Type master data.

:::admin
Maintained under General Master Info → Relationship Types.
:::

## RelatedEmployee {#EmployeeRelationship.RelatedEmployeeId}
<!-- alias: loc:RelatedEmployee, dom:EmployeeRelationship_RelatedEmployeeId -->

**What it is.** The other employee.

**Example.** Sara Al-Mutairi (1188)

**Rules.** Required. Chosen from the employee list.
