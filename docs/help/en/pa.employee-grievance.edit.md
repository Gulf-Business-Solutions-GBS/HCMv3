---
helpId: pa.employee-grievance.edit
lang: en
title: Employee Grievances — Create / Edit
entity: HCMv3.PersonnelAdministration.EmployeeGrievance
surfaces:
  - angular/src/app/admin/personnel-administration/employee-detail/grievances/create-or-edit-employee-grievance-modal.component.html
audience: [admin]
permissions: [Pages.PersonnelAdministration.EmployeeGrievances.Create]
---

## Overview

A grievance raised by the employee. The same type of grievance cannot be recorded twice on the same date.

It is recorded for reference; no calculation or report reads it at present.

## GrievanceTypeId {#EmployeeGrievance.GrievanceTypeId}
<!-- alias: loc:GrievanceTypeId, dom:EmployeeGrievance_GrievanceTypeId -->

**What it is.** The kind of grievance.

**Example.** Working conditions

**Rules.** Required. Chosen from Grievance Type master data.

:::admin
Maintained under General Master Info → Grievance Types.
:::

## GrievanceDate {#EmployeeGrievance.GrievanceDate}
<!-- alias: loc:GrievanceDate, dom:EmployeeGrievance_GrievanceDate -->

**What it is.** The date the grievance was raised.

**Example.** 2 Sep 2026

**Rules.** Required. Date value.
