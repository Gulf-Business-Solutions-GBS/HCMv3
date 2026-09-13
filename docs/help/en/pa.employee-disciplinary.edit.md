---
helpId: pa.employee-disciplinary.edit
lang: en
title: Employee Disciplinaries — Create / Edit
entity: HCMv3.PersonnelAdministration.EmployeeDisciplinary
surfaces:
  - angular/src/app/admin/personnel-administration/employee-detail/disciplinaries/create-or-edit-employee-disciplinary-modal.component.html
audience: [admin]
permissions: [Pages.PersonnelAdministration.EmployeeDisciplinaries.Create]
---

## Overview

A disciplinary offence recorded against the employee.

Disciplinary records can be routed for approval. When they are, the reporting employee and work location are required, and on approval the deduction type is set from the disciplinary type's tiers according to how many earlier offences of the same type fall within its repeat period. No deduction is taken in payroll at present.

## OrganizationUnitId {#EmployeeDisciplinary.OrganizationUnitId}
<!-- alias: loc:OrganizationUnitId, dom:EmployeeDisciplinary_OrganizationUnitId -->

**What it is.** The department where the offence happened.

**Example.** Warehouse Operations

**Why it matters.** Shown on the disciplinary report.

**Rules.** Optional. Chosen from Organization Unit master data.

## WorkLocationId {#EmployeeDisciplinary.WorkLocationId}
<!-- alias: loc:WorkLocationId, dom:EmployeeDisciplinary_WorkLocationId -->

**What it is.** The work location where the offence happened.

**Example.** Warehouse — Shuwaikh

**Why it matters.** Required when the record is routed for approval.

**Rules.** Optional. Chosen from Work Location master data.

## DisciplinaryTypeId {#EmployeeDisciplinary.DisciplinaryTypeId}
<!-- alias: loc:DisciplinaryTypeId, dom:EmployeeDisciplinary_DisciplinaryTypeId -->

**What it is.** The kind of offence.

**Example.** Late arrival

**Why it matters.** Earlier offences of the same type within its repeat period decide the escalation tier used on approval.

**Rules.** Required. Chosen from Disciplinary Type master data.

:::admin
Maintained under General Master Info → Disciplinary Types.
:::

## DisciplinaryDate {#EmployeeDisciplinary.DisciplinaryDate}
<!-- alias: loc:DisciplinaryDate, dom:EmployeeDisciplinary_DisciplinaryDate -->

**What it is.** The date of the offence.

**Example.** 14 Sep 2026

**Why it matters.** The repeat period is counted back from this date.

**Rules.** Required. Date value.

## DisciplinaryTime {#EmployeeDisciplinary.DisciplinaryTime}
<!-- alias: loc:DisciplinaryTime, dom:EmployeeDisciplinary_DisciplinaryTime -->

**What it is.** The time of the offence.

**Example.** 08:45

**Why it matters.** Recorded for reference; no calculation or report reads it at present.

**Rules.** Optional. Date value.

## ReportedById {#EmployeeDisciplinary.ReportedById}
<!-- alias: loc:ReportedById, dom:EmployeeDisciplinary_ReportedById -->

**What it is.** The employee who reported the offence.

**Example.** Khaled Al-Rashidi (1044)

**Why it matters.** Required when the record is routed for approval.

**Rules.** Optional. Chosen from the employee list.

## Severity {#EmployeeDisciplinary.Severity}
<!-- alias: loc:Severity, dom:EmployeeDisciplinary_Severity -->

**What it is.** How serious the offence is.

**Example.** Low

**Rules.** Required. One of: Low, High.

## DisciplinaryDeductionTypeId {#EmployeeDisciplinary.DisciplinaryDeductionTypeId}
<!-- alias: loc:DisciplinaryDeductionTypeId, dom:EmployeeDisciplinary_DisciplinaryDeductionTypeId -->

**What it is.** The penalty applied.

**Example.** One day's pay

**Why it matters.** Set from the disciplinary type's tiers on approval when the record is routed. Not deducted in payroll at present.

**Rules.** Optional. Chosen from Disciplinary Deduction Type master data.

## WlSendToEmp {#EmployeeDisciplinary.WLSendToEmp}
<!-- alias: loc:WlSendToEmp, dom:EmployeeDisciplinary_WlSendToEmp -->

**What it is.** Whether the warning letter was sent to the employee.

**Why it matters.** Recorded for reference; no calculation or report reads it at present.

**Rules.** Yes or no.

## WlRecvFromEmp {#EmployeeDisciplinary.WLRecvFromEmp}
<!-- alias: loc:WlRecvFromEmp, dom:EmployeeDisciplinary_WlRecvFromEmp -->

**What it is.** Whether the signed warning letter was received back from the employee.

**Why it matters.** Recorded for reference; no calculation or report reads it at present.

**Rules.** Yes or no.
