---
helpId: pa.employee-contract.edit
lang: en
title: Employee Contracts — Create / Edit
entity: HCMv3.PersonnelAdministration.EmployeeContract
surfaces:
  - angular/src/app/admin/personnel-administration/employee-detail/contracts/create-or-edit-employee-contract-modal.component.html
audience: [admin]
permissions: [Pages.PersonnelAdministration.EmployeeContracts.Create]
---

## Overview

The employee's employment contracts and the periods they cover. Saving a contract does not close an earlier one; periods for the same employee cannot overlap, and no two can start on the same date.

For leave types that accrue over the whole employment, the accrual period runs from the joining date to the end date of the employee's latest contract.

## ContractType {#EmployeeContract.ContractTypeId}
<!-- alias: loc:ContractType, dom:EmployeeContract_ContractTypeId -->

**What it is.** The kind of contract.

**Example.** Limited — 2 years

**Why it matters.** Shown on contract and employee reports.

**Rules.** Required. Chosen from Contract Type master data.

:::admin
Maintained under General Master Info → Contract Types.
:::
