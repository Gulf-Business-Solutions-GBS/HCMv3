---
helpId: pa.employee-abs-quota-forfeit.edit
lang: en
title: Employee Absence Quota Forfeitures — Create / Edit
entity: HCMv3.PersonnelAdministration.EmployeeAbsQuotaForfeit
surfaces:
  - angular/src/app/admin/personnel-administration/employee-detail/abs-quota-forfeits/create-or-edit-employee-abs-quota-forfeit-modal.component.html
audience: [admin]
permissions: [Pages.PersonnelAdministration.EmployeeAbsQuotaForfeits.Create]
---

## Overview

A manual record of leave days the employee has lost from a leave type.

Saving it adds the days to the Forfeited figure of the employee's annual leave quota covering the effective date, so the leave balance goes down; changing it first gives back the days saved before, and deleting it gives them all back. Only annual leave types move the quota, and always the quota of the annual leave type with code 1. Nothing moves for a date before the earliest evaluation date in time settings. The save is refused when the days are more than the balance, or when the employee has no annual leave quota for that date.

**Do not update quota balance** -- tick it to save the record without touching the quota. It is not stored: it is always unticked when the form opens, and a delete always gives the days back. Leave forfeiture calculations maintain their own forfeit records.

## EffectiveDate {#EmployeeAbsQuotaForfeit.EffectiveDate}
<!-- alias: loc:EffectiveDate, dom:EmployeeAbsQuotaForfeit_EffectiveDate -->

**What it is.** The date the forfeit takes effect.

**Example.** 31 Mar 2026

**Rules.** Required. Date value.

## AbsenceTypeId {#EmployeeAbsQuotaForfeit.AbsenceTypeId}
<!-- alias: loc:AbsenceTypeId, dom:EmployeeAbsQuotaForfeit_AbsenceTypeId -->

**What it is.** The leave type days are forfeited from.

**Example.** Annual Leave

**Rules.** Required. Chosen from Leave Type master data.

:::admin
Maintained under Payroll Management → Leave Types.
:::

## QuotaForfeited {#EmployeeAbsQuotaForfeit.QuotaForfeited}
<!-- alias: loc:QuotaForfeited, dom:EmployeeAbsQuotaForfeit_QuotaForfeited -->

**What it is.** The number of days forfeited.

**Example.** 3

**Rules.** Optional. Number.
